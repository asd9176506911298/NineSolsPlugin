using HarmonyLib;
using UnityEngine;

namespace NineSolsPlugin
{
    public class Patch
    {
        //Console
        [HarmonyPrefix, HarmonyPatch(typeof(QFSW.QC.QuantumConsole), "IsSupportedState")]
        public static bool IsSupportedState(ref bool __result)
        {
            if (Plugin.Instance.isEnableConsole.Value)
                __result = true;
            else
                return true;

            return false;
        }

        //秒殺
        [HarmonyPrefix, HarmonyPatch(typeof(MonsterBase), "DecreasePosture")]
        public static bool DecreasePosture(ref EffectHitData data, ref float scale)
        {
            if (Plugin.Instance.isOneHitKill)
                scale = 9999f;

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(FOV_Follower), "Update")]
        public static bool FOV_Follower_Update(ref FOV_Follower __instance)
        {
            //Debug.Log($"FOV_Follower_Update {__instance} isFov:{Plugin.Instance.isFov} fov:{Plugin.Instance.fov}");
            if (__instance.followCamera == null)
            {
                return false;
            }

            if (Plugin.Instance.isFov && Plugin.Instance.fov > 0)
                Traverse.Create(__instance).Field("mCamera").GetValue<Camera>().fieldOfView = Plugin.Instance.fov;
            else
                Traverse.Create(__instance).Field("mCamera").GetValue<Camera>().fieldOfView = __instance.followCamera.fieldOfView;

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SkillTreeUI), "UpgradeCheck")]
        public static bool UpgradeCheck(ref SkillTreeUI __instance)
        {
            if (Plugin.Instance.isFastLearnSkill)
                __instance.longPressSubmit.submitTime = 0f;
            else
                __instance.longPressSubmit.submitTime = 1f;
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BossGeneralState), "OnStateUpdate")]
        public static bool OnStateUpdate(ref BossGeneralState __instance)
        {
            if (Plugin.Instance.showSupportWindow)
                Plugin.Instance.SupportText = __instance.gameObject.name;
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MonsterBase), "UnFreeze")]
        public static bool UnFreeze(ref MonsterBase __instance)
        {
            if(Plugin.Instance.isBossSpeed)
            {
                Traverse.Create(__instance).Field("isFreezing").SetValue(false);
                Plugin.Instance.modifyBossSpeed(Plugin.Instance.bossSpeed);
                return false;
            }

            return true;
        }
    }
}
