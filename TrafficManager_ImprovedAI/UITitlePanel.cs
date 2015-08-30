using System;
using System.Collections.Generic;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace TrafficManager_ImprovedAI
{
    // Shamelessly ripped from: https://github.com/justacid/Skylines-ExtendedPublicTransport/blob/master/ExtendedPublicTransportUI/UITitleContainer.cs

    /// <summary>
    /// UIComponent inheriting from UIPanel. Creates top panel with sprite icon, label and close button. Requires the following to be set:
    /// 
    /// TitleText - Text to display on bar.
    /// IconSprite - The sprite icon to be used.
    /// </summary>
    public class UITitlePanel : UIPanel
    {
        private UISprite iconSprite;
        private UILabel titleLabel;
        private UIButton closeButton;
        private UIDragHandle dragHandle;

        public UIPanel Parent { get; set; }
        public string IconSprite { get; set; }

        public string TitleText
        {
            get { return titleLabel.text; }
            set { titleLabel.text = value; }
        }

        public float Height
        {
            get { return height; }
            set { height = value; }
        }

        public float Width
        {
            get { return width; }
            set { width = value; }
        }

        public override void Awake()
        {
            base.Awake();

            iconSprite = AddUIComponent<UISprite>();
            titleLabel = AddUIComponent<UILabel>();
            closeButton = AddUIComponent<UIButton>();
            dragHandle = AddUIComponent<UIDragHandle>();

            height = 40;
            width = 450;
            TitleText = "(None)";
            IconSprite = "";
        }

        public override void Start()
        {
            base.Start();

            if (Parent == null)
            {
                UnityEngine.Debug.Log(String.Format("Parent not set in {0}", this.GetType().Name));
                return;
            }

            width = Parent.width;
            isVisible = true;
            canFocus = true;
            isInteractive = true;

            dragHandle.width = width - 50;
            dragHandle.height = height;
            dragHandle.relativePosition = Vector3.zero;
            dragHandle.target = Parent;

            iconSprite.spriteName = IconSprite;
            iconSprite.size = new Vector2(40,40);
            iconSprite.relativePosition = new Vector3(0, 0);

            titleLabel.relativePosition = new Vector3(180, 13);
            titleLabel.text = TitleText;

            closeButton.relativePosition = new Vector3(width - 35, 2);
            closeButton.normalBgSprite = "buttonclose";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.eventClick += (component, param) => Parent.Hide();
        }
    }
}
