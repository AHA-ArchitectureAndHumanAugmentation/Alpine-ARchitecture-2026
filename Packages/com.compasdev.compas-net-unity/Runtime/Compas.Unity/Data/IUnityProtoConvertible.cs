using Compas.Core.Data;

namespace Compas.Unity.Data
{
    /// <summary>
    /// Marks a Unity geometry class as protobuf-convertible, so that a payload carrying its
    /// proto message comes back as the Unity subclass rather than the base COMPAS type.
    ///
    /// Implemented by: UnityPoint, UnityBox, UnitySphere, UnityCylinder, UnityMesh.
    ///
    /// This is a label, not a mechanism. Implementing it does not register anything - the
    /// registration is the explicit <c>Registry.Register</c> call in
    /// <see cref="UnityProtoConversions"/>. The interface exists so a test can assert the two
    /// stay in step.
    ///
    /// To add a Unity type:
    /// 1. Implement IUnityProtoConvertible{UnityCustom, CustomData} on the class.
    /// 2. Give it <c>public new CustomData ToProto()</c> and
    ///    <c>public static new UnityCustom FromProto(CustomData)</c>.
    /// 3. Add a Register line to <see cref="UnityProtoConversions.Register"/> and list the
    ///    type in <see cref="UnityProtoConversions.ConvertibleTypes"/>.
    /// </summary>
    public interface IUnityProtoConvertible<T, TProto> : IProtoConvertible<T, TProto>
    {
    }
}
