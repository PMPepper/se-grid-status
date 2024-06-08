using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    class SelectControl<T> : View
    {
        public const float FontSize = 0.6f;

        private Label Label { get; }
        private Selector Select { get; set; }
        private Label SelectLabel { get; set; }

        public string LabelText { get { return Label?.Text; } set { if (Label != null) { Label.Text = value; } } }

        private int _SelectedIndex;

        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set
            {
                if ((value != -1 && Options == null) || value < -1 || (Options != null && value >= Options.Count))
                {
                    throw new Exception("Invalid index");
                }

                _SelectedIndex = value;

                if(Select != null)
                {
                    Select.Selected = value;

                    SelectLabel.Text = value == -1 ? "" : Options[value].Label;
                }
            }
        }

        public T Value
        {
            get { return SelectedIndex == -1 || Options == null ? default(T) : Options[SelectedIndex].Value; }
            set
            {
                SelectedIndex = _Options.FindIndex((option) => value.Equals(option.Value));
            }
        }

        private List<SelectOption<T>> _Options;
        public List<SelectOption<T>> Options { 
            get { return _Options; } 
            set {
                T currentValue = Value;

                _Options = value;

                if(Select != null)
                {
                    RemoveChild(Select);
                }

                Select = new Selector(value.Select(option => option.Label).ToList(), OnChangeHandler);
                SelectLabel = new Label(Select.Children[1]);

                AddChild(Select);

                Value = currentValue;
            }
        }

        private Action<T> OnChange;

        public SelectControl(string label, List<SelectOption<T>> options, Action<T> onChange) : base()
        {
            OnChange = onChange;

            //Label
            Label = new Label(label, FontSize, VRage.Game.GUI.TextPanel.TextAlignment.LEFT);
            Label.Flex = new Vector2(1, 0);

            AddChild(Label);

            Options = options;

            //Base props
            Direction = ViewDirection.Column;
            Pixels = this.GetContentSize();
            Flex = new Vector2(1, 0);
            Padding = new Vector4(GridStatusApp.DefaultSpace, 0, GridStatusApp.DefaultSpace, 0);
        }

        private void OnChangeHandler(int index, string option)
        {
            SelectedIndex = index;

            OnChange(Value);
        }
    }

    public struct SelectOption<T>
    {
        public T Value;
        public string Label;

        public SelectOption(T value)
        {
            Value = value;
            Label = value.ToString();
        }

        public SelectOption(T value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}
