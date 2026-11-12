using System;
using System.Collections.Generic;
using Compas.Xr.Messages;
using CompasPb.Data;
using Pb = CompasXr.V1;

[assembly: CompasPbRegistrations(typeof(Compas.Xr.CompasXrConversions))]

namespace Compas.Xr
{
    /// <summary>
    /// Registers the COMPAS XR message types with the CompasPb <see cref="Registry"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// COMPAS XR owns <c>compas_xr.proto</c> and the message model it mirrors; this assembly is
    /// the C# half of that domain. CompasPb, the runtime, knows nothing about either — the two
    /// meet here, at the registry.
    /// </para>
    /// <para>
    /// This is a second registrar in the same Unity assembly as
    /// <c>Compas.Core.Data.CompasProtoConversions</c>, which is why
    /// <see cref="CompasPbRegistrationsAttribute"/> is declared <c>AllowMultiple</c>. The core
    /// registrar covers the COMPAS types; this one covers only the XR messages.
    /// </para>
    /// <para>
    /// Registration is written out explicitly rather than reflected over, so IL2CPP keeps the
    /// generic instantiations and no type scanning happens at runtime.
    /// </para>
    /// </remarks>
    public static class CompasXrConversions
    {
        /// <summary>The message types this assembly maps to protobuf messages.</summary>
        public static readonly IReadOnlyCollection<Type> ConvertibleTypes = new[]
        {
            typeof(GetTrajectoryRequest),
            typeof(GetTrajectoryResult),
            typeof(ApproveTrajectory),
            typeof(ApprovalCounterRequest),
            typeof(ApprovalCounterResult),
            typeof(SendTrajectory),
        };

        /// <summary>
        /// Registers the XR message conversions. Invoked by CompasPb through the assembly
        /// attribute; CompasPb guarantees it runs at most once.
        /// </summary>
        public static void Register()
        {
            Registry.Register<GetTrajectoryRequest, Pb.GetTrajectoryRequest>(
                value => value.ToProto(), GetTrajectoryRequest.FromProto);
            Registry.Register<GetTrajectoryResult, Pb.GetTrajectoryResult>(
                value => value.ToProto(), GetTrajectoryResult.FromProto);
            Registry.Register<ApproveTrajectory, Pb.ApproveTrajectory>(
                value => value.ToProto(), ApproveTrajectory.FromProto);
            Registry.Register<ApprovalCounterRequest, Pb.ApprovalCounterRequest>(
                value => value.ToProto(), ApprovalCounterRequest.FromProto);
            Registry.Register<ApprovalCounterResult, Pb.ApprovalCounterResult>(
                value => value.ToProto(), ApprovalCounterResult.FromProto);
            Registry.Register<SendTrajectory, Pb.SendTrajectory>(
                value => value.ToProto(), SendTrajectory.FromProto);
        }
    }
}
