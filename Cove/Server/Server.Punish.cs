﻿/*
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

using Cove.Server.Actor;
using Cove.Server.Plugins;
using Steamworks;

namespace Cove.Server
{
    public partial class CoveServer
    {
        public void banPlayer(CSteamID id, bool saveToFile = false, string banReason = "")
        {
            var player = new WFPlayer(
                id,
                Steamworks.SteamFriends.GetFriendPersonaName(id),
                new SteamNetworkingIdentity()
            );

            Dictionary<string, object> banPacket = new();
            banPacket["type"] = "client_was_banned";
            sendPacketToPlayer(banPacket, id);

            Dictionary<string, object> leftPacket = new();
            leftPacket["type"] = "peer_was_banned";
            leftPacket["user_id"] = (long)id.m_SteamID;
            sendPacketToPlayers(leftPacket);

            foreach (PluginInstance plugin in loadedPlugins)
            {
                plugin.plugin.onPlayerBanned(player, banReason);
            }

            if (saveToFile)
                writeToBansFile(id, banReason);

            sendBlacklistPacketToAll(id.m_SteamID.ToString());
        }

        public bool isPlayerBanned(CSteamID id)
        {
            string bansFile = $"{AppDomain.CurrentDomain.BaseDirectory}bans.txt";

            if (!File.Exists(bansFile))
            {
                File.Create(bansFile).Close();
                return false; // no one is banned yet
            }

            string[] fileContent = File.ReadAllLines(bansFile);
            foreach (string line in fileContent)
            {
                if (line.Contains(id.m_SteamID.ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        private void writeToBansFile(CSteamID id, string reason)
        {
            string entry = $"{id.m_SteamID} # ";
            string username = PreviousPlayers.Find(p => p.SteamId == id)?.Username ?? "Unknown";
            entry += username;
            if (!string.IsNullOrEmpty(reason))
                entry += $" - {reason}";
            File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}bans.txt", [entry]);
        }

        public void kickPlayer(CSteamID id)
        {
            Dictionary<string, object> leftPacket = new();
            leftPacket["type"] = "peer_was_kicked";
            leftPacket["user_id"] = (long)id.m_SteamID;
            sendPacketToPlayers(leftPacket);

            Dictionary<string, object> kickPacket = new();
            kickPacket["type"] = "client_was_kicked";
            sendPacketToPlayer(kickPacket, id);
        }
    }
}
