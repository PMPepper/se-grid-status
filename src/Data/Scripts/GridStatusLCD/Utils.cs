using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public static class Utils
    {
        public static void ShowNotification(string msg, int disappearTime = 10000, string font = MyFontEnum.Red)
        {
            if (MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification(msg, disappearTime, font);
        }

        public static void WriteToClient(string msg)
        {
            if (Constants.IsClient)
            {
                MyAPIGateway.Utilities.ShowMessage("[GSLCD]: ", msg);
            }
        }

        public static void Log(string msg, int logPriority = 0)
        {
            if (logPriority >= Settings.LOG_LEVEL)
            {
                MyLog.Default.WriteLine($"[GSLCD]: {msg}");
            }

            if (logPriority >= Settings.CLIENT_OUTPUT_LOG_LEVEL)
            {
                MyAPIGateway.Utilities.ShowMessage($"[GSLCD={logPriority}]: ", msg);
            }
        }

        public static void LogException(Exception e, int depth = 0)
        {
            Log($"Exception({depth}) message = {e.Message}, Stack trace:\n{e.StackTrace}", 3);

            if(e.InnerException != null)
            {
                LogException(e.InnerException, ++depth);
            }
        }
    }
}
