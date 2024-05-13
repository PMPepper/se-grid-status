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

        public override void LoadData()
        {
            // amogst the earliest execution points, but not everything is available at this point.

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
            }
        }

        public bool IsControlledEntity(IMyEntity entity)
        {
            return entity == LastControlledEntity;
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
