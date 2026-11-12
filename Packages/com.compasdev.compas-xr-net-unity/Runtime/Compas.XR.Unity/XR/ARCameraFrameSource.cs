using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Compas.XR.Unity.XR
{
    /// <summary>
    /// Frames from the AR session's device camera, read through AR Foundation's CPU image API.
    /// <para>
    /// This is the source to use on a headset or phone running AR: the session already owns the
    /// camera, so opening a second capture would either fail or fight it for the hardware.
    /// </para>
    /// </summary>
    public sealed class ARCameraFrameSource : ICameraFrameSource
    {
        private readonly ARCameraManager cameraManager;

        // Reused across frames; see LuminanceFrame on why the buffer is not handed away. It is kept
        // exactly frame-sized rather than merely large enough, because the decoder reads its length
        // as the extent of the image. Resolution rarely changes, so the reallocation rarely happens.
        private byte[] buffer = Array.Empty<byte>();

        // The provider keeps handing back its latest image until a new one lands, so the timestamp
        // is what distinguishes a fresh frame from the one already decoded.
        private double lastTimestamp = double.NaN;

        public ARCameraFrameSource(ARCameraManager cameraManager)
        {
            this.cameraManager = cameraManager;
        }

        /// <summary>
        /// True only when a running session is present and its provider can hand back pixels.
        /// <see cref="XRCameraSubsystem.TryAcquireLatestCpuImage"/> is contractually allowed to
        /// throw where <c>supportsCameraImage</c> is false, so the descriptor is checked rather
        /// than the call being attempted and caught.
        /// </summary>
        public bool IsAvailable =>
            cameraManager != null &&
            cameraManager.subsystem != null &&
            cameraManager.subsystem.running &&
            cameraManager.descriptor != null &&
            cameraManager.descriptor.supportsCameraImage;

        /// <summary>
        /// None: ARCameraBackground already draws the feed behind the whole scene, and a preview
        /// quad would only hide the thing the user is aiming.
        /// </summary>
        public Texture PreviewTexture => null;

        // The AR session starts and stops the camera as part of its own lifecycle. Acquiring an
        // image is a read of whatever it most recently produced, so there is nothing to open here.
        public void StartCapture() { }

        public void StopCapture() { }

        public void Dispose() { }

        public bool TryAcquireFrame(int maxEdge, out LuminanceFrame frame)
        {
            frame = default;

            if (cameraManager == null || !cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return false;
            }

            try
            {
                if (image.timestamp == lastTimestamp) return false;

                lastTimestamp = image.timestamp;
                return TryConvert(image, maxEdge, out frame);
            }
            finally
            {
                // The image holds a native buffer the provider cannot recycle until it is released.
                image.Dispose();
            }
        }

        /// <summary>
        /// Converts the camera image into the decoder's format, downscaling in the same pass.
        /// <para>
        /// <see cref="TextureFormat.R8"/> is asked for first because the camera's YUV output already
        /// carries luminance in its Y plane: that conversion is the cheapest the provider offers and
        /// arrives in exactly the layout the decoder wants, at a quarter of the bytes of colour.
        /// Whether a provider will emit it is a runtime question, so it is asked rather than assumed.
        /// </para>
        /// </summary>
        private bool TryConvert(XRCpuImage image, int maxEdge, out LuminanceFrame frame)
        {
            frame = default;

            bool grayscale = image.FormatSupported(TextureFormat.R8);
            int divisor = FrameScaling.GetDivisor(image.width, image.height, maxEdge);
            var outputDimensions = new Vector2Int(
                Mathf.Max(1, image.width / divisor),
                Mathf.Max(1, image.height / divisor));

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = outputDimensions,
                outputFormat = grayscale ? TextureFormat.R8 : TextureFormat.RGBA32,

                // Unconverted output is already top-down, which is the order the decoder expects.
                // Note a mirrored code never decodes, so a front-facing rig would need MirrorX here.
                transformation = XRCpuImage.Transformation.None
            };

            int size = image.GetConvertedDataSize(conversionParams);
            if (buffer.Length != size)
            {
                buffer = new byte[size];
            }

            // Pinning the managed array lets the conversion write straight into it, so the pixels
            // reach the decoder without a native intermediate or a second copy.
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                image.Convert(conversionParams, handle.AddrOfPinnedObject(), size);
            }
            catch (InvalidOperationException)
            {
                // The provider rejected these parameters; skip the frame rather than throw every one.
                return false;
            }
            finally
            {
                handle.Free();
            }

            frame = new LuminanceFrame(
                buffer,
                outputDimensions.x,
                outputDimensions.y,
                grayscale ? LuminancePixelFormat.Gray8 : LuminancePixelFormat.RGBA32);
            return true;
        }
    }
}
