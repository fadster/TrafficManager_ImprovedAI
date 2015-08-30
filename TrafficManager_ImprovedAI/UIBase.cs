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
        private static bool _uiShown = false;
        private static bool _aiPanelVisible = false;

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
            button.relativePosition = new Vector3(260f, 20f);

            // Respond to button click.
            button.eventClick += aiClick;

        }
/*
        void Click1(UIComponent component, UIMouseEventParameter eventParam)
        {
            Debug.Log("event 1 - " + eventParam);

            if (Event.current.button == 0) {
                Debug.Log("current button is left");
            } else if (Event.current.button == 1) {
                Debug.Log("current button is right");
            }
            if (Input.GetMouseButtonDown(1)) {
                Debug.Log("get mouse right");
            } else if (Input.GetMouseButtonDown(0)) {
                Debug.Log("get mouse left");
            }
        }
*/
        /*
        void Click2(UIComponent component, UIMouseEventParameter eventParam)
        {
            Debug.Log("2 " + eventParam);

            if (Event.current.button == 0) {
                Debug.Log("current button is left");
                LeftClick();
            } else if (Event.current.button == 1) {
                Debug.Log("current button is right");
                RightClick();
            }
            
            if (Input.GetMouseButtonDown(1)) {
                Debug.Log("right");
                RightClick();
            } else if (Input.GetMouseButtonDown(0)) {
                Debug.Log("left");
                LeftClick();
            } else if (Event.current.button == 0) {
                Debug.Log("left 2");
            } else if (Event.current.button == 1) {
                Debug.Log("right 2");
            }
            
        }
        */

        private void aiClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (!_aiPanelVisible) {
                if (_uiShown) {
                    Close();
                }
                var uiView = UIView.GetAView();
                var aiPanel = uiView.AddUIComponent(typeof(AIPanel));
                _aiPanelVisible = true;
            } else {
                HideAIPanel();
            }
        }

        private void tmClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (_aiPanelVisible) {
                HideAIPanel();
            }
            if (!_uiShown) {
                Show();
            } else {
                Close();
            }
        }

        public bool isVisible()
        {
            return _uiShown;
        }

        public void Show()
        {
            var uiView = UIView.GetAView();

            uiView.AddUIComponent(typeof(UITrafficManager));

            LoadingExtension.Instance.SetToolMode(TrafficManagerMode.TrafficLight);

            _uiShown = true;
        }

        public void Close()
        {
            var uiView = UIView.GetAView();

            var trafficManager = uiView.FindUIComponent("UITrafficManager");

            if (trafficManager != null) {
                UIView.Destroy(trafficManager);
            }

            UITrafficManager.uistate = UITrafficManager.UIState.None;
            TrafficLightTool.setToolMode(TrafficLightTool.ToolMode.None);
            LoadingExtension.Instance.SetToolMode(TrafficManagerMode.None);

            _uiShown = false;
        }

        public static void HideAIPanel()
        {
            var uiView = UIView.GetAView();
            var aiPanel = uiView.FindUIComponent("AIPanel");
            if (aiPanel != null) {
                UIView.Destroy(aiPanel);
            }
            _aiPanelVisible = false;
        }


    }
}