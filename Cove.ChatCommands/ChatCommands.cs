﻿using System;
using Cove.Server;
using Cove.Server.Actor;
using Cove.Server.Plugins;
using Steamworks;

public class ChatCommands : CovePlugin
{
    CoveServer Server { get; set; } // lol

    public ChatCommands(CoveServer server)
        : base(server)
    {
        Server = server;
    }

    // save the time the server was started
    public long serverStartTime = DateTimeOffset.Now.ToUnixTimeSeconds();

    public override void onInit()
    {
        base.onInit();

        RegisterCommand(
            "users",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;
                // Get the command arguments
                int pageNumber = 1;
                int pageSize = 10;
                // Check if a page number was provided
                if (args.Length > 0)
                {
                    if (!int.TryParse(args[0], out pageNumber) || pageNumber < 1)
                    {
                        pageNumber = 1; // Default to page 1 if parsing fails or page number is less than 1
                    }
                }
                var allPlayers = GetAllPlayers();
                int totalPlayers = allPlayers.Length;
                int totalPages = (int)Math.Ceiling((double)totalPlayers / pageSize);
                // Ensure the page number is within the valid range
                if (pageNumber > totalPages)
                    pageNumber = totalPages;
                // Get the players for the current page
                var playersOnPage = allPlayers.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                // Build the message to send
                string messageBody = "";
                foreach (var iPlayer in playersOnPage)
                {
                    messageBody += $"\n{iPlayer.Username}: {iPlayer.FisherID}";
                }
                messageBody += $"\nPage {pageNumber} of {totalPages}";
                SendPlayerChatMessage(
                    player,
                    "Players in the server:" + messageBody + "\nAlways here - Cove"
                );
            }
        );
        SetCommandDescription("users", "Shows all players in the server");

        RegisterCommand(
            "spawn",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;
                if (args.Length == 0)
                {
                    SendPlayerChatMessage(player, "You didn't add an argument!");
                    return;
                }
                var actorType = args[0].ToLower();
                bool spawned = false;
                switch (actorType)
                {
                    case "rain":
                        Server.spawnRainCloud();
                        spawned = true;
                        break;
                    case "fish":
                        Server.spawnFish();
                        spawned = true;
                        break;
                    case "meteor":
                    case "meatball":
                        spawned = true;
                        Server.spawnFish("fish_spawn_alien");
                        break;
                    case "portal":
                        Server.spawnVoidPortal();
                        spawned = true;
                        break;
                    case "metal":
                        Server.spawnMetal();
                        spawned = true;
                        break;
                }
                if (spawned)
                {
                    SendPlayerChatMessage(player, $"Spawned {actorType}");
                }
                else
                {
                    SendPlayerChatMessage(player, $"\"{actorType}\" is not a spawnable actor!");
                }
            }
        );
        SetCommandDescription("spawn", "Spawns an actor");

        /*
        RegisterCommand("kick", (player, args) =>
        {
            if (!IsPlayerAdmin(player)) return;
            string playerIdent = string.Join(" ", args);
            // try find a user with the username first
            WFPlayer kickedplayer = GetAllPlayers().ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            // if there is no player with the username try find someone with that fisher ID
            if (kickedplayer == null)
                kickedplayer = GetAllPlayers().ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            if (kickedplayer == null)
            {
                SendPlayerChatMessage(player, "That's not a player!");
            }
            else
            {
                SendPlayerChatMessage(player, $"Kicked {kickedplayer.Username}");
                KickPlayer(kickedplayer);
                //SendGlobalChatMessage($"{kickedplayer.Username} was kicked from the lobby!");
            }
        });
        SetCommandDescription("kick", "Kicks a player from the server");

        RegisterCommand("ban", (player, args) =>
        {
            if (!IsPlayerAdmin(player)) return;
            // hacky fix,
            // Extract player name from the command message
            string playerIdent = string.Join(" ", args);
            // try find a user with the username first
            WFPlayer playerToBan = GetAllPlayers().ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            // if there is no player with the username try find someone with that fisher ID
            if (playerToBan == null)
                playerToBan = GetAllPlayers().ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            if (playerToBan == null)
            {
                SendPlayerChatMessage(player, "Player not found!");
            }
            else
            {
                BanPlayer(playerToBan);
                SendPlayerChatMessage(player, $"Banned {playerToBan.Username}");
                SendGlobalChatMessage($"{playerToBan.Username} has been banned from the server.");
            }
        });
        SetCommandDescription("ban", "Bans a player from the server");
        */

        RegisterCommand(
            "setjoinable",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;
                string arg = args[0].ToLower();
                if (arg == "true")
                {
                    SteamMatchmaking.SetLobbyJoinable(Server.SteamLobby, true);
                    SendPlayerChatMessage(player, $"Opened lobby!");
                    if (!Server.codeOnly)
                    {
                        SteamMatchmaking.SetLobbyData(Server.SteamLobby, "type", "public");
                        SendPlayerChatMessage(player, $"Unhid server from server list");
                    }
                }
                else if (arg == "false")
                {
                    SteamMatchmaking.SetLobbyJoinable(Server.SteamLobby, false);
                    SendPlayerChatMessage(player, $"Closed lobby!");
                    if (!Server.codeOnly)
                    {
                        SteamMatchmaking.SetLobbyData(Server.SteamLobby, "type", "code_only");
                        SendPlayerChatMessage(player, $"Hid server from server list");
                    }
                }
                else
                {
                    SendPlayerChatMessage(player, $"\"{arg}\" is not true or false!");
                }
            }
        );
        SetCommandDescription("setjoinable", "Sets the lobby to joinable or not");

        RegisterCommand(
            "refreshadmins",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;
                Server.readAdmins();
            }
        );
        SetCommandDescription("refreshadmins", "Refreshes the admin list");

        RegisterCommand(
            "uptime",
            (player, args) =>
            {
                long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                long uptime = currentTime - serverStartTime;
                TimeSpan time = TimeSpan.FromSeconds(uptime);
                int days = time.Days;
                int hours = time.Hours;
                int minutes = time.Minutes;
                int seconds = time.Seconds;
                string uptimeString = "";
                if (days > 0)
                {
                    uptimeString += $"{days} Days, ";
                }
                if (hours > 0)
                {
                    uptimeString += $"{hours} Hours, ";
                }
                if (minutes > 0)
                {
                    uptimeString += $"{minutes} Minutes, ";
                }
                if (seconds > 0)
                {
                    uptimeString += $"{seconds} Seconds";
                }
                SendPlayerChatMessage(player, $"Server uptime: {uptimeString}");
            }
        );
        SetCommandDescription("uptime", "Shows the server uptime");

        RegisterCommand(
            "say",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;
                string message = string.Join(" ", args);
                SendGlobalChatMessage($"[Server] {message}");
            }
        );
        SetCommandDescription("say", "Sends a message to all players");

        RegisterCommand(
            "chalkrecent",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;

                // rearange the list so the most recent is first
                List<WFPlayer> reversedList = new List<WFPlayer>(lastToUseChalk);
                reversedList.Reverse();

                string message = "Most recent chalk users:";
                foreach (var p in reversedList)
                {
                    message += $"\n{p.Username}: {p.FisherID}";
                }

                SendPlayerChatMessage(player, message);
            }
        );
        SetCommandDescription("chalkrecent", "Shows the 10 most recent players to use chalk");

        RegisterCommand(
            "reload",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;

                SendPlayerChatMessage(
                    player,
                    "Reloading plugins, this can cause unintended behaviour in plugins that don't cleanup properly"
                );

                foreach (PluginInstance plugin in Server.loadedPlugins)
                {
                    plugin.plugin.onEnd();
                    Log($"Unloaded plugin {plugin.plugin.GetType().Name}");
                }

                Server.loadedPlugins.Clear(); // clear the list

                Server.loadAllPlugins(true); // reload all plugins

                SendPlayerChatMessage(player, "Plugins have been reloaded!");
            }
        );
        SetCommandDescription("reload", "Reloads all plugins");

        RegisterCommand(
            "plugins",
            (player, args) =>
            {
                if (!IsPlayerAdmin(player))
                    return;
                string message = "Loaded plugins:";
                foreach (PluginInstance plugin in Server.loadedPlugins)
                {
                    message += $"\n{plugin.pluginName} - {plugin.pluginAuthor}";
                }
                SendPlayerChatMessage(player, message);
            }
        );
        SetCommandDescription("plugins", "Shows all loaded plugins");
    }

    private List<WFPlayer> lastToUseChalk = new();

    public override void onNetworkPacket(WFPlayer sender, Dictionary<string, object> packet)
    {
        base.onNetworkPacket(sender, packet);

        object value;
        if (packet.TryGetValue("type", out value))
        {
            if (typeof(string) != value.GetType())
                return;

            string type = value as string;
            if (type == "chalk_packet")
            {
                lastToUseChalk.Add(sender);
                if (lastToUseChalk.Count > 10)
                {
                    lastToUseChalk.RemoveAt(0);
                }
            }
        }
    }

    public override void onEnd()
    {
        base.onEnd();

        // unregister all commands
        // this is needed to allow for the plugin to be reloaded!
        UnregisterCommand("users");
        UnregisterCommand("spawn");
        UnregisterCommand("kick");
        UnregisterCommand("ban");
        UnregisterCommand("setjoinable");
        UnregisterCommand("refreshadmins");
        UnregisterCommand("uptime");
        UnregisterCommand("say");
        UnregisterCommand("chalkrecent");
        UnregisterCommand("reload");
        UnregisterCommand("plugins");
    }
}
