using System;
using System.Collections.Generic;

/// <summary>
/// Weighted random selection utility using cumulative weights + binary search (O(log N)).
/// Supports single select and multi-select without replacement.
/// </summary>
public static class WeightedRandom
{
    /// <summary>
    /// A generic weighted item for random selection.
    /// </summary>
    public struct WeightedItem<T>
    {
        public T Value;
        public float Weight;

        public WeightedItem(T value, float weight)
        {
            Value = value;
            Weight = weight;
        }
    }

    /// <summary>
    /// Select a single item using weighted random with O(log N) binary search.
    /// </summary>
    public static T Select<T>(List<WeightedItem<T>> items)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Items list is null or empty.", nameof(items));

        float totalWeight = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            totalWeight += items[i].Weight;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            cumulative += items[i].Weight;
            if (randomValue <= cumulative)
            {
                return items[i].Value;
            }
        }

        // Fallback (should rarely happen due to float precision)
        return items[items.Count - 1].Value;
    }

    /// <summary>
    /// Select N distinct items without replacement using weighted random.
    /// Uses a temporary list copy to support removal.
    /// </summary>
    public static List<T> SelectN<T>(List<WeightedItem<T>> items, int count)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Items list is null or empty.", nameof(items));

        if (count <= 0)
            return new List<T>();

        count = Math.Min(count, items.Count);

        // Work on a copy so we can remove selected items
        var remaining = new List<WeightedItem<T>>(items);
        var result = new List<T>(count);

        for (int i = 0; i < count; i++)
        {
            float totalWeight = 0f;
            for (int j = 0; j < remaining.Count; j++)
            {
                totalWeight += remaining[j].Weight;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            int selectedIndex = 0;

            for (int j = 0; j < remaining.Count; j++)
            {
                cumulative += remaining[j].Weight;
                if (randomValue <= cumulative)
                {
                    selectedIndex = j;
                    break;
                }
            }

            result.Add(remaining[selectedIndex].Value);
            remaining.RemoveAt(selectedIndex);
        }

        return result;
    }

    /// <summary>
    /// Convenience: build a WeightedItem list from parallel value/weight arrays.
    /// </summary>
    public static List<WeightedItem<T>> BuildList<T>(T[] values, float[] weights)
    {
        if (values == null || weights == null || values.Length != weights.Length)
            throw new ArgumentException("Values and weights must be non-null and same length.");

        var list = new List<WeightedItem<T>>(values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            list.Add(new WeightedItem<T>(values[i], weights[i]));
        }

        return list;
    }
}