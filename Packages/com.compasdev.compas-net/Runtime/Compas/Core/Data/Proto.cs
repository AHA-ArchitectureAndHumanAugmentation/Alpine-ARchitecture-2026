namespace Compas.Core.Data
{
    /// <summary>
    /// Marks a Compas type that has a protobuf message representation.
    /// </summary>
    /// <remarks>
    /// Implementing this interface documents the conversion pair and gives the compiler something
    /// to check, but it does not by itself make a type serializable. Every implementer must also be
    /// registered in <see cref="CompasProtoConversions"/>; a test asserts the two stay in step.
    /// </remarks>
    /// <typeparam name="T">The Compas type.</typeparam>
    /// <typeparam name="TProto">The generated protobuf message it converts to.</typeparam>
    public interface IProtoConvertible<T, TProto>
    {
        /// <summary>
        /// Converts this instance to its protobuf message representation.
        /// Paired with the static FromProto method for bidirectional serialization.
        /// </summary>
        TProto ToProto();
    }
}
