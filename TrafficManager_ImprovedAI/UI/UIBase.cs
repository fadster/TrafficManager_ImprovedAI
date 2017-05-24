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
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenu";
            button.pressedBgSprite = "ButtonMenu";

            // Enable button sounds.
            button.playAudioEvents = true;

            // Place the button.
            button.relativePosition = new Vector3(475f, 20f);

            // Respond to button click.
            button.eventClick += TMClick;

            button = (UIButton)uiView.AddUIComponent(typeof(UIButton));

            // Set the text to show on the button.
            button.text = "AI";

            // Set the button dimensions.
            button.width = 50;
            button.height = 30;

            // Style the button to look like a menu button.
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenu";
            button.pressedBgSprite = "ButtonMenu";

            // Enable button sounds.
            button.playAudioEvents = true;

            // Place the button.
            button.relativePosition = new Vector3(535f, 20f);

            // Respond to button click.
            button.eventClick += AIClick;
        }

        private void TMClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (aiPanel != null && aiPanel.isVisible) {
                aiPanel.isVisible = false;
            }
            if (tmPanel == null || !tmPanel.isVisible) {
                ShowTMPanel();
            } else {
                HideTMPanel();
            }
        }

        private void AIClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (aiPanel == null || !aiPanel.isVisible) {
                if (tmPanel != null && tmPanel.isVisible) {
                    HideTMPanel();
                }
                if (aiPanel == null) {
                    aiPanel = (AIPanel) UIView.GetAView().AddUIComponent(typeof(AIPanel));
                }
                aiPanel.isVisible = true;
            } else {
                aiPanel.isVisible = false;
            }
        }

        public bool isVisible()
        {
            return (tmPanel != null && tmPanel.isVisible);
        }

        public void ShowTMPanel()
        {
            if (tmPanel == null) {
                tmPanel = (UITrafficManager) UIView.GetAView().AddUIComponent(typeof(UITrafficManager));
            }
            tmPanel.isVisible = true;
            LoadingExtension.Instance.SetToolMode(TrafficManagerMode.TrafficLight);
        }

        public void HideTMPanel()
        {
            if (tmPanel != null) {
                tmPanel.isVisible = false;
            }
            UITrafficManager.uistate = UITrafficManager.UIState.None;
            TrafficLightTool.setToolMode(TrafficLightTool.ToolMode.None);
            LoadingExtension.Instance.SetToolMode(TrafficManagerMode.None);
        }

        public void DestroyPanels()
        {
            HideTMPanel();
            if (tmPanel != null) {
                UIView.Destroy(tmPanel);
            }
            if (aiPanel != null) {
                aiPanel.isVisible = false;
                UIView.Destroy(aiPanel);
            }
        }
    }
}