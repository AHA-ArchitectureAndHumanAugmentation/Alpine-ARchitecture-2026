using System.Collections.Generic;
using System.Linq;
using Pb = CompasXr.V1;

namespace Compas.Xr.Messages
{
    /// <summary>Approval decision for a trajectory.</summary>
    public enum ApprovalStatus
    {
        NotApproved = 0,
        Approved = 1,
        ConsensusApproval = 2,
        Cancellation = 3,
    }

    /// <summary>
    /// One waypoint of a robot trajectory.
    /// </summary>
    /// <remarks>
    /// <see cref="JointNames"/> and <see cref="JointValues"/> are parallel: index <c>i</c> of one
    /// corresponds to index <c>i</c> of the other.
    /// </remarks>
    public class JointTrajectoryPoint
    {
        public JointTrajectoryPoint(
            IReadOnlyList<string> jointNames,
            IReadOnlyList<double> jointValues,
            double? timeFromStart = null)
        {
            JointNames = jointNames ?? new List<string>();
            JointValues = jointValues ?? new List<double>();
            TimeFromStart = timeFromStart;
        }

        public IReadOnlyList<string> JointNames { get; }

        public IReadOnlyList<double> JointValues { get; }

        /// <summary>Offset from the start of the trajectory in seconds, when the planner supplies it.</summary>
        public double? TimeFromStart { get; }

        public Pb.JointTrajectoryPoint ToProto()
        {
            var proto = new Pb.JointTrajectoryPoint();
            proto.JointNames.AddRange(JointNames);
            proto.JointValues.AddRange(JointValues);
            if (TimeFromStart.HasValue) proto.TimeFromStart = TimeFromStart.Value;
            return proto;
        }

        public static JointTrajectoryPoint FromProto(Pb.JointTrajectoryPoint proto)
        {
            return new JointTrajectoryPoint(
                proto.JointNames.ToList(),
                proto.JointValues.ToList(),
                proto.HasTimeFromStart ? proto.TimeFromStart : (double?)null);
        }
    }

    /// <summary>
    /// An ordered sequence of waypoints.
    /// </summary>
    /// <remarks>
    /// Replaces the untyped "dict of joint names and joint values" the messages used to carry as
    /// JSON, so this side and <c>compas_xr</c> agree on the shape without either depending on
    /// compas_fab.
    /// </remarks>
    public class JointTrajectory
    {
        public JointTrajectory(IReadOnlyList<JointTrajectoryPoint>? points = null)
        {
            Points = points ?? new List<JointTrajectoryPoint>();
        }

        public IReadOnlyList<JointTrajectoryPoint> Points { get; }

        public Pb.JointTrajectory ToProto()
        {
            var proto = new Pb.JointTrajectory();
            foreach (var point in Points) proto.Points.Add(point.ToProto());
            return proto;
        }

        public static JointTrajectory FromProto(Pb.JointTrajectory? proto)
        {
            if (proto == null) return new JointTrajectory();
            return new JointTrajectory(proto.Points.Select(JointTrajectoryPoint.FromProto).ToList());
        }
    }
}
