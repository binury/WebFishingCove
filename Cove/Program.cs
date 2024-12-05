/*
   Copyright 2024 DrMeepso

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Cove.Server;
using Cove.Server.Actor;
using Steamworks;
using Serilog;


long epoch = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

var serverLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File($"logs/log-{epoch}.txt") // Daily rolling logs
    .CreateLogger();

//Console.SetOut(new SerilogTextWriter(serverLogger));
//Console.SetError(new SerilogTextWriter(serverLogger, true));  

CoveServer webfishingServer = new CoveServer();
webfishingServer.logger = serverLogger;
try
{
    serverLogger.Information("Starting server...");
    webfishingServer.Init(); // start the server
} catch (Exception e)
{
    serverLogger.Fatal("Error occored on main thread");
    serverLogger.Fatal(e.ToString());
    closeServer();
}

void Log(string message)
{
    serverLogger.Information(message);
}

void closeServer()
{
    Dictionary<string, object> closePacket = new();
    closePacket["type"] = "server_close";

    webfishingServer.loadedPlugins.ForEach(plugin => plugin.plugin.onEnd()); // tell all plugins that the server is closing!

    webfishingServer.disconnectAllPlayers();
    SteamMatchmaking.LeaveLobby(webfishingServer.SteamLobby);
    SteamAPI.Shutdown();
    Environment.Exit(0);
}

Console.CancelKeyPress += Console_CancelKeyPress;
void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    Log("Server closed from input");
    closeServer();
}

while (true)
{
    string input = Console.ReadLine();
    string command = input.Split(' ')[0];

    switch(command)
    {
        case "exit":
            Log("Server closed from console");
            closeServer();
            break;
        case "say":
            {
                string message = input.Substring(command.Length + 1);
                webfishingServer.messageGlobal($"Server: {message}");
                Log($"Server: {message}");
            }
            break;
        case "ban":
            {
                string id = input.Substring(command.Length + 1);
                WFPlayer player = webfishingServer.AllPlayers.Find(p => p.Username.ToLower() == id.ToLower());
                if (player != null)
                {
                    if (webfishingServer.isPlayerBanned(player.SteamId))
                    {
                        Log($"Player {player.Username} is already banned!");
                        break;
                    } else
                    {
                        webfishingServer.banPlayer(player.SteamId, true);
                    }
                    Log($"Banned player {player.Username}");
                }
                else
                {
                    Log("Player not found!");
                }
            }
            break;
        case "kick":
            {
                string id = input.Substring(command.Length + 1);
                WFPlayer player = webfishingServer.AllPlayers.Find(p => p.Username.ToLower() == id.ToLower());
                if (player != null)
                {
                    webfishingServer.kickPlayer(player.SteamId);
                    Log($"Kicked player {player.Username}");
                }
                else
                {
                    Log("Player not found!");
                }
            }
            break;
        case "players":
            Log("Players:");
            foreach (WFPlayer player in webfishingServer.AllPlayers)
            {
                Log(player.Username);
            }
            break;
        case "help":
            Log("Commands:");
            Log("exit - Closes the application");
            Log("say <message> - Sends a message to all players");
            Log("ban <player> - Bans a player");
            Log("kick <player> - Kicks a player");
            Log("help - Shows this message");
            Log("players - Lists all players");
            Log("");
            Log("players are the username of the player");
            break;
        default:
            Log("Unknown command! Type 'help' for a list of commands.");
            break;
    }

}
