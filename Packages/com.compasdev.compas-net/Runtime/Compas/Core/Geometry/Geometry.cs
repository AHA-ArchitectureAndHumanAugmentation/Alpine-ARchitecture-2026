namespace Compas.Core.Geometry
{

    public abstract class Geometry<T> : Data.Data where T : Geometry<T>
    {
        /// <summary>
        /// Geometry: A class for defining the geometry interface for objects.
        /// It is used as a base class for all geometry classes in the Compas.Core.Geometry namespace.
        /// It is based off of translations of the COMPAS Geometry class.
        /// </summary>

        /// <summary>
        /// Initializes a new geometry instance.
        /// </summary>
        protected Geometry() : base()
        {
        }

        /// <summary>
        /// Creates a shallow copy of the geometry object.
        /// This method uses MemberwiseClone() to copy value-type fields directly,
        /// while reference-type fields are copied by reference.
        /// </summary>
        /// <returns>A shallow-copied geometry instance.</returns>
        public T ShallowCopy()
        {
            T clone = (T)this.MemberwiseClone();
            return clone;
        }

        /// <summary>
        /// Applies a 4x4 column-major transformation matrix to this geometry.
        /// </summary>
        /// <param name="matrix">Transformation matrix in column-major order.</param>
        /// <returns>A transformed geometry instance.</returns>
        public abstract T Transform(float[] matrix);
    }
}