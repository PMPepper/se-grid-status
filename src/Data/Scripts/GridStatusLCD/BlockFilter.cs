﻿using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.ModAPI;
using IngameIMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public class BlockFilter
    {
        private IMyTerminalBlock _Block;

        [XmlIgnore]
        public IMyTerminalBlock Block
        {
            get { return _Block; }
            set
            {
                _Block = value;

                if (value != null)
                {
                    TerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
                }
            }
        }

        [XmlIgnore]
        public MyCubeGrid Grid { get { return Block?.CubeGrid as MyCubeGrid; } }

        private StringMatcher BlockNameFilterMatcher = new StringMatcher("");
        private StringMatcher GroupNameFilterMatcher = new StringMatcher("");
        private StringMatcher GridNameFilterMatcher = new StringMatcher("");

        [XmlAttribute]
        public string BlockNameFilter { get { return BlockNameFilterMatcher.Value; } set { BlockNameFilterMatcher.Value = value; } }
        [XmlAttribute]
        public string GroupNameFilter { get { return GroupNameFilterMatcher.Value; } set { if (GroupNameFilterMatcher.Value != value) { GroupNameFilterMatcher.Value = value; groupsDirty = true; } } }

        [XmlAttribute]
        public string GridNameFilter { get { return GridNameFilterMatcher.Value; } set { GridNameFilterMatcher.Value = value; } }//a value of "" = self

        private bool groupsDirty = true;
        //Reuse objects to reduce allocations
        [XmlIgnore]
        public List<IMyTerminalBlock> FilteredBlocks { get; private set; } = new List<IMyTerminalBlock>();
        private List<IngameIMyBlockGroup> BlockGroups = new List<IngameIMyBlockGroup>();
        private List<MyCubeGrid> ConnectedGrids = new List<MyCubeGrid>();
        private List<MyCubeGrid> PrevConnectedGrids = new List<MyCubeGrid>();
        private List<MyCubeGrid> ValidGrids = new List<MyCubeGrid>();
        private HashSet<IMyTerminalBlock> AddedBlocks = new HashSet<IMyTerminalBlock>();
        private HashSet<MyCubeGrid> CompareGridsSet = new HashSet<MyCubeGrid>();

        private IMyGridTerminalSystem _TerminalSystem;

        [XmlIgnore]
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

        [XmlIgnore]
        public Func<IMyTerminalBlock, bool> BlockApplicableTest { get; set; }

        public void Dispose()
        {
            TerminalSystem = null;
        }

        public void GridChanged(IMyCubeGrid newGrid)
        {
            groupsDirty = true;

            //update terminal system
            TerminalSystem = newGrid == null ? null : MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(newGrid);
        }

        public void Update()
        {
            FilteredBlocks.Clear();
            AddedBlocks.Clear();
            ValidGrids.Clear();

            bool hasGroupFilter = !string.IsNullOrEmpty(GroupNameFilter);

            //Get valid grids
            if (GridNameFilter == "")
            {
                //just use own grid
                ValidGrids.Add(Grid);
            }
            else//has a grid name filter
            {
                //switch current and prev connected grids list
                var newConnectedGrids = PrevConnectedGrids;
                PrevConnectedGrids = ConnectedGrids;
                ConnectedGrids = newConnectedGrids;

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

                //now check if connected grids set has changed
                if (!CompareGridsSet.SetEquals(ConnectedGrids))
                {
                    //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"Connected grids changed");
                    groupsDirty = true;

                    //update compare grids set to have new set of connected grids, for future comparisons
                    CompareGridsSet.Clear();
                    CompareGridsSet.UnionWith(ConnectedGrids);
                }
            }

            if (ValidGrids.Count > 0)
            {
                if (hasGroupFilter)//if we have a group name filter
                {
                    UpdateGroups();

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
                        foreach (var block in grid.GetFatBlocks())
                        {
                            //..and check if they meet the name and item type filter
                            if (block is IMyTerminalBlock && IsPotentiallyValidBlock(block as IMyTerminalBlock))
                            {
                                FilteredBlocks.Add(block as IMyTerminalBlock);
                            }
                        }
                    }
                }
            }
        }

        public bool IsPotentiallyValidBlock(IMyTerminalBlock block)
        {
            //TODO check block ownership?
            return BlockNameFilterMatcher.Test((block).CustomName) && (BlockApplicableTest == null || BlockApplicableTest(block));
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
            if (!groupsDirty)
            {
                return;
            }

            BlockGroups.Clear();

            if (TerminalSystem != null && !string.IsNullOrEmpty(GroupNameFilter))
            {
                TerminalSystem.GetBlockGroups(BlockGroups, (group) => {
                    return GroupNameFilterMatcher.Test(group.Name);
                });
            }

            groupsDirty = false;
        }

        //Event handlers
        private void OnGroupRemoved(IMyBlockGroup obj)
        {
            groupsDirty = true;
        }

        private void OnGroupAdded(IMyBlockGroup obj)
        {
            groupsDirty = true;
        }
    }
}
