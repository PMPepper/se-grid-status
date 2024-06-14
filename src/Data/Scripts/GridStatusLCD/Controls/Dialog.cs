using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    public class Dialog : Column
    {
        private Heading Heading;
        public Column Content { get; private set; }
        private Row Actions;

        public Dialog(string title, IEnumerable<ActionType> actions) : base()
        {
            BgColor = Modal.BgCol;

            AddChild(Heading = new Heading(title));
            AddChild(Content = new Column());
            AddChild(Actions = new Row());

            Content.Flex = Vector2.One;

            foreach(var actionType in actions)
            {
                Actions.AddChild(new Button(actionType.Label, actionType.ClickHandler));
            }
        }
    }

    public struct ActionType
    {
        public string Label;
        public Action ClickHandler;

        public ActionType(string label, Action clickHandler)
        {
            Label = label;
            ClickHandler = clickHandler;
        }
    }
}
