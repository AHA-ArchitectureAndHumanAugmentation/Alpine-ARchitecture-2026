using System;
using UnityEngine;

namespace Compas.XR.Unity.XR
{
    /// <summary>
    /// A source of camera images for scanning. Each implementation wraps exactly one platform
    /// camera API, which is what lets the scanner itself stay free of platform branching.
    /// <para>
    /// Frames are pulled rather than pushed: the scanner asks for an image at the moment it is
    /// ready to decode one, so a source never converts a frame that would be thrown away.
    /// </para>
    /// </summary>
    public interface ICameraFrameSource : IDisposable
    {
        /// <summary>
        /// Whether this source can actually deliver frames here and now - the hardware exists, the
        /// scene is wired for it, and the platform supports reading the pixels back. Checked before
        /// use so an unavailable source can be swapped for a working one rather than failing.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// A texture showing the live feed, or <c>null</c> when the platform already draws it and a
        /// preview would only cover it up. Valid once <see cref="StartCapture"/> has been called.
        /// </summary>
        Texture PreviewTexture { get; }

        /// <summary>Begins delivering frames. Safe to call when already started.</summary>
        void StartCapture();

        /// <summary>Stops delivering frames, releasing the camera if this source owns it.</summary>
        void StopCapture();

        /// <summary>
        /// Fetches the newest frame that has not been seen yet, downscaled towards a longest edge of
        /// <paramref name="maxEdge"/> pixels.
        /// </summary>
        /// <param name="maxEdge">Target for the longest edge; see <see cref="FrameScaling"/> for the
        /// cap that can leave a very large frame above it.</param>
        /// <param name="frame">The frame, valid only until the next call on this source.</param>
        /// <returns><c>false</c> if no new frame has arrived since the last call.</returns>
        bool TryAcquireFrame(int maxEdge, out LuminanceFrame frame);
    }
}
