using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace TrafficManager_ImprovedAI
{
    public class UIBase : UICustomControl
    {
        private UITrafficManager tmPanel;
        private AIPanel aiPanel;

        public UIBase()
        {
            // Get the UIView object. This seems to be the top-level object for most
            // of the UI.
            var uiView = UIView.GetAView();

            // Add a new button to the view.
            var button = (UIButton)uiView.AddUIComponent(typeof(UIButton));

            // Set the text to show on the button.
            button.text = "TM";

            // Set the button dimensions.
            button.width = 50;
            button.height = 30;

            // Style the button to look like a menu button.
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);

            // Enable button sounds.
            button.playAudioEvents = true;

            // Place the button.
            button.relativePosition = new Vector3(160f, 20f);

            // Respond to button click.
            button.eventClick += tmClick;

            button = (UIButton)uiView.AddUIComponent(typeof(UIButton));

            // Set the text to show on the button.
            button.text = "AI";

            // Set the button dimensions.
            button.width = 50;
            button.height = 30;

            // Style the button to look like a menu button.
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);

            // Enable button sounds.
            button.playAudioEvents = true;

            // Place the button.
            button.relativePosition = new Vector3(220f, 20f);

            // Respond to button click.
            button.eventClick += aiClick;

        }

        private void aiClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (aiPanel == null || !aiPanel.isVisible) {
                if (tmPanel != null && tmPanel.isVisible) {
                    Close();
                }
                if (aiPanel == null) {
                    aiPanel = (AIPanel) UIView.GetAView().AddUIComponent(typeof(AIPanel));
                }
                aiPanel.isVisible = true;
            } else {
                aiPanel.isVisible = false;
            }
        }

        private void tmClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (aiPanel != null && aiPanel.isVisible) {
                aiPanel.isVisible = false;
            }
            if (tmPanel == null || !tmPanel.isVisible) {
                Show();
            } else {
                Close();
            }
        }

        public bool isVisible()
        {
            return (tmPanel != null && tmPanel.isVisible);
        }

        public void Show()
        {
            if (tmPanel == null) {
                tmPanel = (UITrafficManager) UIView.GetAView().AddUIComponent(typeof(UITrafficManager));
            }
            tmPanel.isVisible = true;
            LoadingExtension.Instance.SetToolMode(TrafficManagerMode.TrafficLight);
        }

        public void Close()
        {
            if (tmPanel != null) {
                tmPanel.isVisible = false;
            }
            UITrafficManager.uistate = UITrafficManager.UIState.None;
            TrafficLightTool.setToolMode(TrafficLightTool.ToolMode.None);
            LoadingExtension.Instance.SetToolMode(TrafficManagerMode.None);
        }
    }
}