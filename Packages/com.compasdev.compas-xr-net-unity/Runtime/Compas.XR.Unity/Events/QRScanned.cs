using Compas.Events;

namespace Compas.Unity
{
    public readonly struct QRScanned : IEvent
    {
        public readonly string ScannedText;

        public QRScanned(string text)
        {
            ScannedText = text;
        }
    }
}
