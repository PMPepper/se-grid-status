using Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls;
using Lima.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.ModAPI;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    [XmlInclude(typeof(ShieldStatusEntry))]
    [XmlInclude(typeof(InventoryStatusEntry))]
    [XmlInclude(typeof(OxygenStatusEntry))]
    //TODO [XmlInclude(typeof(HydrogenStatusEntry))]
    public abstract class AStatusEntry
    {
        [XmlIgnore]
        abstract public string Type { get; }
        [XmlAttribute]
        abstract public string Name { get; set; }

        [XmlIgnore]
        public View View { get; protected set; }
        abstract public View Init(GridStatusApp app, IMyCubeBlock block, IMyTextSurface surface);
        abstract public void Update(StringBuilder hudMessageText);
        abstract public void Dispose();

        abstract public void BlockAdded(IMySlimBlock block);
        abstract public void BlockRemoved(IMySlimBlock block);
        abstract public void GridChanged(IMyCubeGrid newGrid);

        public static List<SelectOption<Func<AStatusEntry>>> EntryTypeOptions = new List<SelectOption<Func<AStatusEntry>>>() {
            new SelectOption<Func<AStatusEntry>>(() => new InventoryStatusEntry(), InventoryStatusEntry.TypeName),
            new SelectOption<Func<AStatusEntry>>(() => new OxygenStatusEntry(), OxygenStatusEntry.TypeName),
            new SelectOption<Func<AStatusEntry>>(() => new ShieldStatusEntry(), ShieldStatusEntry.TypeName)

        };
    }
}
