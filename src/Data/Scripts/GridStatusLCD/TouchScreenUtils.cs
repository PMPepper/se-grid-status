using Lima.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public static class TouchScreenUtils
    {
        //left, top, right, bottom
        //X, Y, Z, W
        public static float OuterHeight(this ElementBase element)
        {
            return element.Margin.Y + element.Margin.W + element.Pixels.Y;
        }
    }
}
