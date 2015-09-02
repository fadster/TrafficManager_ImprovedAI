using System;
using System.Collections.Generic;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

// Adapted from Road Assistant mod.

namespace TrafficManager_ImprovedAI
{
    public class UILabelledBox : UIPanel
    {

        private UILabel boxLabel;
        private UICheckBox checkBox;

        public UIPanel Parent { get; set; }

        public UICheckBox CheckBox
        {
            get { return checkBox; }
        }

        public string LabelText
        {
            get { return boxLabel.text; }
            set { boxLabel.text = value; }
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

        // need to handle tick/untick events


        public override void Awake()
        {
            base.Awake();

            boxLabel = AddUIComponent<UILabel>();
            checkBox = AddUIComponent<UICheckBox>();

            height = 40;
            width = 200;
            LabelText = "(None)";
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

            boxLabel.relativePosition = new Vector3(8, 0);
            boxLabel.text = LabelText;
            boxLabel.autoSize = true;

            checkBox.relativePosition = new Vector3(20 + boxLabel.width, -1f);
            checkBox.height = 20;
            checkBox.width = 20;

            UISprite uncheckSprite = checkBox.AddUIComponent<UISprite>();
            uncheckSprite.height = 20;
            uncheckSprite.width = 20;
            uncheckSprite.relativePosition = new Vector3(0, 0);
            uncheckSprite.spriteName = "check-unchecked";
            uncheckSprite.isVisible = true;

            UISprite checkSprite = checkBox.AddUIComponent<UISprite>();
            checkSprite.height = 20;
            checkSprite.width = 20;
            checkSprite.relativePosition = new Vector3(0, 0);
            checkSprite.spriteName = "check-checked";

            checkBox.checkedBoxObject = checkSprite;


        }
    }
}
