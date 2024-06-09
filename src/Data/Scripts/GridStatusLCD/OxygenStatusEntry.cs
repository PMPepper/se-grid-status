using Lima.API;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public class OxygenStatusEntry : ABlockFilterStatusEntry
    {
        public const string TypeName = "Oxygen storage";

        [XmlIgnore]
        public override string Type
        {
            get
            {
                return TypeName;
            }
        }
        private string _Heading = "Oxygen";
        [XmlAttribute]
        override public string Name { get { return _Heading; } set { if (Label != null) { Label.Text = value; } _Heading = value; } }
        [XmlAttribute]
        public bool ShowOnHUD = true;

        private Label Label;
        private ProgressBar StatusBar;

        //private static MyDefinitionId HydrogenId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");
        private static MyDefinitionId OxygenId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");

        public override void BlockAdded(IMySlimBlock block)
        {
            
        }

        public override void BlockRemoved(IMySlimBlock block)
        {
            
        }

        public override View Init(GridStatusApp app, IMyCubeBlock block, IMyTextSurface surface)
        {
            Block = block;

            View = new View();
            View.Flex = new Vector2(1, 0);
            View.Padding = new Vector4(8);
            View.BgColor = GridStatusApp.BgCol;

            Label = new Label(Name);
            Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            Label.Margin = Vector4.Zero;

            StatusBar = new ProgressBar(0, 1, false, 1);
            StatusBar.Value = 0;
            StatusBar.Label.Text = "";
            StatusBar.Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.RIGHT;

            StatusBar.Pixels = new Vector2(0, StatusBar.Pixels.Y);
            StatusBar.Flex = new Vector2(1, 0);

            View.AddChild(Label);
            View.AddChild(StatusBar);

            View.Pixels = new Vector2(0, Label.OuterHeight() + StatusBar.OuterHeight() + View.Padding.Y + View.Padding.W);

            return View;
        }

        override public void Update(StringBuilder hudMessageText)
        {
            base.Update(hudMessageText);

            double totalCapacity = 0;
            double containsGas = FilteredBlocks.Sum((block) => {
                float capacity = 0;
                double filledRatio = 0;

                if(block is IMyGasTank)
                {
                    var tank = block as IMyGasTank;
                    capacity = tank.Capacity;
                    filledRatio = tank.FilledRatio;
                } else if(block is IMyCockpit)
                {
                    var cockpit = block as IMyCockpit;
                    capacity = cockpit.OxygenCapacity;
                    filledRatio = cockpit.OxygenFilledRatio;
                }
                
                totalCapacity += capacity;
                
                return capacity * filledRatio;
            });

            string statusStr = $"{Math.Floor(containsGas)}/{Math.Floor(totalCapacity)}";

            if (StatusBar.Value != containsGas || StatusBar.MaxValue != totalCapacity)
            {
                StatusBar.Value = (float)Math.Min(totalCapacity, containsGas);
                StatusBar.MaxValue = (float)totalCapacity;

                StatusBar.Label.Text = statusStr;
            }

            //HUD text
            if (ShowOnHUD)
            {
                hudMessageText.AppendLine($"{Name}: {statusStr}");

                TextUtils.TextBar(hudMessageText, (float)(containsGas / totalCapacity), 20);
                hudMessageText.Append('\n');
            }
        }


        protected override bool IsPotentiallyValidBlock(IMyCubeBlock block)
        {
            if(base.IsPotentiallyValidBlock(block))
            {
                if(block is IMyGasTank)
                {
                    return (block as IMyGasTank).Components.Get<MyResourceSinkComponent>().AcceptedResources.Contains(OxygenId);
                }

                return block is IMyCockpit;
            }

            return false;
        }
    }
}

