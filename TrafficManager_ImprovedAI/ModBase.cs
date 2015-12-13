using ColossalFramework;
using ICities;
using TrafficManager_ImprovedAI.Redirection;
using TrafficManager_ImprovedAI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TrafficManager_ImprovedAI
{
	public enum TrafficManagerMode
	{
		None = 0,
		TrafficLight = 1
	}

    public class ModBase : IUserMod
    {
        public string Name { get { return "Traffic Manager + Improved AI"; } }

        public string Description { get { return "TM lane changes, traffic lights and priority signs with improved AI from Traffic++."; } }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Traffic Manager + Improved AI");
            group.AddCheckbox("Ignore saved data on startup", LoadingExtension.ignoreSavedData, delegate(bool c) { LoadingExtension.ignoreSavedData = c; });
            group.AddSpace(3);
            group.AddGroup("Enable if errors on startup prevent the game from loading correctly.");
            group.AddGroup("NOTE: This setting will be automatically disabled after startup!");
        }
	}
  
	public sealed class ThreadingExtension : ThreadingExtensionBase
	{
		public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
		{
			base.OnUpdate(realTimeDelta, simulationTimeDelta);

			if (LoadingExtension.Instance == null)
			{
				return;
			}

            if (!LoadingExtension.roadManagerInitialized && SerializableDataExtension.configLoaded) {
                CSL_Traffic.RoadManager.Initialize();
                LoadingExtension.roadManagerInitialized = true;
            }
            /*
			if (LoadingExtension.Instance.ToolMode != TrafficManagerMode.None && ToolsModifierControl.toolController.CurrentTool != LoadingExtension.Instance.TrafficLightTool)
			{
				LoadingExtension.Instance.UI.HideTMPanel();
			}
            */
            if (LoadingExtension.Instance.ToolMode != TrafficManagerMode.None &&
                ToolsModifierControl.toolController.CurrentTool != LoadingExtension.Instance.TrafficLightTool &&
                ToolsModifierControl.toolController.CurrentTool != LoadingExtension.Instance.RoadCustomizerTool &&
                LoadingExtension.Instance.UI.isVisible())
			{
				LoadingExtension.Instance.UI.HideTMPanel();
			}

			if (!LoadingExtension.Instance.detourInited)
			{
                LoadingExtension.Instance.revertMethods[0] = RedirectionHelper.RedirectCalls(
                    typeof(CarAI).GetMethod("CalculateSegmentPosition",
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new Type[] {
                            typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(PathUnit.Position),
                            typeof(PathUnit.Position), typeof(uint), typeof(byte), typeof(PathUnit.Position),
                            typeof(uint), typeof(byte), typeof(Vector3).MakeByRefType(),
                            typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType()
                        },
                        null),
                    typeof(CustomCarAI).GetMethod("CalculateSegmentPosition", BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new Type[] {
                            typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(PathUnit.Position),
                            typeof(PathUnit.Position), typeof(uint), typeof(byte), typeof(PathUnit.Position),
                            typeof(uint), typeof(byte), typeof(Vector3).MakeByRefType(),
                            typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType()
                        },
                        null));

                LoadingExtension.Instance.revertMethods[1] = RedirectionHelper.RedirectCalls(
                    typeof(RoadBaseAI).GetMethod("SimulationStep",
                        new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() }),
                    typeof(CustomRoadAI).GetMethod("SimulationStep", new Type[] {
                        typeof(ushort),
                        typeof(NetNode).MakeByRefType()
                    }));

				LoadingExtension.Instance.revertMethods[2] = RedirectionHelper.RedirectCalls(typeof (HumanAI).GetMethod("CheckTrafficLights",
					BindingFlags.NonPublic | BindingFlags.Instance,
					null,
					new Type[] {typeof (ushort), typeof (ushort)},
					null),
					typeof (CustomHumanAI).GetMethod("CheckTrafficLights"));
	
				LoadingExtension.Instance.detourInited = true;
			}

			if (!LoadingExtension.Instance.nodeSimulationLoaded)
			{
				LoadingExtension.Instance.nodeSimulationLoaded = true;
				ToolsModifierControl.toolController.gameObject.AddComponent<CustomRoadAI>();
			}
            /*
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				LoadingExtension.Instance.UI.HideTMPanel();
			}
            */

            if (Event.current.alt && Input.GetKeyDown(KeyCode.D)) {
                var info = new RoadInfo();
                var netManager = Singleton<NetManager>.instance;
                Debug.Log("dumping road info for " + netManager.m_nodes.m_size + " nodes out of " + NetManager.MAX_NODE_COUNT);
                var nodeCount = 0;
                for (uint i = 0; i < netManager.m_nodes.m_size; i++) {
                    var node = netManager.m_nodes.m_buffer[i];
                    if (node.m_flags != 0 && node.Info.m_class.m_service == ItemClass.Service.Road) {
                        var nodeInfo = new RoadInfo.Node();
                        nodeInfo.id = i;
                        nodeInfo.buildIndex = node.m_buildIndex;
                        nodeInfo.segments[0] = node.m_segment0;
                        nodeInfo.segments[1] = node.m_segment1;
                        nodeInfo.segments[2] = node.m_segment2;
                        nodeInfo.segments[3] = node.m_segment3;
                        nodeInfo.segments[4] = node.m_segment4;
                        nodeInfo.segments[5] = node.m_segment5;
                        nodeInfo.segments[6] = node.m_segment6;
                        nodeInfo.segments[7] = node.m_segment7;
                        info.nodes.Add(nodeInfo);
                        nodeCount++;
                    }
                }
                Debug.Log("found " + nodeCount + " nodes");
                RoadInfo.DumpRoadInfo(info);
            }
		}
	}

    public class RoadInfo
    {
        [Serializable]
        public class Node
        {
            public uint id;
            public uint buildIndex;
            public uint[] segments = new uint[8];
        }
        /*
        [Serializable]
        public class Segment
        {
            public uint id;
            public uint buildIndex;
        }
        */
        public List<Node> nodes = new List<Node>();
        //public List<Segment> segments = new List<Segment>();

        public static void DumpRoadInfo(RoadInfo info)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(RoadInfo));
            var filepath = System.IO.Path.Combine(Application.dataPath, "roadinfo_" + (uint) UnityEngine.Random.Range(1000000f, 2000000f));

            using (var writer = new System.IO.StreamWriter(filepath)) {
                serializer.Serialize(writer, info);
            }
        }
    }
		
	public sealed class LoadingExtension : LoadingExtensionBase
	{		
		public static LoadingExtension Instance = null;

        public static bool ignoreSavedData = false;

        public static bool roadManagerInitialized = false;

		List<RedirectCallsState> m_redirectionStates = new List<RedirectCallsState>();

		public RedirectCallsState[] revertMethods = new RedirectCallsState[8];

		public TrafficManagerMode ToolMode = TrafficManagerMode.None;

		public TrafficLightTool TrafficLightTool = null;

        public CSL_Traffic.RoadCustomizerTool RoadCustomizerTool = null;

		public UIBase UI;

		public bool detourInited = false;

		public bool nodeSimulationLoaded = false;

		public CustomPathManager customPathManager;

		public bool despawnEnabled = true;

		public override void OnCreated(ILoading loading)
		{
			base.OnCreated(loading);
		}

		public override void OnReleased()
		{
			base.OnReleased();

			if (ToolMode != TrafficManagerMode.None)
			{
				ToolMode = TrafficManagerMode.None;
				UITrafficManager.uistate = UITrafficManager.UIState.None;
				DestroyTool();
			}
		}
			
		public override void OnLevelLoaded(LoadMode mode)
		{
			base.OnLevelLoaded(mode);
			if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
			{
				ReplacePathManager();
				CustomCarAI.RedirectCalls(m_redirectionStates);
                CustomPassengerCarAI.RedirectCalls(m_redirectionStates);
                CustomCargoTruckAI.RedirectCalls(m_redirectionStates);

                if (Instance == null)
				{
					Instance = this;
				}
					
				UI = ToolsModifierControl.toolController.gameObject.AddComponent<UIBase>();
				TrafficPriority.leftHandDrive = Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.True;
                RoadCustomizerTool = AddTool<CSL_Traffic.RoadCustomizerTool>(ToolsModifierControl.toolController);
                ToolsModifierControl.SetTool<DefaultTool>();
			}				
		}

		public override void OnLevelUnloading()
		{
			base.OnLevelUnloading();
            LoadingExtension.Instance.UI.DestroyPanels();
            foreach (RedirectCallsState rcs in m_redirectionStates) {
                RedirectionHelper.RevertRedirect(rcs);
            }
   			TrafficPriority.prioritySegments.Clear();
			CustomRoadAI.nodeDictionary.Clear();
			TrafficLightsManual.ManualSegments.Clear();
			TrafficLightsTimed.timedScripts.Clear();
			LoadingExtension.Instance.nodeSimulationLoaded = false;
            SerializableDataExtension.configLoaded = false;
            CSL_Traffic.RoadCustomizerTool customizerTool = ToolsModifierControl.GetTool<CSL_Traffic.RoadCustomizerTool>();
            customizerTool.ClearNodeMarkers();
            CSL_Traffic.RoadManager.sm_lanes = new CSL_Traffic.RoadManager.Lane[NetManager.MAX_LANE_COUNT];
            roadManagerInitialized = false;
            //ToolsModifierControl.SetTool<DefaultTool>();
		}

		void ReplacePathManager()
		{
			if (Singleton<PathManager>.instance as CustomPathManager != null)
				return;

			// Change PathManager to CustomPathManager
			FieldInfo sInstance = typeof(ColossalFramework.Singleton<PathManager>).GetFieldByName("sInstance");
			PathManager originalPathManager = ColossalFramework.Singleton<PathManager>.instance;
			CustomPathManager customPathManager = originalPathManager.gameObject.AddComponent<CustomPathManager>();
			customPathManager.SetOriginalValues(originalPathManager);

			// change the new instance in the singleton
			sInstance.SetValue(null, customPathManager);

			// change the manager in the SimulationManager
			FastList<ISimulationManager> managers = (FastList<ISimulationManager>)typeof(SimulationManager).GetFieldByName("m_managers").GetValue(null);
			managers.Remove(originalPathManager);
			managers.Add(customPathManager);

			// Destroy in 10 seconds to give time to all references to update to the new manager without crashing
			GameObject.Destroy(originalPathManager, 10f);
		}
			
		public void SetToolMode(TrafficManagerMode mode)
		{
			if (mode == ToolMode) return;

			//UI.toolMode = mode;
			ToolMode = mode;

			if (mode != TrafficManagerMode.None)
			{
				DestroyTool();
				EnableTool();
			}
			else
			{
				DestroyTool();
			}
		}

		public void EnableTool()
		{
			if (TrafficLightTool == null)
			{
				TrafficLightTool = ToolsModifierControl.toolController.gameObject.GetComponent<TrafficLightTool>() ??
					ToolsModifierControl.toolController.gameObject.AddComponent<TrafficLightTool>();

//                var laneTool = ToolsModifierControl.toolController.gameObject.GetComponent<CSL_Traffic.RoadCustomizerTool>() ??
//                    ToolsModifierControl.toolController.gameObject.AddComponent<CSL_Traffic.RoadCustomizerTool>();
			}

			ToolsModifierControl.toolController.CurrentTool = TrafficLightTool;
			ToolsModifierControl.SetTool<TrafficLightTool>();
		}

		private void DestroyTool()
		{
			if (TrafficLightTool != null)
			{
				ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();
				ToolsModifierControl.SetTool<DefaultTool>();

				TrafficLightTool.Destroy(TrafficLightTool);
				TrafficLightTool = null;
			}
		}

        private T AddTool<T>(ToolController toolController) where T : ToolBase
        {
            var tool = toolController.GetComponent<T>();

            if (tool == null) {
                tool = toolController.gameObject.AddComponent<T>();

                // contributed by Japa
                FieldInfo toolControllerField = typeof(ToolController).GetField("m_tools", BindingFlags.Instance | BindingFlags.NonPublic);
                if (toolControllerField != null)
                    toolControllerField.SetValue(toolController, toolController.GetComponents<ToolBase>());
                FieldInfo toolModifierDictionary = typeof(ToolsModifierControl).GetField("m_Tools", BindingFlags.Static | BindingFlags.NonPublic);
                if (toolModifierDictionary != null)
                    toolModifierDictionary.SetValue(null, null); // to force a refresh
            }

            return tool;
        }
    }
}
