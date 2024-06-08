using Draygo.API;
using Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls;
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
        private IMyTerminalBlock Block;
        private IMyCubeGrid Grid;

        //HUD API stuff
        private HudAPIv2 HUDTextAPI;
        private HudAPIv2.HUDMessage HUDMessage;
        private StringBuilder HUDMessageText = new StringBuilder();
        private Vector2 HUDMessagePosition = new Vector2(0.5f, 1);//0 = center, 1/-1 = edges
        private const int HUDMessageTTL = 30;

        //state
        private bool IsEditing = true;
        private bool doResetConfigUI = false;

        //UI elements
        View MainContent { get; }
        ScrollView EntriesView { get; }
        ScrollView EditView { get; }
        CheckboxControl HUDEnabledCheckbox { get; }
        SelectControl<PermissionLevel> PermissionViewSelectControl { get; }
        SelectControl<PermissionLevel> PermissionEditSelectControl { get; }
        public Column EditHudOptionsColumn { get; private set; }
        SliderControl HUDScaleSlider { get; }
        SliderControl HUDPositionHorizontalSlider { get; }
        SliderControl HUDPositionVerticalSlider { get; }
        ScrollView EditEntriesView { get; }
        Heading Header { get; }
        View Footer { get; }
        Button EditModeBtn { get; }
        public View EditModeFooter { get; }
        public Button EditModeApplyBtn { get; }
        public Button EditModeCancelBtn { get; }
        public GridStatusLCDScript Script { get; }

        public GridStatusLCDConfig Config { get; protected set; }
        



        //List<AStatusEntry> Entries = new List<AStatusEntry>();

        public static readonly Color BgCol = new Color() { A = 10, R = 70, G = 130, B = 180 };
        public static readonly int DefaultSpace = 8;
        public static readonly Vector4 DefaultSpacing = new Vector4(DefaultSpace);

        public GridStatusApp(IMyCubeBlock block, GridStatusLCDScript script, GridStatusLCDConfig config) : base(block, script.Surface)
        {
            Script = script;
            HUDTextAPI = GridStatusLCDSession.HUDTextAPI; //store local reference
            Block = block as IMyTerminalBlock;
            SetGrid(block.CubeGrid);

            Block.OwnershipChanged += OnBlockOwnershipChanged;

            DefaultBg = true;

            MainContent = new View(ViewDirection.Column);
            MainContent.BgColor = BgCol;
            MainContent.Padding = DefaultSpacing;
            MainContent.Flex = Vector2.One;
            MainContent.Pixels = Vector2.Zero;
            MainContent.Gap = DefaultSpace;

            //Header
            //Heading = new Label("Loading...", 0.6f);
            
            Header = new Heading("Loading...");

            //Entries view
            EntriesView = new ScrollView();
            EntriesView.Pixels = Vector2.Zero;
            EntriesView.Flex = Vector2.One;
            EntriesView.Direction = ViewDirection.Column;
            EntriesView.Gap = 2;
            EntriesView.ScrollAlwaysVisible = false;

            //Edit view
            EditView = new ScrollView();
            EditView.Pixels = Vector2.Zero;
            EditView.Flex = Vector2.One;
            EditView.Direction = ViewDirection.Row;
            EditView.Gap = DefaultSpace;
            EditView.ScrollAlwaysVisible = false;

            //Edit entries view
            EditEntriesView = new ScrollView();
            EditEntriesView.Pixels = new Vector2(200, 0);
            EditEntriesView.Flex = new Vector2(3, 1);
            EditEntriesView.Direction = ViewDirection.Column;
            EditEntriesView.Gap = 2;
            EditEntriesView.ScrollAlwaysVisible = false;

            //Edit global options
            var editOptionsColumn = new Column();
            editOptionsColumn.Flex = new Vector2(1, 1);
            editOptionsColumn.Pixels = new Vector2(200, 0);
            editOptionsColumn.BgColor = BgCol;

            editOptionsColumn.AddChild(new Heading("Global options"));
            editOptionsColumn.AddChild(HUDEnabledCheckbox = new CheckboxControl("HUD summary enabled?", (newValue) => {
                EditHudOptionsColumn.Enabled = Config.HUDMessageEnabled = newValue;
            }));

            editOptionsColumn.AddChild(PermissionViewSelectControl = new SelectControl<PermissionLevel>("Who can view this screen", Permissions.GetPossiblePermissionOptions(Block), (newValue) => { Config.ViewPermission = newValue; }));
            editOptionsColumn.AddChild(PermissionEditSelectControl = new SelectControl<PermissionLevel>("Who can edit this screen", Permissions.GetPossiblePermissionOptions(Block), (newValue) => { Config.EditPermission = newValue; }));


            EditHudOptionsColumn = new Column();

            editOptionsColumn.AddChild(EditHudOptionsColumn);

            EditHudOptionsColumn.AddChild(new Heading("HUD options"));
            EditHudOptionsColumn.AddChild(HUDScaleSlider = new SliderControl("HUD scale", 0.01f, 5, (newValue) => {
                Config.HUDMessageScale = newValue;
                if(HUDMessage != null) HUDMessage.Scale = newValue;
            }, 0, false, 2));
            EditHudOptionsColumn.AddChild(HUDPositionHorizontalSlider = new SliderControl("HUD horizontal position", -1, 1, (newValue) => {
                Config.HUDMessagePosition.X = newValue;
                if (HUDMessage != null) HUDMessage.Origin = Config.HUDMessagePosition;
            }, 0, false, 2));
            EditHudOptionsColumn.AddChild(HUDPositionVerticalSlider = new SliderControl("HUD vertical position", -1, 1, (newValue) => {
                Config.HUDMessagePosition.Y = newValue;
                if (HUDMessage != null) HUDMessage.Origin = Config.HUDMessagePosition;
            }, 0, false, 2));

            EditHudOptionsColumn.Pixels = EditHudOptionsColumn.GetContentSize();

            //editGlobalOptionsView.AddChild(new CheckboxControl("Do another thing", (newValue) => { }));

            //Footer
            Footer = new View();
            Footer.Padding = DefaultSpacing;
            Footer.Pixels = new Vector2(0, 20 + (2 * DefaultSpace));
            Footer.Flex = new Vector2(1, 0);
            Footer.BgColor = BgCol;
            Footer.Direction = ViewDirection.Row;
            Footer.Alignment = ViewAlignment.End;

            EditModeBtn = new Button("Edit", () => IsEditing = true);

            Footer.AddChild(EditModeBtn);

            //edit mode footer
            EditModeFooter = new View();
            EditModeFooter.Padding = DefaultSpacing;
            EditModeFooter.Pixels = new Vector2(0, 20 + (2 * DefaultSpace));
            EditModeFooter.Flex = new Vector2(1, 0);
            EditModeFooter.BgColor = BgCol;
            EditModeFooter.Direction = ViewDirection.Row;
            EditModeFooter.Alignment = ViewAlignment.End;
            EditModeFooter.Gap = DefaultSpace;

            EditModeApplyBtn = new Button("Save", () => {
                IsEditing = false;
                GridStatusLCDSession.Instance.BlockRequiresPersisting(Block);
            });//TODO actually apply/cancel(?) the changes?
            EditModeCancelBtn = new Button("Cancel", () => {
                IsEditing = false;
                SetConfig(GridStatusLCDSession.Instance.GetPersistedState(Script));
            });

            EditModeFooter.AddChild(EditModeApplyBtn);
            EditModeFooter.AddChild(EditModeCancelBtn);

            //add children to view
            MainContent.AddChild(Header);
            MainContent.AddChild(EntriesView);
            MainContent.AddChild(EditView);
            EditView.AddChild(EditEntriesView);
            EditView.AddChild(editOptionsColumn);
            MainContent.AddChild(Footer);
            MainContent.AddChild(EditModeFooter);

            SetConfig(config);

            AddChild(MainContent);
        }

        private void OnBlockOwnershipChanged(IMyTerminalBlock block)
        {
            UpdateAvailablePermissions();
        }

        private void UpdateAvailablePermissions()
        {
            PermissionEditSelectControl.Options = PermissionViewSelectControl.Options = Permissions.GetPossiblePermissionOptions(Block);

        }

        public void SetConfig(GridStatusLCDConfig newConfig)
        {
            if(Config != null)
            {
                Config.Dispose();
            }

            Config = newConfig;

            if(Config != null)
            {
                Config.Entries.RemoveAll(entry => entry == null);

                foreach (var entry in Config.Entries)
                {
                    entry.Init(this, Block, Script.Surface as IMyTextSurface);
                }
            }
            
            SetUIFromConfig();
        }

        private void SetUIFromConfig()
        {
            doResetConfigUI = false;
            
            EntriesView.RemoveAllChildren();
            EditEntriesView.RemoveAllChildren();

            if (Config != null)
            {

                //set values for global config options
                HUDEnabledCheckbox.Value = Config.HUDMessageEnabled;
                PermissionViewSelectControl.Value = Config.ViewPermission;
                PermissionEditSelectControl.Value = Config.EditPermission;

                EditHudOptionsColumn.Enabled = Config.HUDMessageEnabled;
                HUDScaleSlider.Value = (float)Config.HUDMessageScale;
                HUDPositionHorizontalSlider.Value = Config.HUDMessagePosition.X;
                HUDPositionVerticalSlider.Value = Config.HUDMessagePosition.Y;

                if(HUDMessage != null)
                {
                    HUDMessage.Scale = Config.HUDMessageScale;
                    HUDMessage.Origin = Config.HUDMessagePosition;
                }

                //entries
                for (int i = 0; i < Config.Entries.Count; i++)
                {
                    var entry = Config.Entries[i];
                    var index = i;
                    EntriesView.AddChild(entry.View);
                    EditEntriesView.AddChild(new EditEntry(
                        entry,
                        ((index == 0)
                            ? null as Action
                            : () => {
                                Config.Entries.Move(index, index - 1);
                                ConfigUIRequiredReset();
                            }
                        ),
                        ((index == Config.Entries.Count - 1)
                            ? null as Action
                            : () => {
                                Config.Entries.Move(index, index + 2);
                                ConfigUIRequiredReset();
                            }
                        ),
                        () => {
                            Config.Entries.RemoveAt(index);
                            ConfigUIRequiredReset();
                        }
                    ));
                }
            }
        }

        private void ConfigUIRequiredReset()
        {
            doResetConfigUI = true;
        }

        private void OnBlockAdded(IMySlimBlock block)
        {
            if( Config != null)
            {
                foreach (var entry in Config.Entries)
                {
                    entry?.BlockAdded(block);
                }
            }
        }

        private void OnBlockRemoved(IMySlimBlock block)
        {
            if(Config != null)
            {
                foreach (var entry in Config.Entries)
                {
                    entry?.BlockRemoved(block);
                }
            }
        }

        public void Update()
        {
            if (HUDTextAPI.Heartbeat)
            {
                if (HUDMessage == null || HUDMessage.TimeToLive == 0)
                {
                    //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"HUDTextAPI.Heartbeat = true, create message");
                    HUDMessage = new HudAPIv2.HUDMessage(HUDMessageText, Config.HUDMessagePosition, Scale: Config.HUDMessageScale, TimeToLive: HUDMessageTTL);

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
                Header.Text = $"Status for {grid.CustomName}";//TODO customisable
                HUDMessageText.Clear();

                if(Config != null)
                {
                    if(doResetConfigUI)
                    {
                        SetUIFromConfig();
                    }

                    foreach (var entry in Config.Entries)
                    {
                        entry.Update(HUDMessageText);
                    }
                }
                
                EntriesView.Enabled = !IsEditing;
                EditView.Enabled = IsEditing;

                Footer.Enabled = !IsEditing && CanPlayerEdit();
                EditModeFooter.Enabled = IsEditing && CanPlayerEdit();

                if (HUDMessageText != null)
                {
                    HUDMessage.Visible = IsHUDMessageVisible();
                }
            }

            ForceUpdate();
        }

        private bool IsHUDMessageVisible()
        {
            //will only be called if canPlayerView = true
            if(!Config.HUDMessageEnabled)
            {
                return false;
            }

            var grid = Block.CubeGrid;

            //return GridStatusLCDSession.Instance.IsControlledEntity(grid);

            return true;
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

            Config?.Dispose();

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

            if(Config != null)
            {
                foreach (var entry in Config.Entries)
                {
                    entry?.GridChanged(newGrid);
                }
            }
        }

        
    }
}
