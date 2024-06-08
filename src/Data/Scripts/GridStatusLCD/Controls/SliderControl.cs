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
        public const float InputHeight = 20;
        private Slider Slider { get; }
        private Label Label { get; }
        private TextField NumericField { get; }

        public string LabelText { get { return Label?.Text; } set { if (Label != null) { Label.Text = value; } } }

        private float _Value;
        public float Value { 
            get { 
                return Slider.Value; 
            } 
            set {
                //HOW IS THIS CALLED WHEN THERE IS NO SLIDER????
                value = value.Clamp(MinValue, MaxValue);
                _Value = value;

                if(Slider != null)
                {
                    Slider.Value = value;
                }

                UpdateNumericValue();
                
                if (OnChange != null)
                {
                    OnChange(value);
                }
            } 
        }
        private float _MinValue;
        public float MinValue { 
            get { return _MinValue; } 
            set { 
                _MinValue = value;
                FormatNumericField();
                if (Slider != null) { Slider.MinValue = value; }
                Value = Value;//trigger updating of value to ensure it is still clamped within valid range
            } 
        }

        private float _MaxValue;
        public float MaxValue { 
            get { return _MaxValue; } 
            set {
                _MaxValue = value;
                FormatNumericField();
                if (Slider != null) { Slider.MaxValue = value; }
                Value = Value;//trigger updating of value to ensure it is still clamped within valid range
            }
        }

        public Action<float> OnChange;
        private int NumberDecimalPlaces;

        public bool ShowNumericField { get { return NumericField?.Enabled ?? false; } set { if (NumericField != null) { NumericField.Enabled = value; } } }
        

        public SliderControl(string label, float minValue, float maxValue, Action<float> onChange, float value = 0, bool hideNumberField = false, int numberDecimalPlaces = 0) : base()
        {
            _MinValue = minValue;
            _MaxValue = maxValue;
            _Value = value;
            OnChange = onChange;
            NumberDecimalPlaces = numberDecimalPlaces;

            //Label
            Label = new Label(label, FontSize, VRage.Game.GUI.TextPanel.TextAlignment.LEFT);
            Label.Flex = new Vector2(1, 0);

            //Slider
            Slider = new Slider(minValue, maxValue, OnSliderChangeHandler);
            Slider.Flex = new Vector2(1, 0);
            Slider.Pixels = new Vector2(0, Slider.Pixels.Y);

            //Slider row
            var SliderRow = new View();
            SliderRow.Direction = ViewDirection.Row;
            SliderRow.Flex = new Vector2(1, 0);
            SliderRow.Gap = GridStatusApp.DefaultSpace;
            SliderRow.Pixels = new Vector2(0, InputHeight);
            SliderRow.Alignment = ViewAlignment.Center;

            AddChild(Label);
            AddChild(SliderRow);

            SliderRow.AddChild(Slider);

            //Numeric field
            NumericField = new TextField();
            NumericField.Text = value.ToString();
            NumericField.Flex = Vector2.Zero;
            NumericField.Enabled = !hideNumberField;

            FormatNumericField();

            NumericField.OnSubmit = OnNumericValueChangeHandler;

            SliderRow.AddChild(NumericField);

            //Base props
            Direction = ViewDirection.Column;
            Pixels = this.GetContentSize();
            Flex = new Vector2(1, 0);
            Padding = new Vector4(GridStatusApp.DefaultSpace, 0, GridStatusApp.DefaultSpace, 0);
        }

        private void OnSliderChangeHandler(float newValue)
        {
            Value = newValue;
        }

        private void OnNumericValueChangeHandler(string newValue)
        {
            float result;

            if (float.TryParse(newValue, out result))
            {
                Value = result;//if value is valid number, set
            }
            else
            {
                UpdateNumericValue();//reset to correct value
            }
        }

        private void UpdateNumericValue()
        {
            if (NumericField != null)
            {
                NumericField.Text = Math.Round(Value, NumberDecimalPlaces).ToString();
            }
        }

        private void FormatNumericField()
        {
            if(NumericField != null)
            {
                int magnitude = Math.Max(
                    MathUtils.Magnitude(MinValue),
                    MathUtils.Magnitude(MaxValue)
                );

                float charWidth = 9;
                float charsWidth = magnitude * charWidth + (NumberDecimalPlaces > 0 ? (NumberDecimalPlaces * charWidth) + 5 : 0);

                NumericField.Pixels = new Vector2(charsWidth + GridStatusApp.DefaultSpace, InputHeight);
            }
        }
    }
}
