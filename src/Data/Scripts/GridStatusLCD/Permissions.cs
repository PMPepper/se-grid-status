﻿using Grid_Status_Screen.src.Data.Scripts.GridStatusLCD.Controls;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;

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
        private static PermissionLevel[] PermissionsOrder { get; } = new PermissionLevel[] {
            PermissionLevel.Everyone,
            PermissionLevel.Allies,
            PermissionLevel.Faction,
            PermissionLevel.TerminalAccess,
            PermissionLevel.BlockOwner
        };

        //private static Dictionary<PermissionLevel, Func<bool, >>

        //public static bool 

        public static bool CurrentPlayerHasPermissionForBlock(PermissionLevel level, IMyTerminalBlock block)
        {
            return PlayerHasPermissionForBlock(MyAPIGateway.Session.Player, level, block);
        }
        public static bool PlayerHasPermissionForBlock(IMyPlayer player, PermissionLevel level, IMyTerminalBlock block) {
            if(IsPlayerAdmin(player))
            {
                return true;
            }

            var relation = player.GetRelationTo(block.OwnerId);

            switch (level)
            {
                case PermissionLevel.Everyone:
                    return true;
                case PermissionLevel.BlockOwner:
                    return relation == MyRelationsBetweenPlayerAndBlock.Owner;
                case PermissionLevel.TerminalAccess:
                    return block.OwnerId == 0 || relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare;
                case PermissionLevel.Faction:
                    var blockFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);
                    var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
 
                    return blockFaction != null && blockFaction == playerFaction;
                case PermissionLevel.Allies:
                    return relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Friends;
            }

            return false;
        }
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
        public static List<SelectOption<PermissionLevel>> GetPossiblePermissionOptions(IMyTerminalBlock block, PermissionLevel minimum = PermissionLevel.Everyone)
        {
            return GetPossiblePermissions(block, minimum).Select((level) => new SelectOption<PermissionLevel>(level, level.Description())).ToList();
        }

        public static List<PermissionLevel> GetPossiblePermissions(IMyTerminalBlock block, PermissionLevel minimum = PermissionLevel.Everyone)
        {
            var permissions = new List<PermissionLevel>(PermissionsOrder.Length);

            for(int i = Array.FindIndex(PermissionsOrder, (curLevel) => curLevel == minimum); i < PermissionsOrder.Length; i++)
            {
                var level = PermissionsOrder[i];

                if(CurrentPlayerHasPermissionForBlock(level, block))
                {
                    permissions.Add(level);
                }
            }

            /*if(!exclude.Contains(PermissionLevel.Everyone))
            {
                permissions.Add(PermissionLevel.Everyone);
            }
            
            if(!exclude.Contains(PermissionLevel.Allies) && CurrentPlayerHasPermissionForBlock(PermissionLevel.Allies, block)) {
                permissions.Add(PermissionLevel.Allies);
            }

            if (!exclude.Contains(PermissionLevel.Faction) && CurrentPlayerHasPermissionForBlock(PermissionLevel.Faction, block))
            {
                permissions.Add(PermissionLevel.Faction);
            }

            if (!exclude.Contains(PermissionLevel.TerminalAccess) && CurrentPlayerHasPermissionForBlock(PermissionLevel.TerminalAccess, block))
            {
                permissions.Add(PermissionLevel.TerminalAccess);
            }

            if (!exclude.Contains(PermissionLevel.BlockOwner) && CurrentPlayerHasPermissionForBlock(PermissionLevel.BlockOwner, block))
            {
                permissions.Add(PermissionLevel.BlockOwner);
            }*/

            return permissions;
        }

        private static bool IsPlayerAdmin(IMyPlayer player)
        {
            if(MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                return MyAPIGateway.Session.IsUserAdmin(player.SteamUserId);
            }

            return false;
        }
    }
}
