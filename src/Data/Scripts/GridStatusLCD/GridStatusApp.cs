using Draygo.API;
using Lima.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    class GridStatusApp : TouchApp
    {
        //General
        private IMyCubeBlock Block;
        private IMyCubeGrid Grid;

        //HUD API stuff
        private HudAPIv2 HUDTextAPI;
        private HudAPIv2.HUDMessage HUDMessage;
        private StringBuilder HUDMessageText = new StringBuilder();
        private Vector2 HUDMessagePosition = new Vector2(0.5f, 1);//0 = center, 1/-1 = edges
        private const int HUDMessageTTL = 30;

        //UI elements
        ScrollView MainView { get; }
        Label MainViewHeader { get; }

        List<IStatusEntry> Entries = new List<IStatusEntry>();


        public GridStatusApp(IMyCubeBlock block, IMyTextSurface surface) : base(block, surface)
        {
            HUDTextAPI = GridStatusLCDSession.HUDTextAPI; //store local reference
            Block = block;
            SetGrid(block.CubeGrid);

            DefaultBg = true;

            var window = new View(ViewDirection.Column);
            window.BgColor = new Color() { A = 10, R = 70, G = 130, B = 180 };

            MainView = new ScrollView();
            MainView.Flex = new Vector2(1f - (16f / surface.TextureSize.X), 1);
            MainView.Direction = ViewDirection.Column;

            MainView.Margin = new Vector4(8);
            MainView.BgColor = new Color() { A = 10, R = 70, G = 130, B = 180 };//new Color() { A = 30, R = 20, G = 40, B = 80 };
            MainView.Padding = new Vector4(8);
            MainView.Gap = 8;
            MainView.ScrollAlwaysVisible = false;

            MainViewHeader = new Label("Loading...", 0.6f);

            //TODO persist values/load defaults
            Entries.Add(new ShieldStatusEntry());
            Entries.Add(new InventoryStatusEntry() { Heading = "Ice" });
            Entries.Add(new OxygenStatusEntry() { Heading = "Oxygen", GridNameFilter = "*", GroupNameFilter = "* O2 Tanks" });
            //Entries.Add(new ShieldStatusEntry());
            //Entries.Add(new ShieldStatusEntry());
            //Entries.Add(new ShieldStatusEntry());

            MainView.AddChild(MainViewHeader);
            
            foreach(var entry in Entries)
            {
                if(entry != null)
                {
                    MainView.AddChild(entry.Init(this, block, surface));
                }
            }
            
            window.AddChild(MainView);
            
            AddChild(window);
        }

        private void OnBlockAdded(IMySlimBlock block)
        {
            foreach (var entry in Entries)
            {
                entry?.BlockAdded(block);
            }
        }

        private void OnBlockRemoved(IMySlimBlock block)
        {
            foreach (var entry in Entries)
            {
                entry?.BlockRemoved(block);
            }
        }

        public void Update()
        {
            if (HUDTextAPI.Heartbeat)
            {
                if (HUDMessage == null || HUDMessage.TimeToLive == 0)
                {
                    //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"HUDTextAPI.Heartbeat = true, create message");
                    HUDMessage = new HudAPIv2.HUDMessage(HUDMessageText, HUDMessagePosition, Scale: (100 / 100d), TimeToLive: HUDMessageTTL);

                    //TODO background?
                    //var ln = Msg.GetTextLength();
                    //var background = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("SquareIgnoreDepth"), pos, Color.Black * 0.5f, ln / 2d, Width: (float)ln.X, Height: (float)ln.Y);
                    //background.Options |= HudAPIv2.Options.Shadowing;
                } else {
                    HUDMessage.TimeToLive = HUDMessageTTL;//keep resetting this as long as the message gets drawn
                }
            }

            var grid = Block.CubeGrid;

            if(grid != Grid)
            {
                SetGrid(grid);
            }

            //update title
            MainViewHeader.Text = $"Status for {grid.CustomName}";
            HUDMessageText.Clear();

            foreach(var entry in Entries)
            {
                entry.Update(HUDMessageText);
            }

            ForceUpdate();

            if(HUDMessageText != null)
            {
                HUDMessage.Visible = GridStatusLCDSession.Instance.IsControlledEntity(grid);
            }
        }

        public void Dispose()
        {
            //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"GridStatusApp::Dispose");
            if (HUDMessage != null)
            {
                //HUDMessage.TimeToLive = 0;
                HUDMessage.DeleteMessage();
                HUDMessage = null;
            }

            foreach (var entry in Entries)
            {
                entry?.Dispose();
            }

            this?.ForceDispose();

            Block = null;

            SetGrid(null);
        }

        private void SetGrid(IMyCubeGrid newGrid)
        {
            if(newGrid == Grid)
            {
                return;
            }
            if (Grid != null)
            {
                Grid.OnBlockAdded -= OnBlockAdded;
                Grid.OnBlockRemoved -= OnBlockRemoved;
            }

            Grid = newGrid;

            if(newGrid != null)
            {
                Grid.OnBlockAdded += OnBlockAdded;
                Grid.OnBlockRemoved += OnBlockRemoved;
            }

            foreach (var entry in Entries)
            {
                entry?.GridChanged(newGrid);
            }
        }
    }
}
