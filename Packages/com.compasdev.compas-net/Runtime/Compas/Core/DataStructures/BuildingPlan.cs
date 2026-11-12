using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using Compas.Core.Geometry;

namespace Compas.Core.DataStructures
{
    // [System.Serializable]
    // public class BuildingPlanData
    // {
    //     /*
    //     * BuildingPlanData : A class to define the structure of a building plan in the assembly data structure.
    //     * It is based off the Compas data structure for a building plan.
    //     * The building plan contains a dictionary of steps required for assembly, and the last built index.
    //     */
    //     public string LastBuiltIndex { get; set; }
    //     public Dictionary<string, Step> steps { get; set; }
    //     public Dictionary<string, List<string>> PriorityTreeDictionary { get; set; }
    //     public static BuildingPlanData Parse(object jsondata)
    //     {
    //         /*
    //         * Method to create an instance of a the BuildingPlanData class from a json
    //         * Returns BuildingPlanData Class
    //         */
    //         Dictionary<string, object> jsonDataDict = jsondata as Dictionary<string, object>;
    //         BuildingPlanData buildingPlanData = BuildingPlanData.FromData(jsonDataDict);
    //         return buildingPlanData;
    //     }
    //     public static BuildingPlanData FromData(Dictionary<string, object> jsonDataDict)
    //     {
    //         /*
    //         * Method to create an instance of a the BuildingPlanData class from a dictionary.
    //         * Returns BuildingPlan Class instance & PriorityTreeDictionary
    //         */
    //         BuildingPlanData buidingPlanData = new BuildingPlanData();
    //         buidingPlanData.steps = new Dictionary<string, Step>();
    //         buidingPlanData.PriorityTreeDictionary = new Dictionary<string, List<string>>();
    //         if (jsonDataDict.TryGetValue("LastBuiltIndex", out object last_built_index))
    //         {
    //             Console.WriteLine($"Last Built Index Fetched From database: {last_built_index.ToString()}");
    //             buidingPlanData.LastBuiltIndex = last_built_index.ToString();
    //         }
    //         else
    //         {
    //             buidingPlanData.LastBuiltIndex = null;
    //         }
    //         List<object> stepsList = jsonDataDict["steps"] as List<object>;
    //         for(int i =0 ; i < stepsList.Count; i++)
    //         {
    //             string key = i.ToString();
    //             var json_data = stepsList[i];
    //             Step step_data = Step.Parse(json_data);
                
    //             if (step_data.IsValidStep())
    //             {
    //                 buidingPlanData.steps[key] = step_data;
    //                 Console.WriteLine($"FromData: BuildingPlan Step {key} successfully added to the building plan dictionary");

    //                 if (buidingPlanData.PriorityTreeDictionary.ContainsKey(step_data.data.priority.ToString()))
    //                 {
    //                     buidingPlanData.PriorityTreeDictionary[step_data.data.priority.ToString()].Add(key);
    //                 }
    //                 else
    //                 {
    //                     buidingPlanData.PriorityTreeDictionary[step_data.data.priority.ToString()] = new List<string>();
    //                     buidingPlanData.PriorityTreeDictionary[step_data.data.priority.ToString()].Add(key);
    //                 }
    //             }
    //             else
    //             {
    //                 Console.WriteLine($"Invalid Step structure for key '{key}'. Not added to the dictionary.");
    //             }
    //         }
    //         return buidingPlanData;
    //     }
    // }
    
    // [System.Serializable]
    // public class Step
    // {
    //     /*
    //     * Step : A class to define the structure of a step in the building plan data structure.
    //     * It is based off the Compas data structure for a step.
    //     * The step contains the data required for a single step of the building process
    //     */
    //     public Data data { get; set; }
    //     public string dtype { get; set; }
    //     public string guid { get; set; }

    //     public static Step Parse(object jsondata)
    //     {
    //         /*
    //         * Method to create an instance of a the Step class from a json string.
    //         */
    //         Dictionary<string, object> jsonDataDict = jsondata as Dictionary<string, object>;
    //         return FromData(jsonDataDict);
    //     }
    //     public static Step FromData(Dictionary<string, object> jsonDataDict)
    //     {
    //         /*
    //         * Method to create an instance of a the Step class from a dictionary.
    //         */
    //         Step step = new Step();
    //         step.dtype = (string)jsonDataDict["dtype"];
    //         step.guid = (string)jsonDataDict["guid"];

    //         Dictionary<string, object> dataDict = jsonDataDict["data"] as Dictionary<string, object>;
    //         step.data = Data.FromData(dataDict);
    //         return step;
    //     }
    //     public static bool AreEqualSteps(Step step ,Step NewStep)
    //     {
    //         /*
    //         * Method to compare two steps and check if they are equal.
    //         */
    //         if (step != null &&
    //             NewStep != null &&
    //             step.data.device_id == NewStep.data.device_id &&
    //             step.data.element_ids == step.data.element_ids &&
    //             step.data.actor == NewStep.data.actor &&
    //             step.data.location.Point.SequenceEqual(NewStep.data.location.Point) &&
    //             step.data.location.XAxis.SequenceEqual(NewStep.data.location.XAxis) &&
    //             step.data.location.YAxis.SequenceEqual(NewStep.data.location.YAxis) &&
    //             step.data.geometry == NewStep.data.geometry &&
    //             step.data.is_built == NewStep.data.is_built &&
    //             step.data.is_planned == NewStep.data.is_planned &&
    //             step.data.priority == NewStep.data.priority)
    //         {
    //             return true;
    //         }
    //         return false;
    //     }
    //     public bool IsValidStep()
    //     {
    //         /*
    //         * Method to check if the step contains all valid information.
    //         */
    //         if (data != null &&
    //             data.element_ids != null &&
    //             !string.IsNullOrEmpty(data.actor) &&
    //             data.location != null &&
    //             data.geometry != null &&
    //             data.is_built != null &&
    //             data.is_planned != null &&
    //             data.priority != null)
    //         {
    //             return true;
    //         }
    //         return false;
    //     }

    // }

    // [System.Serializable]
    // public class Data
    // {
    //     /*
    //     * Data : A class to define the structure of a data in the building plan data structure.
    //     * data contains the information required for coordinating the building process
    //     */
    //     public string device_id { get; set; }
    //     public string[] element_ids { get; set; }
    //     public string actor { get; set; }
    //     public Frame location { get; set; }
    //     public string geometry { get; set; }
    //     public bool is_built { get; set; }
    //     public bool is_planned { get; set; }
    //     public int priority { get; set; }

    //     public static Data Parse(object jsondata)
    //     {
    //         /*
    //         * Method to create an instance of a the Data class from a json string.
    //         */
    //         Dictionary<string, object> jsonDataDict = jsondata as Dictionary<string, object>;
    //         return FromData(jsonDataDict);
    //     }
    //     public static Data FromData(Dictionary<string, object> dataDict)
    //     {
    //         /*
    //         * Method to create an instance of a the Data class from a dictionary.
    //         */
    //         Data data = new Data();
    //         Dictionary<string, object> locationDataDict = dataDict["location"] as Dictionary<string, object>;
    //         data.location = Frame.FromData(locationDataDict);

    //         if (dataDict.TryGetValue("device_id", out object device_id))
    //         {
    //             data.device_id = device_id.ToString();
    //         }
    //         else
    //         {
    //             data.device_id = null;
    //         }
    //         data.actor = (string)dataDict["actor"];
    //         data.geometry = (string)dataDict["geometry"];
    //         data.is_built = (bool)dataDict["is_built"];
    //         data.is_planned = (bool)dataDict["is_planned"];
    //         data.priority = (int)(long)dataDict["priority"];

    //         List<object> element_ids = dataDict["element_ids"] as List<object>;
    //         if (element_ids != null)
    //         {
    //             data.element_ids = element_ids.Select(x => x.ToString()).ToArray();
    //         }
    //         else
    //         {
    //             Console.WriteLine("FromData (Data): Element_ID's list is empty.");
    //         }
    //         return data;
    //     }

    // }

}
