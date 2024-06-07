using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public class GridStatusLCDConfig
    {
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
