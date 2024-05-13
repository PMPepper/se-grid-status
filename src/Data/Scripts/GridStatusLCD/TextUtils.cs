using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    static class TextUtils
    {
        public static string TextBar(float value, int width)
        {
            var sb = new StringBuilder();

            TextBar(sb, value, width);

            return sb.ToString();
        }

        public static void TextBar(StringBuilder sb, float value, int width)
        {
            int filledChars = (int)Math.Ceiling((float)width * value);

            sb.Append('{');

            for (int i = 0; i < width; i++)
            {
                //:/. works space wise, but looks weird
                if(i >= filledChars)
                {
                    sb.Append("..");
                } else
                {
                    sb.Append("[]");
                }
            }

            sb.Append('}');
        }
    }
}
