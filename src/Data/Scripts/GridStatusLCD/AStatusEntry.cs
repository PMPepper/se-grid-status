using Lima.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    abstract class AStatusEntry
    {
        abstract public View Init(GridStatusApp app, IMyCubeBlock block, IMyTextSurface surface);
        abstract public void Update(StringBuilder hudMessageText);
        abstract public void Dispose();

        abstract public void BlockAdded(IMySlimBlock block);
        abstract public void BlockRemoved(IMySlimBlock block);
        abstract public void GridChanged(IMyCubeGrid newGrid);
    }
}
