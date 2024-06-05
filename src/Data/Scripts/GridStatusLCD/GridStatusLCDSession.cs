using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draygo.API;
using Lima.API;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.Network;
using VRage.Utils;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class GridStatusLCDSession : MySessionComponentBase
    {
        public static GridStatusLCDSession Instance; // the only way to access session comp from other classes and the only accepted static field.
        public static HudAPIv2 HUDTextAPI { get; private set; }
        public static TouchUiKit TouchUIApi { get; private set; }

        private MyEntity LastControlledEntity = null;
        private Dictionary<IMyTerminalBlock, Dictionary<int, GridStatusLCDScript>> BlockScripts = new Dictionary<IMyTerminalBlock, Dictionary<int, GridStatusLCDScript>>();
        private HashSet<IMyTerminalBlock> BlocksToPersist = new HashSet<IMyTerminalBlock>();

        public override void LoadData()
        {
            // amongst the earliest execution points, but not everything is available at this point.

            // These can be used anywhere, not just in this method/class:
            // MyAPIGateway. - main entry point for the API
            // MyDefinitionManager.Static. - reading/editing definitions
            // MyGamePruningStructure. - fast way of finding entities in an area
            // MyTransparentGeometry. and MySimpleObjectDraw. - to draw sprites (from TransparentMaterials.sbc) in world (they usually live a single tick)
            // MyVisualScriptLogicProvider. - mainly designed for VST but has its uses, use as a last resort.
            // System.Diagnostics.Stopwatch - for measuring code execution time.
            // ...and many more things, ask in #programming-modding in keen's discord for what you want to do to be pointed at the available things to use.

            if (MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            Instance = this;
            HUDTextAPI = new HudAPIv2(onRegisteredCallback);

            TouchUIApi = new TouchUiKit();
            TouchUIApi.Load();
        }

        private void onRegisteredCallback()
        {
            //throw new NotImplementedException();
            //HUDTextAPI.
        }

        public override void BeforeStart()
        {
            // executed before the world starts updating
        }

        protected override void UnloadData()
        {
            // executed when world is exited to unregister events and stuff

            Instance = null; // important for avoiding this object to remain allocated in memory
            TouchUIApi?.Unload();

            if (HUDTextAPI != null)
            {
                HUDTextAPI.Close();
                HUDTextAPI = null;
            }
        }

        /*public override void HandleInput()
        {
            // gets called 60 times a second before all other update methods, regardless of framerate, game pause or MyUpdateOrder.
        }*/

        /*public override void UpdateBeforeSimulation()
        {
            // executed every tick, 60 times a second, before physics simulation and only if game is not paused.
        }*/

        /*public override void Simulate()
        {
            // executed every tick, 60 times a second, during physics simulation and only if game is not paused.
            // NOTE in this example this won't actually be called because of the lack of MyUpdateOrder.Simulation argument in MySessionComponentDescriptor
        }*/

        public override void UpdateAfterSimulation()
        {
            // executed every tick, 60 times a second, after physics simulation and only if game is not paused.

            if (Constants.IsClient)
            {
                // Existing code for controlled entities and predictions
                MyEntity controlledEntity = GetControlledGrid();
                MyEntity cockpitEntity = GetControlledCockpit(controlledEntity);

                if(controlledEntity != LastControlledEntity)
                {
                    //controlled entity has changed
                    //Record new controlled entity
                    LastControlledEntity = controlledEntity;
                }

                //TODO persist blocks
                foreach(var block in BlocksToPersist)
                {
                    PersistBlock(block);
                }

                BlocksToPersist.Clear();
            }
        }

        /*public override void Draw()
        {
            // gets called 60 times a second after all other update methods, regardless of framerate, game pause or MyUpdateOrder.
            // NOTE: this is the only place where the camera matrix (MyAPIGateway.Session.Camera.WorldMatrix) is accurate, everywhere else it's 1 frame behind.
        }*/

        /*public override void SaveData()
        {
            // executed AFTER world was saved
        }*/

        /*public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            // executed during world save, most likely before entities.

            return base.GetObjectBuilder(); // leave as-is.
        }*/

        /*public override void UpdatingStopped()
        {
            // executed when game is paused
        }*/

        public bool IsControlledEntity(IMyEntity entity)
        {
            return entity == LastControlledEntity;
        }

        public void AddScriptInstance(GridStatusLCDScript script)
        {
            if (script == null)
            {
                Utils.Log($"GridStatusLCDSession::AddScriptInstance script = null", 3);

                return;
            }

            if(script.Index == -1)
            {
                Utils.Log($"GridStatusLCDSession::AddScriptInstance script.Index = -1", 3);

                return;
            }

            var block = script.Block as IMyTerminalBlock;

            if(block == null)
            {
                Utils.Log($"GridStatusLCDSession::AddScriptInstance block = null", 2);

                return;
            }

            if (!BlockScripts.ContainsKey(block))
            {
                BlockScripts.Add(block, new Dictionary<int, GridStatusLCDScript>());
            }

            BlockScripts[block][script.Index] = script;
        }

        public void RemoveScriptInstance(GridStatusLCDScript script)
        {
            var block = script.Block as IMyTerminalBlock;

            if (block == null)
            {
                Utils.Log($"GridStatusLCDSession::RemoveScriptInstance block = null", 2);
                return;
            }

            if (BlockScripts.ContainsKey(block))
            {
                BlockScripts[block].Remove(script.Index);

                if (BlockScripts[block].Count == 0)
                {
                    BlockScripts.Remove(block);
                }
            }
        }

        public void BlockRequiresPersisting(IMyCubeBlock block)
        {
            var terminalBlock = block as IMyTerminalBlock;

            if (terminalBlock != null && BlockScripts.ContainsKey(terminalBlock))
            {
                BlocksToPersist.Add(terminalBlock);
            }
        }

        private void PersistBlock(IMyTerminalBlock block)
        {
            if(block == null)
            {
                Utils.Log($"GridStatusLCDSession::PersistBlock unable to persist null block", 3);

                return;
            }

            if(!BlockScripts.ContainsKey(block))
            {
                Utils.Log($"GridStatusLCDSession::PersistBlock unable to persist block \"{block}\" as not found in BlockScripts", 3);

                return;
            }

            if((block as IMyTextSurfaceProvider) == null)
            {
                Utils.Log($"GridStatusLCDSession::PersistBlock unable to persist block \"{block}\", it is not an IMyTextSurfaceProvider", 3);

                return;
            }

            var scriptsToPersist = BlockScripts[block];
            var stateToPersist = new GridStatusLCDState[(block as IMyTextSurfaceProvider)?.SurfaceCount ?? 0];//new Dictionary<int, GridStatusLCDState>();

            foreach (var entry in scriptsToPersist)
            {
                if(entry.Key >= 0 && entry.Key < stateToPersist.Length)
                {
                    stateToPersist[entry.Key] = entry.Value.State;
                } else
                {
                    Utils.Log($"GridStatusLCDSession::PersistBlock unable to persist script with index {entry.Key} for block \"{block}\" (SurfaceCount = {stateToPersist.Length})", 2);
                }
            }

            string saveText;

            try
            {
                saveText = MyAPIGateway.Utilities.SerializeToXML(stateToPersist);
            }
            catch(Exception e)
            {
                Utils.Log("GridStatusLCDSession::PersistBlock failed to serialise to XML", 2);
                Utils.LogException(e);
                return;
            }
            
            //Merge into existing custom data, instead of replacing all content
            var ini = new MyIni();

            MyIniParseResult result;
            
            if (!ini.TryParse(block.CustomData, out result))
            {
                Utils.Log("GridStatusLCDSession::PersistBlock failed to parse customData with MyIni", 2);
                Utils.Log(result.ToString(), 2);
            }

            ini.Set(Constants.IniSection, Constants.IniKey, saveText);
            block.CustomData = ini.ToString();
        }

        public GridStatusLCDState GetPersistedState(GridStatusLCDScript script)
        {
            if (script == null)
            {
                Utils.Log($"GridStatusLCDSession::GetPersistedState unable get persisted state for script = null", 3);

                return null;
            }

            if(script.Index == -1)
            {
                Utils.Log($"GridStatusLCDSession::GetPersistedState unable get persisted state for script, as index = -1", 3);

                return null;
            }

            var scriptBlock = script.Block as IMyTerminalBlock;

            string data = scriptBlock?.CustomData;

            if(!string.IsNullOrWhiteSpace(data))
            {
                var ini = new MyIni();

                MyIniParseResult result;

                if (!ini.TryParse(data, out result))
                {
                    Utils.Log("GridStatusLCDSession::GetPersistedState failed to parse customData with MyIni", 2);
                    Utils.Log(result.ToString(), 2);

                    return null;
                }

                var iniData = ini.Get(Constants.IniSection, Constants.IniKey).ToString();

                if (!string.IsNullOrWhiteSpace(iniData))
                {
                    try
                    {
                        var persistedData = MyAPIGateway.Utilities.SerializeFromXML<GridStatusLCDState[]>(iniData);

                        return persistedData[script.Index];
                    } catch(Exception e)
                    {
                        Utils.Log("GridStatusLCDSession::GetPersistedState failed to deserialise persisted state", 2);
                        Utils.LogException(e);
                        Utils.Log($"Persisted data: {iniData}");
                    }
                    
                }
            }

            return null;
        }

        public static MyEntity GetControlledGrid()
        {
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null)
                {
                    return null;
                }

                var controlledEntity = MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity;
                if (controlledEntity == null)
                {
                    return null;
                }

                if (controlledEntity is IMyCockpit || controlledEntity is IMyRemoteControl)
                {
                    return (controlledEntity as IMyCubeBlock).CubeGrid as MyEntity;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error in GetControlledGrid: {e}");
            }

            return null;
        }

        public static MyEntity GetControlledCockpit(MyEntity controlledGrid)
        {
            if (controlledGrid == null)
                return null;

            var grid = controlledGrid as MyCubeGrid;
            if (grid == null)
                return null;

            foreach (var block in grid.GetFatBlocks())
            {
                var cockpit = block as MyCockpit; // Convert the block to MyCockpit
                if (cockpit != null)
                {
                    if (cockpit.WorldMatrix != null)  // Add null check here
                        return cockpit;
                }
            }
            return null;
        }
    }
}
