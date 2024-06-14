using Draygo.API;
using System;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using Lima.API;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Entities;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    [MyTextSurfaceScript("GridStatusLCDScript", "Grid status")]
    public class GridStatusLCDScript : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10; // frequency that Run() is called.
        //private int ScrollTime = 0;

        //private static readonly float ScrollSpeed = 3;//pixels per update
        //private static readonly int ScrollPauseUpdates = 18;//how many updates to say paused at the start and end when scrolling

        private int _Index = -1;
        public int Index { get {
                //first time this runs, we need to find our surface index
                if(_Index == -1)
                {
                    var provider = Block as IMyTextSurfaceProvider;

                    for(int i = 0; i < provider.SurfaceCount; i++)
                    {
                        if(provider.GetSurface(i) == Surface)
                        {
                            _Index = i;
                            break;
                        }
                    }
                }

                return _Index;
        } }

        public GridStatusLCDConfig Config { get; private set; }
        

        IMyCubeBlock _block;
        IMyTerminalBlock _terminalBlock;
        IMyTextSurface _surface;

        GridStatusApp _app;

        bool _init = false;
        int ticks = 0;

        public GridStatusLCDScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            _block = block;
            _surface = surface;
            _terminalBlock = (IMyTerminalBlock)block;

            surface.ScriptBackgroundColor = Color.Black;
            surface.ScriptForegroundColor = Color.SteelBlue;
        }


        public void Init()
        {
            if (!GridStatusLCDSession.TouchUIApi.IsReady)
                return;

            if (_init)
                return;
            _init = true;

            GridStatusLCDSession.Instance.AddScriptInstance(this);
            Config = InitConfig();

            _app = new GridStatusApp(_block, this, Config);
            _app.Cursor.Scale = _app.Theme.Scale;
            
            _terminalBlock.OnMarkForClose += BlockMarkedForClose;
        }

        public override void Dispose()
        {
            //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"GridStatusLCDScript::Dispose");
            base.Dispose();

            try
            {
                _app?.Dispose();
            }
            catch(Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"GridStatusDispose error: {e.Message}");
                MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"{e.StackTrace}");
            }
            
            _terminalBlock.OnMarkForClose -= BlockMarkedForClose;
            GridStatusLCDSession.Instance.RemoveScriptInstance(this);
        }

        void BlockMarkedForClose(IMyEntity ent)
        {
            if ((Block.CubeGrid as MyCubeGrid).Physics == null)
            {
                return;//ignore non-physical grids
            }
            
            Dispose();
        }

        public override void Run()
        {
            try
            {
                if (!_init && ticks++ < (6 * 2)) // 2 secs
                    return;

                if((Block.CubeGrid as MyCubeGrid).Physics == null)
                {
                    return;//ignore non-physical grids
                }

                Init();

                if (_app == null)
                    return;

                base.Run();

                using (var frame = m_surface.DrawFrame())
                {
                    _app.Update();
                    frame.AddRange(_app.GetSprites());
                }
            }
            catch (Exception e)
            {
                _app = null;
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} ]", 5000, MyFontEnum.Red);
            }
        }

        private GridStatusLCDConfig InitConfig()
        {
            var config = GridStatusLCDSession.Instance.GetPersistedState(this);

            if(config == null)
            {
                Utils.Log("GetStatusLCDScript::InitConfig No valid persisted state found, get default config", 2);

                config = new GridStatusLCDConfig();

                //TEMP
                config.Entries.Add(new ShieldStatusEntry());
                config.Entries.Add(new InventoryStatusEntry() { Name = "Ice", BlockFilter = new BlockFilter() });
                config.Entries.Add(new OxygenStatusEntry() { Name = "O2", BlockFilter = new BlockFilter() { GridNameFilter = "*", GroupNameFilter = "* O2 Tanks" } });
                //END TEMP

                GridStatusLCDSession.Instance.BlockRequiresPersisting(Block as IMyCubeBlock);
            }

            return config;
        }

        /*private void StorageExample()
        {
            //ah, shit - this need to happen on the server...
            var Entity = Block as IMyEntity;


            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }

            if (Entity.Storage.ContainsKey(Constants.GridStatusLCDStateGUID))
            {
                MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"Storage: {Entity.Storage[Constants.GridStatusLCDStateGUID]}");
            }

            Entity.Storage[Constants.GridStatusLCDStateGUID] = "blah";

            MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"Write to Storage: {Entity.Storage[Constants.GridStatusLCDStateGUID]}");
        }*/
    }
}
