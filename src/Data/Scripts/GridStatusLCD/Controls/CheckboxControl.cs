using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    class CheckboxControl : View
    {
        public const float FontSize = 0.6f;
        public Checkbox Checkbox { get; }
        public Label Label { get; }

        public string LabelText { get { return Label?.Text; } set { if (Label != null) { Label.Text = value; } } }
        public bool Value { get { return Checkbox.Value; } set { Checkbox.Value = value; } }

        public CheckboxControl(string label, Action<bool> onChange, bool value = false) : base(ViewDirection.Row)
        {
            Label = new Label(label, FontSize, VRage.Game.GUI.TextPanel.TextAlignment.LEFT);
            Label.Flex = new Vector2(1);

            Checkbox = new Checkbox(onChange);
            Checkbox.Flex = new Vector2(0, 0);
            Checkbox.Pixels = new Vector2(Label.OuterHeight());
            Checkbox.Value = value;
            
            AddChild(Checkbox);
            AddChild(Label);
            
            Gap = GridStatusApp.DefaultSpace;
            Pixels = this.GetContentSize();
            Flex = new Vector2(1, 0);
            Padding = new Vector4(GridStatusApp.DefaultSpace, 0, GridStatusApp.DefaultSpace, 0);
        }


    }
}
