using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public static class Constants
    {
        
        public static readonly Guid GridStatusLCDStateGUID = new Guid("de4b3d5d-77fe-48e0-baa3-4c7fe9e1d81d");
        
        public static bool IsDedicated => MyAPIGateway.Utilities.IsDedicated;
        public static bool IsServer => MyAPIGateway.Multiplayer.IsServer;
        public static bool IsMultiplayer => MyAPIGateway.Multiplayer.MultiplayerActive;
        public static bool IsClient => !(IsServer && IsDedicated);

        public static string IniSection = "GridStatus";
        public static string IniKey = "state";
    }
}
