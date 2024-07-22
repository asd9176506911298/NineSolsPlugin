using Battlehub.RTHandles;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
            if(Plugin.Instance.isOneHitKill)
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
    }
}
