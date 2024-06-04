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

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    [MyTextSurfaceScript("GridStatusLCDScript", "Grid status")]
    class GridStatusLCDScript : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10; // frequency that Run() is called.
        //private int ScrollTime = 0;

        //private static readonly float ScrollSpeed = 3;//pixels per update
        //private static readonly int ScrollPauseUpdates = 18;//how many updates to say paused at the start and end when scrolling

        

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
            Surface.ScriptForegroundColor = Color.SteelBlue;
        }


        public void Init()
        {
            if (!GridStatusLCDSession.TouchUIApi.IsReady)
                return;

            if (_init)
                return;
            _init = true;

            //TEMP
            var state = new GridStatusLCDState();
            state.Entries.Add(new ShieldStatusEntry());
            state.Entries.Add(new InventoryStatusEntry() { Heading = "Ice" });
            state.Entries.Add(new OxygenStatusEntry() { Heading = "Oxygen", GridNameFilter = "*", GroupNameFilter = "* O2 Tanks" });

            //string saveText = MyAPIGateway.Utilities.SerializeToXML(state);

            //(block as IMyTerminalBlock).CustomData = saveText;
            //END TEMP

            _app = new GridStatusApp(_block, _surface, state);
            _app.Theme.Scale = Math.Min(Math.Max(Math.Min(this.Surface.SurfaceSize.X, this.Surface.SurfaceSize.Y) / 512, 0.4f), 2);
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
        }

        void BlockMarkedForClose(IMyEntity ent)
        {
            //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"GridStatusLCDScript::BlockMarkedForClose");
            Dispose();
        }

        public override void Run()
        {
            try
            {
                if (!_init && ticks++ < (6 * 2)) // 2 secs
                    return;

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
    }
}
