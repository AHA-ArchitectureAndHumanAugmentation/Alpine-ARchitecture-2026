using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Compas.Core.Data
{
    [System.Serializable]
    public abstract class Data
    {
        /// <summary>
        /// Data: A class for defining data interfaces of objects.
        /// It is used as a base class for all data classes in the Compas.Core.Data and Compas.Core.Geometry namespace.
        /// It is based off of translations of the COMPAS Data class.
        /// </summary>

        // Never null: protobuf string fields reject null.
        private string guid = string.Empty;
        private string name = string.Empty;

        /// <summary>COMPAS identity, carried on the wire. Empty when the sender gave none.</summary>
        public string Guid
        {
            get => guid;
            set => guid = value ?? string.Empty;
        }

        /// <summary>COMPAS display name, carried on the wire. Empty when the sender gave none.</summary>
        public string Name
        {
            get => name;
            set => name = value ?? string.Empty;
        }

        /// <summary>
        /// DType: A unique identifier for the object's type based on its namespace and class name.
        /// </summary>
        /// <returns>The namespace-qualified COMPAS-style type identifier.</returns>
        public virtual string DType
        {
            get
            {
                var namespaceParts = GetType().Namespace?.Split('.');
                var prefix = namespaceParts != null && namespaceParts.Length > 1
                    ? string.Join(".", namespaceParts.Take(2))
                    : GetType().Namespace;
                return $"{prefix}/{GetType().Name}";
            }
        }

        /// <summary>
        /// Convert this instance into a COMPAS-compatible data dictionary.
        /// </summary>
        /// <returns>Serialized dictionary payload.</returns>
        public abstract Dictionary<string, object> ToData();

        /// <summary>
        /// FromData(): Returning a class object into a data dictionary.
        /// IMPLEMENTATION NOTE: This method is not implemented in the base class.
        /// </summary>
        /// <param name="data">Dictionary data payload.</param>
        /// <returns>A derived <see cref="Data"/> instance.</returns>
        public static Data FromData(Dictionary<string, object> data)
        {
            throw new NotImplementedException("Derived classes must implement FromData.");
        }

        /// <summary>
        /// ToJson(): Method for turning the class object into a json string.
        /// </summary>
        /// <typeparam name="T">Input object type.</typeparam>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="writeIndented">Whether to format JSON with indentation.</param>
        /// <returns>Serialized JSON string.</returns>
        public static string ToJson<T>(T obj, bool writeIndented = false)
        {
            return JsonConvert.SerializeObject(obj, writeIndented ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Deserialize a JSON string into a typed object.
        /// </summary>
        /// <typeparam name="T">Target object type.</typeparam>
        /// <param name="json">Input JSON string.</param>
        /// <returns>Deserialized object instance.</returns>
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Deserialize a JSON file into a typed object.
        /// </summary>
        /// <typeparam name="T">Target object type.</typeparam>
        /// <param name="path">Path to JSON file.</param>
        /// <returns>Deserialized object instance.</returns>
        public static T FromJsonFile<T>(string path)
        {
            string json = File.ReadAllText(path);
            return FromJson<T>(json);
        }

        /// <summary>
        /// ToJsonFile(): Method for turning the class object into a json file.
        /// </summary>
        /// <typeparam name="T">Input object type.</typeparam>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="path">Output file path.</param>
        /// <param name="writeIndented">Whether to format JSON with indentation.</param>
        public static void ToJsonFile<T>(T obj, string path, bool writeIndented = false)
        {
            string json = ToJson(obj, writeIndented);
            File.WriteAllText(path, json);
        }
    }
}