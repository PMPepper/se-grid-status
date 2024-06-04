using Lima.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public class ShieldStatusEntry : AStatusEntry
    {
        private string _Heading = "Shields";
        [XmlAttribute]
        public string Heading { get { return _Heading; } set { if (Label != null) { Label.Text = value; } _Heading = value; } }
        [XmlAttribute]
        public bool ShowOnHUD = true;

        private GridStatusApp App;
        private IMyCubeBlock Block;
        private View View;

        private IMyTerminalBlock ShieldGeneratorBlock;

        private Label Label;
        private ProgressBar StatusBar;

        private bool CheckForShieldGenerator = true;

        private static HashSet<string> shieldTypesCython = new HashSet<string>()
        {
          "SmallShipSmallShieldGeneratorBase",
          "SmallShipMicroShieldGeneratorBase",
          "LargeShipSmallShieldGeneratorBase",
          "LargeShipLargeShieldGeneratorBase"
        };
        private static char[] COLON = new char[1] { ':' };
        private static char[] SPACE_PARENS = new char[3] { ' ', '(', ')' };
        private static char[] FWD_SLASH = new char[1] { '/' };

        override public View Init(GridStatusApp app, IMyCubeBlock block, IMyTextSurface surface)
        {
            App = app;
            Block = block;

            View = new View();
            View.Flex = new Vector2(1, 0);
            View.Pixels = new Vector2(0, 36);

            Label = new Label(Heading);
            Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            Label.Margin = Vector4.UnitY * 8;

            StatusBar = new ProgressBar(0, 1, false, 1);
            StatusBar.Value = 0;
            StatusBar.Label.Text = "";
            StatusBar.Label.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.RIGHT;

            StatusBar.Pixels = new Vector2(0, StatusBar.Pixels.Y);
            StatusBar.Flex = new Vector2(1, 0);

            View.AddChild(Label);
            View.AddChild(StatusBar);
            //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"ShieldStatusEntry::Init surface size = {surface.SurfaceSize.X}, texture size = {surface.TextureSize.X}");
            return View;
        }

        override public void Update(StringBuilder hudMessageText)
        {
            var shieldStatus = GetShieldStatus();
            var shieldStatusStr = shieldStatus.ToString();

            if (StatusBar.Value != shieldStatus.Current || StatusBar.MaxValue != shieldStatus.Max)
            {
                //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"shield status changed?: {shieldStatus}, {ShieldStatusBar.Value}/{ShieldStatusBar.MaxValue}");

                if (shieldStatus.Current >= 0)
                {
                    StatusBar.Value = shieldStatus.Current;
                    StatusBar.MaxValue = shieldStatus.Max;
                }
                else
                {
                    StatusBar.Value = 0;
                    StatusBar.MaxValue = 1;
                }

                StatusBar.Label.Text = shieldStatusStr;
            }

            //HUD text
            if(ShowOnHUD)
            {
                hudMessageText.AppendLine($"{Heading}: {shieldStatusStr}");

                if (shieldStatus.Current >= 0)
                {
                    TextUtils.TextBar(hudMessageText, (float)shieldStatus.Current / (float)shieldStatus.Max, 20);
                    hudMessageText.Append('\n');
                }
            }
        }

        override public void Dispose() {
            if (ShieldGeneratorBlock != null)
            {
                ShieldGeneratorBlock = null;
            }
        }

        override public void BlockAdded(IMySlimBlock block) {
            //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"ShieldStatusEntry::BlockAdded {block}");

            if (ShieldGeneratorBlock == null && block.FatBlock != null && IsShieldGeneratorBlock(block.FatBlock))
            {
                CheckForShieldGenerator = true;
            }
        }
        override public void BlockRemoved(IMySlimBlock block)
        {
            if(block == ShieldGeneratorBlock)
            {
                ShieldGeneratorBlock = null;
                CheckForShieldGenerator = true;
            }
        }
        override public void GridChanged(IMyCubeGrid newGrid)
        {

        }

        //Private methods
        private ShieldStatus GetShieldStatus()
        {
            if (ShieldGeneratorBlock == null)
            {
                if (CheckForShieldGenerator)//if this value is false, there is no point in looking for a shield generator again
                {
                    CheckForShieldGenerator = false;
                    //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"find shield block");
                    var grid = Block.CubeGrid;

                    //TODO what about connected grids?
                    //can I use GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock> instead?
                    //var blocks = new List<IMyTerminalBlock>();
                    //MyGridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

                    var blocks = grid.GetFatBlocks<IMyTerminalBlock>();

                    foreach (var block in blocks)
                    {
                        if (IsShieldGeneratorBlock(block))
                        {
                            ShieldGeneratorBlock = block;
                            break;
                        }
                    }
                }

                //Grid has no shields
                if (ShieldGeneratorBlock == null)
                {
                    //MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"no shield block");
                    return ShieldStatus.NO_SHIELDS;
                }
            }

            string shieldName = ShieldGeneratorBlock.CustomName;

            if (!shieldName.Contains(":"))
            {
                MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"initing shield block");
                //Once the colon is added to the custom name, the shiled strenght will be appended to the end of the block custom name
                ShieldGeneratorBlock.CustomName += ":";

                return ShieldStatus.SHIELDS_INITIALISING;
            }

            if (!shieldName.EndsWith(")"))
            {
                MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"awaiting shield block init");
                return ShieldStatus.SHIELDS_INITIALISING;
            }

            try
            {
                string[] tempStringArray = shieldName.Split(COLON);
                string tempString = tempStringArray[1].Trim(SPACE_PARENS);
                string[] splitString = tempString.Split(FWD_SLASH);
                int curShields;
                int maxShields;
                int.TryParse(splitString[0], out curShields);
                int.TryParse(splitString[1], out maxShields);

                return new ShieldStatus(curShields, maxShields);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("[GSA]: ", $"failed to parse shield strength: {e.Message}");
                return new ShieldStatus(0, 0);
            }
        }

        private bool IsShieldGeneratorBlock(IMyCubeBlock block)
        {
            var def = block.BlockDefinition.SubtypeName;

            return shieldTypesCython.Contains(def);
        }
    }

    struct ShieldStatus
    {
        public static ShieldStatus NO_SHIELDS = new ShieldStatus(-1, -1);
        public static ShieldStatus SHIELDS_INITIALISING = new ShieldStatus(-2, -2);
        public static ShieldStatus ERROR = new ShieldStatus(-3, -3);

        public int Current;
        public int Max;

        public ShieldStatus(int current, int max)
        {
            Current = current;
            Max = max;
        }
        public static bool operator ==(ShieldStatus c1, ShieldStatus c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(ShieldStatus c1, ShieldStatus c2)
        {
            return !c1.Equals(c2);
        }

        public override string ToString()
        {
            if (this == NO_SHIELDS)
            {
                return "No shields";
            }

            if (this == SHIELDS_INITIALISING)
            {
                return "initialising";
            }

            if (this == ERROR)
            {
                return "error reading shield strength";
            }

            return $"{Current}/{Max}";
        }

        override public bool Equals(Object obj)
        {
            if (obj is ShieldStatus)
            {
                return (Current == ((ShieldStatus)obj).Current) && (Max == ((ShieldStatus)obj).Max);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            unchecked // Allow arithmetic overflow, numbers will just "wrap around"
            {
                int hashcode = 1430287;
                hashcode = hashcode * 7302013 ^ Current.GetHashCode();
                hashcode = hashcode * 7302013 ^ Max.GetHashCode();

                return hashcode;
            }
        }
    }
}
