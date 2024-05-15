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
    abstract class ABlockFilterStatusEntry : IStatusEntry
    {
        private IMyCubeBlock _Block;
        public IMyCubeBlock Block { get { return _Block; } protected set {

                _Block = value;
                
                if(value != null)
                {
                    TerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
                }
        } }
        public MyCubeGrid Grid { get { return Block?.CubeGrid as MyCubeGrid; } }

        private StringMatcher BlockNameFilterMatcher = new StringMatcher("");
        private StringMatcher GroupNameFilterMatcher = new StringMatcher("");
        private StringMatcher GridNameFilterMatcher = new StringMatcher("");

        public string BlockNameFilter { get { return BlockNameFilterMatcher.Value; } set { BlockNameFilterMatcher.Value = value; } }
        public string GroupNameFilter { get { return GroupNameFilterMatcher.Value; } set { if (GroupNameFilterMatcher.Value != value) { GroupNameFilterMatcher.Value = value; UpdateGroups(); } } }
        //a value of "" = self
        public string GridNameFilter { get { return GridNameFilterMatcher.Value; } set { GridNameFilterMatcher.Value = value; } }

        //Reuse objects to reduce allocations
        public List<IMyCubeBlock> FilteredBlocks { get; private set; } = new List<IMyCubeBlock>();
        private List<IngameIMyBlockGroup> BlockGroups = new List<IngameIMyBlockGroup>();
        private List<MyCubeGrid> ConnectedGrids = new List<MyCubeGrid>();
        private List<MyCubeGrid> ValidGrids = new List<MyCubeGrid>();
        private HashSet<IMyCubeBlock> AddedBlocks = new HashSet<IMyCubeBlock>();

        private IMyGridTerminalSystem _TerminalSystem;
        public IMyGridTerminalSystem TerminalSystem
        {
            get { return _TerminalSystem; }
            private set
            {
                if (_TerminalSystem == value)
                {
                    return;
                }

                if (_TerminalSystem != null)
                {
                    _TerminalSystem.GroupAdded -= OnGroupAdded;
                    _TerminalSystem.GroupRemoved -= OnGroupRemoved;
                }

                _TerminalSystem = value;

                if (value != null)
                {
                    value.GroupAdded += OnGroupAdded;
                    value.GroupRemoved += OnGroupRemoved;
                }

                UpdateGroups();
            }
        }

        public abstract void BlockAdded(IMySlimBlock block);
        public abstract void BlockRemoved(IMySlimBlock block);
        virtual public void Dispose()
        {
            TerminalSystem = null;
        }
        public void GridChanged(IMyCubeGrid newGrid)
        {
            //update terminal system
            TerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
        }
        public abstract View Init(GridStatusApp app, IMyCubeBlock block, IMyTextSurface surface);
        virtual public void Update(StringBuilder hudMessageText)
        {
            FilteredBlocks.Clear();
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
                                FilteredBlocks.Add(block);
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
                                FilteredBlocks.Add(block);
                            }
                        }
                    }
                }
            }
        }

        protected virtual bool IsPotentiallyValidBlock(IMyCubeBlock block)
        {
            return BlockNameFilterMatcher.Test((block as IMyTerminalBlock).CustomName);
        }

        public bool TestBlockNameFilter(string str)
        {
            return BlockNameFilterMatcher.Test(str);
        }

        public bool TestGroupNameFilter(string str)
        {
            return GroupNameFilterMatcher.Test(str);
        }

        public bool TestGridNameFilter(string str)
        {
            return GridNameFilterMatcher.Test(str);
        }

        //private methods
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
            MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"Group removed: {obj.Name}");
            UpdateGroups();
        }

        private void OnGroupAdded(IMyBlockGroup obj)
        {
            MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"Group added: {obj.Name}");
            UpdateGroups();
        }
    }
}
