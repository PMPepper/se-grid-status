using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls
{
    public class Row : View
    {

        public Row() : base(ViewDirection.Row)
        {
            Pixels = Vector2.Zero;
            Flex = new Vector2(1, 0);
            Gap = GridStatusApp.DefaultSpace;
            Alignment = ViewAlignment.Center;
        }
    }
}
