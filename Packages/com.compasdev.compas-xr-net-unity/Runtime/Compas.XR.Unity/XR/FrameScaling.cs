using UnityEngine;

namespace Compas.XR.Unity.XR
{
    /// <summary>
    /// The downscaling rule shared by every <see cref="ICameraFrameSource"/>, so that a frame means
    /// the same thing to the decoder no matter which camera produced it.
    /// </summary>
    internal static class FrameScaling
    {
        /// <summary>
        /// The factor to divide both edges by so the longest one fits <paramref name="maxEdge"/>.
        /// <para>
        /// Only powers of two are returned. Both sources subsample by picking one source pixel per
        /// output pixel, and an integer factor keeps those picks on regular boundaries - an
        /// arbitrary ratio beats irregularly against the fine module grid a QR code is made of and
        /// can erase the very edges the decoder is looking for.
        /// </para>
        /// </summary>
        public static int GetDivisor(int width, int height, int maxEdge)
        {
            if (maxEdge <= 0) return 1;

            int divisor = 1;
            while (Mathf.Max(width, height) / divisor > maxEdge && divisor < MaxDivisor)
            {
                divisor *= 2;
            }
            return divisor;
        }

        /// <summary>
        /// Ceiling on the divisor. Past this the image is too coarse to hold a readable code, so
        /// shrinking further would only trade accuracy for speed the scanner does not need.
        /// </summary>
        private const int MaxDivisor = 8;
    }
}
