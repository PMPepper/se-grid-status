using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public enum HUDVisibleWhen
    {
        Always,
        InSeat
    }

    public class GridStatusLCDConfig
    {
        public PermissionLevel ViewPermission = PermissionLevel.TerminalAccess;
        public PermissionLevel EditPermission = PermissionLevel.BlockOwner;
        public PermissionLevel HUDViewPermission = PermissionLevel.TerminalAccess;
        public bool HUDMessageEnabled = true;
        public Vector2 HUDMessagePosition;
        public double HUDMessageScale { get; set; } = 1;
        public HUDVisibleWhen HUDVisibleWhen;
        public List<AStatusEntry> Entries { get; set; } = new List<AStatusEntry>();

        public void Dispose()
        {
            foreach (var entry in Entries)
            {
                if (entry != null)
                {
                    entry.Dispose();
                }
            }

            Entries.Clear();
        }
    }
}
