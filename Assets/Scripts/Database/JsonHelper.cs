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
}