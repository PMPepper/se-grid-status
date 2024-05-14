using Lima.API;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;
using IngameIMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;
using IngameMyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    class InventoryStatusEntry : IStatusEntry
    {
        private string _Heading = "Ice";
        public string Heading { get { return _Heading; } set { if (Label != null) { Label.Text = value; } _Heading = value; } }
        public bool ShowOnHUD = true;

        private StringMatcher BlockNameFilterMatcher = new StringMatcher("*[ice]*");
        private StringMatcher GroupNameFilterMatcher = new StringMatcher("*[ice]");
        private StringMatcher GridNameFilterMatcher = new StringMatcher("*[test]*");

        public string BlockNameFilter { get { return BlockNameFilterMatcher.Value; } set { BlockNameFilterMatcher.Value = value; } }
        public string GroupNameFilter { get { return GroupNameFilterMatcher.Value; } set { GroupNameFilterMatcher.Value = value; } }
        //a value of "" = self
        public string GridNameFilter { get { return GridNameFilterMatcher.Value; } set { GridNameFilterMatcher.Value = value; } }

        //"MyObjectBuilder_Ore/Ice";
        public string ItemType = "Ore";
        public string ItemSubtype = "Ice";
        public float InvFull = 1000000;

        private IMyGridTerminalSystem _TerminalSystem;
        private IMyGridTerminalSystem TerminalSystem { get { return _TerminalSystem; } set {
                if(_TerminalSystem == value)
                {
                    return;
                }

                if(_TerminalSystem != null)
                {
                    _TerminalSystem.GroupAdded -= OnGroupAdded;
                    _TerminalSystem.GroupRemoved -= OnGroupRemoved;
                }
                
                _TerminalSystem = value;
                
                if(value != null)
                {
                    value.GroupAdded += OnGroupAdded;
                    value.GroupRemoved += OnGroupRemoved;
                }

                UpdateGroups();
        } }

        

        private GridStatusApp App;
        private IMyCubeBlock Block;
        private View View;

        private Label Label;
        private ProgressBar StatusBar;

        private MyCubeGrid Grid { get { return Block?.CubeGrid as MyCubeGrid;  } }

        //Reuse objects to reduce allocations
        private List<IMyCubeBlock> InventoryBlocks = new List<IMyCubeBlock>();
        private List<IngameIMyBlockGroup> BlockGroups = new List<IngameIMyBlockGroup>();
        private List<MyCubeGrid> ConnectedGrids = new List<MyCubeGrid>();
        private List<MyCubeGrid> ValidGrids = new List<MyCubeGrid>();
        private HashSet<IMyCubeBlock> AddedBlocks = new HashSet<IMyCubeBlock>();
        private List<IngameMyInventoryItem> Items = new List<IngameMyInventoryItem>();

        public void BlockAdded(IMySlimBlock block)
        {

        }

        public void BlockRemoved(IMySlimBlock block)
        {
            
        }

        public void Dispose()
        {
            TerminalSystem = null;
        }

        public void GridChanged(IMyCubeGrid newGrid)
        {
            //update terminal system
            TerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
        }

        public View Init(GridStatusApp app, IMyCubeBlock block, IMyTextSurface surface)
        {
            App = app;
            Block = block;
            TerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);

            View = new View();
            View.Flex = new Vector2(1, 0);
            View.Pixels = new Vector2(0, 36);

            Label = new Label(Heading);
            Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            Label.Margin = Vector4.UnitY * 8;

            StatusBar = new ProgressBar(0, InvFull, false, 1);
            StatusBar.Value = 0;
            StatusBar.Label.Text = "";
            StatusBar.Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.RIGHT;

            StatusBar.Pixels = new Vector2(0, StatusBar.Pixels.Y);
            StatusBar.Flex = new Vector2(1, 0);

            View.AddChild(Label);
            View.AddChild(StatusBar);


            return View;
        }

        public void Update(StringBuilder hudMessageText)
        {
            InventoryBlocks.Clear();
            AddedBlocks.Clear();
            ValidGrids.Clear();

            //Get valid grids
            if (GridNameFilter == "")
            {
                //just use own grid
                ValidGrids.Add(Grid);
            }
            else//has a grid name filter
            {
                //Get all potential grids...
                ConnectedGrids.Clear();
                Grid.GetConnectedGrids(GridLinkTypeEnum.Logical, ConnectedGrids);

                //...and filter on the name
                foreach (var grid in ConnectedGrids)
                {
                    if (GridNameFilterMatcher.Test(grid.DisplayName))
                    {
                        ValidGrids.Add(grid);
                    }
                }
            }
            
            if (ValidGrids.Count > 0)
            {
                if (!string.IsNullOrEmpty(GroupNameFilter))//if we have a group name filter
                {
                    foreach (var group in BlockGroups)//for each valid group
                    {
                        //check all blocks in the group and if they meet the name and item type filter, are in a valid grid AND are not already added to the list
                        group.GetBlocksOfType<IMyTerminalBlock>(null, (block) => {
                            if (!AddedBlocks.Contains(block) && ValidGrids.Contains(block.CubeGrid) && IsPotentiallyValidBlock(block))
                            {
                                //add to the list
                                InventoryBlocks.Add(block);
                                AddedBlocks.Add(block);
                            }

                            return false;
                        });
                    }
                }
                else
                {
                    //No group filter, just get all inventory blocks on valid grids..
                    foreach (var grid in ValidGrids)
                    {
                        foreach (var block in grid.Inventories)
                        {
                            
                            //..and check if they meet the name and item type filter
                            if (IsPotentiallyValidBlock(block))
                            {
                                InventoryBlocks.Add(block);
                            }
                        }
                    }
                }
            }

            string unit = "";

            //InventoryBlocks list will now have up to date list of filtered blocks
            float inventoryContains = InventoryBlocks.Sum((block) =>
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
                hudMessageText.AppendLine($"{Heading}: statusStr");

                TextUtils.TextBar(hudMessageText, inventoryContains / InvFull, 20);
                hudMessageText.Append('\n');
            }
        }


        //Private methods

        //Checks if block can store requested item, and name matches filter
        private bool IsPotentiallyValidBlock(IMyCubeBlock block)
        {
            if(block.HasInventory)
            {
                if (!BlockNameFilterMatcher.Test((block as IMyTerminalBlock).CustomName))
                {
                    return false;
                }


                //Check this inventory is capable of storing the requested item
                var inventory = block.GetInventory();

                var acceptedItems = new List<VRage.Game.ModAPI.Ingame.MyItemType>();
                inventory.GetAcceptedItems(acceptedItems);

                foreach(var itemType in acceptedItems)
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

        private void UpdateGroups()
        {
            BlockGroups.Clear();

            if (TerminalSystem != null && !string.IsNullOrEmpty(GroupNameFilter))
            {
                TerminalSystem.GetBlockGroups(BlockGroups, (group) => {
                    //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"Group: {group.Name}, is valid = {GroupNameFilterMatcher.Test(group.Name)}");
                    return GroupNameFilterMatcher.Test(group.Name);
                });
            }
        }

        //Event handlers
        private void OnGroupRemoved(IMyBlockGroup obj)
        {
            UpdateGroups();
        }

        private void OnGroupAdded(IMyBlockGroup obj)
        {
            UpdateGroups();
        }
    }
}
