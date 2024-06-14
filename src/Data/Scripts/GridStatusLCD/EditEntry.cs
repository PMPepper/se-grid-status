using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public class EditEntry : View
    {
        public AStatusEntry Entry { get; }

        public EditEntry(AStatusEntry entry, Action onMoveUp, Action onMoveDown, Action onRemove, Action onEdit) : base(ViewDirection.Row)
        {
            Entry = entry;

            Direction = ViewDirection.Column;
            BgColor = GridStatusApp.BgCol;
            Padding = GridStatusApp.DefaultSpacing;
            Gap = GridStatusApp.DefaultSpace;
            Flex = new Vector2(1, 0);

            //Label
            var label = new Label(entry.Type);
            label.Flex = new Vector2(1, 0);
            label.Pixels = new Vector2(0, label.Pixels.Y);
            label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;

            //Controls container
            var controlsContainer = new View();
            controlsContainer.Gap = GridStatusApp.DefaultSpace;
            controlsContainer.Direction = ViewDirection.Row;

            //Controls
            var editName = new TextField();
            editName.Flex = new Vector2(1, 0);
            editName.Text = entry.Name;
            editName.OnSubmit = (newValue) => {
                entry.Name = newValue;
            };

            var editBtn = new Button("Edit", onEdit);
            StyleBtn(editBtn);
            
            var upBtn = new Button("Up", onMoveUp);
            StyleBtn(upBtn);
            upBtn.Disabled = onMoveUp == null;

            var downBtn = new Button("Down", onMoveDown);
            StyleBtn(downBtn);
            downBtn.Disabled = onMoveDown == null;

            var deleteBtn = new Button("X", onRemove);
            StyleBtn(deleteBtn);
            deleteBtn.Disabled = onRemove == null;

            AddChild(label);
            AddChild(controlsContainer);
            controlsContainer.AddChild(editName);
            controlsContainer.AddChild(editBtn);
            controlsContainer.AddChild(upBtn);
            controlsContainer.AddChild(downBtn);
            controlsContainer.AddChild(deleteBtn);

            controlsContainer.Flex = new Vector2(1, 0);
            controlsContainer.Pixels = new Vector2(0, editName.OuterHeight());

            Pixels = new Vector2(0, label.OuterHeight() + Gap + controlsContainer.OuterHeight() + Padding.Y + Padding.W);
        }

        private static void StyleBtn(Button btn)
        {
            btn.Flex = new Vector2(0, 0);
            btn.Pixels = new Vector2(50, btn.Pixels.Y);

            btn.Margin = VRageMath.Vector4.Zero;
        }

        public void Dispose()
        {

        }
    }
}
