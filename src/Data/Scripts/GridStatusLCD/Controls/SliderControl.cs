using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    class SliderControl : View
    {
        public const float FontSize = 0.6f;
        public Slider Slider { get; }
        public Label Label { get; }

        public string LabelText { get { return Label?.Text; } set { if (Label != null) { Label.Text = value; } } }
        public float Value { get { return Slider.Value; } set { Slider.Value = value; } }
        public float MinValue { get { return Slider.MinValue; } set { Slider.MinValue = value; } }
        public float MaxValue { get { return Slider.MaxValue; } set { Slider.MaxValue = value; } }

        public SliderControl(string label, float minValue, float maxValue, Action<float> onChange, float value = 0) : base()
        {
            Label = new Label(label, FontSize, VRage.Game.GUI.TextPanel.TextAlignment.LEFT);
            Label.Flex = new Vector2(1, 0);

            Slider = new Slider(minValue, maxValue, onChange);
            Slider.Value = value;
            Slider.Flex = new Vector2(1, 0);

            AddChild(Label);
            AddChild(Slider);

            Direction = ViewDirection.Column;
            Pixels = this.GetContentSize();
            Flex = new Vector2(1, 0);
            Padding = new Vector4(GridStatusApp.DefaultSpace, 0, GridStatusApp.DefaultSpace, 0);

            //TODO optional numeric controls 
        }
    }
}
