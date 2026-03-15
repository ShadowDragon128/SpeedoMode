using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace SpeedoMode
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ModBase : BaseUnityPlugin
    {
        private const string pluginGuid = "ShadowAPI.LethalCompany.SpeedoMode";
        private const string pluginName = "SpeedoMode";
        private const string pluginVersion = "1.0.0";

        public static ManualLogSource Logger_;

        private void Awake()
        {
            Logger.LogInfo("Awake Called");
            Logger_ = Logger;
            Logger.LogInfo("SikoMode");

            Harmony harmonyInstance = new Harmony(pluginGuid);

            harmonyInstance.PatchAll(typeof(PlayerControllerBPatch));
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static InputAction action;

        private static bool sprintToggled;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void UpdateFunc(ref float ___sprintMultiplier, ref bool ___isSprinting, ref bool ___isFallingFromJump)
        {
            if (___isSprinting && sprintToggled)
            {
                ___sprintMultiplier = 100f;
            }
        }

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void AwakeFunc()
        {
            ModBase.Logger_.LogInfo("AwakeFunc Called");
            action = new InputAction("Toggle", InputActionType.Value, "<Keyboard>/r");
            action.performed += delegate
            {
                sprintToggled = !sprintToggled;
            };
            action.Enable();
        }
    }
}
