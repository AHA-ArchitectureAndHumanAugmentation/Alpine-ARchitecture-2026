using CompasPb.Data;

namespace Compas.Xr
{
    /// <summary>
    /// Entry point that makes the COMPAS XR message conversions available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// CompasPb discovers registrars by sweeping the assemblies the runtime has loaded, and .NET
    /// loads a referenced assembly lazily — on first use of one of its types, not because a
    /// project referenced it. A host that receives a message off the wire before it has
    /// constructed any XR type of its own therefore has this assembly unloaded at that moment,
    /// its registrar has never run, and <c>Unpack</c> fails with
    /// <c>No deserializer is registered for protobuf type 'compas_xr.v1.…'</c>.
    /// </para>
    /// <para>
    /// That is the normal shape of an MQTT subscriber, so calling <see cref="Initialize"/> once
    /// during start-up is the reliable fix. In Unity, call it from a
    /// <c>[RuntimeInitializeOnLoadMethod]</c> in the package that owns the XR scene code, since
    /// this assembly deliberately carries no UnityEngine reference.
    /// </para>
    /// <para>
    /// Calling it more than once is harmless: CompasPb keeps registration idempotent.
    /// </para>
    /// </remarks>
    public static class CompasXr
    {
        /// <summary>
        /// Registers the COMPAS XR message conversions. Call once before the first
        /// <c>Unpack</c>.
        /// </summary>
        public static void Initialize()
        {
            // Referencing the type is what loads this assembly; RegisterAssembly then runs the
            // registrar immediately rather than waiting for CompasPb's next sweep.
            Registry.RegisterAssembly(typeof(CompasXrConversions).Assembly);
        }
    }
}
