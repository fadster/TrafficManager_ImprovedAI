using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Xml.Serialization;
using ColossalFramework;
using ColossalFramework.IO;
using ICities;
using UnityEngine;

namespace TrafficManager_ImprovedAI
{
    public class SerializableDataExtension : ISerializableDataExtension
    {
        public static string dataID = "TrafficManager_v0.9";
        public static UInt32 uniqueID;

        public static ISerializableData SerializableData;

        private static Timer _timer;

        public void OnCreated(ISerializableData serializableData)
        {
            uniqueID = 0u;
            SerializableData = serializableData;
        }

        public void OnReleased()
        {
        }

        public static void GenerateUniqueID()
        {
            uniqueID = (uint)UnityEngine.Random.Range(1000000f, 2000000f);

            while (File.Exists(Path.Combine(Application.dataPath, "trafficManagerSave_" + uniqueID + ".xml"))) {
                uniqueID = (uint)UnityEngine.Random.Range(1000000f, 2000000f);
            }
        }

        public void OnLoadData()
        {
            byte[] data = SerializableData.LoadData(dataID);

            if (data == null) {
                GenerateUniqueID();
            } else {
                _timer = new System.Timers.Timer(2000);
                // Hook up the Elapsed event for the timer. 
                _timer.Elapsed += OnLoadDataTimed;
                _timer.Enabled = true;
            }
        }

        public static void OnLoadDataTimed(System.Object source, ElapsedEventArgs e)
        {
            byte[] data = SerializableData.LoadData(dataID);

            uniqueID = 0u;
            var i = 0;

            for (i = 0; i < data.Length - 3; i++) {
                uniqueID = BitConverter.ToUInt32(data, i);
            }

            var filepath = Path.Combine(Application.dataPath, "trafficManagerSave_" + uniqueID + ".xml");
            _timer.Enabled = false;

            if (!File.Exists(filepath)) {
                
                return;
            }

            var configuration = Configuration.Deserialize(filepath);

            try {
                for (i = 0; i < configuration.data.prioritySegments.Count; i++) {
                    if (!TrafficPriority.isPrioritySegment((ushort)configuration.data.prioritySegments[i][0],
                        configuration.data.prioritySegments[i][1])) {
                        TrafficPriority.addPrioritySegment((ushort)configuration.data.prioritySegments[i][0],
                            configuration.data.prioritySegments[i][1],
                            (PrioritySegment.PriorityType)configuration.data.prioritySegments[i][2]);
                    }
                }
            } catch (Exception ex) {
                Debug.Log("prio segments exception at " + i + " - " + ex);
            }


            try {
                for (i = 0; i < configuration.data.nodeDictionary.Count; i++) {
                    if (CustomRoadAI.GetNodeSimulation((ushort)configuration.data.nodeDictionary[i][0]) == null) {
                        CustomRoadAI.AddNodeToSimulation((ushort)configuration.data.nodeDictionary[i][0]);
                        var nodeDict = CustomRoadAI.GetNodeSimulation((ushort)configuration.data.nodeDictionary[i][0]);

                        nodeDict._manualTrafficLights = Convert.ToBoolean(configuration.data.nodeDictionary[i][1]);
                        nodeDict._timedTrafficLights = Convert.ToBoolean(configuration.data.nodeDictionary[i][2]);
                        nodeDict.TimedTrafficLightsActive = Convert.ToBoolean(configuration.data.nodeDictionary[i][3]);
                    }
                }
            } catch (Exception ex) {
                Debug.Log("nodes exception at " + i + " - " + ex);
            }

            try {
                for (i = 0; i < configuration.data.manualSegments.Count; i++) {
                    var segmentData = configuration.data.manualSegments[i];

                    if (!TrafficLightsManual.IsSegmentLight((ushort)segmentData[0], segmentData[1])) {
                        TrafficLightsManual.AddSegmentLight((ushort)segmentData[0], segmentData[1],
                            RoadBaseAI.TrafficLightState.Green);
                        var segment = TrafficLightsManual.GetSegmentLight((ushort)segmentData[0], segmentData[1]);
                        segment.currentMode = (ManualSegmentLight.Mode)segmentData[2];
                        segment.lightLeft = (RoadBaseAI.TrafficLightState)segmentData[3];
                        segment.lightMain = (RoadBaseAI.TrafficLightState)segmentData[4];
                        segment.lightRight = (RoadBaseAI.TrafficLightState)segmentData[5];
                        segment.lightPedestrian = (RoadBaseAI.TrafficLightState)segmentData[6];
                        segment.lastChange = (uint)segmentData[7];
                        segment.lastChangeFrame = (uint)segmentData[8];
                        segment.pedestrianEnabled = Convert.ToBoolean(segmentData[9]);
                    }
                }
            } catch (Exception ex) {
                Debug.Log("traf lights manual exception at " + i + " - " + ex);
            }

            var timedStepCount = 0;
            var timedStepSegmentCount = 0;

            try {
                for (i = 0; i < configuration.data.timedNodes.Count; i++) {
                    var nodeid = (ushort)configuration.data.timedNodes[i][0];

                    var nodeGroup = new List<ushort>();
                    for (var j = 0; j < configuration.data.timedNodeGroups[i].Length; j++) {
                        nodeGroup.Add(configuration.data.timedNodeGroups[i][j]);
                    }

                    if (!TrafficLightsTimed.IsTimedLight(nodeid)) {
                        TrafficLightsTimed.AddTimedLight(nodeid, nodeGroup);
                        var timedNode = TrafficLightsTimed.GetTimedLight(nodeid);

                        timedNode.currentStep = configuration.data.timedNodes[i][1];

                        for (var j = 0; j < configuration.data.timedNodes[i][2]; j++) {
                            var cfgstep = configuration.data.timedNodeSteps[timedStepCount];

                            timedNode.addStep(cfgstep[0]);

                            var step = timedNode.steps[j];

                            for (var k = 0; k < cfgstep[1]; k++) {
                                step.lightLeft[k] = (RoadBaseAI.TrafficLightState)configuration.data.timedNodeStepSegments[timedStepSegmentCount][0];
                                step.lightMain[k] = (RoadBaseAI.TrafficLightState)configuration.data.timedNodeStepSegments[timedStepSegmentCount][1];
                                step.lightRight[k] = (RoadBaseAI.TrafficLightState)configuration.data.timedNodeStepSegments[timedStepSegmentCount][2];
                                step.lightPedestrian[k] = (RoadBaseAI.TrafficLightState)configuration.data.timedNodeStepSegments[timedStepSegmentCount][3];

                                timedStepSegmentCount++;
                            }

                            timedStepCount++;
                        }

                        if (Convert.ToBoolean(configuration.data.timedNodes[i][3])) {
                            timedNode.start();
                        }
                    }
                }
            } catch (Exception ex) {
                Debug.Log("timed traf lights exception at " + i + " - " + ex);
            }


            var j1 = 0;
            var i1 = 0;
            try {
                for (i1 = 0; i1 < 32768; i1++) {
                    if (Singleton<NetManager>.instance.m_nodes.m_buffer[i1].Info.m_class.m_service ==
                    ItemClass.Service.Road && Singleton<NetManager>.instance.m_nodes.m_buffer[i1].m_flags != 0) {
                        var trafficLight = configuration.data.nodeTrafficLights[j1];

                        if (trafficLight == '1') {
                            Singleton<NetManager>.instance.m_nodes.m_buffer[i1].m_flags |= NetNode.Flags.TrafficLights;
                        } else {
                            Singleton<NetManager>.instance.m_nodes.m_buffer[i1].m_flags &= ~NetNode.Flags.TrafficLights;
                        }

                        j1++;
                    }
                }
            } catch (Exception ex) {
                Debug.Log("traf lights exception at i1 = " + i1 + " j1 = " + j1 + " - " + ex);
            }

            var j2 = 0;
            var i2 = 0;
            try {
                for (i2 = 0; i2 < 32768; i2++) {
                    if (Singleton<NetManager>.instance.m_nodes.m_buffer[i2].Info.m_class.m_service ==
                    ItemClass.Service.Road && Singleton<NetManager>.instance.m_nodes.m_buffer[i2].m_flags != 0) {
                        var crossWalk = configuration.data.nodeCrosswalk[j2];

                        if (crossWalk == '1') {
                            Singleton<NetManager>.instance.m_nodes.m_buffer[i2].m_flags |= NetNode.Flags.Junction;
                        } else {
                            Singleton<NetManager>.instance.m_nodes.m_buffer[i2].m_flags &= ~NetNode.Flags.Junction;
                        }

                        j2++;
                    }
                }
            } catch (Exception ex) {
                Debug.Log("crosswalk exception at i2 = " + i2 + " j2 = " + j2 + " - " + ex);
            }

            var lanes = configuration.data.laneFlags.TrimEnd(',').Split(',');

            Debug.Log("found " + lanes.Length + " lane assignments");

            try {
                for (i = 0; i < lanes.Length; i++) {
                    var split = lanes[i].Split(':');
                    uint laneId = Convert.ToUInt32(split[0]);
                    //NetLane lane = Singleton<NetManager>.instance.m_lanes.m_buffer [laneId];
                    //ushort segmentId = lane.m_segment;
                    //NetSegment segment = Singleton<NetManager>.instance.m_segments.m_buffer [segmentId];
                    //segment.Info.m_netAI.UpdateLanes(segmentId, ref segment, false);

                    Singleton<NetManager>.instance.m_lanes.m_buffer[laneId].m_flags = Convert.ToUInt16(split[1]);
                }
            } catch (Exception ex) {
                Debug.Log("exception reading lane flags at lane " + i + " lanes[] = [" + lanes[i] + "] - " + ex);
            }

            if (configuration.data.congestionCostFactor > 0) {
                Debug.Log("setting AI values");
                CustomPathFind.congestionCostFactor = configuration.data.congestionCostFactor;
                CustomPathFind.minLaneSpace = configuration.data.minLaneSpace;
                CustomPathFind.lookaheadLanes = configuration.data.lookaheadLanes;
                CustomPathFind.congestedLaneThreshold = configuration.data.congestedLaneThreshold;
                CustomPathFind.obeyTMLanes = configuration.data.obeyTMLanes;
            }
        }

        public void OnSaveData()
        {

            FastList<byte> data = new FastList<byte>();

            GenerateUniqueID(); 

            byte[] uniqueIdBytes = BitConverter.GetBytes(uniqueID);
            foreach (byte uniqueIdByte in uniqueIdBytes) {
                data.Add(uniqueIdByte);
            }

            byte[] dataToSave = data.ToArray();
            SerializableData.SaveData(dataID, dataToSave);

            var filepath = Path.Combine(Application.dataPath, "trafficManagerSave_" + uniqueID + ".xml");

            var configuration = new Configuration();

            for (var i = 0; i < 32768; i++) {
                if (TrafficPriority.prioritySegments.ContainsKey(i)) {
                    if (TrafficPriority.prioritySegments[i].node_1 != 0) {
                        configuration.data.prioritySegments.Add(new int[3] {
                            TrafficPriority.prioritySegments[i].node_1,
                            i,
                            (int)TrafficPriority.prioritySegments[i].instance_1.type
                        });
                    } 
                    if (TrafficPriority.prioritySegments[i].node_2 != 0) {
                        configuration.data.prioritySegments.Add(new int[3] {
                            TrafficPriority.prioritySegments[i].node_2,
                            i,
                            (int)TrafficPriority.prioritySegments[i].instance_2.type
                        });
                    }
                }

                if (CustomRoadAI.nodeDictionary.ContainsKey((ushort)i)) {
                    var nodeDict = CustomRoadAI.nodeDictionary[(ushort)i];

                    configuration.data.nodeDictionary.Add(new int[4] {
                        nodeDict.NodeId,
                        Convert.ToInt32(nodeDict._manualTrafficLights),
                        Convert.ToInt32(nodeDict._timedTrafficLights),
                        Convert.ToInt32(nodeDict.TimedTrafficLightsActive)
                    });
                }

                if (TrafficLightsManual.ManualSegments.ContainsKey(i)) {
                    if (TrafficLightsManual.ManualSegments[i].node_1 != 0) {
                        var manualSegment = TrafficLightsManual.ManualSegments[i].instance_1;

                        configuration.data.manualSegments.Add(new int[10] {
                            (int)manualSegment.node,
                            manualSegment.segment,
                            (int)manualSegment.currentMode,
                            (int)manualSegment.lightLeft,
                            (int)manualSegment.lightMain,
                            (int)manualSegment.lightRight,
                            (int)manualSegment.lightPedestrian,
                            (int)manualSegment.lastChange,
                            (int)manualSegment.lastChangeFrame,
                            Convert.ToInt32(manualSegment.pedestrianEnabled)
                        });
                    }
                    if (TrafficLightsManual.ManualSegments[i].node_2 != 0) {
                        var manualSegment = TrafficLightsManual.ManualSegments[i].instance_2;

                        configuration.data.manualSegments.Add(new int[10] {
                            (int)manualSegment.node,
                            manualSegment.segment,
                            (int)manualSegment.currentMode,
                            (int)manualSegment.lightLeft,
                            (int)manualSegment.lightMain,
                            (int)manualSegment.lightRight,
                            (int)manualSegment.lightPedestrian,
                            (int)manualSegment.lastChange,
                            (int)manualSegment.lastChangeFrame,
                            Convert.ToInt32(manualSegment.pedestrianEnabled)
                        });
                    }
                }

                if (TrafficLightsTimed.timedScripts.ContainsKey((ushort)i)) {
                    var timedNode = TrafficLightsTimed.GetTimedLight((ushort)i);

                    configuration.data.timedNodes.Add(new int[4] {
                        timedNode.nodeID,
                        timedNode.currentStep,
                        timedNode.NumSteps(),
                        Convert.ToInt32(timedNode.isStarted())
                    });

                    var nodeGroup = new ushort[timedNode.nodeGroup.Count];

                    for (var j = 0; j < timedNode.nodeGroup.Count; j++) {
                        nodeGroup[j] = timedNode.nodeGroup[j];
                    }

                    configuration.data.timedNodeGroups.Add(nodeGroup);

                    for (var j = 0; j < timedNode.NumSteps(); j++) {
                        configuration.data.timedNodeSteps.Add(new int[2] {
                            timedNode.steps[j].numSteps,
                            timedNode.steps[j].segments.Count
                        });

                        for (var k = 0; k < timedNode.steps[j].segments.Count; k++) {
                            configuration.data.timedNodeStepSegments.Add(new int[4] {
                                (int)timedNode.steps[j].lightLeft[k],
                                (int)timedNode.steps[j].lightMain[k],
                                (int)timedNode.steps[j].lightRight[k],
                                (int)timedNode.steps[j].lightPedestrian[k],
                            });
                        }
                    }
                }
            }

            for (var i = 0; i < Singleton<NetManager>.instance.m_nodes.m_buffer.Length; i++) {
                var nodeFlags = Singleton<NetManager>.instance.m_nodes.m_buffer[i].m_flags;

                if (nodeFlags != 0) {
                    if (Singleton<NetManager>.instance.m_nodes.m_buffer[i].Info.m_class.m_service ==
                        ItemClass.Service.Road) {
                        configuration.data.nodeTrafficLights +=
                            Convert.ToInt16((nodeFlags & NetNode.Flags.TrafficLights) != NetNode.Flags.None);
                        configuration.data.nodeCrosswalk +=
                            Convert.ToInt16((nodeFlags & NetNode.Flags.Junction) != NetNode.Flags.None);
                    }
                }
            }

            var laneCount = 0;
            for (var i = 0; i < Singleton<NetManager>.instance.m_lanes.m_buffer.Length; i++) {
                var laneSegment = Singleton<NetManager>.instance.m_lanes.m_buffer[i].m_segment;

                if (TrafficPriority.prioritySegments.ContainsKey(laneSegment)) {
                    configuration.data.laneFlags += i + ":" + Singleton<NetManager>.instance.m_lanes.m_buffer[i].m_flags + ",";
                    laneCount++;
                }
            }
            configuration.data.laneFlags.TrimEnd(',');

            configuration.data.congestionCostFactor = CustomPathFind.congestionCostFactor;
            configuration.data.minLaneSpace = CustomPathFind.minLaneSpace;
            configuration.data.lookaheadLanes = CustomPathFind.lookaheadLanes;
            configuration.data.congestedLaneThreshold = CustomPathFind.congestedLaneThreshold;
            configuration.data.obeyTMLanes = AIPanel.IsObeyingTMLanes();

            Configuration.Serialize(filepath, configuration);
        }
    }

    public class Configuration
    {
        public class Data
        {
            public string nodeTrafficLights;
            public string nodeCrosswalk;
            public string laneFlags;

            public List<int[]> prioritySegments = new List<int[]>();
            public List<int[]> nodeDictionary = new List<int[]>();
            public List<int[]> manualSegments = new List<int[]>();

            public List<int[]> timedNodes = new List<int[]>();
            public List<ushort[]> timedNodeGroups = new List<ushort[]>();
            public List<int[]> timedNodeSteps = new List<int[]>();
            public List<int[]> timedNodeStepSegments = new List<int[]>();

            public float congestionCostFactor;
            public float minLaneSpace;
            public int lookaheadLanes;
            public int congestedLaneThreshold;
            public bool obeyTMLanes;
        }

        public Data data = new Data();
        public int hashCode;

        public override string ToString()
        {
            var s = "\n";

            try {
                s += "Traffic Manager\n";
                s += "---------------\n";
                s += "priority segments: " + data.prioritySegments.Count + "\n";
                s += "nodes: " + data.nodeDictionary.Count + "\n";
                s += "manual segments: " + data.manualSegments.Count + "\n";
                s += "timed nodes: " + data.timedNodes.Count + "\n";
                s += "timed node groups: " + data.timedNodeGroups.Count + "\n";
                s += "timed node steps: " + data.timedNodeSteps.Count + "\n";
                s += "timed node step segments: " + data.timedNodeStepSegments.Count + "\n";
                s += "traffic lights: " + data.nodeTrafficLights.Length + "\n";
                s += "crosswalks: " + data.nodeCrosswalk.Length + "\n";
                s += "lane flags: " + data.laneFlags.TrimEnd(',').Split(',').Length + "\n\n";

                s += "Improved AI\n";
                s += "-----------\n";
                s += "minimum lane space = " + data.minLaneSpace + "\n";
                s += "congestion cost factor = " + data.congestionCostFactor + "\n";
                s += "lookahead lanes = " + data.lookaheadLanes + "\n";
                s += "congested lane threshold = " + data.congestedLaneThreshold + "\n";
                s += "obey traffic manager lane flags = " + data.obeyTMLanes + "\n\n";

                s += "configuration data hash code - stored: " + hashCode + " computed: " + data.GetHashCode();
            } catch (Exception e) {
                Debug.Log("error constructing string representation of configuration data, probable corruption! - " + e);
            }

            return s;
        }

        public void OnPreSerialize()
        {
        }

        public void OnPostDeserialize()
        {
        }

        private static void UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            Debug.Log("unknown attribute " + sender.ToString() + " " + e.ToString());
        }

        private static void UnknownElement(object sender, XmlElementEventArgs e)
        {
            Debug.Log("unknown element " + sender.ToString() + " " + e.ToString());
        }

        private static void UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Debug.Log("unknown node " + sender.ToString() + " " + e.ToString());
        }

        private static void UnreferencedObject(object sender, UnreferencedObjectEventArgs e)
        {
            Debug.Log("unreferenced object " + sender.ToString() + " " + e.ToString());
        }

        private static void RegisterEvents(ref XmlSerializer s)
        {
            s.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttribute);
            s.UnknownElement += new XmlElementEventHandler(UnknownElement);
            s.UnknownNode += new XmlNodeEventHandler(UnknownNode);
            s.UnreferencedObject += new UnreferencedObjectEventHandler(UnreferencedObject);
        }

        public static void Serialize(string filename, Configuration config)
        {
            config.hashCode = config.data.GetHashCode();
            Debug.Log("serializing to " + filename);
            Debug.Log(config.ToString());

            var serializer = new XmlSerializer(typeof(Configuration));
            RegisterEvents(ref serializer);    

            using (var writer = new StreamWriter(filename)) {
                config.OnPreSerialize();
                serializer.Serialize(writer, config);
            }
        }

        public static Configuration Deserialize(string filename)
        {
            Debug.Log("deserializing from " + filename);

            var serializer = new XmlSerializer(typeof(Configuration));
            RegisterEvents(ref serializer);    

            try {
                using (var reader = new StreamReader(filename)) {
                    var config = (Configuration)serializer.Deserialize(reader);
                    config.OnPostDeserialize();
                    Debug.Log(config.ToString());
                    return config;
                }
            } catch (Exception e) {
                Debug.Log("deserialize exception " + e);
            }

            Debug.Log("deserialized null config!");
            return null;
        }
    }
}
