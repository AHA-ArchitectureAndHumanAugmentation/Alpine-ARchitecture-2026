using Compas.Core.Geometry;
using Pb = CompasXr.V1;

namespace Compas.Xr.Messages
{
    /// <summary>
    /// Shared conventions for the COMPAS XR message set.
    /// </summary>
    internal static class MessageConventions
    {
        /// <summary>
        /// The trajectory id derived from an element id. Mirrors the Python side, which sets
        /// <c>"trajectory_id_" + str(element_id)</c> in every message constructor.
        /// </summary>
        internal static string TrajectoryIdFor(string elementId)
        {
            return "trajectory_id_" + elementId;
        }
    }

    /// <summary>Published by a user to request a trajectory for a building element.</summary>
    public class GetTrajectoryRequest
    {
        public GetTrajectoryRequest(string elementId, string robotName, Header header)
        {
            ElementId = elementId;
            RobotName = robotName;
            Header = header;
            TrajectoryId = MessageConventions.TrajectoryIdFor(elementId);
        }

        public Header Header { get; }
        public string ElementId { get; }
        public string RobotName { get; }
        public string TrajectoryId { get; }

        public Pb.GetTrajectoryRequest ToProto()
        {
            return new Pb.GetTrajectoryRequest
            {
                Header = Header.ToProto(),
                ElementId = ElementId ?? string.Empty,
                RobotName = RobotName ?? string.Empty,
                TrajectoryId = TrajectoryId ?? string.Empty,
            };
        }

        public static GetTrajectoryRequest FromProto(Pb.GetTrajectoryRequest proto)
        {
            return new GetTrajectoryRequest(proto.ElementId, proto.RobotName, Header.FromProto(proto.Header));
        }
    }

    /// <summary>Published by the CAD to all active devices with the computed trajectory.</summary>
    public class GetTrajectoryResult
    {
        public GetTrajectoryResult(
            string elementId,
            string robotName,
            Frame? robotBaseFrame,
            JointTrajectory trajectory,
            Header header,
            bool pickAndPlace = false,
            int? pickIndex = null,
            string? endEffectorLinkName = null)
        {
            ElementId = elementId;
            RobotName = robotName;
            RobotBaseFrame = robotBaseFrame;
            Trajectory = trajectory ?? new JointTrajectory();
            Header = header;
            PickAndPlace = pickAndPlace;
            PickIndex = pickIndex;
            EndEffectorLinkName = endEffectorLinkName;
            TrajectoryId = MessageConventions.TrajectoryIdFor(elementId);
        }

        public Header Header { get; }
        public string ElementId { get; }
        public string RobotName { get; }
        public Frame? RobotBaseFrame { get; }
        public string TrajectoryId { get; }
        public JointTrajectory Trajectory { get; }
        public bool PickAndPlace { get; }
        public int? PickIndex { get; }
        public string? EndEffectorLinkName { get; }

        public Pb.GetTrajectoryResult ToProto()
        {
            var proto = new Pb.GetTrajectoryResult
            {
                Header = Header.ToProto(),
                ElementId = ElementId ?? string.Empty,
                RobotName = RobotName ?? string.Empty,
                TrajectoryId = TrajectoryId ?? string.Empty,
                Trajectory = Trajectory.ToProto(),
                PickAndPlace = PickAndPlace,
            };
            if (RobotBaseFrame != null) proto.RobotBaseFrame = RobotBaseFrame.ToProto();
            if (PickIndex.HasValue) proto.PickIndex = PickIndex.Value;
            if (EndEffectorLinkName != null) proto.EndEffectorLinkName = EndEffectorLinkName;
            return proto;
        }

        public static GetTrajectoryResult FromProto(Pb.GetTrajectoryResult proto)
        {
            return new GetTrajectoryResult(
                proto.ElementId,
                proto.RobotName,
                proto.RobotBaseFrame != null ? Frame.FromProto(proto.RobotBaseFrame) : null,
                JointTrajectory.FromProto(proto.Trajectory),
                Header.FromProto(proto.Header),
                proto.PickAndPlace,
                proto.HasPickIndex ? proto.PickIndex : (int?)null,
                proto.HasEndEffectorLinkName ? proto.EndEffectorLinkName : null);
        }
    }

    /// <summary>Published by each user to signal approval or rejection of a trajectory.</summary>
    public class ApproveTrajectory
    {
        public ApproveTrajectory(
            string elementId,
            string robotName,
            JointTrajectory trajectory,
            ApprovalStatus approvalStatus,
            Header header)
        {
            ElementId = elementId;
            RobotName = robotName;
            Trajectory = trajectory ?? new JointTrajectory();
            ApprovalStatus = approvalStatus;
            Header = header;
            TrajectoryId = MessageConventions.TrajectoryIdFor(elementId);
        }

        public Header Header { get; }
        public string ElementId { get; }
        public string RobotName { get; }
        public string TrajectoryId { get; }
        public JointTrajectory Trajectory { get; }
        public ApprovalStatus ApprovalStatus { get; }

        public Pb.ApproveTrajectory ToProto()
        {
            return new Pb.ApproveTrajectory
            {
                Header = Header.ToProto(),
                ElementId = ElementId ?? string.Empty,
                RobotName = RobotName ?? string.Empty,
                TrajectoryId = TrajectoryId ?? string.Empty,
                Trajectory = Trajectory.ToProto(),
                ApprovalStatus = (Pb.ApprovalStatus)ApprovalStatus,
            };
        }

        public static ApproveTrajectory FromProto(Pb.ApproveTrajectory proto)
        {
            return new ApproveTrajectory(
                proto.ElementId,
                proto.RobotName,
                JointTrajectory.FromProto(proto.Trajectory),
                (ApprovalStatus)proto.ApprovalStatus,
                Header.FromProto(proto.Header));
        }
    }

    /// <summary>Published by one user to count the active devices.</summary>
    public class ApprovalCounterRequest
    {
        public ApprovalCounterRequest(string elementId, Header header)
        {
            ElementId = elementId;
            Header = header;
            TrajectoryId = MessageConventions.TrajectoryIdFor(elementId);
        }

        public Header Header { get; }
        public string ElementId { get; }
        public string TrajectoryId { get; }

        public Pb.ApprovalCounterRequest ToProto()
        {
            return new Pb.ApprovalCounterRequest
            {
                Header = Header.ToProto(),
                ElementId = ElementId ?? string.Empty,
                TrajectoryId = TrajectoryId ?? string.Empty,
            };
        }

        public static ApprovalCounterRequest FromProto(Pb.ApprovalCounterRequest proto)
        {
            return new ApprovalCounterRequest(proto.ElementId, Header.FromProto(proto.Header));
        }
    }

    /// <summary>Published by each active device in response to an <see cref="ApprovalCounterRequest"/>.</summary>
    public class ApprovalCounterResult
    {
        public ApprovalCounterResult(string elementId, Header header)
        {
            ElementId = elementId;
            Header = header;
            TrajectoryId = MessageConventions.TrajectoryIdFor(elementId);
        }

        public Header Header { get; }
        public string ElementId { get; }
        public string TrajectoryId { get; }

        public Pb.ApprovalCounterResult ToProto()
        {
            return new Pb.ApprovalCounterResult
            {
                Header = Header.ToProto(),
                ElementId = ElementId ?? string.Empty,
                TrajectoryId = TrajectoryId ?? string.Empty,
            };
        }

        public static ApprovalCounterResult FromProto(Pb.ApprovalCounterResult proto)
        {
            return new ApprovalCounterResult(proto.ElementId, Header.FromProto(proto.Header));
        }
    }

    /// <summary>Published by a user to release an approved trajectory for execution.</summary>
    public class SendTrajectory
    {
        public SendTrajectory(string elementId, string robotName, JointTrajectory trajectory, Header header)
        {
            ElementId = elementId;
            RobotName = robotName;
            Trajectory = trajectory ?? new JointTrajectory();
            Header = header;
            TrajectoryId = MessageConventions.TrajectoryIdFor(elementId);
        }

        public Header Header { get; }
        public string ElementId { get; }
        public string RobotName { get; }
        public string TrajectoryId { get; }
        public JointTrajectory Trajectory { get; }

        public Pb.SendTrajectory ToProto()
        {
            return new Pb.SendTrajectory
            {
                Header = Header.ToProto(),
                ElementId = ElementId ?? string.Empty,
                RobotName = RobotName ?? string.Empty,
                TrajectoryId = TrajectoryId ?? string.Empty,
                Trajectory = Trajectory.ToProto(),
            };
        }

        public static SendTrajectory FromProto(Pb.SendTrajectory proto)
        {
            return new SendTrajectory(
                proto.ElementId,
                proto.RobotName,
                JointTrajectory.FromProto(proto.Trajectory),
                Header.FromProto(proto.Header));
        }
    }
}
