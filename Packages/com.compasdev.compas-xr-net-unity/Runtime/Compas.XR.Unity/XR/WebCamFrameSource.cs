using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Compas.XR.Unity.XR
{
    /// <summary>
    /// Frames from an ordinary webcam, for running outside an AR session - in the Editor, on a
    /// desktop build, or on a device where AR is unavailable.
    /// <para>
    /// Unlike the AR source this one owns the camera: it opens the device on
    /// <see cref="StartCapture"/> and must be disposed to give it back.
    /// </para>
    /// </summary>
    public sealed class WebCamFrameSource : ICameraFrameSource
    {
        private WebCamTexture webCamTexture;
        private Color32[] pixels;
        private byte[] buffer = Array.Empty<byte>();

        /// <summary>
        /// True when the machine has a camera attached. Whether the user will grant access is only
        /// answered once capture is attempted, so a permission refusal surfaces as frames that
        /// never arrive rather than as unavailability.
        /// </summary>
        public bool IsAvailable => WebCamTexture.devices.Length > 0;

        /// <summary>
        /// The live camera texture. Nothing else draws this feed, so a scanning UI has to show it
        /// for the user to have any idea what they are pointing at.
        /// </summary>
        public Texture PreviewTexture => webCamTexture;

        public void StartCapture()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Without this the texture opens and then simply never updates, with no error.
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
#endif

            if (webCamTexture == null)
            {
                webCamTexture = new WebCamTexture();
            }

            if (!webCamTexture.isPlaying)
            {
                webCamTexture.Play();
            }
        }

        public void StopCapture()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
        }

        public void Dispose()
        {
            if (webCamTexture == null) return;

            webCamTexture.Stop();
            UnityEngine.Object.Destroy(webCamTexture);
            webCamTexture = null;
        }

        public bool TryAcquireFrame(int maxEdge, out LuminanceFrame frame)
        {
            frame = default;

            // Before the first frame lands the texture reports a placeholder size, and
            // didUpdateThisFrame stays false until there is something real to read.
            if (webCamTexture == null || !webCamTexture.didUpdateThisFrame) return false;

            int sourceWidth = webCamTexture.width;
            int sourceHeight = webCamTexture.height;
            if (sourceWidth <= 0 || sourceHeight <= 0) return false;

            if (pixels == null || pixels.Length != sourceWidth * sourceHeight)
            {
                pixels = new Color32[sourceWidth * sourceHeight];
            }
            webCamTexture.GetPixels32(pixels);

            int divisor = FrameScaling.GetDivisor(sourceWidth, sourceHeight, maxEdge);
            int width = Mathf.Max(1, sourceWidth / divisor);
            int height = Mathf.Max(1, sourceHeight / divisor);

            // Kept exactly frame-sized, not merely large enough: the decoder reads the buffer's
            // length as the extent of the image.
            if (buffer.Length != width * height)
            {
                buffer = new byte[width * height];
            }

            ConvertToLuminance(sourceWidth, sourceHeight, divisor, width, height);

            frame = new LuminanceFrame(buffer, width, height, LuminancePixelFormat.Gray8);
            return true;
        }

        /// <summary>
        /// Collapses the colour pixels to 8-bit luminance, subsampling and flipping in the same
        /// pass so the frame is walked exactly once.
        /// <para>
        /// Converting here rather than handing the decoder colour bytes is not extra work: it would
        /// do the same weighting internally, and doing it during the copy that has to happen anyway
        /// means a quarter as many bytes make the trip.
        /// </para>
        /// </summary>
        private void ConvertToLuminance(int sourceWidth, int sourceHeight, int divisor, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                // Unity hands pixels back bottom-up; reading the rows in reverse emits the top-down
                // order the decoder assumes. It matters because a QR code survives rotation but not
                // reflection, and an upside-down read of a flipped buffer is a reflection.
                int sourceRow = (sourceHeight - 1 - y * divisor) * sourceWidth;
                int targetRow = y * width;

                for (int x = 0; x < width; x++)
                {
                    Color32 pixel = pixels[sourceRow + x * divisor];

                    // Rec. 601 luma weights in fixed point: (0.299, 0.587, 0.114) scaled by 256.
                    buffer[targetRow + x] = (byte)((pixel.r * 77 + pixel.g * 150 + pixel.b * 29) >> 8);
                }
            }
        }
    }
}
