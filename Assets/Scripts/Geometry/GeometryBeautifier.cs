using UnityEngine;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Gives each received object a material from the palette.
    /// </summary>
    public class GeometryBeautifier : MonoBehaviour
    {
        [SerializeField] private MaterialPalette materials;

        private void OnEnable()
        {
            GeoViewerHub.Instance.GeometryEvents.OnObjectDrawn.AddListener(OnObjectDrawn);
        }

        private void OnDisable()
        {
            GeoViewerHub.Instance.GeometryEvents.OnObjectDrawn.RemoveListener(OnObjectDrawn);
        }

        private void OnObjectDrawn(string path, GameObject go, bool isNew)
        {
            if (!isNew || materials == null || materials.IsEmpty)
            {
                return;
            }

            Material material = materials.GetFor(path);
            if (material == null)
            {
                return;
            }

            // sharedMaterial, not material! otherwise it clones the mat asset per object
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}