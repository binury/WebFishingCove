using Cove.Server.Actor;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Cove.Server
{

    public class RegisteredCommand
    {
        public string Command;
        public string Description;
        public Action<WFPlayer, string[]> Callback;
        public RegisteredCommand(string command, string description, Action<WFPlayer, string[]> callback)
        {
            Command = command.ToLower(); // make sure its lower case to not mess anything up
            Description = description;
            Callback = callback;
        }

        public void Invoke(WFPlayer player, string[] args)
        {
            Callback(player, args);
        }

    }

    public partial class CoveServer
    {
        List<RegisteredCommand> Commands = [];

        void RegisterDefaultCommands()
        {
            RegisterCommand("help", (player, args) =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("Commands:");
                foreach (var cmd in Commands)
                {
                    sb.AppendLine($"{cmd.Command} - {cmd.Description}");
                }
                messagePlayer(sb.ToString(), player.SteamId);
            });
            SetCommandDescription("help", "Shows all commands");

            RegisterCommand("exit", (player, args) =>
            {
                // make sure the player is the host
                if (player.SteamId != serverPlayer.SteamId)
                {
                    messagePlayer("You are not the host!", player.SteamId);
                    return;
                }
                messagePlayer("Server is shutting down!", player.SteamId);

                Stop(); // stop the server

            });
            SetCommandDescription("exit", "Shuts down the server (host only)");

            RegisterCommand("kick", (player, args) =>
            {
                if (!isPlayerAdmin(player.SteamId)) return;
                string playerIdent = string.Join(" ", args);
                // try find a user with the username first
                WFPlayer kickedplayer = AllPlayers.ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                // if there is no player with the username try find someone with that fisher ID
                if (kickedplayer == null)
                    kickedplayer = AllPlayers.ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                if (kickedplayer == null)
                {
                    messagePlayer("That's not a player!" , player.SteamId);
                }
                else
                {
                    messagePlayer($"Kicked {kickedplayer.Username}", player.SteamId);
                    kickPlayer(kickedplayer.SteamId);
                    //SendGlobalChatMessage($"{kickedplayer.Username} was kicked from the lobby!");
                }
            });
            SetCommandDescription("kick", "Kicks a player from the server");

            RegisterCommand("ban", (player, args) =>
            {
                if (!isPlayerAdmin(player.SteamId)) return;
                // hacky fix,
                // Extract player name from the command message
                string playerIdent = string.Join(" ", args);
                // try find a user with the username first
                WFPlayer playerToBan = AllPlayers.ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                // if there is no player with the username try find someone with that fisher ID
                if (playerToBan == null)
                    playerToBan = AllPlayers.ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                if (playerToBan == null)
                {
                    messagePlayer("Player not found!", player.SteamId);
                }
                else
                {

                    if (isPlayerBanned(playerToBan.SteamId))
                        banPlayer(playerToBan.SteamId);
                    else
                        banPlayer(playerToBan.SteamId, true); // save to file if they are not already in there!

                    messagePlayer($"Banned {playerToBan.Username}", player.SteamId);
                    messageGlobal($"{playerToBan.Username} has been banned from the server.");
                }
            });
            SetCommandDescription("ban", "Bans a player from the server");

        }

        public void RegisterCommand(string command, Action<WFPlayer, string[]> cb)
        {

            if (Commands.Any(c => c.Command == command))
            {
                Log($"Command '{command}' is already registerd!");
                return;
            }

            Commands.Add(new RegisteredCommand(command, "", cb));

        }

        public void UnregisterCommand(string command)
        {
            Commands.RemoveAll(c => c.Command == command);
        }

        public void SetCommandDescription(string command, string description)
        {
            var cmd = Commands.Find(c => c.Command == command);
            if (cmd == null)
            {
                Log($"Command '{command}' not found!");
                return;
            }
            cmd.Description = description;
        }

        public void InvokeCommand(WFPlayer player, string command, string[] args)
        {
            var cmd = Commands.Find(c => c.Command == command);
            if (cmd == null)
            {
                Log($"Command '{command}' not found!");
                return;
            }
            cmd.Invoke(player, args);
        }

        public bool DoseCommandExist(string command)
        {
            var cmd = Commands.Find(c => c.Command == command);
            if (cmd == null)
                return false;

            return true;
        }
    }
}
