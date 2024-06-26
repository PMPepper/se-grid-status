﻿using Draygo.API;
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
        private const int HUDMessageTTL = 30;

        //state
        private bool IsEditing = true;
        private bool doResetConfigUI = false;
        private List<Action> DeferredActions = new List<Action>();

        //UI elements
        View PermissionDeniedView { get; }
        View MainContent { get; }
        ScrollView EntriesView { get; }
        Row AddEntryRow { get; } 
        View EditView { get; }
        Column EditOptionsColumn { get; }
        CheckboxControl HUDEnabledCheckbox { get; }
        SelectControl<PermissionLevel> PermissionViewSelectControl { get; }
        SelectControl<PermissionLevel> PermissionEditSelectControl { get; }
        Column EditHudOptionsColumn { get; }
        SliderControl HUDScaleSlider { get; }
        SliderControl HUDPositionHorizontalSlider { get; }
        SliderControl HUDPositionVerticalSlider { get; }
        SelectControl<PermissionLevel> PermissionViewHUDSelectControl { get; }
        ScrollView EditEntriesView { get; }
        Heading Header { get; }
        View Footer { get; }
        Button EditModeBtn { get; }
        public View EditModeFooter { get; }
        public Button EditModeApplyBtn { get; }
        public Button EditModeCancelBtn { get; }
        public GridStatusLCDScript Script { get; }

        public GridStatusLCDConfig Config { get; protected set; }
        SelectControl<HUDVisibleWhen> HUDVisibleWhenSelectControl { get; }
        public Switch OptionsSectionSwitch { get; private set; }
        public Column EditGeneralOptionsColumn { get; }
        internal SelectControl<Func<AStatusEntry>> AddEntrySelectControl { get; }

        public static readonly Color BgCol = new Color() { A = 10, R = 70, G = 130, B = 180 };
        public static readonly int DefaultSpace = 8;
        public static readonly Vector4 DefaultSpacing = new Vector4(DefaultSpace);
        public IMyTextSurface Surface { get; private set; }
        private View EditEntryModal { get; set; }

        public GridStatusApp(IMyCubeBlock block, GridStatusLCDScript script, GridStatusLCDConfig config) : base(block, script.Surface)
        {
            //TODO Theme.Scale adjust?

            Script = script;
            Surface = script.Surface as IMyTextSurface;
            HUDTextAPI = GridStatusLCDSession.HUDTextAPI; //store local reference
            Block = block as IMyTerminalBlock;
            SetGrid(block.CubeGrid);
            
            Block.OwnershipChanged += OnBlockOwnershipChanged;

            DefaultBg = true;

            PermissionDeniedView = new View();
            PermissionDeniedView.Flex = Vector2.One;
            PermissionDeniedView.BgColor = BgCol;
            PermissionDeniedView.Enabled = false;
            PermissionDeniedView.Alignment = ViewAlignment.Center;
            PermissionDeniedView.AddChild(new Label("Permission denied", 2) { Flex = new Vector2(1, 0), Alignment = TextAlignment.CENTER });

            MainContent = new View(ViewDirection.Column);
            MainContent.BgColor = BgCol;
            MainContent.Padding = DefaultSpacing;
            MainContent.Flex = Vector2.One;
            MainContent.Pixels = Vector2.Zero;
            MainContent.Gap = DefaultSpace;

            //Header
            Header = new Heading("Loading...");

            //Entries view
            EntriesView = new ScrollView();
            EntriesView.Pixels = Vector2.Zero;
            EntriesView.Flex = Vector2.One;
            EntriesView.Direction = ViewDirection.Column;
            EntriesView.Gap = 2;
            EntriesView.ScrollAlwaysVisible = false;

            AddEntryRow = new Row();
            AddEntryRow.Anchor = ViewAnchor.End;
            AddEntryRow.Padding = DefaultSpacing;
            AddEntryRow.BgColor = BgCol;

            AddEntryRow.AddChild(new Label("Add") { Flex = Vector2.Zero, Pixels = new Vector2(40, 24) });
            AddEntryRow.AddChild(AddEntrySelectControl = new SelectControl<Func<AStatusEntry>>(null, AStatusEntry.EntryTypeOptions, (newValue) => {}));

            AddEntrySelectControl.Flex = new Vector2(0, 0);
            AddEntrySelectControl.Pixels = new Vector2(200, AddEntrySelectControl.Pixels.Y);

            AddEntryRow.AddChild(new Button("Go", () => {
                if(AddEntrySelectControl.Value != null)
                {
                    var newEntry = AddEntrySelectControl.Value();
                    Config.Entries.Add(newEntry);
                    newEntry.Init(this, Block, Script.Surface as IMyTextSurface);

                    ConfigUIRequiredReset();
                }
            }) { Flex = Vector2.Zero, Pixels = new Vector2(50, 24) });

            AddEntryRow.SizeToContent();
            AddEntryRow.Pixels = new Vector2(0, AddEntryRow.Pixels.Y);//This is needed because AddEntryRow.Anchor = ViewAnchor.End seems to be broken, or just work in a crazy way

            //Edit view
            EditView = new View();
            EditView.Pixels = Vector2.Zero;
            EditView.Flex = Vector2.One;
            EditView.Direction = ViewDirection.Row;
            EditView.Gap = DefaultSpace;

            //Edit entries view
            EditEntriesView = new ScrollView();
            EditEntriesView.Pixels = new Vector2(200, 0);
            EditEntriesView.Flex = new Vector2(3, 1);
            EditEntriesView.Direction = ViewDirection.Column;
            EditEntriesView.Gap = 2;
            EditEntriesView.ScrollAlwaysVisible = false;

            //Edit global options
            EditOptionsColumn = new Column();
            EditOptionsColumn.Flex = new Vector2(1, 1);
            EditOptionsColumn.Pixels = new Vector2(200, 0);
            EditOptionsColumn.BgColor = BgCol;
            EditOptionsColumn.Padding = new Vector4(0, 0, 0, DefaultSpace);

            OptionsSectionSwitch = new Switch(new string[] { "General options", "HUD" }, 0, (newValue) => {
                UpdateVisibleOptions();
            });
            
            EditOptionsColumn.AddChild(OptionsSectionSwitch);

            EditGeneralOptionsColumn = new Column();

            EditOptionsColumn.AddChild(EditGeneralOptionsColumn);

            //EditGeneralOptionsColumn.AddChild(new Heading("Global options"));
            EditGeneralOptionsColumn.AddChild(HUDEnabledCheckbox = new CheckboxControl("HUD summary enabled?", (newValue) => {
                EditHudOptionsColumn.Enabled = Config.HUDMessageEnabled = newValue;

                UpdateVisibleOptions();
            }));

            EditGeneralOptionsColumn.AddChild(PermissionViewSelectControl = new SelectControl<PermissionLevel>("Who can view this screen", Permissions.GetPossiblePermissionOptions(Block), (newValue) => { Config.ViewPermission = newValue; UpdatePermissionOptions(); }));
            EditGeneralOptionsColumn.AddChild(PermissionEditSelectControl = new SelectControl<PermissionLevel>("Who can edit this screen", Permissions.GetPossiblePermissionOptions(Block), (newValue) => { Config.EditPermission = newValue; }));

            EditHudOptionsColumn = new Column();

            EditOptionsColumn.AddChild(EditHudOptionsColumn);

            //EditHudOptionsColumn.AddChild(new Heading("HUD options"));
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

            EditHudOptionsColumn.AddChild(HUDVisibleWhenSelectControl = new SelectControl<HUDVisibleWhen>(
                    "HUD visible when", 
                    new List<SelectOption<HUDVisibleWhen>>() {
                        new SelectOption<HUDVisibleWhen>(HUDVisibleWhen.Always, "Panel is rendered"),
                        new SelectOption<HUDVisibleWhen>(HUDVisibleWhen.InSeat, "Player in seat/bed/etc"),
                    }, 
                    (newValue) => {
                        Config.HUDVisibleWhen = newValue;
                    }
                )
            );

            EditHudOptionsColumn.AddChild(PermissionViewHUDSelectControl = new SelectControl<PermissionLevel>(
                "Users who can see HUD", 
                Permissions.GetPossiblePermissionOptions(Block, PermissionLevel.Everyone), 
                (newValue) => {
                    Config.HUDViewPermission = newValue;
                }
            ));

            EditHudOptionsColumn.Pixels = EditHudOptionsColumn.GetContentSize();
            
            //Footer
            Footer = new View();
            Footer.Padding = DefaultSpacing;
            Footer.Pixels = new Vector2(0, 20 + (2 * DefaultSpace));
            Footer.Flex = new Vector2(1, 0);
            Footer.BgColor = BgCol;
            Footer.Direction = ViewDirection.Row;
            Footer.Alignment = ViewAlignment.End;

            EditModeBtn = new Button("Edit", () => {
                IsEditing = true;
            });

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
                if (EditEntryModal != null)
                {
                    return;
                }

                IsEditing = false;
                GridStatusLCDSession.Instance.BlockRequiresPersisting(Block);
            });//TODO actually apply/cancel(?) the changes?
            EditModeCancelBtn = new Button("Cancel", () => {
                if (EditEntryModal != null)
                {
                    return;
                }

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
            EditView.AddChild(EditOptionsColumn);
            MainContent.AddChild(Footer);
            MainContent.AddChild(EditModeFooter);

            SetConfig(config);

            AddChild(MainContent); 
            AddChild(PermissionDeniedView);
        }

        private void UpdatePermissionOptions()
        {
            PermissionEditSelectControl.Options = Permissions.GetPossiblePermissionOptions(Block, Config.ViewPermission);
            PermissionViewHUDSelectControl.Options = Permissions.GetPossiblePermissionOptions(Block, PermissionLevel.Allies > Config.ViewPermission ? PermissionLevel.Allies : Config.ViewPermission);
            
            if(PermissionEditSelectControl.SelectedIndex == -1)
            {
                PermissionEditSelectControl.SelectedIndex = 0;
            }

            if (PermissionViewHUDSelectControl.SelectedIndex == -1)
            {
                PermissionViewHUDSelectControl.SelectedIndex = 0;
            }
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

        private void UpdateVisibleOptions()
        {
            OptionsSectionSwitch.Buttons[1].Enabled = Config.HUDMessageEnabled;

            EditGeneralOptionsColumn.Enabled = OptionsSectionSwitch.Index == 0;
            EditHudOptionsColumn.Enabled = OptionsSectionSwitch.Index == 1;
        }

        private void SetUIFromConfig()
        {
            doResetConfigUI = false;
            
            EntriesView.RemoveAllChildren();
            EditEntriesView.RemoveAllChildren();

            if (Config != null)
            {
                UpdateVisibleOptions();
                UpdatePermissionOptions();

                //set values for global config options
                HUDEnabledCheckbox.Value = Config.HUDMessageEnabled;
                PermissionViewSelectControl.Value = Config.ViewPermission;
                PermissionEditSelectControl.Value = Config.EditPermission;

                
                HUDScaleSlider.Value = (float)Config.HUDMessageScale;
                HUDPositionHorizontalSlider.Value = Config.HUDMessagePosition.X;
                HUDPositionVerticalSlider.Value = Config.HUDMessagePosition.Y;
                
                HUDVisibleWhenSelectControl.Value = Config.HUDVisibleWhen;
                PermissionViewHUDSelectControl.Value = Config.HUDViewPermission;

                if (HUDMessage != null)
                {
                    HUDMessage.Scale = Config.HUDMessageScale;
                    HUDMessage.Origin = Config.HUDMessagePosition;
                }
                
                //entries
                if (Config.Entries != null)
                {
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
                                    if (EditEntryModal != null)
                                    {
                                        return;
                                    }
                                    Config.Entries.Move(index, index - 1);
                                    ConfigUIRequiredReset();
                                }
                            ),
                            ((index == Config.Entries.Count - 1)
                                ? null as Action
                                : () => {
                                    if (EditEntryModal != null)
                                    {
                                        return;
                                    }
                                    Config.Entries.Move(index, index + 2);
                                    ConfigUIRequiredReset();
                                }
                            ),
                            () => {
                                if (EditEntryModal != null)
                                {
                                    return;
                                }
                                Config.Entries.RemoveAt(index);
                                ConfigUIRequiredReset();
                            },
                            () => {
                                if (EditEntryModal != null)
                                {
                                    return;
                                }
                                DeferredActions.Add(() => {
                                    SetEditEntryModal(entry.GetEditEntryModal());
                                });
                            }
                        ));
                    }
                }
            }

            EditEntriesView.AddChild(AddEntryRow);
        }

        private void SetEditEntryModal(View editModal)
        {
            if(EditEntryModal != null)
            {
                EditEntryModal.Parent?.RemoveChild(EditEntryModal);
                EditEntryModal = null;
            }

            EditEntryModal = editModal;

            MainContent.AddChild(EditEntryModal);
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
                    
                    //Text colour can be changed using:
                    //HUDMessage.InitialColor = Color.Red;
                    
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
                PermissionDeniedView.Enabled = true;

                if (HUDMessageText != null)
                {
                    HUDMessage.Visible = false;
                }
            } else
            {
                MainContent.Enabled = true;
                PermissionDeniedView.Enabled = false;

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
                
                EntriesView.Enabled = !IsEditing || !CanPlayerEdit();
                EditView.Enabled = IsEditing && CanPlayerEdit();

                Footer.Enabled = !IsEditing && CanPlayerEdit();
                EditModeFooter.Enabled = IsEditing && CanPlayerEdit();

                if (HUDMessageText != null)
                {
                    HUDMessage.Visible = IsHUDMessageVisible();
                }
            }

            foreach(var action in DeferredActions)
            {
                if (action != null)
                {
                    action();
                }
            }

            DeferredActions.Clear();

            ForceUpdate();
        }

        private bool IsHUDMessageVisible()
        {
            //will only be called if canPlayerView = true
            if(!Config.HUDMessageEnabled)
            {
                return false;
            }

            if(!Permissions.CurrentPlayerHasPermissionForBlock(Config.HUDViewPermission, Block)) {
                return false;
            }

            if(Config.HUDVisibleWhen == HUDVisibleWhen.InSeat)
            {
                return GridStatusLCDSession.Instance.IsControlledEntity(Block.CubeGrid);
            }

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
            if(Config == null)
            {
                return false;
            }

            return Permissions.CurrentPlayerHasPermissionForBlock(Config.ViewPermission, Block);
        }

        public bool CanPlayerEdit()
        {
            if (Config == null)
            {
                return false;
            }

            return Permissions.CurrentPlayerHasPermissionForBlock(Config.EditPermission, Block);
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
