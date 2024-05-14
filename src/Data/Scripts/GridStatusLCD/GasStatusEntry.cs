using Lima.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    class GasStatusEntry : ABlockFilterStatusEntry
    {
        private string _Heading = "Gas";
        public string Heading { get { return _Heading; } set { if (Label != null) { Label.Text = value; } _Heading = value; } }
        public bool ShowOnHUD = true;

        private View View;

        private Label Label;
        private ProgressBar StatusBar;

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
            View.Pixels = new Vector2(0, 36);

            Label = new Label(Heading);
            Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            Label.Margin = Vector4.UnitY * 8;

            StatusBar = new ProgressBar(0, 1, false, 1);
            StatusBar.Value = 0;
            StatusBar.Label.Text = "";
            StatusBar.Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.RIGHT;

            StatusBar.Pixels = new Vector2(0, StatusBar.Pixels.Y);
            StatusBar.Flex = new Vector2(1, 0);

            View.AddChild(Label);
            View.AddChild(StatusBar);

            return View;
        }

        override public void Update(StringBuilder hudMessageText)
        {
            base.Update(hudMessageText);

            //TODO
        }

            //TODO actually check the block can contain gas?
            /*protected override bool IsPotentiallyValidBlock(IMyCubeBlock block)
            {
                throw new NotImplementedException();
            }*/
        }
}
