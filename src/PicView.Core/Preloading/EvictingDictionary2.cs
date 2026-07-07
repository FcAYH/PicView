using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

namespace PicView.Core.Preloading;

/// <summary>
/// A thread-safe, capacity-constrained container responsible for storing loaded images and managing eviction.
/// <para>
/// This dictionary separates logical slots (Owner ID + Index) from physical data (File Paths) to allow 
/// deduplication across tabs. It enforces an eviction policy that removes items that are calculated 
/// to be too far away from the current viewing index of the specific tab owner.
/// </para>
/// </summary>
public class EvictingDictionary2<TValue> : IEnumerable<KeyValuePair<(string OwnerId, int Index), TValue>>
    where TValue : class
{
    private static readonly CacheKeyComparer Comparer = new();
    private readonly Dictionary<string, TValue> _data;

    // Storing Tuple (Path, Value) to avoid double-lookups on reads
    private readonly Dictionary<CacheKey, (string Path, TValue Value)> _indexMap;

    private readonly int _initialMaxSize;
    private readonly Lock _lock = new();
    private int _currentMaxSize;

    public EvictingDictionary2(int initialMaxSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialMaxSize);

        _initialMaxSize = initialMaxSize;
        _currentMaxSize = initialMaxSize;

        _indexMap = new Dictionary<CacheKey, (string, TValue)>(initialMaxSize, Comparer);

        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        _data = new Dictionary<string, TValue>(initialMaxSize, pathComparer);
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
                if (_data.Count == 0)
                {
                    return Array.Empty<TValue>();
                }

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
                snapshot.Add(new KeyValuePair<(string, int), TValue>(
                    (kvp.Key.OwnerId, kvp.Key.Index), kvp.Value.Value));
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
            _currentMaxSize += _initialMaxSize;
        }
    }

    public void DecreaseCapacity(string id)
    {
        lock (_lock)
        {
            List<CacheKey> keysToRemove = [];
            foreach (var key in _indexMap.Keys)
            {
                if (key.OwnerId == id)
                {
                    keysToRemove.Add(key);
                }
            }

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

    // ------------------------------------------------------------------------
    // HYBRID LOOKUP OPTIMIZATION
    // ------------------------------------------------------------------------

    /// <summary>
    /// FAST PATH: Uses standard string lookup to benefit from cached hash codes.
    /// Use this when you already have a string.
    /// </summary>
    public bool TryGetValueByPath(string filePath, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            return _data.TryGetValue(filePath, out value);
        }
    }

    /// <summary>
    /// ALLOCATION-FREE PATH: Uses Span lookup.
    /// Use this when your path is a slice of a larger buffer/span.
    /// </summary>
    public bool TryGetValueByPath(ReadOnlySpan<char> filePathSpan, out TValue? value)
    {
        lock (_lock)
        {
            var lookup = _data.GetAlternateLookup<ReadOnlySpan<char>>();
            return lookup.TryGetValue(filePathSpan, out value);
        }
    }

    public bool TryGetValue(string ownerId, int index, out TValue? value)
    {
        lock (_lock)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_indexMap, new CacheKey(ownerId, index));
            if (!Unsafe.IsNullRef(ref entry))
            {
                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }
    }

    // ------------------------------------------------------------------------
    // ADD / REMOVE LOGIC
    // ------------------------------------------------------------------------

    public bool TryAdd(string ownerId, int index, string filePath, TValue value,
        int totalCount, bool isReverse, [MaybeNullWhen(false)] out TValue evictedValue)
    {
        lock (_lock)
        {
            var newKey = new CacheKey(ownerId, index);
            evictedValue = null;

            ref var existingEntry = ref CollectionsMarshal.GetValueRefOrNullRef(_indexMap, newKey);

            if (!Unsafe.IsNullRef(ref existingEntry))
            {
                if (existingEntry.Path == filePath)
                {
                    // IMPORTANT: ensure the slot points to the canonical instance
                    if (_data.TryGetValue(filePath, out var canonical) &&
                        !ReferenceEquals(existingEntry.Value, canonical))
                    {
                        existingEntry = (filePath, canonical);
                    }

                    return false; // no eviction, nothing else to do
                }

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

            // Canonicalize the value by filePath
            if (!_data.TryGetValue(filePath, out var storedValue))
            {
                _data[filePath] = value;
                storedValue = value;
            }
            // Always link the logical slot to the canonical stored value
            _indexMap[newKey] = (filePath, storedValue);

            // NOTE: if storedValue != value, value is redundant; if TValue can hold resources,
            // consider disposing it at the call-site or changing the API to return it.
            return evictedValue is not null;
        }
    }


    public bool TryRemove(string ownerId, int index, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            value = RemoveInternal(new CacheKey(ownerId, index));
            return value != null;
        }
    }

    /// <summary>
    /// Resynchronizes the cache for a specific owner with a new list of files.
    /// Items that are no longer in the list are removed and returned for disposal.
    /// Items that are in the list are moved to their new indices.
    /// </summary>
    /// <param name="ownerId">The owner of the cache items.</param>
    /// <param name="newFiles">The new list of files.</param>
    /// <returns>A list of values that were evicted/removed because they are no longer in the file list.</returns>
    public List<TValue> Resynchronize(string ownerId, IReadOnlyList<FileInfo> newFiles)
    {
        var evictedItems = new List<TValue>();

        lock (_lock)
        {
            if (newFiles.Count == 0)
            {
                // Clear all for this owner
                foreach (var kvp in _indexMap)
                {
                    if (kvp.Key.OwnerId == ownerId)
                    {
                        var evicted = RemoveInternal(kvp.Key);
                        if (evicted is not null)
                        {
                            evictedItems.Add(evicted);
                        }
                    }
                }
                return evictedItems;
            }

            // 1. Build lookup for new files
            // Use StringComparer based on OS
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var newFileMap = new Dictionary<string, int>(newFiles.Count, comparer);
            for (var i = 0; i < newFiles.Count; i++)
            {
                // We use "last wins" strategy for duplicates, or "first wins" doesn't matter much if files are same.
                // Preloader used overwriting, so we do too.
                newFileMap[newFiles[i].FullName] = i;
            }

            // 2. Identify keys to process
            var entries = new List<(int OldIndex, string Path, TValue Value)>();
            
            foreach (var kvp in _indexMap)
            {
                if (kvp.Key.OwnerId == ownerId)
                {
                    entries.Add((kvp.Key.Index, kvp.Value.Path, kvp.Value.Value));
                }
            }

            var moves = new List<(int NewIndex, string Path, TValue Value)>();

            // 3. Process entries
            foreach (var entry in entries)
            {
                if (newFileMap.TryGetValue(entry.Path, out var newIndex))
                {
                    if (newIndex != entry.OldIndex)
                    {
                        moves.Add((newIndex, entry.Path, entry.Value));
                    }
                    // Else: Index is same, do nothing.
                    // But wait, if we are doing a full resync, we should be careful about other moves overwriting this if we don't 'protect' it.
                    // But 'moves' only contains things that change index.
                    // Things that don't change index stay in _indexMap.
                    // If a move targets an index that is currently occupied by a 'stayer',
                    // then that 'stayer' MUST be wrong (because newFileMap says that index belongs to the Mover).
                    // So the 'stayer' should have been identified as a Mover or a Remover.
                    // So we are safe.
                }
                else
                {
                    // Item no longer in list -> Remove
                    var evicted = RemoveInternal(new CacheKey(ownerId, entry.OldIndex));
                    if (evicted is not null)
                    {
                        evictedItems.Add(evicted);
                    }
                }
            }

            if (moves.Count > 0)
            {
                // 4. Perform moves
                
                // First, remove the old keys for all movers
                // We do NOT use RemoveInternal because we don't want to decrement refs in _data yet
                // We know these items exist and we are keeping them.
                foreach (var entry in entries)
                {
                    if (newFileMap.TryGetValue(entry.Path, out var newIndex) && newIndex != entry.OldIndex)
                    {
                        _indexMap.Remove(new CacheKey(ownerId, entry.OldIndex));
                    }
                }

                // Second, add them at new indices
                foreach (var move in moves)
                {
                    var newKey = new CacheKey(ownerId, move.NewIndex);
                    // It's possible _indexMap already has something here? 
                    // No, because we iterated ALL entries for this owner.
                    // Any entry that was at 'NewIndex' must have been processed.
                    // Either it stayed (NewIndex == OldIndex), which contradicts that 'move' is claiming NewIndex.
                    // Or it moved/removed.
                    // So the slot should be free (or occupied by a 'stayer' which is impossible if mapping is unique).
                    // IF mapping is unique (1:1). 
                    // If duplicates exist in file list, multiple files might map to same index? No, list index is unique.
                    // If duplicates exist in _indexMap (same file at multiple indices)? 
                    // _indexMap is (Owner, Index) unique.
                    // _data is unique by Path.
                    
                    _indexMap[newKey] = (move.Path, move.Value);
                }
            }
        }

        return evictedItems;
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
            // ALLOCATION FIX: Manual iteration instead of LINQ
            List<CacheKey> toRemove = [];
            foreach (var k in _indexMap.Keys)
            {
                if (k.OwnerId == ownerId)
                {
                    toRemove.Add(k);
                }
            }

            foreach (var key in toRemove)
            {
                RemoveInternal(key);
            }
        }
    }

    /// <summary>
    /// Removes a logical key and checks if the underlying data should also be removed (ref counting).
    /// Returns the data value if it was fully evicted (orphaned), otherwise default.
    /// </summary>
    private TValue? RemoveInternal(CacheKey key)
    {
        if (!_indexMap.Remove(key, out var entry))
        {
            return null;
        }

        // Optimization: Check values manually to avoid LINQ/Lambdas
        var isReferenced = false;
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var val in _indexMap.Values)
        {
            if (val.Path != entry.Path)
            {
                continue;
            }

            isReferenced = true;
            break;
        }

        if (isReferenced)
        {
            return null;
        }

        _data.Remove(entry.Path, out var valOut);
        return valOut;
    }

    private CacheKey SelectEvictionCandidate(string ownerId, int currentIndex, int totalCount, bool isReverse)
    {
        var maxDistance = -1;
        var keyToEvict = default(CacheKey);
        var foundCandidate = false;

        // ALLOCATION FIX: Replaced LINQ .Where() with manual foreach
        // This removes the Enumerator allocation on every eviction.
        foreach (var candidateKey in _indexMap.Keys)
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

        // Fallback: just remove first key if we are full and logic above failed
        if (_indexMap.Count < _currentMaxSize)
        {
            return default;
        }

        foreach (var k in _indexMap.Keys)
        {
            return k;
        }

        return default;
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    public readonly record struct CacheKey(string OwnerId, int Index);

    public readonly ref struct CacheKeyLookup(ReadOnlySpan<char> ownerIdSpan, int index)
    {
        public readonly ReadOnlySpan<char> OwnerIdSpan = ownerIdSpan;
        public readonly int Index = index;
    }

    private class CacheKeyComparer : IEqualityComparer<CacheKey>, IAlternateEqualityComparer<CacheKey, CacheKeyLookup>
    {
        public CacheKeyLookup Create(CacheKey alternate)
        {
            return new CacheKeyLookup(alternate.OwnerId.AsSpan(), alternate.Index);
        }

        public bool Equals(CacheKey key, CacheKeyLookup lookup)
        {
            return key.Index == lookup.Index && key.OwnerId.AsSpan().SequenceEqual(lookup.OwnerIdSpan);
        }

        public bool Equals(CacheKey x, CacheKey y)
        {
            return x.Index == y.Index && string.Equals(x.OwnerId, y.OwnerId, StringComparison.Ordinal);
        }

        public int GetHashCode(CacheKey obj)
        {
            return HashCode.Combine(obj.OwnerId, obj.Index);
        }

        public int GetHashCode(CacheKeyLookup lookup)
        {
            var hash = new HashCode();
            hash.Add(string.GetHashCode(lookup.OwnerIdSpan, StringComparison.Ordinal));
            hash.Add(lookup.Index);
            return hash.ToHashCode();
        }

        public CacheKey Create(CacheKeyLookup lookup)
        {
            return new CacheKey(lookup.OwnerIdSpan.ToString(), lookup.Index);
        }
    }
}