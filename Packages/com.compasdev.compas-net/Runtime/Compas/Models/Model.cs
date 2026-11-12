using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CompasPb.Data;
using Compas.Core.Geometry;
using Newtonsoft.Json.Linq;

// Plural on purpose. These types come from compas_model, not from compas core — the dtypes
// below say so — so they do not belong under Compas.Core. The obvious name, Compas.Model,
// does not compile: C# resolves a bare name through enclosing namespaces before any using
// directive, so a Compas.Model namespace shadows the Model class for every file under
// Compas.* (CS0118). Compas.Models also happens to match the Python module Model lives in,
// compas_model.models.
namespace Compas.Models
{
    /// <summary>
    /// C# representation of a compas_model Model.
    /// </summary>
    [Serializable]
    public class Model : Compas.Core.Data.Data, ICompasFallback
    {
        /// <summary>
        /// The COMPAS dtype this model is dumped under. Registered as a CompasPb fallback,
        /// so it has to be available without an instance.
        /// </summary>
        public const string ModelDType = "compas_model.models/Model";

        public override string DType => ModelDType;

        public object Transformation { get; set; }
        public Dictionary<string, Element> Elements { get; } = new Dictionary<string, Element>();
        public Dictionary<string, object> Materials { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Tree { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Graph { get; set; } = new Dictionary<string, object>();

        public static Model Parse(object jsonData)
        {
            return FromData(ModelDataConversions.ToDictionary(jsonData));
        }

        /// <summary>
        /// Reads either a COMPAS JSON dump or the raw Model.__data__ mapping used in Firebase.
        /// </summary>
        public new static Model FromData(Dictionary<string, object> payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            var model = new Model
            {
                Guid = ModelDataConversions.GetString(payload, "guid"),
                Name = ModelDataConversions.GetString(payload, "name")
            };

            var data = ModelDataConversions.GetOptionalInnerData(payload);
            data.TryGetValue("transformation", out var transformation);
            model.Transformation = transformation;
            model.Materials = ModelDataConversions.GetDictionary(data, "materials");
            model.Tree = ModelDataConversions.GetDictionary(data, "tree");
            model.Graph = ModelDataConversions.GetDictionary(data, "graph");
            model.ReadElements(ModelDataConversions.GetDictionary(data, "elements"));
            return model;
        }

        /// <summary>
        /// Reads the project-level Firebase contract: model plus elements.
        /// </summary>
        public static Model FromProjectData(Dictionary<string, object> projectData)
        {
            if (projectData == null) throw new ArgumentNullException(nameof(projectData));
            if (!projectData.TryGetValue("model", out var modelValue))
                throw new KeyNotFoundException("Project data does not contain the 'model' key.");

            var model = FromData(ModelDataConversions.ToDictionary(modelValue));
            var projectElements = ModelDataConversions.GetDictionary(projectData, "elements");
            if (projectElements.Count > 0) model.ReadElements(projectElements);
            return model;
        }

        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                ["transformation"] = Transformation,
                ["elements"] = Elements.ToDictionary(item => item.Key, item => (object)item.Value),
                ["materials"] = Materials,
                ["tree"] = Tree,
                ["graph"] = Graph,
            };
        }

        public IDictionary ToFallbackData()
        {
            var dump = new Dictionary<string, object>
            {
                ["dtype"] = DType,
                ["data"] = ToData(),
                ["inheritance"] = new List<object>(),
                ["guid"] = Guid,
            };
            if (!string.IsNullOrEmpty(Name)) dump["name"] = Name;
            return dump;
        }

        private void ReadElements(Dictionary<string, object> values)
        {
            foreach (var item in values)
            {
                var element = item.Value as Element ?? Element.FromData(ModelDataConversions.ToDictionary(item.Value), item.Key);
                Elements[item.Key] = element;
            }
        }
    }

    /// <summary>
    /// C# representation of a compas_model Element and its common spatial data.
    /// </summary>
    [Serializable]
    public class Element : Compas.Core.Data.Data, ICompasFallback
    {
        public const string ElementDType = "compas_model.elements/Element";

        public string ElementType { get; set; } = ElementDType;
        public override string DType => ElementType;
        public Frame Frame { get; set; } = Frame.WorldXY();
        public bool HasTransformation { get; set; }
        public ElementDimensions Dimensions { get; set; } = new ElementDimensions();
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();

        public static Element Parse(object jsonData, string guid = "")
        {
            return FromData(ModelDataConversions.ToDictionary(jsonData), guid);
        }

        public static Element FromData(Dictionary<string, object> payload, string guid = "")
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var data = ModelDataConversions.GetOptionalInnerData(payload);

            var element = new Element
            {
                Guid = ModelDataConversions.GetString(payload, "guid", guid),
                Name = ModelDataConversions.GetString(data, "name", ModelDataConversions.GetString(payload, "name")),
                ElementType = ModelDataConversions.GetString(payload, "dtype", ElementDType),
                Payload = new Dictionary<string, object>(data),
                Dimensions = new ElementDimensions
                {
                    Length = ModelDataConversions.GetFloat(data, "length"),
                    Width = ModelDataConversions.GetFloat(data, "width"),
                    Height = ModelDataConversions.GetFloat(data, "depth", ModelDataConversions.GetFloat(data, "height")),
                }
            };

            if (data.TryGetValue("transformation", out var transformation) && transformation != null)
            {
                element.Frame = ModelDataConversions.FrameFromTransformation(transformation);
                element.HasTransformation = true;
            }
            else if (data.TryGetValue("frame", out var frame) && frame != null)
            {
                element.Frame = ModelDataConversions.FrameFromValue(frame);
                element.HasTransformation = true;
            }

            return element;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Guid) && !string.IsNullOrEmpty(DType) && Frame != null;
        }

        public override Dictionary<string, object> ToData()
        {
            var data = new Dictionary<string, object>(Payload)
            {
                ["transformation"] = HasTransformation
                    ? (object)ModelDataConversions.TransformationFromFrame(Frame)
                    : null,
                ["name"] = Name,
            };

            if (Dimensions.Length.HasValue) data["length"] = Dimensions.Length.Value;
            if (Dimensions.Width.HasValue) data["width"] = Dimensions.Width.Value;
            if (Dimensions.Height.HasValue)
            {
                string key = ElementType.EndsWith("/BeamElement", StringComparison.Ordinal) ? "depth" : "height";
                data[key] = Dimensions.Height.Value;
            }
            return data;
        }

        public IDictionary ToFallbackData()
        {
            var dump = new Dictionary<string, object>
            {
                ["dtype"] = DType,
                ["data"] = ToData(),
                ["guid"] = Guid,
            };
            if (!string.IsNullOrEmpty(Name)) dump["name"] = Name;
            return dump;
        }
    }

    [Serializable]
    public class ElementDimensions
    {
        public float? Length { get; set; }
        public float? Width { get; set; }
        public float? Height { get; set; }
    }

    internal static class ModelDataConversions
    {
        public static Dictionary<string, object> ToDictionary(object value)
        {
            if (value is Dictionary<string, object> dictionary) return dictionary;
            if (value is JObject jsonObject)
                return jsonObject.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
            if (value is IDictionary nonGeneric)
            {
                var result = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in nonGeneric)
                {
                    if (entry.Key != null) result[entry.Key.ToString()] = entry.Value;
                }
                return result;
            }
            return new Dictionary<string, object>();
        }

        public static Dictionary<string, object> GetOptionalInnerData(Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("data", out var inner))
            {
                var dictionary = ToDictionary(inner);
                if (dictionary.Count > 0) return dictionary;
            }
            return payload;
        }

        public static Dictionary<string, object> GetDictionary(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? ToDictionary(value) : new Dictionary<string, object>();
        }

        public static string GetString(Dictionary<string, object> data, string key, string fallback = "")
        {
            return data.TryGetValue(key, out var value) && value != null ? value.ToString() : fallback;
        }

        public static float? GetFloat(Dictionary<string, object> data, string key, float? fallback = null)
        {
            return data.TryGetValue(key, out var value) && value != null ? Convert.ToSingle(value) : fallback;
        }

        public static Frame FrameFromValue(object value)
        {
            if (value is Frame frame) return frame;
            if (value is FrameData frameData) return Frame.FromProto(frameData);
            return Frame.FromData(GetOptionalInnerData(ToDictionary(value)));
        }

        public static Frame FrameFromTransformation(object value)
        {
            if (value is TransformationData transformation) return FrameFromMatrix(transformation.Matrix);

            var outer = ToDictionary(value);
            var data = GetOptionalInnerData(outer);
            if (!data.TryGetValue("matrix", out var matrix))
                throw new FormatException("Element transformation does not contain a matrix.");
            return FrameFromMatrix(FlattenMatrix(matrix));
        }

        public static TransformationData TransformationFromFrame(Frame frame)
        {
            var zaxis = frame.ZAxis;
            var transformation = new TransformationData();
            transformation.Matrix.AddRange(new double[]
            {
                frame.XAxis.X, frame.YAxis.X, zaxis.X, frame.Point.X,
                frame.XAxis.Y, frame.YAxis.Y, zaxis.Y, frame.Point.Y,
                frame.XAxis.Z, frame.YAxis.Z, zaxis.Z, frame.Point.Z,
                0.0, 0.0, 0.0, 1.0,
            });
            return transformation;
        }

        private static Frame FrameFromMatrix(IEnumerable<double> values)
        {
            var matrix = values.ToArray();
            if (matrix.Length != 16) throw new FormatException("A COMPAS transformation matrix must contain 16 values.");
            return new Frame(
                new Point((float)matrix[3], (float)matrix[7], (float)matrix[11]),
                new Vector((float)matrix[0], (float)matrix[4], (float)matrix[8]),
                new Vector((float)matrix[1], (float)matrix[5], (float)matrix[9]));
        }

        private static IEnumerable<double> FlattenMatrix(object value)
        {
            if (value is JArray jsonArray)
            {
                foreach (var token in jsonArray.SelectMany(item => item is JArray row ? row : new JArray(item)))
                    yield return Convert.ToDouble(token);
                yield break;
            }

            if (value is IEnumerable rows && !(value is string))
            {
                foreach (var row in rows)
                {
                    if (row is IEnumerable cells && !(row is string))
                    {
                        foreach (var cell in cells) yield return Convert.ToDouble(cell);
                    }
                    else
                    {
                        yield return Convert.ToDouble(row);
                    }
                }
            }
        }
    }
}
