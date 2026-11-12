using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;

namespace Compas.User
{
    [System.Serializable]
    public class UserCurrentInfo
    {
        /// <summary>
        /// UserCurrentInfo: A class to define the structure of a user current information in the building plan data structure.
        /// UserCurrentInfo contains the information required for tracking multiple users across the building process.
        /// </summary>
        public string currentStep { get; set; }
        public string timeStamp { get; set; }
        /// <summary>
        /// Parses user current info from loosely-typed JSON dictionary data.
        /// </summary>
        /// <param name="jsondata">Input JSON dictionary object.</param>
        /// <returns>User info instance.</returns>
        public static UserCurrentInfo Parse(object jsondata)
        {
            Dictionary<string, object> jsonDataDict = jsondata as Dictionary<string, object>;
            return FromData(jsonDataDict);
        }

        /// <summary>
        /// Creates user current info from dictionary data.
        /// </summary>
        /// <param name="jsonDataDict">Dictionary payload.</param>
        /// <returns>User info instance.</returns>
        public static UserCurrentInfo FromData(Dictionary<string, object> jsonDataDict)
        {
            //Create class instances of node elements
            UserCurrentInfo userCurrentInfo = new UserCurrentInfo();
            userCurrentInfo.currentStep = (string)jsonDataDict["currentStep"];
            userCurrentInfo.timeStamp = (string)jsonDataDict["timeStamp"];
            return userCurrentInfo;
        }

    }
}