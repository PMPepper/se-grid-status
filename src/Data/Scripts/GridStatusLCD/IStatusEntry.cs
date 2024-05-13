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
    interface IStatusEntry
    {
        View Init(GridStatusApp app, IMyCubeBlock block, IMyTextSurface surface);
        void Update(StringBuilder hudMessageText);
        void Dispose();

        void BlockAdded(IMySlimBlock block);
        void BlockRemoved(IMySlimBlock block);
        void GridChanged(IMyCubeGrid newGrid);
    }
}
