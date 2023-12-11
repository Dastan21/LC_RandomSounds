using UnityEngine;

namespace RandomSounds
{
    internal class RCPSPlayerJoin : MonoBehaviour
    {
        private static int playerCount = 1;
        private static float lobbyCheckTimer;
        private static bool wantToSyncSeed;

        public void Awake() { }

        public void Update()
        {
            if (GameNetworkManager.Instance != null)
            {
                if (playerCount < GameNetworkManager.Instance.connectedPlayers)
                {
                    lobbyCheckTimer = 4.5f;
                    wantToSyncSeed = true;
                }
                playerCount = GameNetworkManager.Instance.connectedPlayers;
            }

            if (lobbyCheckTimer > 0)
            {
                lobbyCheckTimer -= Time.deltaTime;
            }
            else if (wantToSyncSeed)
            {
                wantToSyncSeed = false;
                SyncSeed();
            }
        }

        private static void SyncSeed()
        {
            if (HUDManager.Instance == null || !HUDManager.Instance.IsServer) return;

            RandomSounds.Instance.logger.LogInfo($"Broadcasting seed {RandomSounds.Seed} & offset {RandomSounds.SeedOffset} to other players.");
            LC_API.ServerAPI.Networking.Broadcast(RandomSounds.Seed + "_" + RandomSounds.SeedOffset, RandomSounds.SeedRPCSignature);
        }
    }
}
