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
using Serilog;
using Steamworks;

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
}
catch (Exception e)
{
    serverLogger.Fatal("Error occored on main thread");
    serverLogger.Fatal(e.ToString());
    webfishingServer.Stop();
}

AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    serverLogger.Fatal("Unhandled exception occured");
    serverLogger.Fatal(args.ExceptionObject.ToString());
    webfishingServer.Stop();
};

void Log(string message)
{
    serverLogger.Information(message);
}

Console.CancelKeyPress += Console_CancelKeyPress;
void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    Log("Server closed from input");
    webfishingServer.Stop();
}

while (true)
{
    var input = Console.ReadLine();

    // prevent null inputs
    if (input == null)
        continue;

    string command = input.Split(' ')[0];

    if (webfishingServer.DoseCommandExist(command))
    {
        string[] commandArgs = input.Split(' ').Skip(1).ToArray();
        webfishingServer.InvokeCommand(webfishingServer.serverPlayer, command, commandArgs);
    }
    else
        Log("Command not found!");
}
