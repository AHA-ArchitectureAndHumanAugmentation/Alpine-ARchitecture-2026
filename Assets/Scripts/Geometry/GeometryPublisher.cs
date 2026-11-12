using Compas.Core.Transformations;
using System.Collections.Generic;
using UnityEngine;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Sends every placed object back to Rhino as one message. Call Publish from a button's On Click.
    /// </summary>
    public class GeometryPublisher : MonoBehaviour
    {
        [Tooltip("Coordinate system the receiver expects. Unity geometry is converted to this before sending.")]
        public SupportedSystem targetSystem = SupportedSystem.RhinoZUp;

        [SerializeField] private PlacedObjects _placedObjects;

        public void Publish()
        {
            var message = new List<object>();
            foreach (GeometryLogic placedObject in _placedObjects.Objects)
            {
                if (placedObject != null)
                {
                    message.Add(placedObject.ToData(targetSystem));
                }
            }

            if (message.Count == 0)
            {
                Debug.Log("Nothing to publish - place something first");
                return;
            }

            GeoViewerHub.Instance.Publish(message);
        }
    }
}
