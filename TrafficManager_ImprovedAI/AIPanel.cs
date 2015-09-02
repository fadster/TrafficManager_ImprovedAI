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
        private static UILabelledBox obeyTMLaneFlags;
        private static UIButton resetButton;

        public override void Start()
        {
            this.backgroundSprite = "MenuPanel2";
            this.width = 455;
            this.height = 320;
            this.relativePosition = new Vector3(220f, 60f);

            UITitlePanel titlePanel = this.AddUIComponent<UITitlePanel>();
            titlePanel.Parent = this;
            titlePanel.relativePosition = Vector3.zero;
            titlePanel.IconSprite = "ToolbarIconRoads";
            titlePanel.TitleText = "Improved AI";

            float yVal = 57;
            minLaneSpace = this.AddUIComponent<UISliderInput>();
            minLaneSpace.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(minLaneSpace, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.minLaneSpace = value;
                Monitor.Exit(minLaneSpace);
            };
            minLaneSpace.Parent = this;
            minLaneSpace.relativePosition = new Vector3(3, yVal);
            minLaneSpace.MinValue = CustomPathFind.MIN_MIN_LANE_SPACE;
            minLaneSpace.MaxValue = CustomPathFind.MAX_MIN_LANE_SPACE;
            minLaneSpace.StepSize = 0.1f;
            minLaneSpace.Slider.scrollWheelAmount = 0.1f;
            minLaneSpace.LabelText = "minimum lane space";
            minLaneSpace.SliderValue = CustomPathFind.minLaneSpace;

            yVal += 56;
            congestionCostFactor = this.AddUIComponent<UISliderInput>();
            congestionCostFactor.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(congestionCostFactor, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.congestionCostFactor = value;
                Monitor.Exit(congestionCostFactor);
            };
            congestionCostFactor.Parent = this;
            congestionCostFactor.relativePosition = new Vector3(3, yVal);
            congestionCostFactor.MinValue = CustomPathFind.MIN_CONGESTION_COST_FACTOR;
            congestionCostFactor.MaxValue = CustomPathFind.MAX_CONGESTION_COST_FACTOR;
            congestionCostFactor.StepSize = 0.1f;
            congestionCostFactor.Slider.scrollWheelAmount = 0.1f;
            congestionCostFactor.LabelText = "congestion cost factor";
            congestionCostFactor.SliderValue = CustomPathFind.congestionCostFactor;

            yVal += 56;
            lookaheadLanes = this.AddUIComponent<UISliderInput>();
            lookaheadLanes.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(lookaheadLanes, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.lookaheadLanes = (int) value;
                Monitor.Exit(lookaheadLanes);
            };
            lookaheadLanes.Parent = this;
            lookaheadLanes.relativePosition = new Vector3(3, yVal);
            lookaheadLanes.MinValue = CustomPathFind.MIN_LOOKAHEAD_LANES;
            lookaheadLanes.MaxValue = CustomPathFind.MAX_LOOKAHEAD_LANES;
            lookaheadLanes.StepSize = 1;
            lookaheadLanes.Slider.scrollWheelAmount = 1;
            lookaheadLanes.LabelText = "lookahead lanes";
            lookaheadLanes.SliderValue = CustomPathFind.lookaheadLanes;

            yVal += 56;
            congestedLaneThreshold = this.AddUIComponent<UISliderInput>();
            congestedLaneThreshold.Slider.eventValueChanged += delegate(UIComponent sender, float value) {
                while (!Monitor.TryEnter(congestedLaneThreshold, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.congestedLaneThreshold = (int) value;
                Monitor.Exit(congestedLaneThreshold);
            };
            congestedLaneThreshold.Parent = this;
            congestedLaneThreshold.relativePosition = new Vector3(3, yVal);
            congestedLaneThreshold.MinValue = CustomPathFind.MIN_CONGESTED_LANE_THRESHOLD;
            congestedLaneThreshold.MaxValue = CustomPathFind.MAX_CONGESTED_LANE_THRESHOLD;
            congestedLaneThreshold.StepSize = 1;
            congestedLaneThreshold.Slider.scrollWheelAmount = 1;
            congestedLaneThreshold.LabelText = "congested lane threshold";
            congestedLaneThreshold.SliderValue = CustomPathFind.congestedLaneThreshold;

            yVal += 58;
            obeyTMLaneFlags = this.AddUIComponent<UILabelledBox>();
            obeyTMLaneFlags.Parent = this;
            obeyTMLaneFlags.relativePosition = new Vector3(0, yVal);
            obeyTMLaneFlags.LabelText = "obey traffic manager lane flags";
            obeyTMLaneFlags.CheckBox.isChecked = CustomPathFind.obeyTMLaneFlags;
            obeyTMLaneFlags.CheckBox.eventCheckChanged += delegate(UIComponent component, bool value) {
                while (!Monitor.TryEnter(obeyTMLaneFlags, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                CustomPathFind.obeyTMLaneFlags = value;
                Monitor.Exit(obeyTMLaneFlags);
            };

            resetButton = this.AddUIComponent<UIButton>();
            resetButton.text = "reset";
            resetButton.width = 57;
            resetButton.height = 33;
            resetButton.normalBgSprite = "ButtonMenu";
            resetButton.hoveredBgSprite = "ButtonMenuHovered";
            resetButton.focusedBgSprite = "ButtonMenu";
            resetButton.pressedBgSprite = "ButtonMenu";
            resetButton.playAudioEvents = true;
            resetButton.relativePosition = new Vector3(378f, yVal - 8.5f);
            resetButton.eventClick += delegate(UIComponent component, UIMouseEventParameter eventParam) {
                CustomPathFind.ResetAIParameters();
            };
        }

        protected override void OnGotFocus(UIFocusEventParameter p)
        {
            base.OnGotFocus(p);
        }

        public override void Update()
        {
            base.Update();
            ReconcileValues();
        }

        public new void Hide()
        {
            base.Hide();
            isVisible = false;
        }

        public void SetObeyTMLanes(Boolean obey)
        {
            obeyTMLaneFlags.CheckBox.isChecked = obey;
            CustomPathFind.obeyTMLaneFlags = obey;
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
            if (!Mathf.Approximately(congestedLaneThreshold.SliderValue, CustomPathFind.congestedLaneThreshold)) {
                while (!Monitor.TryEnter(congestedLaneThreshold, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                congestedLaneThreshold.SliderValue = CustomPathFind.congestedLaneThreshold;
                Monitor.Exit(congestedLaneThreshold);
            }
            if (!Mathf.Approximately(lookaheadLanes.SliderValue, CustomPathFind.lookaheadLanes)) {
                while (!Monitor.TryEnter(lookaheadLanes, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                lookaheadLanes.SliderValue = CustomPathFind.lookaheadLanes;
                Monitor.Exit(lookaheadLanes);
            }
            if (obeyTMLaneFlags.CheckBox.isChecked != CustomPathFind.obeyTMLaneFlags) {
                while (!Monitor.TryEnter(obeyTMLaneFlags, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
                }
                obeyTMLaneFlags.CheckBox.isChecked = CustomPathFind.obeyTMLaneFlags;
                Monitor.Exit(obeyTMLaneFlags);
            }
        }
    }
}
