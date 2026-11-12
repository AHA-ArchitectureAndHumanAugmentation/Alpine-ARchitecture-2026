using UnityEngine;

namespace Compas.GeoViewerXR
{
    [CreateAssetMenu(menuName = "Compas/Material Palette", fileName = "MaterialPalette")]
    public class MaterialPalette : ScriptableObject
    {
        [SerializeField] private Material[] materials;

        public bool IsEmpty => materials == null || materials.Length == 0;

        public Material GetRandom() =>
            IsEmpty
            ? null
            : materials[Random.Range(0, materials.Length)];

        /// <summary>Same key always gets the same material, so geometry keeps its colour
        /// across updates instead of flickering on every incoming message.</summary>
        public Material GetFor(string key) =>
            IsEmpty
            ? null
            : materials[(int)((uint)key.GetHashCode() % materials.Length)];
    }
}