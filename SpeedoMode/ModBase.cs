using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace SpeedoMode
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ModBase : BaseUnityPlugin
    {
        public const string pluginGuid = "ShadowAPI." + pluginName;
        public const string pluginName = "SpeedoMode";
        public const string pluginVersion = "2.0.0";

        internal static ManualLogSource logger { get { return _logger; } }
        private static ManualLogSource _logger;

        internal static Harmony harmonyInstance { get { return _harmonyInstance; } }
        private static Harmony _harmonyInstance;

        internal static BaseUnityPlugin pluginInstance { get { return _pluginInstance; } }
        private static BaseUnityPlugin _pluginInstance;

        private void Awake()
        {
            _logger = Logger;
            _logger.LogInfo("SikoMode");

            _pluginInstance = this;

            _harmonyInstance = new Harmony(pluginGuid);

            _harmonyInstance.PatchAll();
        }
    }

    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        private static InputAction action;

        private static bool sprintToggled;

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void UpdateFunc(ref float ___sprintMultiplier, ref bool ___isSprinting, ref bool ___isFallingFromJump)
        {
            if (ModConfiguration.Sync.instance.isHostEnabled && ___isSprinting && sprintToggled) // if the host dosen't have it shouldn't activate otherwise it will
            {
                float localMaxSpeed = Math.Max(ModConfiguration.runningSpeed.Value, ___sprintMultiplier);

                ___sprintMultiplier = NetworkManager.Singleton.IsClient && ModConfiguration.Sync.instance.speedLimit > ___sprintMultiplier ? Math.Min(ModConfiguration.Sync.instance.speedLimit, localMaxSpeed) : localMaxSpeed;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
        [HarmonyPostfix]
        public static void AwakeFunc(PlayerControllerB __instance)
        {
            if (action != null)
                return;

            ModBase.logger.LogInfo(ModConfiguration.keyBinding.Value);
            action = new InputAction("Toggle", InputActionType.Value, ModConfiguration.keyBinding.Value);
            action.performed += delegate
            {
                sprintToggled = !sprintToggled;
            };
            action.Enable();

            ModBase.harmonyInstance.Unpatch(typeof(PlayerControllerB).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic), typeof(PlayerControllerBPatch).GetMethod(nameof(AwakeFunc))); // Unpatch me baby
            ModBase.logger.LogInfo("PlayerControllerB.AwakeFunc Unpatched");
        }
    }
}
