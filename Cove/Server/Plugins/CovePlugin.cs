using Cove.Server.Actor;

namespace Cove.Server.Plugins
{
    public class CovePlugin
    {

        public CoveServer ParentServer;

        public CovePlugin(CoveServer parent)
        {
            ParentServer = parent;
        }

        // triggered when the plugin is started
        public virtual void onInit() { }
        // triggered when the plugin is stopped or the server is stopped
        public virtual void onEnd() { }
        // triggered 12/s
        public virtual void onUpdate() { }
        // triggered when a player speaks in anyway (exluding / commands)
        public virtual void onChatMessage(WFPlayer sender, string message) { }
        // triggered when a player enters the server
        public virtual void onPlayerJoin(WFPlayer player) { }
        // triggered when a player leaves the server
        public virtual void onPlayerLeave(WFPlayer player) { }

        public WFPlayer[] GetAllPlayers()
        {
            return ParentServer.AllPlayers.ToArray();
        }

        public void SendPlayerChatMessage(WFPlayer receiver, string message)
        {
            // remove a # incase its given
            ParentServer.messagePlayer(message, receiver.SteamId);
        }

        public void SendGlobalChatMessage(string message)
        {
            ParentServer.messageGlobal(message);
        }

        public WFActor[] GetAllServerActors()
        {
            return ParentServer.serverOwnedInstances.ToArray();
        }

        public WFActor? GetActorFromID(int id)
        {
            return ParentServer.serverOwnedInstances.Find(a => a.InstanceID == id);
        }

        // please make sure you use the correct actorname or the game freaks out!
        public WFActor SpawnServerActor(string actorType)
        {
            return ParentServer.spawnGenericActor(actorType);
        }

        public void RemoveServerActor(WFActor actor)
        {
            ParentServer.removeServerActor(actor);
        }

        // i on god dont know what this dose to the actual actor but it works in game, so if you nee this its here
        public void SetServerActorZone(WFActor actor, string zoneName, int zoneOwner)
        {
            ParentServer.setActorZone(actor, zoneName, zoneOwner);
        }

        public void KickPlayer(WFPlayer player)
        {
            ParentServer.kickPlayer(player.SteamId);
        }

        public void BanPlayer(WFPlayer player)
        {
            if (ParentServer.isPlayerBanned(player.SteamId))
            {
                ParentServer.banPlayer(player.SteamId);
            } else
            {
                ParentServer.banPlayer(player.SteamId, true); // save to file if they are not already in there!
            }
        }

        public void Log(string message)
        {
            ParentServer.printPluginLog(message, this);
        }

        public bool IsPlayerAdmin(WFPlayer player)
        {
            return ParentServer.isPlayerAdmin(player.SteamId);
        }

        public void SendPacketToPlayer(Dictionary<string, object> packet, WFPlayer player)
        {
            ParentServer.sendPacketToPlayer(packet, player.SteamId);
        }

        public void SendPacketToAll(Dictionary<string, object> packet)
        {
            ParentServer.sendPacketToPlayers(packet);
        }

    }
}
