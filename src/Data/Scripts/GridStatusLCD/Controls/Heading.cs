using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    public class Heading : View
    {
        public const float FontSize = 0.6f;

        public Label Label { get; }

        public string Text { get { return Label?.Text; } set { if (Label != null) { Label.Text = value; } } }

        public Heading(string text) : base()
        {
            Label = new Label(text, FontSize, VRage.Game.GUI.TextPanel.TextAlignment.LEFT);

            AddChild(Label);

            Padding = GridStatusApp.DefaultSpacing;
            Pixels = new Vector2(0, Label.Pixels.Y + Padding.Y + Padding.W);
            Flex = new Vector2(1, 0);
            BgColor = GridStatusApp.BgCol;
        }
    }
}
