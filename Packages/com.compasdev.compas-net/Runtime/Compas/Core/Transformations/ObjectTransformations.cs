using Compas.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Compas.Core.Transformations
{
    /// <summary>
    /// Coordinate systems supported for transformations.
    /// </summary>
    public enum SupportedSystem
    {
        RhinoZUp,    // Right-handed, Z axis up, Y axis forward, X axis right
        RhinoYUp,    // Right-handed, Y axis up, Z axis forward, X axis right  
        Unity,       // Left-handed, Y axis up, Z axis forward, X axis right
        Motive       // Right-handed, Y axis up, Z axis forward, X axis right
    }

    /// <summary>
    /// Describes handedness conversion between source and target coordinate systems.
    /// </summary>
    public enum HandednessChange
    {
        LeftToRight,  // Transform from left-handed to right-handed
        RightToLeft,  // Transform from right-handed to left-handed
        NoChange      // Preserve handedness
    }
    /// <summary>
    /// Named transformation variants used to select predefined matrix mappings.
    /// </summary>
    public enum TransformVariant
    {
        FlipZ_PreswappedYZ,     // RhinoYUp → Unity: flip Z (axes already swapped by Rhino export)
        FlipZ,       // Motive → Unity: flip Z axis (right-handed Motive to left-handed)
        SwapYZ,                 // RhinoZUp → Unity, Motive ↔ RhinoZUp: swap Y↔Z axes
        SwapZY,                 // Unity → RhinoZUp: swap Z↔Y axes (inverse perspective)
    }

    /// <summary>
    /// Indicates whether Y and Z axes are swapped by a transform.
    /// </summary>
    public enum AxisSwapping
    {
        NoSwap,      // Axes maintain their orientation (may be flipped/negated)
        YZSwapped    // Y and Z axes are swapped
    }


    /// <summary>
    /// Provides transformation matrices and helpers for converting COMPAS geometry
    /// between supported coordinate systems.
    /// </summary>
    public static class ObjectTransformations
    {
        // Stores direct 4×4 transformation matrices between supported systems in column-major order.
        private static readonly Dictionary<(SupportedSystem, SupportedSystem), float[]> TransformMatrices =
        new Dictionary<(SupportedSystem, SupportedSystem), float[]>()
        {
            // RhinoZUp -> Unity: swap Y↔Z (right-handed to left-handed)
            // RhinoZUp(X,Y,Z) -> Unity(X,Z,Y): X→right stays, Y→forward becomes Z→forward, Z→up becomes Y→up
            {(SupportedSystem.RhinoZUp, SupportedSystem.Unity), new float[]
            {
                1,  0,  0, 0,  // Unity.X = RhinoZUp.X
                0,  0,  1, 0,  // Unity.Y = RhinoZUp.Z
                0,  1,  0, 0,  // Unity.Z = RhinoZUp.Y
                0,  0,  0, 1
            }},

            // RhinoYUp -> Unity: flip Z (right-handed to left-handed, same axis orientation)
            // RhinoYUp(X,Y,Z) -> Unity(X,Y,-Z): axes align, flip Z for handedness change
            {(SupportedSystem.RhinoYUp, SupportedSystem.Unity), new float[]
            {
               1,  0,  0, 0,  // Unity.X = RhinoYUp.X
               0,  1,  0, 0,  // Unity.Y = RhinoYUp.Y
               0,  0, -1, 0,  // Unity.Z = -RhinoYUp.Z (flip for handedness change)
               0,  0,  0, 1
            }},

            // Unity -> RhinoZUp: swap Y↔Z (left-handed to right-handed)
            // Unity(X,Y,Z) -> RhinoZUp(X,Z,Y): inverse of RhinoZUp->Unity
            {(SupportedSystem.Unity, SupportedSystem.RhinoZUp), new float[]
            {
                1,  0,  0, 0,  // RhinoZUp.X = Unity.X
                0,  0,  1, 0,  // RhinoZUp.Y = Unity.Z
                0,  1,  0, 0,  // RhinoZUp.Z = Unity.Y
                0,  0,  0, 1
            }},

            // Motive -> Unity: flip Z (right-handed to left-handed, same axis orientation)
            // Motive(X,Y,Z) -> Unity(X,Y,-Z)
            {(SupportedSystem.Motive, SupportedSystem.Unity), new float[]
            {
                1,  0,  0, 0,  // Unity.X = Motive.X
                0,  1,  0, 0,  // Unity.Y = Motive.Y
                0,  0, -1, 0,  // Unity.Z = -Motive.Z (flip for handedness change)
                0,  0,  0, 1
            }},

            // Motive -> RhinoZUp: swap Y↔Z (right-handed to right-handed)
            // Motive(X,Y,Z) -> RhinoZUp(X,Z,Y): Y→up becomes Z→up, Z→forward becomes Y→forward
            {(SupportedSystem.Motive, SupportedSystem.RhinoZUp), new float[]
            {
              1,  0,  0, 0,  // RhinoZUp.X = Motive.X
              0,  0,  1, 0,  // RhinoZUp.Y = Motive.Z
              0,  1,  0, 0,  // RhinoZUp.Z = Motive.Y
              0,  0,  0, 1
            }},

            // RhinoZUp -> Motive: swap Y↔Z (right-handed to right-handed)
            // RhinoZUp(X,Y,Z) -> Motive(X,Z,Y): inverse of Motive->RhinoZUp
            {(SupportedSystem.RhinoZUp, SupportedSystem.Motive), new float[]
            {
               1,  0,  0, 0,  // Motive.X = RhinoZUp.X
               0,  0,  1, 0,  // Motive.Y = RhinoZUp.Z
               0,  1,  0, 0,  // Motive.Z = RhinoZUp.Y
               0,  0,  0, 1
            }}
        };

        // Maps (HandednessChange, TransformVariant) to (source system, target system).
        private static readonly Dictionary<(HandednessChange, TransformVariant), (SupportedSystem from, SupportedSystem to)> VariantMap =
        new Dictionary<(HandednessChange, TransformVariant), (SupportedSystem from, SupportedSystem to)>()
        {
            { (HandednessChange.RightToLeft, TransformVariant.FlipZ_PreswappedYZ), (SupportedSystem.RhinoYUp, SupportedSystem.Unity) },
            { (HandednessChange.RightToLeft, TransformVariant.FlipZ),              (SupportedSystem.Motive,   SupportedSystem.Unity) },
            { (HandednessChange.RightToLeft, TransformVariant.SwapYZ),             (SupportedSystem.RhinoZUp, SupportedSystem.Unity) },
            { (HandednessChange.LeftToRight, TransformVariant.SwapZY),             (SupportedSystem.Unity,    SupportedSystem.RhinoZUp) },
            { (HandednessChange.NoChange,    TransformVariant.SwapYZ),             (SupportedSystem.Motive,   SupportedSystem.RhinoZUp) },
            { (HandednessChange.NoChange,    TransformVariant.SwapZY),             (SupportedSystem.RhinoZUp, SupportedSystem.Motive) }
        };

        // Reverse lookup: (source, target) systems to TransformVariant
        private static readonly Dictionary<(SupportedSystem, SupportedSystem), TransformVariant> SystemPairToVariant =
            SystemPairToVariantMap();

        private static Dictionary<(SupportedSystem, SupportedSystem), TransformVariant> SystemPairToVariantMap()
        {
            var map = new Dictionary<(SupportedSystem, SupportedSystem), TransformVariant>();
            foreach (var variantEntry in VariantMap)
            {
                var (handednessChange, variant) = variantEntry.Key;
                var (sourceSystem, targetSystem) = variantEntry.Value;
                map[(sourceSystem, targetSystem)] = variant;
            }
            return map;
        }

        /// <summary>
        /// Gets a predefined transformation matrix between two supported systems.
        /// </summary>
        /// <param name="source">Source coordinate system.</param>
        /// <param name="target">Target coordinate system.</param>
        /// <returns>4x4 column-major transformation matrix.</returns>
        public static float[] GetTransform(SupportedSystem source, SupportedSystem target)
        {
            if (TransformMatrices.TryGetValue((source, target), out var matrix))
                return matrix;
            throw new ArgumentException($"No transform defined from {source} to {target}.");
        }

        /// <summary>
        /// Resolves a transformation matrix from a variant and handedness conversion.
        /// </summary>
        /// <param name="variant">Transform variant.</param>
        /// <param name="handednessChange">Handedness conversion mode.</param>
        /// <returns>4x4 column-major transformation matrix.</returns>
        public static float[] GetTransformFromVariant(TransformVariant variant, HandednessChange handednessChange)
        {
            if (VariantMap.TryGetValue((handednessChange, variant), out var systems))
                return GetTransform(systems.from, systems.to);

            throw new ArgumentException($"No transform defined for variant {variant} with handedness change {handednessChange}.");
        }


        // Determines axis swapping from TransformVariant
        /// <summary>
        /// Determines axis-swapping behavior for a transform variant.
        /// </summary>
        /// <param name="variant">Transform variant.</param>
        /// <returns>Axis swapping mode.</returns>
        public static AxisSwapping GetAxisSwapping(TransformVariant variant)
        {
            return variant switch
            {
                TransformVariant.SwapYZ => AxisSwapping.YZSwapped,
                TransformVariant.SwapZY => AxisSwapping.YZSwapped,
                TransformVariant.FlipZ => AxisSwapping.NoSwap,
                TransformVariant.FlipZ_PreswappedYZ => AxisSwapping.NoSwap,
                _ => throw new ArgumentException($"Unknown variant: {variant}")
            };
        }

        // Determines axis swapping from coordinate systems by deriving the TransformVariant.
        /// <summary>
        /// Determines axis-swapping behavior for a source-target transform.
        /// </summary>
        /// <param name="source">Source coordinate system.</param>
        /// <param name="target">Target coordinate system.</param>
        /// <returns>Axis swapping mode.</returns>
        public static AxisSwapping GetAxisSwapping(SupportedSystem source, SupportedSystem target)
        {
            if (SystemPairToVariant.TryGetValue((source, target), out var variant))
            {
                return GetAxisSwapping(variant);
            }
            throw new ArgumentException($"No transform defined from {source} to {target}.");
        }

        // TRANSFORM METHODS - DIRECT MATRIX APPLICATION
        /// <summary>
        /// Transforms a geometry object using an explicit matrix.
        /// </summary>
        /// <typeparam name="T">Geometry type.</typeparam>
        /// <param name="geometry">Geometry instance to transform.</param>
        /// <param name="matrix">4x4 column-major transformation matrix.</param>
        /// <returns>Transformed geometry.</returns>
        public static T Transform<T>(Geometry<T> geometry, float[] matrix) where T : Geometry<T>
        {
            return geometry.Transform(matrix);
        }

        // TRANSFORM METHODS - BETWEEN COORDINATE SYSTEMS
        /// <summary>
        /// Transforms a geometry object from one supported system to another.
        /// </summary>
        /// <typeparam name="T">Geometry type.</typeparam>
        /// <param name="geometry">Geometry instance to transform.</param>
        /// <param name="source">Source coordinate system.</param>
        /// <param name="target">Target coordinate system.</param>
        /// <returns>Transformed geometry.</returns>
        public static T Transform<T>(Geometry<T> geometry, SupportedSystem source, SupportedSystem target) where T : Geometry<T>
        {
            var matrix = GetTransform(source, target);
            return geometry.Transform(matrix);
        }


        // TRANSFORM METHODS - USING VARIANTS

        /// <summary>
        /// Transforms a geometry object using a transform variant + handedness mode.
        /// </summary>
        /// <typeparam name="T">Geometry type.</typeparam>
        /// <param name="geometry">Geometry instance to transform.</param>
        /// <param name="variant">Transform variant.</param>
        /// <param name="handednessChange">Handedness conversion mode.</param>
        /// <returns>Transformed geometry.</returns>
        public static T Transform<T>(Geometry<T> geometry, TransformVariant variant, HandednessChange handednessChange) where T : Geometry<T>
        {
            var matrix = GetTransformFromVariant(variant, handednessChange);
            return geometry.Transform(matrix);
        }

        public static float Determinant3x3(float[] m) =>
              m[0] * (m[5] * m[10] - m[6] * m[9])
            - m[1] * (m[4] * m[10] - m[6] * m[8])
            + m[2] * (m[4] * m[9] - m[5] * m[8]);

        public static Matrix4x4 ColumnMajorToRowMajor(float[] matrix)
            => new Matrix4x4(
               matrix[0], matrix[4], matrix[8], matrix[12],   // Row 1 (from columns)
               matrix[1], matrix[5], matrix[9], matrix[13],   // Row 2
               matrix[2], matrix[6], matrix[10], matrix[14],  // Row 3
               matrix[3], matrix[7], matrix[11], matrix[15]   // Row 4
           );

    }
}