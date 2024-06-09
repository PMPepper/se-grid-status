using Lima.API;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.ModAPI;
using VRageMath;
using IngameIMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;
using IngameMyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public class InventoryStatusEntry : AStatusEntry
    {
        public const string TypeName = "Inventory status"; 
        [XmlAttribute]
        override public string Name { get { return _Name; } set { if (Label != null) { Label.Text = value; } _Name = value; } }
        [XmlIgnore]
        public override string Type
        {
            get
            {
                return TypeName;
            }
        }

        [XmlAttribute]
        public bool ShowOnHUD = true;

        public BlockFilter BlockFilter = new BlockFilter();

        //"MyObjectBuilder_Ore/Ice";
        [XmlAttribute]
        public string ItemType = "Ore";
        [XmlAttribute]
        public string ItemSubtype = "Ice";
        [XmlAttribute]
        public float InvFull = 1000000;
        
        //private vars
        private string _Name = "Inventory";

        private Label Label;
        private ProgressBar StatusBar;

        //Reuse objects to reduce allocations
        private List<IngameMyInventoryItem> Items = new List<IngameMyInventoryItem>();

        override public void BlockAdded(IMySlimBlock block) { }
        override public void BlockRemoved(IMySlimBlock block) { }
        public override void Dispose()
        {
            if (BlockFilter != null)
            {
                BlockFilter.Dispose();
                BlockFilter = null;
            }
        }
        override public void GridChanged(IMyCubeGrid newGrid)
        {
            if (BlockFilter != null)
            {
                BlockFilter.GridChanged(newGrid);
            }
        }

        override public View Init(GridStatusApp app, IMyTerminalBlock block, IMyTextSurface surface)
        {
            if (BlockFilter != null)
            {
                BlockFilter.Block = block;
                BlockFilter.BlockApplicableTest = BlockApplicableTest;
            }

            View = new View();
            View.Flex = new Vector2(1, 0);
            View.Padding = new Vector4(8);
            View.BgColor = GridStatusApp.BgCol;

            Label = new Label(Name);
            Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            Label.Margin = Vector4.Zero;

            StatusBar = new ProgressBar(0, InvFull, false, 1);
            StatusBar.Value = 0;
            StatusBar.Label.Text = "";
            StatusBar.Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.RIGHT;

            StatusBar.Pixels = new Vector2(0, StatusBar.Pixels.Y);
            StatusBar.Flex = new Vector2(1, 0);

            View.AddChild(Label);
            View.AddChild(StatusBar);

            View.Pixels = new Vector2(0, Label.OuterHeight() + StatusBar.OuterHeight() + View.Padding.Y + View.Padding.W);

            return View;
        }

        override public void Update(StringBuilder hudMessageText)
        {
            float inventoryContains = 0;

            if (BlockFilter != null)
            {
                BlockFilter.Update();

                //InventoryBlocks list will now have up to date list of filtered blocks
                inventoryContains = BlockFilter.FilteredBlocks.Sum((block) =>
                {
                    Items.Clear();
                    var inventory = block.GetInventory();
                    inventory.GetItems(Items);

                    return Items.Sum((item) =>
                    {
                        //hudMessageText.AppendLine($"{item.Type.TypeId}, {item.Type.SubtypeId}");
                        return IsCorrectItemType(item.Type) ? (float)item.Amount : 0;
                    });
                });
            }

            string statusStr = $"{inventoryContains}/{InvFull}";

            if (StatusBar.Value != inventoryContains || StatusBar.MaxValue != InvFull)
            {
                //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"shield status changed?: {shieldStatus}, {ShieldStatusBar.Value}/{ShieldStatusBar.MaxValue}");

                StatusBar.Value = Math.Min(InvFull, inventoryContains);
                StatusBar.MaxValue = InvFull;

                StatusBar.Label.Text = statusStr;
            }

            //HUD text
            if (ShowOnHUD)
            {
                //hudMessageText.AppendLine($"{Heading}: TODO Grids = {ValidGrids.Count}, Groups = {BlockGroups.Count}, Blocks = {InventoryBlocks.Count}");
                hudMessageText.AppendLine($"{Name}: {statusStr}");

                TextUtils.TextBar(hudMessageText, inventoryContains / InvFull, 20);
                hudMessageText.Append('\n');
            }
        }


        //Private methods

        //Checks if block can store requested item, and name matches filter
        private bool BlockApplicableTest(IMyTerminalBlock block)
        {
            if (block.HasInventory)
            {
                //Check this inventory is capable of storing the requested item
                var inventory = block.GetInventory();

                var acceptedItems = new List<VRage.Game.ModAPI.Ingame.MyItemType>();
                inventory.GetAcceptedItems(acceptedItems);

                foreach (var itemType in acceptedItems)
                {
                    //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"Accepted item: {itemType.TypeId} / {itemType.SubtypeId}");
                    if (IsCorrectItemType(itemType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsCorrectItemType(VRage.Game.ModAPI.Ingame.MyItemType itemType)
        {
            return itemType.TypeId == $"MyObjectBuilder_{ItemType}" && (string.IsNullOrEmpty(ItemSubtype) || itemType.SubtypeId == ItemSubtype);
        }
    }
}
