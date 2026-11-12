using Compas.Unity.Core.Mqtt;
using System;
using UnityEngine;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Loads a saved geometry file from a Resources folder, as if it had arrived from Rhino.
    /// </summary>
    public class GeometryFileLoader : MonoBehaviour
    {
        [Tooltip("Sets each loaded file down on the surface in front of the camera.")]
        [SerializeField] private SurfacePlacer _surfacePlacer;

        /// <summary>Loads a file by its path inside Resources, e.g. Samples/brick_wall.</summary>
        public void Load(string resourcesPath)
        {
            // Resources.Load wants the path without extension; .bytes keeps the raw binary untouched.
            string path = System.IO.Path.ChangeExtension(resourcesPath, null);
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning($"{nameof(GeometryFileLoader)}: no TextAsset at Resources/{path}.", this);
                return;
            }

            if (asset.bytes.Length == 0)
            {
                Debug.LogWarning($"{nameof(GeometryFileLoader)}: Resources/{path} is empty.", this);
                return;
            }

            GeoViewerHub hub = GeoViewerHub.Instance;

            try
            {
                (object decoded, _) = MqttMessageSerializer.Decode(SerializationProtocol.ProtoBuf, asset.bytes);
                hub.Receive(path, decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(GeometryFileLoader)}: failed to load '{path}': {ex.Message}", this);
                return;
            }

            if (_surfacePlacer != null && hub.TryGetGeometryRoot(path, out Transform root))
            {
                _surfacePlacer.PlaceInFrontOfCamera(root);
            }
        }
    }
}
