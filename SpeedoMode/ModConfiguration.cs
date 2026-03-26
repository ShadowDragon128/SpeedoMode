using BepInEx.Configuration;
using HarmonyLib;
using System.Runtime.InteropServices;
using Unity.Netcode;

namespace SpeedoMode
{
    [HarmonyPatch]
    internal static class ModConfiguration
    {
        public static ConfigEntry<float> runningSpeed = ModBase.pluginInstance.Config.Bind<float>("Settings", "RunningSpeed", 100f, "Well its up to you how fast you want to move. Though, the host needs the mod installed and enabled (This is enforced by the Thunderstore ;(, my bad holmes)");
        public static ConfigEntry<string> keyBinding = ModBase.pluginInstance.Config.Bind<string>("Settings", "Keybind", "<Keyboard>/r", "Which key to use as the toggle. Default (R)");

        [HarmonyPatch]
        public class Sync
        {
            public static SyncData instance; // idgaf
            public static bool isSynced = false;

            [StructLayout(LayoutKind.Sequential)]
            public struct SyncData // Just unmanaged types. If you don't, I will look for you, I will find you, Y te voy a dar con EL CINTURÓN >:(. MÁS TE VALE EH.
            {
                public bool isHostEnabled;
                public float speedLimit;
            }

            private const string serverHandlerName = ModBase.pluginName + "-OnTxConfigSync";
            private const string clientHandlerName = ModBase.pluginName + "-OnRxConfigSync";
            private static int retries = 0;

            //StartOfRound.OnPlayerConnectedClientRpc
            //NetworkManager.OnClientStarted
            [HarmonyPatch(typeof(NetworkManager), "Initialize")]
            [HarmonyPostfix]
            public static void OnInitialize(bool server)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += (_) => { SetSyncMode(server); };
            }

            private static void SetSyncMode(bool server)
            {
                if (server)
                {
                    ModBase.logger.LogInfo("Set as Server");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(serverHandlerName, new CustomMessagingManager.HandleNamedMessageDelegate(OnServerConfigSync));
                    instance.isHostEnabled = true;
                    instance.speedLimit = runningSpeed.Value;
                    isSynced = true;
                }
                else
                {
                    ModBase.logger.LogInfo("Set as Client");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(clientHandlerName, new CustomMessagingManager.HandleNamedMessageDelegate(OnClientConfigSync));
                    RequestConfigSync();
                }
            }

            private unsafe static void OnServerConfigSync(ulong clientID, FastBufferReader readBuffer)
            {
                ModBase.logger.LogInfo("Config request received from client: " + clientID);
                FastBufferWriter writeBuffer;

                fixed (SyncData* pSyncData = &instance) // It's just a pointer.
                {
                    byte* pSyncBytes = (byte*)pSyncData;
                    int payloadSize = sizeof(SyncData); // yeah
                    ModBase.logger.LogInfo("Payload size: " + payloadSize + " bytes");

                    // Why JSON? When you CAN DO THIS!! >:)

                    writeBuffer = new FastBufferWriter(payloadSize, Unity.Collections.Allocator.Temp);
                    writeBuffer.WriteBytes(pSyncBytes, payloadSize); // don't need the bound checks for this one
                }

                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(clientHandlerName, clientID, writeBuffer);
                ModBase.logger.LogInfo("Config payload sent");
            }

            private unsafe static void OnClientConfigSync(ulong clientID, FastBufferReader readBuffer)
            {
                int bufferSize = readBuffer.Length - readBuffer.Position; // trust holmes
                if (retries == 3)
                {
                    ModBase.logger.LogError("Failed to sync data, I adviced to check over your mods. Which one? idk bro can't tell.");
                    return;
                }
                else if (bufferSize != sizeof(SyncData))
                {
                    ModBase.logger.LogError("Anomaly Detected: Payload size is " + bufferSize + " bytes, it should be " + sizeof(SyncData) + " bytes");
                    ModBase.logger.LogWarning($"Discarding Data. Retry {retries} of 3");
                    retries++;
                    RequestConfigSync();
                    return;
                }

                ModBase.logger.LogInfo("Received host config. Payload size: " + bufferSize);

                //byte[] syncData = new byte[sizeof(SyncData)]; // to get the sizeof is unsafe apparently
                //readBuffer.ReadBytesSafe(ref syncData, syncData.Length);

                //MemoryMarshal.Read<>(syncData);

                //values = MemoryMarshal.Read<SyncData>(syncData); 

                fixed (SyncData* pSyncData = &instance)
                {
                    byte* pSyncBytes = (byte*)pSyncData; // yes, ah its just data. I̶̖͘T̶̝̄S̷̼̓ ̵̣͆F̶̮͠I̷̥̅N̴̺̊Ē̶̻
                    readBuffer.ReadBytes(pSyncBytes, sizeof(SyncData)); // Alight, you better be safe. OH SHł₮, ØØⱧⱧⱧ ₦Ø ₦Ø ₦Ø₦ ₩₳I̴͇̍Ť̸̨ ̶̲̄W̸͝ͅA̷͈͘I̷̖͑T̶̩̈́ ̵̪̈W̸̛͓A̸̜͑I̴̡̋T̴̻͛
                }

                isSynced = true;
                ModBase.logger.LogInfo("Config synced");
                ModBase.logger.LogInfo("Server Speed Limit: " + Sync.instance.speedLimit);
            }

            private static void RequestConfigSync()
            {
                FastBufferWriter bufferWriter = new FastBufferWriter(4, Unity.Collections.Allocator.Temp);
                bufferWriter.WriteValue<int>(0xD15EA5E); // OHhhhhh, you better watch out.
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(serverHandlerName, 0, bufferWriter);
                ModBase.logger.LogInfo("Config request sent to server. Payload size: " + bufferWriter.Length);
            }
        }
    }
}
