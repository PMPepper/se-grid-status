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
    public class GridStatusApp : TouchApp
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
        View MainContent { get; }
        ScrollView EntriesView { get; }
        View Header { get; }
        Label Heading { get; }
        View Footer { get; }

        public GridStatusLCDConfig Config;

        //List<AStatusEntry> Entries = new List<AStatusEntry>();


        public GridStatusApp(IMyCubeBlock block, IMyTextSurface surface, GridStatusLCDConfig config) : base(block, surface)
        {
            Config = config;
            HUDTextAPI = GridStatusLCDSession.HUDTextAPI; //store local reference
            Block = block;
            SetGrid(block.CubeGrid);

            DefaultBg = true;

            var bgCol = new Color() { A = 10, R = 70, G = 130, B = 180 };
            var defaultSpace = 8;
            var defaultSpacing = new Vector4(defaultSpace);

            MainContent = new View(ViewDirection.Column);
            MainContent.BgColor = bgCol;
            MainContent.Padding = defaultSpacing;
            MainContent.Flex = Vector2.One;
            MainContent.Pixels = Vector2.Zero;
            MainContent.Gap = defaultSpace;

            EntriesView = new ScrollView();
            EntriesView.Pixels = Vector2.Zero;
            EntriesView.Flex = Vector2.One;// new Vector2(1f - (16f / surface.TextureSize.X), 1);//?
            EntriesView.Direction = ViewDirection.Column;
            //EntriesView.BgColor = bgCol;
            //EntriesView.Padding = defaultSpacing;
            EntriesView.Gap = 2;
            EntriesView.ScrollAlwaysVisible = false;
            
            Heading = new Label("Loading...", 0.6f);
            
            Header = new View();
            Header.Padding = defaultSpacing;
            Header.Pixels = new Vector2(0, Heading.Pixels.Y + (2 * defaultSpace));
            Header.Flex = new Vector2(1, 0);
            Header.BgColor = bgCol;

            
            

            Header.AddChild(Heading);

            MainContent.AddChild(Header);
            
            foreach(var entry in config.Entries)
            {
                if(entry != null)
                {
                    var view = entry.Init(this, block, surface);
                    view.BgColor = bgCol;
                    EntriesView.AddChild(view);
                }
            }

            MainContent.AddChild(EntriesView);

            //Footer
            Footer = new View();
            Footer.Padding = defaultSpacing;
            Footer.Pixels = new Vector2(0, 20 + (2 * defaultSpace));
            Footer.Flex = new Vector2(1, 0);
            Footer.BgColor = bgCol;


            MainContent.AddChild(Footer);

            AddChild(MainContent);
        }

        public void SetConfig(GridStatusLCDConfig newConfig)
        {

        }

        private void OnBlockAdded(IMySlimBlock block)
        {
            foreach (var entry in Config.Entries)
            {
                entry?.BlockAdded(block);
            }
        }

        private void OnBlockRemoved(IMySlimBlock block)
        {
            foreach (var entry in Config.Entries)
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

            if(!CanPlayerView())
            {
                //TODO display 'access denied' message
                MainContent.Enabled = false;

                if (HUDMessageText != null)
                {
                    HUDMessage.Visible = false;
                }
            } else
            {
                MainContent.Enabled = true;

                //update title
                Heading.Text = $"Status for {grid.CustomName}";//TODO customisable
                HUDMessageText.Clear();

                foreach (var entry in Config.Entries)
                {
                    entry.Update(HUDMessageText);
                }

                if (HUDMessageText != null)
                {
                    HUDMessage.Visible = GridStatusLCDSession.Instance.IsControlledEntity(grid);
                }

                Footer.Enabled = CanPlayerEdit();
            }

            ForceUpdate();
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

            foreach (var entry in Config.Entries)
            {
                entry?.Dispose();
            }

            this?.ForceDispose();

            Block = null;

            SetGrid(null);
        }

        public bool CanPlayerView()
        {
            return true;//TODO
        }

        public bool CanPlayerEdit()
        {
            return true;
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

            foreach (var entry in Config.Entries)
            {
                entry?.GridChanged(newGrid);
            }
        }
    }
}
