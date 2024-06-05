using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public static class Settings
    {
        public static readonly int LOG_LEVEL = 1;//messages with logPriority >= this will get logged, less than will be ignored
        public static readonly int CLIENT_OUTPUT_LOG_LEVEL = 2;//messages with logPriority >= this will get output to clients
    }
}
