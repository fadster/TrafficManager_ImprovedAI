using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TrafficManager_ImprovedAI
{
    public class AIPanel : UIPanel
    {
        private static UISliderInput congestionCostFactor;
        private static UISliderInput minLaneSpace;
        private static UISliderInput lookaheadLanes;
        private static UISliderInput congestedLaneThreshold;

        public override void Start()
        {
            this.backgroundSprite = "MenuPanel2";
            this.width = 435;
            this.height = 285;
            this.transformPosition = new Vector3(-1.3f, 0.9f);

            UITitlePanel titlePanel = this.AddUIComponent<UITitlePanel>();
            titlePanel.Parent = this;
            titlePanel.relativePosition = Vector3.zero;
            titlePanel.IconSprite = "ToolbarIconRoads";
            titlePanel.TitleText = "Improved AI";

            congestionCostFactor = this.AddUIComponent<UISliderInput>();
            minLaneSpace = this.AddUIComponent<UISliderInput>();
            lookaheadLanes = this.AddUIComponent<UISliderInput>();
            congestedLaneThreshold = this.AddUIComponent<UISliderInput>();

            congestionCostFactor.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(congestionCostFactor, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.congestionCostFactor = value;
                Monitor.Exit(congestionCostFactor);
            };

            minLaneSpace.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(minLaneSpace, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.minLaneSpace = value;
                Monitor.Exit(minLaneSpace);
            };

            lookaheadLanes.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(lookaheadLanes, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.lookaheadLanes = (int) value;
                Monitor.Exit(lookaheadLanes);
            };

            congestedLaneThreshold.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(congestedLaneThreshold, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.congestedLaneThreshold = (int) value;
                Monitor.Exit(congestedLaneThreshold);
            };

            float yVal = 55;

            congestionCostFactor.Parent = this;
            congestionCostFactor.relativePosition = new Vector3(0, yVal);
            congestionCostFactor.MinValue = 3f;
            congestionCostFactor.MaxValue = 100f;
            congestionCostFactor.StepSize = 0.1f;
            congestionCostFactor.Slider.scrollWheelAmount = 0.1f;
            congestionCostFactor.LabelText = "congestion cost factor";
            congestionCostFactor.SliderValue = CustomPathFind.congestionCostFactor;

            yVal += 55;

            minLaneSpace.Parent = this;
            minLaneSpace.relativePosition = new Vector3(0, yVal);
            minLaneSpace.MinValue = 5f;
            minLaneSpace.MaxValue = 50f;
            minLaneSpace.StepSize = 0.1f;
            minLaneSpace.Slider.scrollWheelAmount = 0.1f;
            minLaneSpace.LabelText = "minimum lane space";
            minLaneSpace.SliderValue = CustomPathFind.minLaneSpace;

            yVal += 55;

            lookaheadLanes.Parent = this;
            lookaheadLanes.relativePosition = new Vector3(0, yVal);
            lookaheadLanes.MinValue = 1;
            lookaheadLanes.MaxValue = 20;
            lookaheadLanes.StepSize = 1;
            lookaheadLanes.Slider.scrollWheelAmount = 1;
            lookaheadLanes.LabelText = "lookahead lanes";
            lookaheadLanes.SliderValue = CustomPathFind.lookaheadLanes;

            yVal += 55;

            congestedLaneThreshold.Parent = this;
            congestedLaneThreshold.relativePosition = new Vector3(0, yVal);
            congestedLaneThreshold.MinValue = 1;
            congestedLaneThreshold.MaxValue = 20;
            congestedLaneThreshold.StepSize = 1;
            congestedLaneThreshold.Slider.scrollWheelAmount = 1;
            congestedLaneThreshold.LabelText = "congested lane threshold";
            congestedLaneThreshold.SliderValue = CustomPathFind.congestedLaneThreshold;
        }

        public override void Update()
        {
            base.Update();
            ReconcileValues();
        }

        public new void Hide()
        {
            base.Hide();
            UIBase.HideAIPanel();
        }

        private void ReconcileValues()
        {
            if (!Mathf.Approximately(congestionCostFactor.SliderValue, CustomPathFind.congestionCostFactor)) {
                while (!Monitor.TryEnter(congestionCostFactor, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                congestionCostFactor.SliderValue = CustomPathFind.congestionCostFactor;
                Monitor.Exit(congestionCostFactor);
            }
            if (!Mathf.Approximately(minLaneSpace.SliderValue, CustomPathFind.minLaneSpace)) {
                while (!Monitor.TryEnter(minLaneSpace, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                minLaneSpace.SliderValue = CustomPathFind.minLaneSpace;
                Monitor.Exit(minLaneSpace);
            }
            if (!Mathf.Approximately(lookaheadLanes.SliderValue, CustomPathFind.lookaheadLanes)) {
                while (!Monitor.TryEnter(lookaheadLanes, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                lookaheadLanes.SliderValue = CustomPathFind.lookaheadLanes;
                Monitor.Exit(lookaheadLanes);
            }
            if (!Mathf.Approximately(congestedLaneThreshold.SliderValue, CustomPathFind.congestedLaneThreshold)) {
                while (!Monitor.TryEnter(congestedLaneThreshold, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                congestedLaneThreshold.SliderValue = CustomPathFind.congestedLaneThreshold;
                Monitor.Exit(congestedLaneThreshold);
            }
        }
    }
}
