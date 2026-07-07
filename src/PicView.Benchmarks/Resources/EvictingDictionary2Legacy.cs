using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PicView.Core.DebugTools;
using ZLinq;

namespace PicView.Benchmarks.Resources;

/// <summary>
/// A thread-safe, capacity-constrained container responsible for storing loaded images and managing eviction.
/// <para>
/// This dictionary separates logical slots (Owner ID + Index) from physical data (File Paths) to allow 
/// deduplication across tabs. It enforces an eviction policy that removes items that are calculated 
/// to be too far away from the current viewing index of the specific tab owner.
/// </para>
/// </summary>
public class EvictingDictionary2Legacy<TValue> : IEnumerable<KeyValuePair<(string OwnerId, int Index), TValue>>
{
    //  Data Store: Tracks the actual loaded images by FilePath
    //    Controls the "Pixel" memory (large footprint)
    private readonly Dictionary<string, TValue> _data;

    //  Logical Map: Tracks which Tab/Index points to which File
    //    Controls the "Navigation" memory (small footprint)
    private readonly Dictionary<CacheKey, string> _indexMap;

    private readonly int _initialMaxSize;

    private readonly Lock _lock = new();
    private int _currentMaxSize;

    public EvictingDictionary2Legacy(int initialMaxSize)
    {
        if (initialMaxSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialMaxSize), "Size must be positive.");
        }

        _initialMaxSize = initialMaxSize;
        _currentMaxSize = initialMaxSize;

        _indexMap = new Dictionary<CacheKey, string>(initialMaxSize);
        _data = new Dictionary<string, TValue>(initialMaxSize);
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _indexMap.Count;
            }
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            lock (_lock)
            {
                return _data.Values.ToArray();
            }
        }
    }

    public IEnumerator<KeyValuePair<(string OwnerId, int Index), TValue>> GetEnumerator()
    {
        List<KeyValuePair<(string, int), TValue>> snapshot;
        lock (_lock)
        {
            // Reconstruct the view: Iterate logical keys -> fetch data
            snapshot = new List<KeyValuePair<(string, int), TValue>>(_indexMap.Count);
            foreach (var kvp in _indexMap)
            {
                if (_data.TryGetValue(kvp.Value, out var val))
                {
                    snapshot.Add(new KeyValuePair<(string, int), TValue>(
                        (kvp.Key.OwnerId, kvp.Key.Index), val));
                }
            }
        }

        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void ExpandCapacity(string id)
    {
        lock (_lock)
        {
            // Fix: Capacity should increase when a new tab acts as an owner
            _currentMaxSize += _initialMaxSize;
#if DEBUG
            DebugHelper.LogDebug(nameof(EvictingDictionary2Legacy<>), nameof(ExpandCapacity),
                $"Expanded by '{id}'. New Capacity: {_currentMaxSize}");
#endif
        }
    }

    public void DecreaseCapacity(string id)
    {
        lock (_lock)
        {
            var keysToRemove = _indexMap.Keys.Where(k => k.OwnerId == id).ToArray();
            foreach (var key in keysToRemove)
            {
                RemoveInternal(key);
            }

            // Only decrease capacity if we are sure this ID was contributing to it.
            // If keysToRemove.Length == 0, it might be a double-dispose or invalid ID.
            // Ideally, track registered owners, but checking keys > 0 is a decent proxy 
            // IF we assume a tab always loads at least one thing before closing.
            // Otherwise, just trusting the caller is acceptable if the Service layer is robust.

            if (_currentMaxSize > _initialMaxSize)
            {
                _currentMaxSize -= _initialMaxSize;
            }

            // Safety clamp
            if (_currentMaxSize < _initialMaxSize)
            {
                _currentMaxSize = _initialMaxSize;
            }
        }
    }

    public bool TryAdd(string ownerId, int index, string filePath, TValue value,
        int totalCount, bool isReverse, [MaybeNullWhen(false)] out TValue evictedValue)
    {
        lock (_lock)
        {
            var newKey = new CacheKey(ownerId, index);
            evictedValue = default;

            // 1. Check for Overwrite
            if (_indexMap.TryGetValue(newKey, out var oldPath))
            {
                if (oldPath == filePath)
                {
                    return false;
                }

                // Remove the old link. Capture the value if it was the last ref.
                evictedValue = RemoveInternal(newKey);

                // Note: capacity didn't change (removed 1, about to add 1), 
                // so we don't need to run eviction logic below.
            }
            // 2. Check Capacity (Only if we didn't just free up a slot)
            else if (_indexMap.Count >= _currentMaxSize)
            {
                var keyToEvict = SelectEvictionCandidate(ownerId, index, totalCount, isReverse);
                if (!EqualityComparer<CacheKey>.Default.Equals(keyToEvict, default))
                {
                    // If we already have an evictedValue from above? Impossible (due to 'else if')
                    evictedValue = RemoveInternal(keyToEvict);
                }
            }

            // 3. Add Data
            _data.TryAdd(filePath, value);

            // 4. Update Map
            _indexMap[newKey] = filePath;

            return !EqualityComparer<TValue>.Default.Equals(evictedValue, default);
        }
    }

    /// <summary>
    /// Removes a logical key and checks if the underlying data should also be removed (ref counting).
    /// Returns the data value if it was fully evicted (orphaned), otherwise default.
    /// </summary>
    private TValue? RemoveInternal(CacheKey key)
    {
        if (!_indexMap.Remove(key, out var path))
        {
            return default;
        }

        // Check if any other keys point to this path
        // Optimization: We could maintain a RefCount dictionary, but iterating _indexMap is acceptable 
        // if capacity is small (~50-100 items). For larger caps, add a RefCount Dict.
        var isReferenced = _indexMap.ContainsValue(path);

        if (isReferenced)
        {
            return default;
        }

        // Orphaned! Remove actual data.
        if (!_data.Remove(path, out var val))
        {
            return default;
        }

#if DEBUG
        if (DebugHelper.ShowCacheAdditionsAndRemovals)
        {
            Trace.WriteLine($"Fully Evicted file: {Path.GetFileName(path)}");
        }
#endif
        return val;
    }

    private CacheKey SelectEvictionCandidate(string ownerId, int currentIndex, int totalCount, bool isReverse)
    {
        var maxDistance = -1;
        var keyToEvict = default(CacheKey);
        var foundCandidate = false;

        // Only evict items belonging to the current OwnerId (Polite Eviction)
        // If we are desperate (no items for this owner), we might need to evict others,
        // but typically we expand capacity per owner so this shouldn't happen.
        var candidates = _indexMap.Keys.Where(k => k.OwnerId == ownerId);

        foreach (var candidateKey in candidates)
        {
            int distance;
            if (isReverse)
            {
                // Moving backward: Evict furthest ahead
                distance = (candidateKey.Index - currentIndex + totalCount) % totalCount;
            }
            else
            {
                // Moving forward: Evict furthest behind
                distance = (currentIndex - candidateKey.Index + totalCount) % totalCount;
            }

            if (distance <= maxDistance)
            {
                continue;
            }

            maxDistance = distance;
            keyToEvict = candidateKey;
            foundCandidate = true;
        }

        if (foundCandidate)
        {
            return keyToEvict;
        }

        // Fallback: If current owner has NO items (first load?), but cache is full of OTHER owners.
        // We must evict someone else.
        if (_indexMap.Count >= _currentMaxSize)
        {
            // Just pick the first one. (Or implement LRU for cross-tab eviction)
            return _indexMap.Keys.FirstOrDefault();
        }

        return default;
    }

    public bool TryRemove(string ownerId, int index, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            if (_indexMap.Remove(new CacheKey(ownerId, index), out var path))
            {
                value = _data[path];
                return _data.Remove(path);
            }
            value = default;
            return false;
        }
    }

    // Explicit TryGetValue for logic that just checks existence
    public bool TryGetValue(string ownerId, int index, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            if (_indexMap.TryGetValue(new CacheKey(ownerId, index), out var path))
            {
                return _data.TryGetValue(path, out value);
            }

            value = default;
            return false;
        }
    }

    // Optimized: Direct lookup in Data dictionary
    public bool TryGetValueByPath(string filePath, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            return _data.TryGetValue(filePath, out value);
        }
    }

    public bool TryFindKeyByPath(string filePath, out CacheKey key)
    {
        lock (_lock)
        {
            foreach (var kvp in _indexMap.Where(kvp => kvp.Value == filePath).AsValueEnumerable())
            {
                key = kvp.Key;
                return true;
            }

            key = default;
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _indexMap.Clear();
            _data.Clear();
        }
    }

    public void Clear(string ownerId)
    {
        lock (_lock)
        {
            var keysToRemove = _indexMap.Keys.Where(k => k.OwnerId == ownerId).AsValueEnumerable();
            foreach (var key in keysToRemove)
            {
                RemoveInternal(key);
            }
        }
    }

    // Composite key to ensure uniqueness across tabs for logical mapping
    public readonly record struct CacheKey(string OwnerId, int Index);
}