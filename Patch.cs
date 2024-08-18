using HarmonyLib;
using RCGFSM.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
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
            if (Plugin.Instance.isBossSpeed)
            {
                Traverse.Create(__instance).Field("isFreezing").SetValue(false);
                Plugin.Instance.modifyBossSpeed(Plugin.Instance.bossSpeed);
                return false;
            }

            return true;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(SkillNodeUIControlButton), "UpdateView")]
        public static bool UpdateView(ref SkillNodeUIControlButton __instance)
        {
            __instance.GetComponent<Animator>().SetBool(Animator.StringToHash("Activated"), false);

            return true;
        }

        //[HarmonyPrefix, HarmonyPatch(typeof(PoolObject), "UseSceneAsPool",MethodType.Getter)]
        //public static bool UseSceneAsPool(ref PoolObject __instance, ref bool __result)
        //{
        //    __result = false;
        //    return false;
        //}

        //[HarmonyPrefix, HarmonyPatch(typeof(PoolManager), "ReturnToPool")]
        //public static bool ReturnToPool(ref PoolManager __instance, PoolObject obj)
        //{
        //    if (obj.name.Contains("尋影箭"))
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        //[HarmonyPrefix, HarmonyPatch(typeof(PlayerArrowProjectileFollower), "Update")]
        //public static bool Update(ref PlayerArrowProjectileFollower __instance)
        //{
        //    __instance.minSpeed = 300f;
        //    __instance.MaxSpeed = 300f;
        //    //__instance.SpeedAcc = 0f;
        //    //__instance.SpeedReduce = 100000f;
        //    //__instance.MaxRotateSpeed = 100000f;
        //    //__instance.RedirectionRatio = 100f;
        //    Traverse.Create(__instance).Field("timeCount").SetValue(0f);
        //    //__instance.timeCount = 0f;
        //    //__instance.maxFollowTime = 10f;
        //    //__instance.ForceStartAccTime = 10f;

        //    var projectile = Traverse.Create(__instance).Field("projectile").GetValue<Projectile>(); ;
        //    //var chasingList = Traverse.Create(__instance).Field("chasingList").GetValue<List<MonsterBase>>();

        //    //var firstNonNullMonster = chasingList.FirstOrDefault(monster => monster != null);
        //    //var engagingMonsterList = MonsterManager.Instance.EngagingMonsterList;
        //    //var myList = new List<MonsterBase>(MonsterManager.Instance.monsterDict.Values);
        //    //myList.Sort(delegate (MonsterBase a, MonsterBase b)
        //    //{
        //    //    float num = Vector3.Distance(SingletonBehaviour<GameCore>.Instance.player.transform.position, a.transform.position);
        //    //    float value = Vector3.Distance(SingletonBehaviour<GameCore>.Instance.player.transform.position, b.transform.position);
        //    //    return num.CompareTo(value);
        //    //});

        //    if (true/*myList.Count != 0*/)
        //    {
        //        //var monsterbase = myList[0];
        //        var trueBody = GameObject.Find("P2_R22_Savepoint_GameLevel/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/LogicRoot/ButterFly_BossFight_Logic").GetComponent<ButterflyBossFightLogic>().allMonsters[0].GetComponent<ButterflyTrueBody>();
        //        //Plugin.LogInfo(trueBody);
        //        //var monsterbase = trueBody
        //        //projectile.transform.position = trueBody.transform.position;
        //        //if (monsterbase != null && Vector2.Distance(monsterbase.transform.position, Player.i.transform.position) < radius)
        //        //{
        //        //    if (monsterbase.postureSystem.CurrentHealthValue > 0)
        //        //    {
        //        //        //__instance.ChaseMonsterTarget(monsterbase);
        //        //        projectile.transform.position = monsterbase.transform.position;
        //        //        //projectile.transform.position = Player.i.Center;
        //        //    }
        //        //    else
        //        //    {
        //        //        projectile.transform.position = new Vector3(Player.i.Center.x, Player.i.Center.y + 40f); // or any default position 
        //        //    }
        //        //}
        //        //else
        //        //{
        //        //    // Handle the case where there is no non-null element in the list
        //        //    // For example, you could set a default position or log an error
        //        //    projectile.transform.position = new Vector3(Player.i.Center.x, Player.i.Center.y + 40f); // or any default position                                      // Logger.LogInfo("No non-null monsters found in chasingList.");
        //        //}
        //    }
        //    //else
        //    //{
        //    //    projectile.transform.position = new Vector3(Player.i.Center.x, Player.i.Center.y + 40f); // or any default position                                      // Logger.LogInfo("No non-null monsters found in chasingList.");
        //    //}
        //    /*
        //    PoolManager ReturnToPool
        //    static bool Prefix(PoolManager __instance, PoolObject __0)
        //    {

        //        if(__0.name.Contains("尋影箭"))
        //        {
        //            return false;
        //        }

        //    return true;  
        //    }
        //     */



        //    //if (MonsterManager.Instance.FindClosestMonster() != null)
        //    //{
        //    //    //projectile.transform.position = firstNonNullMonster.transform.position;
        //    //    __instance.ChaseMonsterTarget(MonsterManager.Instance.FindClosestMonster());
        //    //    //projectile.transform.position = MonsterManager.Instance.FindClosestMonster().transform.position;
        //    //}
        //    //else
        //    //{
        //    //    // Handle the case where there is no non-null element in the list
        //    //    // For example, you could set a default position or log an error
        //    //    projectile.transform.position = Player.i.Center; // or any default position
        //    //                                                  // Logger.LogInfo("No non-null monsters found in chasingList.");
        //    //}
        //    return true;
        //}

        //[HarmonyPrefix, HarmonyPatch(typeof(PlayerArrowProjectileFollower), "Update")]
        //public static bool Update(ref PlayerArrowProjectileFollower __instance)
        //{
        //    var projectile = Traverse.Create(__instance).Field("projectile").GetValue<Projectile>();
        //    //var trueBody = GameObject.Find("P2_R22_Savepoint_GameLevel/EventBinder/General Boss Fight FSM Object Variant/FSM Animator/LogicRoot/ButterFly_BossFight_Logic").GetComponent<ButterflyBossFightLogic>().allMonsters[0].GetComponent<ButterflyTrueBody>();
        //    projectile.transform.position = Traverse.Create(typeof(ButterflyTrueBody)).Property("TrueBody").GetValue<ButterflyTrueBody>().transform.position;

        //    return true;
        //}
    }
}
