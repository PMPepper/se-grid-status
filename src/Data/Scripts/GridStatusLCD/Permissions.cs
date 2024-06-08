using Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace Grid_Status_Screen.src.Data.Scripts.GridStatusLCD
{
    public enum PermissionLevel {
        Everyone,
        Allies,
        Faction,
        TerminalAccess,
        BlockOwner
    }
    public static class Permissions
    {
        
        //new List<string>() { "Everyone", "Block owner + allies", "Block owner + faction member", "Users with terminal access", "Block owner" })
        /*public const string Everyone = "Everyone";
        public const string Allies = "Block owner + allies";
        public const string Faction = "Block owner + faction member";
        public const string TerminalAccess = "Users with block terminal access";
        public const string BlockOwner = "Block owner";
*/
        public static string Description(this PermissionLevel level)
        {
            switch (level)
            {
                case PermissionLevel.BlockOwner:
                    return "Block owner";
                case PermissionLevel.TerminalAccess:
                    return "Users with terminal access";
                case PermissionLevel.Faction:
                    return "Block owner + faction members";
                case PermissionLevel.Allies:
                    return "Block owner + allies";
                default:
                    return level.ToString();
            }
        }
        public static List<SelectOption<PermissionLevel>> GetPossiblePermissionOptions(IMyTerminalBlock block)
        {
            return GetPossiblePermissions(block).Select((level) => new SelectOption<PermissionLevel>(level, level.Description())).ToList();
        }

        public static List<PermissionLevel> GetPossiblePermissions(IMyTerminalBlock block)
        {
            var permissions = new List<PermissionLevel>(5);

            permissions.Add(PermissionLevel.Everyone);

            if(IsPlayerAlly(block)) {
                permissions.Add(PermissionLevel.Allies);
            }

            if (IsPlayerInFaction(block))
            {
                permissions.Add(PermissionLevel.Faction);
            }

            if (IsPlayerHasTerminalAccess(block))
            {
                permissions.Add(PermissionLevel.TerminalAccess);
            }

            if (IsPlayerBlockOwner(block))
            {
                permissions.Add(PermissionLevel.BlockOwner);
            }

            return permissions;
        }

        private static bool IsPlayerAdmin()
        {
            if(MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                return MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId);
            }

            return false;
        }

        public static bool IsPlayerBlockOwner(IMyTerminalBlock block)
        {
            var relation = MyAPIGateway.Session.Player.GetRelationTo(block.OwnerId);

            return IsPlayerAdmin() || relation == MyRelationsBetweenPlayerAndBlock.Owner;
        }

        public static bool IsPlayerHasTerminalAccess(IMyTerminalBlock block)
        {
            var relation = MyAPIGateway.Session.Player.GetRelationTo(block.OwnerId);

            return IsPlayerAdmin() || block.OwnerId == 0 || relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare;
        }

        public static bool IsPlayerInFaction(IMyTerminalBlock block)
        {
            var player = MyAPIGateway.Session.Player;

            var blockFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);
            var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);

            return IsPlayerAdmin() || (blockFaction != null && blockFaction == playerFaction);
        }

        public static bool IsPlayerAlly(IMyTerminalBlock block)
        {
            var relation = MyAPIGateway.Session.Player.GetRelationTo(block.OwnerId);

            return IsPlayerAdmin() || relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Friends;
        }
    }
}
