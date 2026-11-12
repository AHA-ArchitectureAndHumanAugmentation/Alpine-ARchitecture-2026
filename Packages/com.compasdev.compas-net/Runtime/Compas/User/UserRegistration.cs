using System;
using System.Collections.Generic;

namespace Compas.User
{
    /// <summary>
    /// Represents a user with registered devices.
    /// </summary>
    public class User
    {
        public Dictionary<string, Device> devices;

        /// <summary>
        /// Initializes a new user with an empty device collection.
        /// </summary>
        public User()
        {
            devices = new Dictionary<string, Device>();
        }
    }

    [System.Serializable]
    public class Device
    {
        /// <summary>
        /// Device: Class is used to manage the device record and configuration settings.
        /// </summary>
        public List<string> dates;

        //TODO: ADD MORE DEVICE INFORMATION???? - Unique identifier, etc. Trying to maximize capabilities, but change the implementation on the other side so it uses what is needed.
        /// <summary>
        /// Initializes a new device with an empty date list.
        /// </summary>
        public Device()
        {
            dates = new List<string>();
        }
    }
}