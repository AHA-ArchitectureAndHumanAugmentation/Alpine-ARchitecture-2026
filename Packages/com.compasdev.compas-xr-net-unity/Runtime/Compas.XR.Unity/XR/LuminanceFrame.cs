namespace Compas.XR.Unity.XR
{
    /// <summary>
    /// Pixel layout of the bytes in a <see cref="LuminanceFrame"/>.
    /// </summary>
    public enum LuminancePixelFormat
    {
        /// <summary>One byte per pixel, already luminance.</summary>
        Gray8,

        /// <summary>Four bytes per pixel in red, green, blue, alpha order.</summary>
        RGBA32
    }

    /// <summary>
    /// One camera image in the form a barcode decoder can read: a tightly packed byte buffer plus
    /// the dimensions and layout needed to interpret it, in top-down raster order.
    /// <para>
    /// The buffer belongs to the <see cref="ICameraFrameSource"/> that produced it and is reused
    /// between frames, so a caller that needs the pixels beyond the current decode must copy them.
    /// Reuse is the point: a scanner running many times a second should not hand the collector a
    /// megabyte of garbage each time it looks at the camera.
    /// </para>
    /// </summary>
    public readonly struct LuminanceFrame
    {
        /// <summary>The pixel bytes, laid out according to <see cref="Format"/>.</summary>
        public byte[] Data { get; }

        /// <summary>Width of the image in pixels.</summary>
        public int Width { get; }

        /// <summary>Height of the image in pixels.</summary>
        public int Height { get; }

        /// <summary>How to interpret <see cref="Data"/>.</summary>
        public LuminancePixelFormat Format { get; }

        public LuminanceFrame(byte[] data, int width, int height, LuminancePixelFormat format)
        {
            Data = data;
            Width = width;
            Height = height;
            Format = format;
        }
    }
}
