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

			if (LoadingExtension.Instance.ToolMode != TrafficManagerMode.None && ToolsModifierControl.toolController.CurrentTool != LoadingExtension.Instance.TrafficLightTool)
			{
				LoadingExtension.Instance.UI.HideTMPanel();
			}

			if (ToolsModifierControl.toolController.CurrentTool != LoadingExtension.Instance.TrafficLightTool && LoadingExtension.Instance.UI.isVisible())
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

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				LoadingExtension.Instance.UI.HideTMPanel();
			}
		}
	}
		
	public sealed class LoadingExtension : LoadingExtensionBase
	{		
		public static LoadingExtension Instance = null;

		List<RedirectCallsState> m_redirectionStates = new List<RedirectCallsState>();

		public RedirectCallsState[] revertMethods = new RedirectCallsState[8];

		public TrafficManagerMode ToolMode = TrafficManagerMode.None;

		public TrafficLightTool TrafficLightTool = null;

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
                //CustomPassengerCarAI.RedirectCalls(m_redirectionStates);
                CustomCargoTruckAI.RedirectCalls(m_redirectionStates);

                if (Instance == null)
				{
					Instance = this;
				}
					
				UI = ToolsModifierControl.toolController.gameObject.AddComponent<UIBase>();
				TrafficPriority.leftHandDrive = Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.True;
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

        T TryGetComponent<T>(string name)
        {
            string[] sm_collectionPrefixes = new string[] { "", "Europe " };

            foreach (string prefix in sm_collectionPrefixes)
            {
                GameObject go = GameObject.Find(prefix + name);
                if (go != null)
                    return go.GetComponent<T>();
            }

            return default(T);
        }

        void ReplacePassengerCarAI()
        {
            VehicleCollection collection = TryGetComponent<VehicleCollection>("Residential Low");
            VehicleInfo info = collection.m_prefabs[0];
            PassengerCarAI originalAI = (PassengerCarAI) info.GetComponent<VehicleAI>();
            CustomPassengerCarAI newAI = info.gameObject.AddComponent<CustomPassengerCarAI>();
            CopyVehicleAIAttributes<CustomPassengerCarAI>(originalAI, newAI);
            //m_replacedAIs[vehicle.name] = originalAI;
            info.m_vehicleAI = newAI;
            newAI.m_info = info;
        }

        void CopyVehicleAIAttributes<T>(VehicleAI from, T to)
        {
            foreach (FieldInfo fi in typeof(T).BaseType.GetFields())
            {
                fi.SetValue(to, fi.GetValue(from));
            }
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
    }
}
