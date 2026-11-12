using Compas.Core.Data;
using Compas.Core.Data.Conversions;
using Compas.Core.Transformations;
using CompasPb;
using CompasPb.Data;
using Google.Protobuf.Collections;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Compas.Core.Geometry
{
    /// <summary>
    /// Polygon mesh data structure using COMPAS-style dictionary and protobuf serialization.
    /// Stores vertices as points and faces as index lists, allowing n-gon support.
    /// </summary>
    [Serializable]
    public class Mesh : Geometry<Mesh>, IProtoConvertible<Mesh, MeshData>
    {
        // MeshData.EdgeKeys and AttributeColumn.Values are AnyData fields, so building a MeshData
        // means converting arbitrary values at the level below Pack/Unpack. CompasPb exposes that
        // on the serializer; its internal Serializer/Deserializer helpers are not public.
        private static readonly ICompasPbSerializer Codec = new CompasPbSerializer();

        /// <summary>
        /// Arbitrary mesh-level metadata.
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Default attributes applied to edges when serialized/deserialized in COMPAS-style data.
        /// </summary>
        public Dictionary<string, object> DefaultEdgeAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Default attributes applied to faces when serialized/deserialized in COMPAS-style data.
        /// </summary>
        public Dictionary<string, object> DefaultFaceAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Default attributes applied to vertices when serialized/deserialized in COMPAS-style data.
        /// </summary>
        public Dictionary<string, object> DefaultVertexAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Vertex list for this mesh.
        /// </summary>
        public List<Point> Vertices { get; set; } = new List<Point>();

        /// <summary>
        /// Face list, where each face stores vertex indices into <see cref="Vertices"/>.
        /// </summary>
        public List<List<int>> Faces { get; set; } = new List<List<int>>();

        /// <summary>
        /// Explicit per-vertex attributes, aligned by index with <see cref="Vertices"/>.
        /// Coordinate attributes are represented by <see cref="Vertices"/> and are not duplicated here.
        /// </summary>
        public List<Dictionary<string, object>> VertexAttributes { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// Explicit per-face attributes, aligned by index with <see cref="Faces"/>.
        /// </summary>
        public List<Dictionary<string, object>> FaceAttributes { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// Explicit attributes for edges that carry data. Entries retain their serialized COMPAS edge key.
        /// </summary>
        public List<MeshEdgeAttributes> EdgeAttributes { get; set; } = new List<MeshEdgeAttributes>();

        /// <summary>
        /// Highest known face index in COMPAS-style payloads.
        /// </summary>
        public int MaxFace { get; private set; }

        /// <summary>
        /// Highest known vertex index in COMPAS-style payloads.
        /// </summary>
        public int MaxVertex { get; private set; }

        /// <summary>
        /// Initializes a mesh with optional geometry and metadata dictionaries.
        /// </summary>
        /// <param name="vertices">Optional vertex collection.</param>
        /// <param name="faces">Optional face collection; each face is a sequence of vertex indices.</param>
        /// <param name="attributes">Optional mesh-level attributes.</param>
        /// <param name="defaultEdgeAttributes">Optional default edge attributes.</param>
        /// <param name="defaultFaceAttributes">Optional default face attributes.</param>
        /// <param name="defaultVertexAttributes">Optional default vertex attributes.</param>
        /// <param name="vertexAttributes">Optional per-vertex attribute dictionaries in vertex order.</param>
        /// <param name="faceAttributes">Optional per-face attribute dictionaries in face order.</param>
        /// <param name="edgeAttributes">Optional edge keys and their explicit attributes.</param>
        /// <param name="guid">Optional COMPAS guid.</param>
        /// <param name="name">Optional COMPAS name.</param>
        public Mesh(
            IEnumerable<Point>? vertices = null,
            IEnumerable<IEnumerable<int>>? faces = null,
            Dictionary<string, object>? attributes = null,
            Dictionary<string, object>? defaultEdgeAttributes = null,
            Dictionary<string, object>? defaultFaceAttributes = null,
            Dictionary<string, object>? defaultVertexAttributes = null,
            IEnumerable<Dictionary<string, object>>? vertexAttributes = null,
            IEnumerable<Dictionary<string, object>>? faceAttributes = null,
            IEnumerable<MeshEdgeAttributes>? edgeAttributes = null,
            string guid = "",
            string name = "") : base()
        {
            Vertices = vertices?.ToList() ?? new List<Point>();
            Faces = faces?.Select(face => face.ToList()).ToList() ?? new List<List<int>>();

            Attributes = attributes ?? new Dictionary<string, object>();
            DefaultEdgeAttributes = defaultEdgeAttributes ?? new Dictionary<string, object>();
            DefaultFaceAttributes = defaultFaceAttributes ?? new Dictionary<string, object>();
            DefaultVertexAttributes = defaultVertexAttributes ?? new Dictionary<string, object>();

            VertexAttributes = NormalizeAttributeRows(vertexAttributes, Vertices.Count, nameof(vertexAttributes));
            FaceAttributes = NormalizeAttributeRows(faceAttributes, Faces.Count, nameof(faceAttributes));
            EdgeAttributes = edgeAttributes?.Select(CloneEdgeAttributes).ToList() ?? new List<MeshEdgeAttributes>();
            Guid = guid ?? string.Empty;
            Name = name ?? string.Empty;

            MaxVertex = Vertices.Count - 1;
            MaxFace = Faces.Count - 1;
        }

        /// <summary>
        /// Serializes this mesh to COMPAS-style dictionary data.
        /// </summary>
        /// <returns>Dictionary payload containing geometry, metadata, and dtype fields.</returns>
        public override Dictionary<string, object> ToData()
        {
            var vertexDict = new Dictionary<string, object>();
            for (int index = 0; index < Vertices.Count; index++)
            {
                var vertexData = new Dictionary<string, object>(GetAttributeRow(VertexAttributes, index));
                vertexData["x"] = Vertices[index].X;
                vertexData["y"] = Vertices[index].Y;
                vertexData["z"] = Vertices[index].Z;
                vertexDict[index.ToString()] = vertexData;
            }

            var faceDataDict = new Dictionary<string, object>();
            for (int index = 0; index < Faces.Count; index++)
            {
                var attributes = GetAttributeRow(FaceAttributes, index);
                if (attributes.Count > 0)
                {
                    faceDataDict[index.ToString()] = attributes;
                }
            }

            var faceDict = new Dictionary<string, object>();
            for (int index = 0; index < Faces.Count; index++)
            {
                faceDict[index.ToString()] = Faces[index];
            }

            var edgeDataDict = new Dictionary<string, object>();
            foreach (var edge in EdgeAttributes)
            {
                edgeDataDict[edge.Key.ToString() ?? string.Empty] = edge.Attributes;
            }

            return new Dictionary<string, object>
            {
                { "attributes", Attributes },
                { "default_edge_attributes", DefaultEdgeAttributes },
                { "default_face_attributes", DefaultFaceAttributes },
                { "default_vertex_attributes", DefaultVertexAttributes },
                { "vertex", vertexDict },
                { "face", faceDict },
                { "facedata", faceDataDict },
                { "edgedata", edgeDataDict },
                { "max_face", MaxFace },
                { "max_vertex", MaxVertex },
                { "dtype", DType }
            };
        }

        /// <summary>
        /// Parses mesh data from a JSON string, JObject, or dictionary payload.
        /// </summary>
        /// <param name="jsonData">Input payload as string, JObject, or dictionary.</param>
        /// <returns>Parsed mesh instance.</returns>
        public static Mesh Parse(object jsonData)
        {
            if (jsonData is string jsonString)
            {
                var parsed = JObject.Parse(jsonString);
                return FromData(parsed.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>());
            }

            if (jsonData is JObject jsonObject)
            {
                return FromData(jsonObject.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>());
            }

            return FromData(jsonData as Dictionary<string, object> ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Creates a mesh from COMPAS-style dictionary data.
        /// Supports wrapped <c>data</c> payloads and both <c>face</c>/<c>faces</c> keys.
        /// </summary>
        /// <param name="jsonDataDict">Dictionary payload to deserialize.</param>
        /// <returns>Deserialized mesh.</returns>
        public new static Mesh FromData(Dictionary<string, object> jsonDataDict)
        {
            var data = JsonObjectConverters.GetOptionalInnerData(jsonDataDict ?? throw new ArgumentNullException(nameof(jsonDataDict)));

            var attributes = JsonObjectConverters.GetObjectDictionary(data, "attributes");
            var defaultEdgeAttributes = JsonObjectConverters.GetObjectDictionary(data, "default_edge_attributes");
            var defaultFaceAttributes = JsonObjectConverters.GetObjectDictionary(data, "default_face_attributes");
            var defaultVertexAttributes = JsonObjectConverters.GetObjectDictionary(data, "default_vertex_attributes");

            var vertices = ParseVertices(data, defaultVertexAttributes);
            var faces = ParseFaces(data);
            var vertexAttributes = ParseVertexAttributes(data);
            var faceAttributes = ParseFaceAttributes(data, faces.Count);
            var edgeAttributes = ParseEdgeAttributes(data);

            int maxFace = data.TryGetValue("max_face", out var maxFaceObj)
                ? Convert.ToInt32(maxFaceObj)
                : (faces.Count - 1);
            int maxVertex = data.TryGetValue("max_vertex", out var maxVertexObj)
                ? Convert.ToInt32(maxVertexObj)
                : (vertices.Count - 1);

            var mesh = new Mesh(
                vertices,
                faces,
                attributes,
                defaultEdgeAttributes,
                defaultFaceAttributes,
                defaultVertexAttributes,
                vertexAttributes,
                faceAttributes,
                edgeAttributes,
                GetString(jsonDataDict, "guid"),
                GetString(jsonDataDict, "name"));

            mesh.MaxFace = maxFace;
            mesh.MaxVertex = maxVertex;

            return mesh;
        }

        /// <summary>
        /// Deserializes a mesh from protobuf payload.
        /// </summary>
        /// <param name="meshData">Protobuf mesh payload.</param>
        /// <returns>Deserialized mesh.</returns>
        public static Mesh FromProto(CompasPb.Data.MeshData meshData)
        {
            if (meshData.Vertices.Count % 3 != 0)
            {
                throw new ArgumentException("Mesh protobuf vertices must contain flat x, y, z triplets.", nameof(meshData));
            }

            var vertices = new List<Point>();
            for (int index = 0; index < meshData.Vertices.Count; index += 3)
            {
                vertices.Add(new Point(
                    (float)meshData.Vertices[index],
                    (float)meshData.Vertices[index + 1],
                    (float)meshData.Vertices[index + 2]));
            }

            var faces = new List<List<int>>();
            int offset = 0;
            foreach (uint faceSize in meshData.FaceSizes)
            {
                int count = checked((int)faceSize);
                if (offset + count > meshData.FaceVertices.Count)
                {
                    throw new ArgumentException("Mesh protobuf face sizes exceed the face vertex array.", nameof(meshData));
                }
                faces.Add(meshData.FaceVertices.Skip(offset).Take(count).Select(i => checked((int)i)).ToList());
                offset += count;
            }

            if (offset != meshData.FaceVertices.Count)
            {
                throw new ArgumentException("Mesh protobuf face sizes do not consume the face vertex array.", nameof(meshData));
            }
            var vertexAttributes = CreateAttributeRows(vertices.Count);
            var faceAttributes = CreateAttributeRows(faces.Count);
            ReadAttributeColumns(meshData.VertexAttributeColumns, vertexAttributes, "vertex");
            ReadAttributeColumns(meshData.FaceAttributeColumns, faceAttributes, "face");

            var edgeAttributes = new List<MeshEdgeAttributes>();
            foreach (var edgeKey in meshData.EdgeKeys)
            {
                var key = Codec.UnpackAnyData(edgeKey);
                if (key == null)
                {
                    throw new ArgumentException("Mesh protobuf edge keys cannot be null.", nameof(meshData));
                }
                edgeAttributes.Add(new MeshEdgeAttributes(key));
            }
            ReadAttributeColumns(
                meshData.EdgeAttributeColumns,
                edgeAttributes.Select(edge => edge.Attributes).ToList(),
                "edge");

            return new Mesh(
                vertices,
                faces,
                ReadAttributeMap(meshData.Attributes),
                ReadAttributeMap(meshData.DefaultEdgeAttributes),
                ReadAttributeMap(meshData.DefaultFaceAttributes),
                ReadAttributeMap(meshData.DefaultVertexAttributes),
                vertexAttributes,
                faceAttributes,
                edgeAttributes,
                meshData.HasGuid ? meshData.Guid : string.Empty,
                meshData.HasName ? meshData.Name : string.Empty);
        }

        /// <summary>
        /// Serializes this mesh to protobuf payload.
        /// </summary>
        /// <returns>Protobuf mesh payload.</returns>
        public CompasPb.Data.MeshData ToProto()
        {
            var meshData = new CompasPb.Data.MeshData();
            foreach (var vertex in Vertices)
            {
                meshData.Vertices.Add(vertex.X);
                meshData.Vertices.Add(vertex.Y);
                meshData.Vertices.Add(vertex.Z);
            }
            foreach (var face in Faces)
            {
                meshData.FaceSizes.Add(checked((uint)face.Count));
                meshData.FaceVertices.AddRange(face.Select(i => checked((uint)i)));
            }

            if (!string.IsNullOrEmpty(Guid)) meshData.Guid = Guid;
            if (!string.IsNullOrEmpty(Name)) meshData.Name = Name;

            FillAttributeMap(meshData.Attributes, Attributes);
            FillAttributeMap(meshData.DefaultEdgeAttributes, DefaultEdgeAttributes);
            FillAttributeMap(meshData.DefaultFaceAttributes, DefaultFaceAttributes);
            FillAttributeMap(meshData.DefaultVertexAttributes, DefaultVertexAttributes);

            var vertexAttributes = NormalizeAttributeRows(VertexAttributes, Vertices.Count, nameof(VertexAttributes));
            var faceAttributes = NormalizeAttributeRows(FaceAttributes, Faces.Count, nameof(FaceAttributes));
            FillAttributeColumns(
                meshData.VertexAttributeColumns,
                vertexAttributes,
                new HashSet<string>(StringComparer.Ordinal) { "x", "y", "z" });
            FillAttributeColumns(meshData.FaceAttributeColumns, faceAttributes);

            foreach (var edge in EdgeAttributes)
            {
                meshData.EdgeKeys.Add(Codec.PackAsAnyData(edge.Key));
            }
            FillAttributeColumns(
                meshData.EdgeAttributeColumns,
                EdgeAttributes.Select(edge => edge.Attributes).ToList());
            return meshData;
        }

        /// <summary>
        /// Returns a transformed copy of this mesh by applying a 4x4 column-major matrix to all vertices.
        /// Faces and metadata are preserved.
        /// </summary>
        /// <param name="matrix">4x4 transformation matrix in column-major order.</param>
        /// <returns>Transformed mesh copy.</returns>
        public override Mesh Transform(float[] matrix)
        {
            if (matrix == null || matrix.Length != 16)
            {
                throw new ArgumentException("Transform matrix must be a 4x4 matrix in column-major order.", nameof(matrix));
            }


            var transformed = new Mesh(
                Vertices.Select(vertex => vertex.Transform(matrix)).ToList(),
                Faces,
                new Dictionary<string, object>(Attributes),
                new Dictionary<string, object>(DefaultEdgeAttributes),
                new Dictionary<string, object>(DefaultFaceAttributes),
                new Dictionary<string, object>(DefaultVertexAttributes),
                VertexAttributes.Select(attributes => new Dictionary<string, object>(attributes)),
                FaceAttributes.Select(attributes => new Dictionary<string, object>(attributes)),
                EdgeAttributes.Select(CloneEdgeAttributes),
                Guid,
                Name)
            {
                MaxFace = MaxFace,
                MaxVertex = MaxVertex
            };

            // A mirroring matrix (det < 0) inverts every derived face normal, because a cross
            // product transforms as a pseudovector. Reverse the winding to compensate.
            if (ObjectTransformations.Determinant3x3(matrix) >= 0)
                return transformed;

            var reversedFaces = transformed.Faces
                 .Select(f => Enumerable.Reverse(f).ToList())
                 .ToList();

            transformed.Faces = reversedFaces;
            return transformed;
        }

        /// <summary>
        /// Parses vertex entries from COMPAS-style dictionary data.
        /// </summary>
        /// <param name="data">Dictionary containing mesh payload fields.</param>
        /// <returns>Ordered vertex list.</returns>
        private static List<Point> ParseVertices(
            Dictionary<string, object> data,
            Dictionary<string, object> defaults)
        {
            var vertexDict = JsonObjectConverters.GetObjectDictionary(data, "vertex");
            var parsed = new List<(int index, Point point)>();

            float defaultX = defaults.TryGetValue("x", out var defaultXObj) ? Convert.ToSingle(defaultXObj) : 0f;
            float defaultY = defaults.TryGetValue("y", out var defaultYObj) ? Convert.ToSingle(defaultYObj) : 0f;
            float defaultZ = defaults.TryGetValue("z", out var defaultZObj) ? Convert.ToSingle(defaultZObj) : 0f;

            foreach (var kvp in vertexDict)
            {
                var item = JsonObjectConverters.ToDictionary(kvp.Value);
                float x = item.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : defaultX;
                float y = item.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : defaultY;
                float z = item.TryGetValue("z", out var zObj) ? Convert.ToSingle(zObj) : defaultZ;

                int index = int.TryParse(kvp.Key, out var parsedIndex) ? parsedIndex : parsed.Count;
                parsed.Add((index, new Point(x, y, z)));
            }

            return parsed
                .OrderBy(entry => entry.index)
                .Select(entry => entry.point)
                .ToList();
        }

        /// <summary>
        /// Parses face entries from COMPAS-style dictionary data.
        /// Supports both <c>face</c> and <c>faces</c> keys.
        /// </summary>
        /// <param name="data">Dictionary containing mesh payload fields.</param>
        /// <returns>Ordered face index lists.</returns>
        private static List<List<int>> ParseFaces(Dictionary<string, object> data)
        {
            string? faceKey = data.ContainsKey("face") ? "face" : (data.ContainsKey("faces") ? "faces" : null);
            if (faceKey == null)
            {
                return new List<List<int>>();
            }

            var faceDict = JsonObjectConverters.GetObjectDictionary(data, faceKey);
            var parsed = new List<(int index, List<int> face)>();

            foreach (var kvp in faceDict)
            {
                int index = int.TryParse(kvp.Key, out var parsedIndex) ? parsedIndex : parsed.Count;
                parsed.Add((index, JsonObjectConverters.ConvertToIntList(kvp.Value)));
            }

            return parsed
                .OrderBy(entry => entry.index)
                .Select(entry => entry.face)
                .ToList();
        }

        private static List<Dictionary<string, object>> ParseVertexAttributes(Dictionary<string, object> data)
        {
            var vertexDict = JsonObjectConverters.GetObjectDictionary(data, "vertex");
            var parsed = new List<(int index, Dictionary<string, object> attributes)>();

            foreach (var kvp in vertexDict)
            {
                var attributes = JsonObjectConverters.ToDictionary(kvp.Value);
                attributes.Remove("x");
                attributes.Remove("y");
                attributes.Remove("z");
                int index = int.TryParse(kvp.Key, out var parsedIndex) ? parsedIndex : parsed.Count;
                parsed.Add((index, attributes));
            }

            return parsed
                .OrderBy(entry => entry.index)
                .Select(entry => entry.attributes)
                .ToList();
        }

        private static List<Dictionary<string, object>> ParseFaceAttributes(
            Dictionary<string, object> data,
            int faceCount)
        {
            string? faceKey = data.ContainsKey("face") ? "face" : (data.ContainsKey("faces") ? "faces" : null);
            if (faceKey == null) return CreateAttributeRows(faceCount);

            var faces = JsonObjectConverters.GetObjectDictionary(data, faceKey);
            var faceData = JsonObjectConverters.GetObjectDictionary(data, "facedata");
            var orderedKeys = faces.Keys
                .Select((key, position) => new
                {
                    Key = key,
                    Index = int.TryParse(key, out var parsedIndex) ? parsedIndex : position,
                })
                .OrderBy(entry => entry.Index)
                .Select(entry => entry.Key)
                .ToList();

            return orderedKeys
                .Select(key => faceData.TryGetValue(key, out var value)
                    ? JsonObjectConverters.ToDictionary(value)
                    : new Dictionary<string, object>())
                .ToList();
        }

        private static List<MeshEdgeAttributes> ParseEdgeAttributes(Dictionary<string, object> data)
        {
            var edgeData = JsonObjectConverters.GetObjectDictionary(data, "edgedata");
            return edgeData
                .Select(entry => new MeshEdgeAttributes(
                    entry.Key,
                    JsonObjectConverters.ToDictionary(entry.Value)))
                .ToList();
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) && value != null
                ? value.ToString() ?? string.Empty
                : string.Empty;
        }

        private static Dictionary<string, object> GetAttributeRow(
            IReadOnlyList<Dictionary<string, object>> rows,
            int index)
        {
            return index < rows.Count ? rows[index] : new Dictionary<string, object>();
        }

        private static List<Dictionary<string, object>> CreateAttributeRows(int count)
        {
            var rows = new List<Dictionary<string, object>>(count);
            for (int index = 0; index < count; index++)
            {
                rows.Add(new Dictionary<string, object>());
            }
            return rows;
        }

        private static List<Dictionary<string, object>> NormalizeAttributeRows(
            IEnumerable<Dictionary<string, object>>? attributes,
            int expectedCount,
            string parameterName)
        {
            var rows = attributes?
                .Select(row => new Dictionary<string, object>(row ?? throw new ArgumentException(
                    "Mesh attribute rows cannot be null.",
                    parameterName)))
                .ToList() ?? new List<Dictionary<string, object>>();

            if (rows.Count > expectedCount)
            {
                throw new ArgumentException(
                    $"Mesh {parameterName} has {rows.Count} rows for {expectedCount} elements.",
                    parameterName);
            }
            while (rows.Count < expectedCount)
            {
                rows.Add(new Dictionary<string, object>());
            }
            return rows;
        }

        private static MeshEdgeAttributes CloneEdgeAttributes(MeshEdgeAttributes edge)
        {
            if (edge == null) throw new ArgumentException("Mesh edge attributes cannot be null.");
            return new MeshEdgeAttributes(edge.Key, new Dictionary<string, object>(edge.Attributes));
        }

        private static void FillAttributeMap(
            IDictionary<string, AnyData> destination,
            IDictionary<string, object> attributes)
        {
            foreach (var attribute in attributes)
            {
                destination.Add(attribute.Key, Codec.PackAsAnyData(attribute.Value));
            }
        }

        private static Dictionary<string, object> ReadAttributeMap(IDictionary<string, AnyData> source)
        {
            var attributes = new Dictionary<string, object>(source.Count);
            foreach (var attribute in source)
            {
                attributes[attribute.Key] = Codec.UnpackAnyData(attribute.Value)!;
            }
            return attributes;
        }

        private static void FillAttributeColumns(
            RepeatedField<AttributeColumn> destination,
            IReadOnlyList<Dictionary<string, object>> rows,
            ISet<string>? excludedNames = null)
        {
            var indicesByName = new Dictionary<string, List<int>>();
            var valuesByName = new Dictionary<string, List<object?>>();

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                foreach (var attribute in rows[rowIndex])
                {
                    if (excludedNames != null && excludedNames.Contains(attribute.Key)) continue;
                    if (!valuesByName.TryGetValue(attribute.Key, out var values))
                    {
                        indicesByName[attribute.Key] = new List<int>();
                        values = new List<object?>();
                        valuesByName[attribute.Key] = values;
                    }
                    indicesByName[attribute.Key].Add(rowIndex);
                    values.Add(attribute.Value);
                }
            }

            foreach (var entry in valuesByName)
            {
                var values = entry.Value;
                var column = new AttributeColumn { Name = entry.Key };
                var indices = indicesByName[entry.Key];
                if (indices.Count != rows.Count)
                {
                    column.Indices.AddRange(indices.Select(index => checked((uint)index)));
                }

                if (values.Count > 0 && values.All(IsFloatingPoint))
                {
                    column.Kind = 0;
                    column.Doubles.AddRange(values.Select(value => Convert.ToDouble(value)));
                }
                else if (values.Count > 0 && values.All(IsIntegral))
                {
                    column.Kind = 1;
                    column.Ints.AddRange(values.Select(value => Convert.ToInt64(value)));
                }
                else if (values.Count > 0 && values.All(value => value is bool))
                {
                    column.Kind = 2;
                    column.Bools.AddRange(values.Select(value => (bool)value!));
                }
                else
                {
                    column.Kind = 3;
                    column.Values.AddRange(values.Select(Codec.PackAsAnyData));
                }
                destination.Add(column);
            }
        }

        private static void ReadAttributeColumns(
            IEnumerable<AttributeColumn> columns,
            IReadOnlyList<Dictionary<string, object>> rows,
            string elementName)
        {
            foreach (var column in columns)
            {
                List<object?> values;
                switch (column.Kind)
                {
                    case 0:
                        values = column.Doubles.Select(value => (object?)value).ToList();
                        break;
                    case 1:
                        values = column.Ints.Select(value => (object?)value).ToList();
                        break;
                    case 2:
                        values = column.Bools.Select(value => (object?)value).ToList();
                        break;
                    default:
                        values = column.Values.Select(value => Codec.UnpackAnyData(value)).ToList();
                        break;
                }

                var indices = column.Indices.Count > 0
                    ? column.Indices.Select(index => checked((int)index)).ToList()
                    : Enumerable.Range(0, values.Count).ToList();

                if (indices.Count != values.Count)
                {
                    throw new ArgumentException(
                        $"Mesh protobuf {elementName} attribute column '{column.Name}' has mismatched indices and values.");
                }
                if (column.Indices.Count == 0 && values.Count != rows.Count)
                {
                    throw new ArgumentException(
                        $"Mesh protobuf dense {elementName} attribute column '{column.Name}' has {values.Count} values for {rows.Count} elements.");
                }

                for (int valueIndex = 0; valueIndex < values.Count; valueIndex++)
                {
                    int rowIndex = indices[valueIndex];
                    if (rowIndex < 0 || rowIndex >= rows.Count)
                    {
                        throw new ArgumentException(
                            $"Mesh protobuf {elementName} attribute column '{column.Name}' references index {rowIndex}, but there are {rows.Count} elements.");
                    }
                    rows[rowIndex][column.Name] = values[valueIndex]!;
                }
            }
        }

        private static bool IsIntegral(object? value)
        {
            return value is sbyte
                || value is byte
                || value is short
                || value is ushort
                || value is int
                || value is uint
                || value is long
                || value is ulong;
        }

        private static bool IsFloatingPoint(object? value)
        {
            return value is float || value is double || value is decimal;
        }
    }

    /// <summary>
    /// A serialized COMPAS edge key and the explicit attributes stored for that edge.
    /// </summary>
    [Serializable]
    public sealed class MeshEdgeAttributes
    {
        public object Key { get; set; }
        public Dictionary<string, object> Attributes { get; set; }

        public MeshEdgeAttributes(
            object key,
            Dictionary<string, object>? attributes = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Attributes = attributes ?? new Dictionary<string, object>();
        }
    }
}
