using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    public class ModalDialog : Modal
    {


        public ModalDialog(GridStatusApp app, string title, IEnumerable<ActionType> actions) : base(app, new Dialog(title, actions))
        {
            Content = (Content as Dialog).Content;
        }
    }
}
