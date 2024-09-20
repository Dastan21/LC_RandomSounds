using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace RandomSounds.Networking
{
    [HarmonyPatch]
    public static class SyncSeed
    {
        public static bool requestedSync = false;
        public static bool isSynced = false;
        public static HashSet<ulong> syncedClients = [];

        public static bool IsClient { get => NetworkManager.Singleton.IsClient; }
        public static bool IsServer { get => NetworkManager.Singleton.IsServer; }


        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void ResetValues()
        {
            isSynced = false;
            requestedSync = false;
        }


        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        private static void Init()
        {
            isSynced = false;
            requestedSync = false;
            if (IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("RandomSounds.OnRequestSyncServerRpc", OnRequestSyncServerRpc);
            }
            else if (IsClient)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("RandomSounds.OnRequestSyncClientRpc", OnRequestSyncClientRpc);
            }
        }


        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        private static void RequestSync(PlayerControllerB __instance)
        {
            if (!IsClient || IsServer) return;

            if (!isSynced && !requestedSync && __instance == StartOfRound.Instance?.localPlayerController)
            {
                requestedSync = true;
                SendSyncRequest();
            }
        }


        public static void SendSyncRequest()
        {
            if (!IsClient || IsServer) return;

            RandomSounds.Instance.logger.LogInfo("Sending sync request to server");
            var writer = new FastBufferWriter(0, Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("RandomSounds.OnRequestSyncServerRpc", NetworkManager.ServerClientId, writer);
        }

        private static void OnRequestSyncServerRpc(ulong clientId, FastBufferReader reader)
        {
            if (!IsServer) return;

            RandomSounds.Instance.logger.LogInfo("Receiving sync request from client: " + clientId);
            var writer = new FastBufferWriter(2 * sizeof(int), Allocator.Temp);
            writer.WriteValue(RandomSounds.Seed);
            writer.WriteValue(RandomSounds.SeedOffset);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("RandomSounds.OnRequestSyncClientRpc", clientId, writer);
        }

        private static void OnRequestSyncClientRpc(ulong clientId, FastBufferReader reader)
        {
            if (!IsClient) return;

            reader.ReadValue(out RandomSounds.Seed);
            reader.ReadValue(out RandomSounds.SeedOffset);

            RandomSounds.SyncRandom();
            isSynced = true;

            RandomSounds.Instance.logger.LogInfo($"Receiving sync response from server: {RandomSounds.Seed}+{RandomSounds.SeedOffset}");
        }
    }
}