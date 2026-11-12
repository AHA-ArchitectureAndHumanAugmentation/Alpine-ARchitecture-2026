using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;

namespace Compas.Core.Data.Conversions
{
    /// <summary>
    /// Numeric conversion helpers for deserializing loosely-typed payload values.
    /// </summary>
    [System.Serializable]
    public static class NumericConverters
    {
        /// <summary>
        /// Converts supported numeric container types into a float array.
        /// Accepts <see cref="List{Object}"/>, <see cref="List{Double}"/>, <see cref="List{Single}"/>,
        /// numeric arrays, and <see cref="JArray"/>.
        /// </summary>
        /// <param name="data">Input numeric container.</param>
        /// <returns>Converted float array.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="data"/> is not a supported numeric container type.</exception>
        public static float[] ConvertNumericDatatoFloatArray(object data)
        {
            if (data is List<object> dataList)
            {
                return dataList.Select(Convert.ToSingle).ToArray();
            }
            else if (data is float[] floatArray)
            {
                return floatArray;
            }
            else if (data is List<System.Double> doubleList)
            {
                return doubleList.Select(Convert.ToSingle).ToArray();
            }
            else if (data is System.Single[] singleArray)
            {
                return singleArray.ToArray();
            }
            else if (data is List<System.Single> singleList)
            {
                return singleList.Select(Convert.ToSingle).ToArray();
            }
            else if (data is System.Double[] doubleArray)
            {
                return doubleArray.Select(Convert.ToSingle).ToArray();
            }
            else if (data is JArray dataArray)
            {
                return dataArray.Select(token => (float)token).ToArray();
            }
            else
            {
                throw new FormatException("DataParser: Data is not a List<Object>, List<System.Double>, System.Double Array, System.Single Array, List<System.Single>,  float Array, or JArray.");
            }
        }
    }

    /// <summary>
    /// JSON/object conversion helpers for dictionary and index-list extraction.
    /// </summary>
    [System.Serializable]
    public static class JsonObjectConverters
    {
        /// <summary>
        /// Converts a supported object to <see cref="Dictionary{String, Object}"/>.
        /// Returns an empty dictionary if conversion is not possible.
        /// </summary>
        /// <param name="value">Input object value.</param>
        /// <returns>Dictionary representation or empty dictionary.</returns>
        public static Dictionary<string, object> ToDictionary(object? value)
        {
            if (value is Dictionary<string, object> dict)
            {
                return dict;
            }

            if (value is JObject jsonObject)
            {
                return jsonObject.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
            }

            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Returns a nested dictionary from <paramref name="data"/> when <paramref name="key"/> exists and is non-empty.
        /// Otherwise returns the original dictionary.
        /// </summary>
        /// <param name="data">Outer dictionary payload.</param>
        /// <param name="key">Nested payload key. Defaults to <c>data</c>.</param>
        /// <returns>The nested dictionary when present and non-empty; otherwise the original dictionary.</returns>
        public static Dictionary<string, object> GetOptionalInnerData(Dictionary<string, object> data, string key = "data")
        {
            if (data.TryGetValue(key, out var inner))
            {
                var innerDict = ToDictionary(inner);
                if (innerDict.Count > 0)
                {
                    return innerDict;
                }
            }

            return data;
        }

        /// <summary>
        /// Retrieves a dictionary value by key from an outer dictionary.
        /// Returns an empty dictionary when the key is missing or not dictionary-compatible.
        /// </summary>
        /// <param name="data">Outer dictionary payload.</param>
        /// <param name="key">Lookup key.</param>
        /// <returns>Dictionary value when available; otherwise an empty dictionary.</returns>
        public static Dictionary<string, object> GetObjectDictionary(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var value))
            {
                return new Dictionary<string, object>();
            }

            return ToDictionary(value);
        }

        /// <summary>
        /// Converts supported integer container types into a list of indices.
        /// Accepts <see cref="Int32"/> arrays/lists, <see cref="List{Object}"/> with numeric values, and <see cref="JArray"/>.
        /// </summary>
        /// <param name="value">Input index container.</param>
        /// <returns>Converted integer index list, or an empty list if conversion is not possible.</returns>
        public static List<int> ConvertToIntList(object value)
        {
            if (value is int[] array)
            {
                return array.ToList();
            }

            if (value is List<int> list)
            {
                return new List<int>(list);
            }

            if (value is List<object> objectList)
            {
                return objectList.Select(Convert.ToInt32).ToList();
            }

            if (value is JArray jsonArray)
            {
                return jsonArray.Select(token => (int)token).ToList();
            }

            return new List<int>();
        }
    }
}