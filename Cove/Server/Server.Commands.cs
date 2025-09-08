﻿using System.Text;
using Cove.Server.Actor;
using Cove.Server.Utils;
using Steamworks;

namespace Cove.Server
{
    public class RegisteredCommand
    {
        public string Command;
        public string Description;
        public Action<WFPlayer, string[]> Callback;

        public RegisteredCommand(
            string command,
            string description,
            Action<WFPlayer, string[]> callback
        )
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

        public void RegisterDefaultCommands()
        {
            RegisterCommand(
                "help",
                (player, args) =>
                {
                    var sb = new StringBuilder();
                    sb.AppendLineLF("Commands:");
                    foreach (var cmd in Commands)
                    {
                        sb.AppendLineLF($"{cmd.Command} - {cmd.Description}");
                    }
                    messagePlayer(sb.ToString(), player.SteamId);
                }
            );
            SetCommandDescription("help", "Shows all commands");

            RegisterCommand(
                "exit",
                (player, args) =>
                {
                    // make sure the player is the host
                    if (player.SteamId != serverPlayer.SteamId)
                    {
                        messagePlayer("You are not the host!", player.SteamId);
                        return;
                    }
                    messagePlayer("Server is shutting down!", player.SteamId);

                    Stop(); // stop the server
                }
            );
            SetCommandDescription("exit", "Shuts down the server (host only)");

            RegisterCommand(
                "kick",
                (player, args) =>
                {
                    if (!isPlayerAdmin(player.SteamId))
                        return;
                    WFPlayer? playerToKick = null;
                    string playerIdent = string.Join(" ", args);

                    var targetIsSteamID = System.Text.RegularExpressions.Regex.IsMatch(
                        playerIdent,
                        @"^7656119\d{10}$"
                    );
                    // find player by username
                    var playerMatchingUsername = AllPlayers
                        .ToList()
                        .Find(p =>
                            p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase)
                        );
                    // find player by fisher ID shortcode
                    var targetIsFID = AllPlayers
                        .ToList()
                        .Find(p =>
                            p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase)
                        );

                    if (targetIsSteamID)
                    {
                        CSteamID steamId = new CSteamID(Convert.ToUInt64(playerIdent));
                        var username = Steamworks.SteamFriends.GetFriendPersonaName(steamId);
                        playerToKick = new WFPlayer(
                            id: steamId,
                            fisherName: username,
                            identity: new SteamNetworkingIdentity()
                        )
                        {
                            Username = username == string.Empty ? playerIdent : username,
                        };
                    }
                    else if (playerMatchingUsername != null)
                    {
                        playerToKick = playerMatchingUsername;
                    }
                    else if (targetIsFID != null)
                    {
                        playerToKick = targetIsFID;
                    }

                    if (playerToKick == null)
                    {
                        messagePlayer(
                            $"${playerIdent} is not a known player or ID",
                            player.SteamId
                        );
                    }
                    else
                    {
                        kickPlayer(playerToKick.SteamId);
                    }
                }
            );
            SetCommandDescription("kick", "!kick (username|steamID|fisherID)");

            RegisterCommand(
                "ban",
                (player, args) =>
                {
                    if (!isPlayerAdmin(player.SteamId))
                        return;

                    WFPlayer? playerToBan = null;
                    string playerIdent;

                    string rawArgs = string.Join(" ", args);
                    string banReason = string.Empty;

                    var numQuotesInArgs = rawArgs.Count(c => c == '"');
                    var hasBanReason = numQuotesInArgs >= 2;
                    // While we'd hope admins use delimiters properly, it's actually totally fine for this case if they
                    // e.g. use quotes inside quotes to quote the target's offending message within the banReason
                    if (hasBanReason)
                    {
                        var firstQuoteIndex = rawArgs.IndexOf('"');
                        var lastQuoteIndex = rawArgs.LastIndexOf('"');
                        banReason = rawArgs
                            .Substring(firstQuoteIndex + 1, lastQuoteIndex - firstQuoteIndex - 1)
                            .Trim();
                        rawArgs = rawArgs.Remove(
                            firstQuoteIndex,
                            lastQuoteIndex - firstQuoteIndex + 1
                        );
                    }
                    playerIdent = rawArgs.Trim();

                    var targetIsSteamID = System.Text.RegularExpressions.Regex.IsMatch(
                        playerIdent,
                        @"^7656119\d{10}$"
                    );

                    // find player by username
                    var playerMatchingUsername = AllPlayers
                        .ToList()
                        .Find(p =>
                            p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase)
                        );
                    // find player by fisher ID shortcode
                    var targetIsFID = AllPlayers
                        .ToList()
                        .Find(p =>
                            p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase)
                        );

                    if (targetIsSteamID)
                    {
                        CSteamID steamId = new CSteamID(Convert.ToUInt64(playerIdent));
                        var username = Steamworks.SteamFriends.GetFriendPersonaName(steamId);
                        playerToBan = new WFPlayer(steamId, username, new SteamNetworkingIdentity())
                        {
                            Username = username == string.Empty ? playerIdent : username,
                        };
                    }
                    else if (playerMatchingUsername != null)
                    {
                        playerToBan = playerMatchingUsername;
                    }
                    else if (targetIsFID != null)
                    {
                        playerToBan = targetIsFID;
                    }
                    else
                    {
                        // (Defer these searches to last resort, as they could potentially be costlier)
                        var previousPlayer = PreviousPlayers
                            .ToList()
                            .Find(p =>
                                $"#{p.FisherID}".Equals(
                                    playerIdent,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            );
                        if (previousPlayer != null)
                        {
                            messagePlayer(
                                $"There is a previous player with that FisherID, if you meant to ban them add a # before the ID: #{playerIdent}",
                                player.SteamId
                            );
                            return;
                        }
                        previousPlayer = PreviousPlayers
                            .ToList()
                            .Find(p =>
                                p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase)
                            );
                        if (previousPlayer != null)
                        {
                            playerToBan = new WFPlayer(
                                previousPlayer.SteamId,
                                previousPlayer.Username,
                                new SteamNetworkingIdentity()
                            );
                        }
                    }

                    if (playerToBan == null)
                    {
                        messagePlayer("Player not found!", player.SteamId);
                    }
                    else
                    {
                        banPlayer(
                            playerToBan.SteamId,
                            !isPlayerBanned(playerToBan.SteamId),
                            banReason
                        );
                    }
                }
            );
            SetCommandDescription(
                "ban",
                "Usage: !ban (username|steamID|FisherID) \"Reason for ban\""
            );

            RegisterCommand(
                "prev",
                (player, args) =>
                {
                    if (!isPlayerAdmin(player.SteamId))
                        return;
                    var sb = new StringBuilder();
                    sb.AppendLineLF("Previous Players:");
                    foreach (var prevPlayer in PreviousPlayers)
                    {
                        if (prevPlayer.State == PlayerState.InGame)
                            continue;

                        if (
                            (
                                DateTime.UtcNow
                                - DateTimeOffset
                                    .FromUnixTimeSeconds(prevPlayer.leftTimestamp)
                                    .UtcDateTime
                            ).TotalMinutes > 29
                        )
                        {
                            continue;
                        }

                        // get the time since the player left in a human readable format
                        string timeLeft =
                            $"{Math.Round((DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(prevPlayer.leftTimestamp).UtcDateTime).TotalMinutes)} minutes ago";
                        sb.Append(
                            $"{prevPlayer.Username} ({prevPlayer.FisherID}) - Left: {timeLeft}\n"
                        );
                    }
                    messagePlayer(sb.ToString(), player.SteamId);
                }
            );
            SetCommandDescription(
                "prev",
                "Shows a list of previous players that were connected to the server"
            );
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
