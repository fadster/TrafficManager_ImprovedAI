using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TrafficManager_ImprovedAI
{
    public class AIPanel : UIPanel
    {
        private static UITextField congestionCostFactor;
        private static UITextField minLaneSpace;
        private static UITextField lookaheadLanes;
        private static UITextField congestedLaneThreshold;

        public override void Start()
        {
            this.backgroundSprite = "GenericPanel";
            this.color = new Color32(75, 75, 135, 255);
            this.width = 350;
            this.height = 140;
            this.relativePosition = new Vector3(300f, 80f);

            UILabel title = this.AddUIComponent<UILabel>();
            title.text = "Improved AI";
            title.relativePosition = new Vector3(65.0f, 5.0f);

            congestionCostFactor = CreateTextField("congestion cost factor", new Vector3(35f, 30f), CustomPathFind.congestionCostFactor, true);
            minLaneSpace = CreateTextField("minimum lane space", new Vector3(35f, 60f), CustomPathFind.minLaneSpace, true);
            lookaheadLanes = CreateTextField("lookahead lanes", new Vector3(35f, 90f), CustomPathFind.lookaheadLanes, false);
            congestedLaneThreshold = CreateTextField("congested lane threshold", new Vector3(35f, 120f), CustomPathFind.congestedLaneThreshold, false);
        }

        public override void Update()
        {
            base.Update();
            if (!this.containsFocus) {
                ReconcileValues();
            }
        }

        private UITextField CreateTextField(String name, Vector3 pos, float value, bool allowFloats)
        {
            var label = this.AddUIComponent<UILabel>();
            label.text = name;
            label.relativePosition = pos;

            var textField = this.AddUIComponent<UITextField>();
            textField.width = 50;
            textField.relativePosition = pos + new Vector3(250f, 0f);
            textField.allowFloats = allowFloats;
            textField.numericalOnly = true;
            //textField.normalBgSprite = "TextFieldPanel";
            //textField.hoveredBgSprite = "TextFieldPanelHovered";
            textField.selectionBackgroundColor = new Color32(127, 0, 0, 255);
            //textField.focusedBgSprite = "TextFieldUnderline";
            textField.text = allowFloats ? String.Format("{0:0.0}", value) : value.ToString();
            textField.isInteractive = true;
            textField.enabled = true;
            textField.readOnly = false;
            textField.builtinKeyNavigation = true; 
            textField.eventTextSubmitted += TextSubmittedHandler;
            /*
            textField.eventDoubleClick += delegate(UIComponent component, UIMouseEventParameter param) {
                ((UITextField) component).SelectAll();
            };
            */

            return textField;
        }

        private void ReconcileValues()
        {
            var fval = 0f;
            var ival = 0;

            if (!float.TryParse(congestionCostFactor.text, out fval) || (fval != CustomPathFind.congestionCostFactor)) {
                congestionCostFactor.text = String.Format("{0:0.0}", CustomPathFind.congestionCostFactor);
            }
            if (!float.TryParse(minLaneSpace.text, out fval) || (fval != CustomPathFind.minLaneSpace)) {
                minLaneSpace.text = String.Format("{0:0.0}", CustomPathFind.minLaneSpace);
            }
            if (!int.TryParse(lookaheadLanes.text, out ival) || (ival != CustomPathFind.lookaheadLanes)) {
                lookaheadLanes.text = CustomPathFind.lookaheadLanes.ToString();
            }
            if (!int.TryParse(congestedLaneThreshold.text, out ival) || (ival != CustomPathFind.congestedLaneThreshold)) {
                congestedLaneThreshold.text = CustomPathFind.congestedLaneThreshold.ToString();
            }
        }

        private void TextSubmittedHandler(UIComponent component, String text)
        {
            var fval = 0f;
            var ival = 0;

            if (component.CompareTo(congestionCostFactor) == 0) {
                if (float.TryParse(text, out fval)) {
                    CustomPathFind.congestionCostFactor = fval;
                }
            } else if (component.CompareTo(minLaneSpace) == 0) {
                if (float.TryParse(text, out fval)) {
                    CustomPathFind.minLaneSpace = fval;
                }
            } else if (component.CompareTo(lookaheadLanes) == 0) {
                if (int.TryParse(text, out ival)) {
                    CustomPathFind.lookaheadLanes = ival;
                }
            } else if (component.CompareTo(congestedLaneThreshold) == 0) {
                if (int.TryParse(text, out ival)) {
                    CustomPathFind.congestedLaneThreshold = ival;
                }
            }

            ReconcileValues();
        }

        /*
        private UISlider CreateSlider(Vector3 pos, float val, float min, float max)
        {
            var slider = this.AddUIComponent<UISlider>();
            slider.width = 300;
            slider.height = 20;
            slider.relativePosition = pos;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = val;
            slider.playAudioEvents = true;
            slider.eventValueChanged += OnSliderValueChanged;

            return slider;
        }
        */

        /*
        private void OnSliderValueChanged(UIComponent component, float value)
        {
            Debug.Log("component " + component.tag + " value = " + value);
            if (component.CompareTag(sliderCostFactor.tag)) {
                Debug.Log("setting cost factor to " + value);
                TrafficManager_ImprovedAI.CustomPathFind.m_congestionCostFactor = value;
            } else if (component.CompareTag(sliderLaneSpace.tag)) {
                Debug.Log("setting lane space to " + value);
                TrafficManager_ImprovedAI.CustomPathFind.m_minLaneSpace = value;
            }
        }
        */
    }
}

