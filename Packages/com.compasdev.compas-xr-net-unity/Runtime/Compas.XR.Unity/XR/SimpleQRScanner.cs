using Compas.Events;
using Compas.Unity;
using Compas.XR.Unity.Events;
using Compas.XR.Unity.XR;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ZXing;

public class SimpleQRScanner : MonoBehaviour
{
    [Tooltip("The AR rig's camera. When its session is running, frames come from there instead of " +
             "opening a second camera - which on device would fight the session for the hardware.")]
    public ARCameraManager arCameraManager;

    [Tooltip("Shows the webcam feed. Stays hidden under AR, where the background already draws it.")]
    public RawImage previewImage;

    [Tooltip("Frames are downscaled towards this longest edge before decoding.")]
    public int maxEdge = 640;

    [Tooltip("Seconds between decode attempts. Decoding every frame is the most expensive thing " +
             "in this loop and buys nothing: a code held up to the camera stays there for many frames.")]
    public float scanInterval = 0.1f;

    private ICameraFrameSource source;
    private readonly IBarcodeReader reader = new BarcodeReader();
    private bool scanning = false;
    private float nextScanTime;

    public static event Action<string> QRScanned;

    private void Awake()
    {
        EventBus<StartQRScanningRequested>.AddListener(OnStartQRScanningRequested);

        reader.Options.PossibleFormats = new List<BarcodeFormat>
        {
            BarcodeFormat.QR_CODE
        };

        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        EventBus<StartQRScanningRequested>.RemoveListener(OnStartQRScanningRequested);
        source?.Dispose();
    }

    private void Update()
    {
        if (!scanning || Time.time < nextScanTime)
            return;

        nextScanTime = Time.time + scanInterval;

        if (!source.TryAcquireFrame(maxEdge, out LuminanceFrame frame))
            return;

        try
        {
            var format = frame.Format == LuminancePixelFormat.Gray8
                ? RGBLuminanceSource.BitmapFormat.Gray8
                : RGBLuminanceSource.BitmapFormat.RGBA32;

            Result result = reader.Decode(frame.Data, frame.Width, frame.Height, format);
            if (result != null)
            {
                StopScanning();
                HandleScanResult(result.Text);
            }
        }
        catch { /* decode errors on non-QR frames are normal, ignore */ }
    }

    private void OnStartQRScanningRequested(StartQRScanningRequested requested)
    {
        StartScanning();
    }

    public void StartScanning()
    {
        // Chosen here rather than at startup: the AR session is not yet running when the scene
        // loads, so an early check would always fall through to the webcam.
        source = SelectSource();
        source.StartCapture();

        if (previewImage != null)
        {
            previewImage.texture = source.PreviewTexture;
            previewImage.gameObject.SetActive(source.PreviewTexture != null);
        }

        // Scan on the very next frame rather than waiting out an interval that began before the
        // user asked for anything.
        nextScanTime = 0f;
        scanning = true;
    }

    /// <summary>
    /// Picks the camera to scan through: the AR session's in a player, an ordinary webcam otherwise.
    /// </summary>
    /// <remarks>
    /// The Editor is ruled out by compilation rather than by availability, because availability is
    /// not what separates the two cases. XR Simulation registers a camera subsystem that runs and
    /// reports <c>supportsCameraImage</c>, so the AR source genuinely is available in the Editor. It
    /// just serves renders of the simulated environment, and no amount of holding a code up to the
    /// laptop will put a QR code into that.
    /// </remarks>
    private ICameraFrameSource SelectSource()
    {
        source?.Dispose();

#if !UNITY_EDITOR
        var arSource = new ARCameraFrameSource(arCameraManager);
        if (arSource.IsAvailable)
        {
            Debug.Log("[SimpleQRScanner] Scanning through the AR session camera.");
            return arSource;
        }

        arSource.Dispose();
#endif
        Debug.Log("[SimpleQRScanner] Scanning through a webcam.");
        return new WebCamFrameSource();
    }

    private void HandleScanResult(string qrText)
    {
        Debug.Log($"<color=cyan>Scanned: {qrText}</color>");
        QRScanned?.Invoke(qrText);
        EventBus<QRScanned>.Raise(new QRScanned(qrText));
    }

    private void StopScanning()
    {
        scanning = false;
        source.StopCapture();

        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
        }
    }
}
