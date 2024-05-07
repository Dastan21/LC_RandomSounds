using Unity.Netcode;

namespace RandomSounds
{
    internal class NetworkHandler
    {
        public static NetworkHandler Instance { get; private set; }

        [ClientRpc]
        public void ReceiveSeedSyncClientRpc(int seed, int offset)
        {
            RandomSounds.SetSeedSync(seed, offset);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendSeedSyncServerRpc(int seed, int offset)
        {
            ReceiveSeedSyncClientRpc(seed, offset);
        }
    }
}
