using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PicView.Core.DebugTools;
using ZLinq;

namespace PicView.Benchmarks.Resources;

/// <summary>
/// A thread-safe, capacity-constrained container responsible for storing loaded images and managing eviction.
/// <para>
/// Optimized for .NET 9+:
/// 1. Uses <see cref="CollectionsMarshal"/> for high-speed ref access.
/// 2. Implements <see cref="IAlternateEqualityComparer{T,TAlternate}"/> for zero-allocation lookups using Spans.
/// 3. Denormalizes storage to avoid double-lookups on reads.
/// </para>
/// </summary>
public class EvictingDictionary2Span<TValue> : IEnumerable<KeyValuePair<(string OwnerId, int Index), TValue>> 
    where TValue : class // Constraint added to ensure reference type logic works smoothly
{
    // Data Store: Tracks the actual loaded images by FilePath
    private readonly Dictionary<string, TValue> _data;

    // Logical Map: Tracks which Tab/Index points to which File.
    // OPTIMIZATION: Stores (Path, Value) tuple to avoid a second lookup in _data.
    private readonly Dictionary<CacheKey, (string Path, TValue Value)> _indexMap;

    private readonly int _initialMaxSize;
    private readonly Lock _lock = new();
    private int _currentMaxSize;

    // Comparer instance to handle standard and Span-based lookups
    private static readonly CacheKeyComparer Comparer = new();

    public EvictingDictionary2Span(int initialMaxSize)
    {
        if (initialMaxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialMaxSize), "Size must be positive.");

        _initialMaxSize = initialMaxSize;
        _currentMaxSize = initialMaxSize;

        // Use the custom comparer that supports AlternateLookup
        _indexMap = new Dictionary<CacheKey, (string, TValue)>(initialMaxSize, Comparer);
        
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
                // Optimization: Snapshot to array directly
                if (_data.Count == 0) return Array.Empty<TValue>();
                var arr = new TValue[_data.Count];
                _data.Values.CopyTo(arr, 0);
                return arr;
            }
        }
    }

    public IEnumerator<KeyValuePair<(string OwnerId, int Index), TValue>> GetEnumerator()
    {
        List<KeyValuePair<(string, int), TValue>> snapshot;
        lock (_lock)
        {
            snapshot = new List<KeyValuePair<(string, int), TValue>>(_indexMap.Count);
            foreach (var kvp in _indexMap)
            {
                // kvp.Value is now (Path, Value), so we can access Value directly
                snapshot.Add(new KeyValuePair<(string, int), TValue>(
                    (kvp.Key.OwnerId, kvp.Key.Index), kvp.Value.Value));
            }
        }

        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void ExpandCapacity(string id)
    {
        lock (_lock)
        {
            _currentMaxSize += _initialMaxSize;
#if DEBUG
            DebugHelper.LogDebug(nameof(EvictingDictionary2Span<>), nameof(ExpandCapacity),
                $"Expanded by '{id}'. New Capacity: {_currentMaxSize}");
#endif
        }
    }

    public void DecreaseCapacity(string id)
    {
        lock (_lock)
        {
            // Optimization: Use CollectionsMarshal isn't easy for "Where" queries, 
            // but we can iterate keys manually if performance is critical here. 
            // For now, standard Linq is safe as this is a rare operation.
            var keysToRemove = _indexMap.Keys.Where(k => k.OwnerId == id).ToArray();
            foreach (var key in keysToRemove)
            {
                RemoveInternal(key);
            }

            if (_currentMaxSize > _initialMaxSize)
            {
                _currentMaxSize -= _initialMaxSize;
            }

            if (_currentMaxSize < _initialMaxSize)
            {
                _currentMaxSize = _initialMaxSize;
            }
        }
    }

    // Standard TryAdd
    public bool TryAdd(string ownerId, int index, string filePath, TValue value,
        int totalCount, bool isReverse, [MaybeNullWhen(false)] out TValue evictedValue)
    {
        lock (_lock)
        {
            var newKey = new CacheKey(ownerId, index);
            evictedValue = null;

            // OPTIMIZATION: Use Ref to peek into the dictionary
            ref var existingEntry = ref CollectionsMarshal.GetValueRefOrNullRef(_indexMap, newKey);

            if (!Unsafe.IsNullRef(ref existingEntry))
            {
                // Key exists. Check if path matches.
                if (existingEntry.Path == filePath)
                {
                    return false;
                }

                // Path changed. Remove the old one.
                evictedValue = RemoveInternal(newKey);
            }
            else if (_indexMap.Count >= _currentMaxSize)
            {
                var keyToEvict = SelectEvictionCandidate(ownerId, index, totalCount, isReverse);
                if (!EqualityComparer<CacheKey>.Default.Equals(keyToEvict, default))
                {
                    evictedValue = RemoveInternal(keyToEvict);
                }
            }

            // 3. Add Data
            _data.TryAdd(filePath, value);

            // 4. Update Map (Store BOTH Path and Value)
            _indexMap[newKey] = (filePath, value);

            return evictedValue is not null; // Returns true if something was evicted
        }
    }

    /// <summary>
    /// OPTIMIZED: Standard lookup using string ownerId.
    /// Uses CollectionsMarshal to get the value in one hop.
    /// </summary>
    public bool TryGetValue(string ownerId, int index, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_indexMap, new CacheKey(ownerId, index));
            
            if (!Unsafe.IsNullRef(ref entry))
            {
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// OPTIMIZED: Lookup using ReadOnlySpan for OwnerId.
    /// Uses .NET 9 AlternateLookup to avoid allocating the OwnerId string.
    /// </summary>
    public bool TryGetValue(ReadOnlySpan<char> ownerIdSpan, int index, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            // Create the ref struct for lookup
            var lookupKey = new CacheKeyLookup(ownerIdSpan, index);
            
            // Get the AlternateLookup from the dictionary
            var lookup = _indexMap.GetAlternateLookup<CacheKeyLookup>();

            if (lookup.TryGetValue(lookupKey, out var entry))
            {
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// OPTIMIZED: Lookup using ReadOnlySpan for FilePath.
    /// Uses .NET 9 AlternateLookup on the string dictionary.
    /// </summary>
    public bool TryGetValueByPath(ReadOnlySpan<char> filePathSpan, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            var lookup = _data.GetAlternateLookup<ReadOnlySpan<char>>();
            return lookup.TryGetValue(filePathSpan, out value);
        }
    }

    public bool TryRemove(string ownerId, int index, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            // RemoveInternal handles the logic
            value = RemoveInternal(new CacheKey(ownerId, index));
            return value != null;
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
            // Optimization: Get keys first to avoid invalidating enumerator
            var keysToRemove = _indexMap.Keys.Where(k => k.OwnerId == ownerId).ToArray();
            foreach (var key in keysToRemove)
            {
                RemoveInternal(key);
            }
        }
    }

    private TValue? RemoveInternal(CacheKey key)
    {
        if (!_indexMap.Remove(key, out var entry))
        {
            return null;
        }

        // Check if referenced by others.
        // We iterate Values now, which contain (Path, Value).
        var isReferenced = _indexMap.Values.AsValueEnumerable().Any(value => value.Path == entry.Path);

        if (isReferenced)
        {
            return null;
        }

        _data.Remove(entry.Path, out var val);
        
#if DEBUG
        if (DebugHelper.ShowCacheAdditionsAndRemovals)
        {
            Trace.WriteLine($"Fully Evicted file: {Path.GetFileName(entry.Path)}");
        }
#endif
        return val;
    }

    private CacheKey SelectEvictionCandidate(string ownerId, int currentIndex, int totalCount, bool isReverse)
    {
        var maxDistance = -1;
        var keyToEvict = default(CacheKey);
        var foundCandidate = false;

        var candidates = _indexMap.Keys.Where(k => k.OwnerId == ownerId);

        foreach (var candidateKey in candidates)
        {
            int distance;
            if (isReverse)
            {
                distance = (candidateKey.Index - currentIndex + totalCount) % totalCount;
            }
            else
            {
                distance = (currentIndex - candidateKey.Index + totalCount) % totalCount;
            }

            if (distance <= maxDistance) continue;

            maxDistance = distance;
            keyToEvict = candidateKey;
            foundCandidate = true;
        }

        if (foundCandidate) return keyToEvict;

        if (_indexMap.Count >= _currentMaxSize)
        {
            return _indexMap.Keys.FirstOrDefault();
        }

        return default;
    }

    // -------------------------------------------------------------------------
    //  .NET 9 AlternateLookup Implementation
    // -------------------------------------------------------------------------

    /// <summary>
    /// The standard key stored in the dictionary.
    /// </summary>
    public readonly record struct CacheKey(string OwnerId, int Index);

    /// <summary>
    /// The lookup key used with ReadOnlySpan (ref struct).
    /// </summary>
    public readonly ref struct CacheKeyLookup
    {
        public readonly ReadOnlySpan<char> OwnerIdSpan;
        public readonly int Index;

        public CacheKeyLookup(ReadOnlySpan<char> ownerIdSpan, int index)
        {
            OwnerIdSpan = ownerIdSpan;
            Index = index;
        }
    }

    /// <summary>
    /// Custom Comparer that enables looking up CacheKey using CacheKeyLookup.
    /// </summary>
    private class CacheKeyComparer : IEqualityComparer<CacheKey>, IAlternateEqualityComparer<CacheKey, CacheKeyLookup>
    {
        // Standard Equality
        public bool Equals(CacheKey x, CacheKey y) => 
            x.Index == y.Index && string.Equals(x.OwnerId, y.OwnerId, StringComparison.Ordinal);

        public int GetHashCode(CacheKey obj) => 
            HashCode.Combine(obj.OwnerId, obj.Index);

        public CacheKeyLookup Create(CacheKey alternate)
        {
            return new CacheKeyLookup(alternate.OwnerId.AsSpan(), alternate.Index);
        }

        // Alternate Equality (CacheKey vs CacheKeyLookup)
        public bool Equals(CacheKey key, CacheKeyLookup lookup)
        {
            return key.Index == lookup.Index && 
                   key.OwnerId.AsSpan().SequenceEqual(lookup.OwnerIdSpan);
        }

        public int GetHashCode(CacheKeyLookup lookup)
        {
            var hash = new HashCode();
            // Important: Use string.GetHashCode(span) to match the string hash
            hash.Add(string.GetHashCode(lookup.OwnerIdSpan, StringComparison.Ordinal));
            hash.Add(lookup.Index);
            return hash.ToHashCode();
        }

        // Required by interface: Create a storable key from the lookup key
        public CacheKey Create(CacheKeyLookup lookup)
        {
            return new CacheKey(lookup.OwnerIdSpan.ToString(), lookup.Index);
        }
    }
}