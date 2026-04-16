using System;
using UnityEngine;

public static class JsonHelper
{
    [Serializable]
    private class ArrayWrapper<T>
    {
        public T[] items;
    }

    public static T[] FromJsonArray<T>(string json)
    {
        if (string.IsNullOrEmpty(json)) return Array.Empty<T>();

        var wrapped = "{\"items\":" + json + "}";
        var result = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
        return result != null && result.items != null ? result.items : Array.Empty<T>();
    }

    public static bool TryFromJsonObject<T>(string json, out T result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            result = JsonUtility.FromJson<T>(json.Trim());
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static bool TryFromJsonArray<T>(string json, out T[] result)
    {
        result = Array.Empty<T>();

        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            string trimmed = json.Trim();

            if (trimmed.StartsWith("["))
            {
                result = FromJsonArray<T>(trimmed);
                return true;
            }

            if (trimmed.StartsWith("{"))
            {
                var wrapped = JsonUtility.FromJson<ArrayWrapper<T>>(trimmed);
                if (wrapped != null && wrapped.items != null)
                {
                    result = wrapped.items;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            result = Array.Empty<T>();
            return false;
        }
    }
}