using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using RCGFSM.PlayerAbility;
using UnityEngine.UI;
using System.Collections;
using static Linefy.PolygonalMesh;
using static UnityEngine.UI.Image;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine.Pool;
using UnityEngine.Events;

namespace NineSolsPlugin
{
    [BepInPlugin("NineSols.Yukikaco.plugin", "Nine Sols Cheat Menu Made By Yuki.kaco", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }

        private bool isInit = false;
        private Vector2Int lastScreenSize;
        private GUIStyle titleStyle;
        private GUIStyle toggleStyle;
        private GUIStyle textFieldStyle;
        private GUIStyle buttonStyle;
        private GUIStyle supportTextStyle;
        private float basetitleSize = 24.0f;
        private float baseToggleSize = 24.0f;
        private float baseTextFieldSize = 24.0f;
        private float baseButtonSize = 24.0f;


        private LocalizationManager localizationManager;

        private ConfigEntry<KeyCode> MenuToggleKey;
        private ConfigEntry<KeyCode> SpeedToggleKey;
        private ConfigEntry<KeyCode> FovToggleKey;
        private ConfigEntry<KeyCode> MouseTeleportKey;
        private ConfigEntry<KeyCode> SkipKey;
        private ConfigEntry<string> Language;
        public ConfigEntry<bool> isEnableConsole;

        private bool showMenu = false;
        public bool isFov = false;
        public bool isOneHitKill = false;
        public bool isInvincible = false;
        public bool isGetAll = false;
        public bool isGetAllWithoutActiveSkill = false;
        public bool isSpeed = false;
        public bool isFastLearnSkill = false;
        public bool isAutoHeal = false;
        public bool isInfiniteChi = false;
        public bool isInfinitePotion = false;
        public bool isInfiniteAmmo = false;
        public bool isBossSpeed = false;
        public bool isAttackMult = false;
        public bool isInjeryMult = false;
        private bool previousIsBossSpeed;
        private bool previousIsAttackMult;
        private bool previousIsInjeryMult;
        public float fov = 68f;
        public float speed = 2f;
        public float bossSpeed = 1f;
        public float attackMult = 1f;
        public float injeryMult = 1f;
        private float previousBossSpeed;
        private float previousAttackMult;
        private float previousInjeryMult;
        private string speedInput = "2";
        private string bossSpeedInput = "1";
        private string attackMultInput = "1";
        private string injeryMultInput = "1";
        private Rect windowRect;
        private Rect supportRect;

        public bool showSupportWindow = false;
        public string SupportText = "test";
        private bool isShowSupportWindowNoBackGround = false;

        private void Awake()
        {
            RCGLifeCycle.DontDestroyForever(gameObject);
            Debug.Log("九日修改器");
            Instance = this;

            MenuToggleKey = Config.Bind<KeyCode>("Menu", "MenuToggleKey", KeyCode.F3, "Open Cheat Menu ShortCut\n開啟選單快捷鍵\n开启选单热键");
            SpeedToggleKey = Config.Bind<KeyCode>("Menu", "SpeedToggleKey", KeyCode.F4, "Timer ShortCut\n加速快捷鍵\n加速热键");
            FovToggleKey = Config.Bind<KeyCode>("Menu", "FOVToggleKey", KeyCode.F5, "FOV ShortCut\nFOV快捷鍵\nFOV热键");
            MouseTeleportKey = Config.Bind<KeyCode>("Menu", "MouseTeleportKey", KeyCode.F2, "Mouse Move Character ShortCut\n滑鼠移動快捷鍵\n滑鼠移动热键");
            SkipKey = Config.Bind<KeyCode>("Menu", "SkilKey", KeyCode.LeftControl, "Skip ShortCut\n跳過快捷鍵\n跳過热键");
            isEnableConsole = Config.Bind<bool>("Menu", "isEnableConsole", true, "Is Enable Console? F1 Open Console\n是否開啟控制台 F1開啟控制台\n是否开启控制台 F1开启控制台");
            Language = Config.Bind<string>("Menu", "MenuLanguage", "en-us", "Menu Language\n選單語言\n选单语言\nen-us, zh-tw, zh-cn");

            localizationManager = new LocalizationManager();
            localizationManager.SetLanguage(Language.Value);

            Harmony.CreateAndPatchAll(typeof(Patch));

            // Initialize window size based on screen dimensions
            float width = Screen.width * 0.5f;
            float height = Screen.height * 0.92f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            supportRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height / 6);
        }

        private void Start()
        {
            previousIsBossSpeed = isBossSpeed;
            previousBossSpeed = bossSpeed;

            previousIsAttackMult = isAttackMult;
            previousAttackMult = attackMult;

            previousIsInjeryMult = isInjeryMult;
            previousInjeryMult = injeryMult;
        }

        async void Kanghui(string SceneName, Vector3 teleportPostion, List<string> flags = null)
        {
            HandleTeleportButtonClick(SceneName, teleportPostion);

            if (flags != null)
            {
                foreach (var flag in flags)
                {
                    ModifyFlag(flag, 1);
                }
            }

            await checkMove();

            foreach (PlayerAbilityModifyPackApplyAction playerAbilityModifyPackApplyAction in UnityEngine.Object.FindObjectsOfType<PlayerAbilityModifyPackApplyAction>())
            {
                playerAbilityModifyPackApplyAction.ExitLevelAndDestroy(); //A5 Jail Debuff Pack 虛弱監獄 (PlayerAbilityScenarioModifyPack)
            }
        }

        async void Jee(string SceneName, Vector3 teleportPostion, List<string> flags = null)
        {
            HandleTeleportButtonClick(SceneName, teleportPostion);

            if (flags != null)
            {
                foreach (var flag in flags)
                {
                    ModifyFlag(flag, 1);
                }
            }

            await checkMove();

            if (Player.i != null)
                Traverse.Create(Player.i).Method("UnlockParryJumpKickAbility").GetValue();
        }

        async void PerformActionsAfterTeleport(string SceneName, Vector3 teleportPostion, List<string> flags = null)
        {
            HandleTeleportButtonClick(SceneName, teleportPostion);

            //HandleTeleportButtonClick("A2_S2_ReactorRight_Final", new Vector3(-4690, -1968, 0f)); // 天綱步衛－角端
            //ModifyFlag("574d3e20-47c5-4841-a21c-121d7806ed6e_c3c3f30fb046d9743aea48eb8f4833bcScriptableDataBool", 1);
            //ModifyFlag("5d67c34b-0553-482f-8e4d-dd4c02d0c359_c3c3f30fb046d9743aea48eb8f4833bcScriptableDataBool", 1);
            //ModifyFlag("ff553e6df36c89644ae08124aaa2913eScriptableDataBool", 1);
            if(flags != null)
            {
                foreach (var flag in flags)
                {
                    ModifyFlag(flag, 1);
                }
            }

            await checkMove();

            // Now that the player is on the ground, call KillAllEnemies
            KillAllEnemiesExcept(MonsterLevel.MiniBoss);
        }

        async UniTask checkMove()
        {
            // Wait until the player is instantiated and on the ground
            while (Player.i == null || Player.i.moveVec.x == 0)
            {
                await UniTask.Yield();
            }
        }

        private async UniTaskVoid PrePrecoess(string sceneName, TeleportPointData teleportPointData)
        {
            
            await WaitForSceneLoad(sceneName);

            await WaitForEnterGame();
            
            GameCore.Instance.DiscardUnsavedFlagsAndReset();
            if(teleportPointData != null)
                GameCore.Instance.TeleportToSavePoint(teleportPointData);
            // Now the scene is loaded, run the appropriate method
            checkMultiplier();
            CheckGetAll();
        }

        private async UniTask WaitForEnterGame()
        {
            // Wait until GameCore.Instance is not null
            while (GameCore.Instance == null)
            {
                await UniTask.Yield();
            }

            // Wait until GameCore.Instance.currentCoreState is Playing
            while (GameCore.Instance.currentCoreState != GameCore.GameCoreState.Playing)
            {
                await UniTask.Yield();
            }
        }

        private async UniTask WaitForSceneLoad(string sceneName)
        {
            // Wait until the scene is loaded
            while (SceneManager.GetActiveScene().name != sceneName)
            {
                await UniTask.Yield();
            }
        }

        void OnScreenSizeChanged(float width, float height)
        {
            width *= 0.5f;
            height *= 0.92f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            supportRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height / 6);
            // Implement your logic here when screen size changes
            Debug.Log($"Screen size changed to: {width}x{height}");
            if(isInit)
                UpdateGUIStyle();
        }

        void UpdateGUIStyle()
        {
            float scaleFactor = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            int toggleSize = Mathf.RoundToInt(baseToggleSize * scaleFactor);
            int buttonSize = Mathf.RoundToInt(baseButtonSize * scaleFactor);
            int textFieldSize = Mathf.RoundToInt(baseTextFieldSize * scaleFactor);
            int supportTextSize = Mathf.RoundToInt(baseTextFieldSize * scaleFactor * 2);

            toggleStyle.fontSize = toggleSize;
            toggleStyle.padding = new RectOffset(toggleSize * 2, toggleSize * 2, toggleSize / 2, toggleSize / 2);
            textFieldStyle.fontSize = textFieldSize;
            buttonStyle.fontSize = buttonSize;
            buttonStyle.padding = new RectOffset(buttonSize / 3, buttonSize / 3, buttonSize / 3, buttonSize / 3);
            titleStyle.fontSize = Mathf.RoundToInt(basetitleSize * scaleFactor);
            supportTextStyle.fontSize = supportTextSize;
        }

        private void OnDestory()
        {
            Harmony.UnpatchAll();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckScreenSize()
        {
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            {
                // Update lastScreenSize with the current screen dimensions
                lastScreenSize.x = Screen.width;
                lastScreenSize.y = Screen.height;


                // Call a method or raise an event indicating screen size change
                OnScreenSizeChanged(lastScreenSize.x, lastScreenSize.y);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessShortCut()
        {
            if (Input.GetKeyDown(MenuToggleKey.Value))
            {
                showMenu = !showMenu;
            }

            if (Input.GetKeyDown(SpeedToggleKey.Value))
            {
                isSpeed = !isSpeed;
            }

            if (Input.GetKeyDown(FovToggleKey.Value))
            {
                isFov = !isFov;
            }

            if (Input.GetKeyDown(SkipKey.Value))
            {
                SkippableManager.Instance.TrySkip();
            }

            if (Input.GetKey(MouseTeleportKey.Value))
            {
                var cheatManagerInstance = CheatManager.Instance;
                if (cheatManagerInstance != null && Player.i != null)
                {
                    Traverse.Create(cheatManagerInstance).Method("DropPlayerAtMousePosition").GetValue();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TickLogic()
        {
            if (showMenu || Input.GetKey(MouseTeleportKey.Value))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (isFov)
            {
                float scrollInput = Input.GetAxis("Mouse ScrollWheel") * 30f;
                float newFov = fov - scrollInput;
                if (newFov > 0 && newFov < 180)
                    fov = newFov;
            }

            TimePauseManager.GlobalSimulationSpeed = isSpeed && speed > 0 ? speed : 1f;

            if (isInvincible)
            {
                if (Player.i != null && Player.i.GetHealth != null)
                    Player.i.GetHealth.isInvincibleVote.Vote(Player.i.gameObject, true);
            }
            else
            {
                if (Player.i != null && Player.i.GetHealth != null)
                    Player.i.GetHealth.isInvincibleVote.Vote(Player.i.gameObject, false);
            }

            if (Player.i != null)
            {
                if (isAutoHeal && Player.i.health != null)
                    Player.i.health.GainFull();

                if (isInfiniteChi && Player.i.chiContainer != null)
                    Player.i.chiContainer.GainFull();

                if (isInfinitePotion && Player.i.potion != null)
                    Player.i.potion.GainFull();

                if (isInfiniteAmmo && Player.i.ammo != null)
                    Player.i.ammo.GainFull();
            }

            if (isBossSpeed != previousIsBossSpeed || bossSpeed != previousBossSpeed)
            {
                    modifyBossSpeed(bossSpeed);
            }
            previousIsBossSpeed = isBossSpeed;
            previousBossSpeed = bossSpeed;

            if (isAttackMult != previousIsAttackMult || attackMult != previousInjeryMult)
                checkMultiplier();

            previousIsAttackMult = isAttackMult;
            previousAttackMult = attackMult;

            if (isInjeryMult != previousIsInjeryMult || injeryMult != previousInjeryMult)
                checkMultiplier();

            previousIsInjeryMult = isInjeryMult;
            previousInjeryMult = injeryMult;
        }
        
        private void checkMultiplier()
        {
            if (isAttackMult)
                modifyStat("0_PlayerAttackBaseDamageRatio 主角攻擊的基礎倍率", attackMult);
            else
                modifyStat("0_PlayerAttackBaseDamageRatio 主角攻擊的基礎倍率", 1.0f);

            if (isInjeryMult)
                modifyStat("1_PlayerTakeDamageRatio 主角受傷倍率", injeryMult);
            else
                modifyStat("1_PlayerTakeDamageRatio 主角受傷倍率", 1.0f);
        }

        private void modifyStat(string name, float value)
        {
            if (SaveManager.Instance == null) return;
            if (SaveManager.Instance.allStatData == null) return;

            var allStatData = SaveManager.Instance.allStatData;
            allStatData.GetStat(name).Stat.BaseValue = value;
            //Traverse.Create(allStatData.GetStat(name).Stat).Field("_value").SetValue(value);
        }

        public void modifyBossSpeed(float speed)
        {
            foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
            {
                if (isBossSpeed)
                {
                    if (monsterBase.monsterStat.monsterLevel == MonsterLevel.Boss || monsterBase.monsterStat.monsterLevel == MonsterLevel.MiniBoss)
                        monsterBase.animator.speed = speed;
                }else
                    monsterBase.animator.speed = 1;
            }
        }

        private void KillAllEnemies()
        {
            foreach (MonsterLevel level in Enum.GetValues(typeof(MonsterLevel)))
            {
                KillAllEnemies(level);
            }
        }

        private void KillAllEnemiesExcept(MonsterLevel killType)
        {
            foreach (MonsterLevel level in Enum.GetValues(typeof(MonsterLevel)))
            {
                if(killType != level)
                    KillAllEnemies(level);
            }
        }

        private void KillAllEnemies(MonsterLevel killType)
        {

            foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
            {
                bool flag = monsterBase != null && monsterBase.IsAlive() && monsterBase.isActiveAndEnabled && monsterBase.gameObject.activeInHierarchy && monsterBase.gameObject.activeSelf && monsterBase.monsterStat.monsterLevel == killType;
                if (flag)
                {
                    if (monsterBase != null)
                    {
                        monsterBase.DieSelfDesctruction();
                    }
                }
            }
        }
        IEnumerator DelayedExecution()
        {
            Vector3 position = Player.i.transform.localPosition;
            position.x += 10f;
            Player.i.transform.localPosition = position;
            // Wait for 2 seconds
            yield return new WaitForSeconds(0.5f);
            Player.i.FlipFacing();

            position = Player.i.transform.localPosition;
            position.x += 10f;
            Player.i.transform.localPosition = position;
            // Code to execute after the delay
            Debug.Log("This code is executed after a 2-second delay");
        }

        private void Update()
        {
            CheckScreenSize();
            ProcessShortCut();
            TickLogic();
            //Player.i.animator.SetFloat(Animator.StringToHash("OnGround"), 10f);

            #if DEBUG
            if(Input.GetKeyDown(KeyCode.Keypad1))
                //Sword
                foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
                    monsterBase.ChangeStateIfValid(MonsterBase.States.Attack3);

            if (Input.GetKeyDown(KeyCode.Keypad2))
                foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
                    //Up
                    monsterBase.ChangeStateIfValid(MonsterBase.States.Attack4);

            if (Input.GetKeyDown(KeyCode.Keypad3))
                foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
                    //AOE Sword Quick
                    monsterBase.ChangeStateIfValid(MonsterBase.States.Attack11);

            if (Input.GetKeyDown(KeyCode.Keypad4))
                foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
                    // Three and Back
                    monsterBase.ChangeStateIfValid(MonsterBase.States.Attack13);

            if (Input.GetKeyDown(KeyCode.Keypad5))
                foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
                    //Direct Foo
                    monsterBase.ChangeStateIfValid(MonsterBase.States.Attack16);

            if (Input.GetKeyDown(KeyCode.Keypad6))
                foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
                    // Sword Up and down 3 sword
                    monsterBase.ChangeStateIfValid(MonsterBase.States.Attack17);

            if (Input.GetKeyDown(KeyCode.Insert))
            {

                Logger.LogInfo("Execute");
                var testList = new List<MonsterPoolObjectWrapper>();
                foreach (MonsterPoolObjectWrapper monsterPoolObjectWrapper in UnityEngine.Object.FindObjectsOfType<MonsterPoolObjectWrapper>())
                {
                    Logger.LogInfo($"{monsterPoolObjectWrapper.gameObject.name} {monsterPoolObjectWrapper.transform.Find("MonsterCore").gameObject.activeSelf} {monsterPoolObjectWrapper.GetComponent<MonsterBase>().enabled}");
                    if(!monsterPoolObjectWrapper.transform.Find("MonsterCore").gameObject.activeSelf)
                    {
                        monsterPoolObjectWrapper.transform.Find("MonsterCore").gameObject.SetActive(true);
                    }
                    if (!monsterPoolObjectWrapper.GetComponent<MonsterBase>().enabled)
                    {
                        monsterPoolObjectWrapper.GetComponent<MonsterBase>().enabled = true;
                    }
                    testList.Add(monsterPoolObjectWrapper);
                }

                var nest = GameObject.Find("A1_S2_GameLevel/Room/MonsterNest");
                MonsterSpawner spawner = nest.GetComponentInChildren<MonsterSpawner>();
                spawner.spawnTargetList = testList;
                spawner.IgnoreMaximum = true;
                nest.transform.position = Player.i.transform.position;
                nest.SetActive(true);

                //MonsterManager monsterManager = UnityEngine.Object.FindObjectsOfType<MonsterManager>()[0];

                //var testList = new List<MonsterPoolObjectWrapper>();

                //foreach (var x in monsterManager.monsterDict)
                //{
                //    Logger.LogInfo(x.Value);
                //    // Create a new GameObject and attach MonsterPoolObjectWrapper to it
                //    GameObject obj = new GameObject(x.Value.name);
                //    MonsterPoolObjectWrapper t = obj.AddComponent<MonsterPoolObjectWrapper>();
                //    var poolObject = obj.AddComponent<PoolObject>();
                //    UnityEvent OnPoolDestroy = new UnityEvent();

                //    if (t == null)
                //    {
                //        Logger.LogError("MonsterPoolObjectWrapper is null after being added to GameObject.");
                //        continue;
                //    }

                //    // Set the necessary fields
                //    Traverse.Create(t).Field("bindingMonsterBase").SetValue(x.Value);
                //    Traverse.Create(t).Field("poolObject").SetValue(poolObject);
                //    Traverse.Create(t).Field("OnPoolDestroy").SetValue(OnPoolDestroy);

                //    testList.Add(t);
                //    Logger.LogInfo($"Added MonsterPoolObjectWrapper to testList: {t}");
                //}
                //var nest = GameObject.Find("A1_S2_GameLevel/Room/MonsterNest");
                //MonsterSpawner spawner = nest.GetComponentInChildren<MonsterSpawner>();
                ////var testList = new List<MonsterPoolObjectWrapper>();
                ////foreach (MonsterPoolObjectWrapper monsterPoolObjectWrapper in UnityEngine.Object.FindObjectsOfType<MonsterPoolObjectWrapper>())
                ////{
                ////    testList.Add(monsterPoolObjectWrapper);
                ////    Logger.LogInfo(monsterPoolObjectWrapper.gameObject.name);
                ////}
                //spawner.spawnTargetList = testList;
                //spawner.IgnoreMaximum = true;
                //nest.transform.position = Player.i.transform.position;
                //nest.SetActive(true);

                //Sword
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack3);
                //Up
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack4);
                //AOE Sword
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack8);
                //AOE Sword Quick
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack11);
                // UP Down Bomp
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack12);
                // Three and Back
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack13);
                //Direct Foo
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack16);
                // Sword Up and down 3 sword
                //monsterBase.ChangeStateIfValid(MonsterBase.States.Attack17);


                //foreach (FxPlayer fxPlayer in UnityEngine.Object.FindObjectsOfType<FxPlayer>())
                //{
                //    Logger.LogInfo(fxPlayer);
                //    fxPlayer.PlayCustomObject();
                //}


                //Player.i.animator.SetFloat(Animator.StringToHash("IsFastRun"), 10f);

                //StartCoroutine(DelayedExecution());




                //KillAllEnemies();
                //KillAllEnemiesExcept(MonsterLevel.MiniBoss);
                //KillAllEnemies(MonsterLevel.Minion);
                //KillAllEnemies(MonsterLevel.Elite);
                //var flagDict = SaveManager.Instance.allFlags.FlagDict;
                //if (flagDict.TryGetValue("740a8b30-e3cc-4acc-9f5f-da3aaae1df5e_51c211e21fecd9e4c92f41d8d72aa395ScriptableDataBool", out var value)){
                //    Logger.LogInfo(value.GetType().Name);
                //    var scriptableDataBool = value as ScriptableDataBool;
                //    Logger.LogInfo(scriptableDataBool.isValid);
                //}
                //Logger.LogInfo("---------------------------------------");
                //foreach (var x in flagDict)
                //{
                //    if (x.Value.GetType().Name == "ScriptableDataBool")
                //    switch(x.Value.GetType().Name)
                //    {
                //        case "ScriptableDataBool":
                //            var scriptableDataBool = x.Value as ScriptableDataBool;
                //            Logger.LogInfo($"{x.Key}
                //            break;
                //        case "InterestPointData":
                //            var interestPointData = x.Value as InterestPointData;
                //            Logger.LogInfo($"{x.Key}
                //            break;
                //                //var scriptableDataBool = x.Value as ScriptableDataBool;
                //                //Logger.LogInfo($"{x.Key}
                //        }
                //}
                //Logger.LogInfo("---------------------------------------");



                //PrintFlag("A1_S2_CloseDoorFight_Clear", "f16b8e148a1886748a31706af319bac3ScriptableDataBool");
                //PrintFlag("A1_S2_CloseDoorFight_Played", "d347f85a9a8174b059d534a7dfd48806ScriptableDataBool");
                //PrintFlag("A1_S2_ConnectionToElevator_Final_[Variable] 完成關門戰", "588059894ec47e048a5ae9bf2cff8a39ScriptableDataBool");
                //PrintFlag("A1_S2_FirstTimeMeetBoss", "e95d7b8dbf0bfd645941e24f25c6e63dScriptableDataBool");
                //PrintFlag("A1_S2_GiantBladeDead", "0bfe3d3347e05184aab8f67ed3437a72ScriptableDataBool");

                //PrintFlag("Red Tiger Killed", "740a8b30-e3cc-4acc-9f5f-da3aaae1df5e_51c211e21fecd9e4c92f41d8d72aa395ScriptableDataBool");
                //PrintFlag("Red Tiger First Time Concat", "78e30d4b-cb41-452f-aa98-ebd5d386a1f9_51c211e21fecd9e4c92f41d8d72aa395ScriptableDataBool");

                //PrintFlag("t", "51c211e21fecd9e4c92f41d8d72aa395b3d34dc1-c360-4e0c-863b-446c45bade1aScriptableDataBool");
                //PrintFlag("t", "51c211e21fecd9e4c92f41d8d72aa3955ba890a7-445a-4edc-a123-51ecb0f87612ScriptableDataBool");

                //GameFlagBase x;
                //SaveManager.Instance.allFlags.FlagDict.TryGetValue("740a8b30-e3cc-4acc-9f5f-da3aaae1df5e_51c211e21fecd9e4c92f41d8d72aa395InterestPointData", out x);
                //var z = x as InterestPointData;
                //z.SetSolved(false);
                //SaveManager.Instance.allFlags.FlagDict.TryGetValue("51c211e21fecd9e4c92f41d8d72aa395b3d34dc1-c360-4e0c-863b-446c45bade1aInterestPointData", out x);
                //z = x as InterestPointData;
                //z.SetSolved(false);
                //PrintFlag("t", "740a8b30-e3cc-4acc-9f5f-da3aaae1df5e_51c211e21fecd9e4c92f41d8d72aa395InterestPointData");
                //PrintFlag("t", "51c211e21fecd9e4c92f41d8d72aa395b3d34dc1-c360-4e0c-863b-446c45bade1aInterestPointData");
            }
            if (Input.GetKeyDown(KeyCode.Home))
            {
                //var nest = GameObject.Find("A1_S2_GameLevel/Room/MonsterNest");
                //MonsterSpawner spawner = nest.GetComponentInChildren<MonsterSpawner>();
                //spawner.TryEmitMonster(spawner.spawnTargetList.Count);
                foreach (MonsterPoolObjectWrapper monsterPoolObjectWrapper in UnityEngine.Object.FindObjectsOfType<MonsterPoolObjectWrapper>())
                {
                    if (!monsterPoolObjectWrapper.transform.Find("MonsterCore").gameObject.activeSelf || !monsterPoolObjectWrapper.GetComponent<MonsterBase>().enabled)
                    {
                        monsterPoolObjectWrapper.transform.Find("MonsterCore").gameObject.SetActive(true);
                        var monsterBase = monsterPoolObjectWrapper.GetComponent<MonsterBase>();
                        monsterBase.enabled = true;
                        monsterBase.LevelReset();
                        var playerPos = Player.i.transform.position;
                        if (Player.i.Facing == Facings.Right)
                            monsterBase.transform.position = new Vector3(playerPos.x + 250, playerPos.y, 0f);
                        else if (Player.i.Facing == Facings.Left)
                            monsterBase.transform.position = new Vector3(playerPos.x - 250, playerPos.y, 0f);
                        continue;
                    }
                    SpawnMonster(monsterPoolObjectWrapper);
                }
            }
        }
#endif

        void SpawnMonster(MonsterPoolObjectWrapper monster)
        {
            LoopWanderingPointGenerator overridePointGenerator = null;
            MonsterPoolObjectWrapper monsterPoolObjectWrapper = monster;
            MonsterPoolObjectWrapper monsterPoolObjectWrapper2;
            if (monsterPoolObjectWrapper.gameObject.scene == SceneManager.GetActiveScene())
            {
                Debug.Log("SpawnMonster In Scece", monsterPoolObjectWrapper);
                monsterPoolObjectWrapper2 = monsterPoolObjectWrapper;
                monsterPoolObjectWrapper2.gameObject.SetActive(true);
            }
            else
            {
                monsterPoolObjectWrapper2 = SingletonBehaviour<PoolManager>.Instance.BorrowOrInstantiate<MonsterPoolObjectWrapper>(monsterPoolObjectWrapper, base.transform.position, Quaternion.identity, base.transform, null);
            }
            monsterPoolObjectWrapper2.transform.parent = null;
            MonsterBase monsterBase = monsterPoolObjectWrapper2.GetComponent<MonsterBase>();
            StealthWandering stealthWandering = monsterBase.FindState(MonsterBase.States.Wandering) as StealthWandering;
            StealthWanderingIdle stealthWanderingIdle = monsterBase.FindState(MonsterBase.States.WanderingIdle) as StealthWanderingIdle;
            FlyingMonsterWandering flyingMonsterWandering = monsterBase.FindState(MonsterBase.States.Wandering) as FlyingMonsterWandering;
            if (stealthWanderingIdle != null)
            {
                stealthWanderingIdle.newPosTime = 2f;
                stealthWanderingIdle.SinglePointIdle = false;
            }
            if (overridePointGenerator != null)
            {
                if (stealthWandering != null)
                {
                    stealthWandering.wanderingPointGenerator.OverridePoints(overridePointGenerator.TargetPoints);
                }
                else if (flyingMonsterWandering != null)
                {
                    flyingMonsterWandering.patrolPoints.Clear();
                    flyingMonsterWandering.patrolPoints.Add(overridePointGenerator.TargetPoints[0].transform);
                    flyingMonsterWandering.patrolPoints.Add(overridePointGenerator.TargetPoints[1].transform);
                }
            }
            else if (stealthWandering != null)
            {
                stealthWandering.wanderingPointGenerator.DetachFromParent();
            }

            monsterPoolObjectWrapper2.transform.position = base.transform.position;
            monsterPoolObjectWrapper2.gameObject.SetActive(true);
            monsterBase.FacePlayer();
            monsterBase.UpdateScaleFacing();
            monsterBase.ChangeStateIfValid(MonsterBase.States.ZEnter);

            var playerPos = Player.i.transform.position;
            if (Player.i.Facing == Facings.Right)
                monsterBase.transform.position = new Vector3(playerPos.x + 250, playerPos.y, 0f);
            else if (Player.i.Facing == Facings.Left)
                monsterBase.transform.position = new Vector3(playerPos.x - 250, playerPos.y, 0f);

        }

        void PrintFlag(string key)
        {
            GameFlagBase x;
            SaveManager.Instance.allFlags.FlagDict.TryGetValue(key, out x);
            var z = x as ScriptableDataBool;
            z.CurrentValue = true;
            //Logger.LogInfo($"{name}:{z.CurrentValue}");
        }

        private void FullLight()
        {
            GameObject fxCamera = GameObject.Find("SceneCamera/AmplifyLightingSystem/FxCamera");

            // Check if the GameObject was found
            if (fxCamera != null)
            {
                if(fxCamera.activeSelf)
                    fxCamera.SetActive(false);
                else
                    fxCamera.SetActive(true);
                Debug.Log("FxCamera GameObject deactivated.");
            }
            else
            {
                Debug.LogError("FxCamera GameObject not found.");
            }
        }

        private void OnGUI()
        {
            if (!isInit)
            {
                titleStyle = new GUIStyle(GUI.skin.window);
                toggleStyle = new GUIStyle(GUI.skin.toggle);
                textFieldStyle = new GUIStyle(GUI.skin.textField);
                buttonStyle = new GUIStyle(GUI.skin.button);
                supportTextStyle = new GUIStyle(GUI.skin.label);
                float scaleFactor = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
                int toggleSize = Mathf.RoundToInt(baseToggleSize * scaleFactor);
                int buttonSize = Mathf.RoundToInt(baseButtonSize * scaleFactor);
                int textFieldSize = Mathf.RoundToInt(baseTextFieldSize * scaleFactor);
                int supportTextSize = Mathf.RoundToInt(baseTextFieldSize * scaleFactor * 2);

                toggleStyle.fontSize = toggleSize;
                toggleStyle.padding = new RectOffset(toggleSize * 2, toggleSize * 2, toggleSize / 2, toggleSize / 2);
                textFieldStyle.fontSize = textFieldSize;
                buttonStyle.fontSize = buttonSize;
                buttonStyle.padding = new RectOffset(buttonSize / 3, buttonSize / 3, buttonSize / 3, buttonSize / 3);
                titleStyle.fontSize = Mathf.RoundToInt(basetitleSize * scaleFactor);
                supportTextStyle.fontSize = supportTextSize;
                isInit = true;
            }

            if (showMenu)
            {
                windowRect = GUI.Window(156789, windowRect, DoMyWindow, localizationManager.GetString("title"), titleStyle);
            }

            if (showSupportWindow)
            {
                // Create a draggable window
                if(isShowSupportWindowNoBackGround)
                    supportRect = GUI.Window(1354564, supportRect, DrawWindow, "",GUIStyle.none);
                else
                    supportRect = GUI.Window(1299856, supportRect, DrawWindow, "Predict to Attack");
            }
        }

        void DrawWindow(int windowID)
        {
            // Display the text in the window
            GUILayout.Label(SupportText, supportTextStyle);

            // Make the window draggable
            GUI.DragWindow();
        }

        public void DoMyWindow(int windowID)
        {
            GUILayout.BeginArea(new Rect(10, 20, windowRect.width - 20, windowRect.height - 30));
            {
                GUILayout.BeginHorizontal();
                {
                    isInvincible = GUILayout.Toggle(isInvincible, localizationManager.GetString("Invincible"), toggleStyle);
                    isOneHitKill = GUILayout.Toggle(isOneHitKill, localizationManager.GetString("OHK"), toggleStyle);
                    isFastLearnSkill = GUILayout.Toggle(isFastLearnSkill, localizationManager.GetString("FastLearnSkill"), toggleStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    isAutoHeal = GUILayout.Toggle(isAutoHeal, localizationManager.GetString("AutoHeal"), toggleStyle);
                    isInfiniteChi = GUILayout.Toggle(isInfiniteChi, localizationManager.GetString("InfiniteChi"), toggleStyle);
                    isInfinitePotion = GUILayout.Toggle(isInfinitePotion, localizationManager.GetString("InfinitePotion"), toggleStyle);
                    isInfiniteAmmo = GUILayout.Toggle(isInfiniteAmmo, localizationManager.GetString("InfiniteAmmo"), toggleStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        isFov = GUILayout.Toggle(isFov, localizationManager.GetString("FOV"), toggleStyle);
                        fov = GUILayout.HorizontalSlider(fov, 1f, 180f, GUILayout.Width(200));
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    {
                        isSpeed = GUILayout.Toggle(isSpeed, localizationManager.GetString("Timer"), toggleStyle);
                        speedInput = GUILayout.TextField(speedInput, textFieldStyle);
                        float.TryParse(speedInput, out speed);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button(localizationManager.GetString("FullBright"), buttonStyle))
                    FullLight();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("English"), buttonStyle))
                    {
                        Language.Value = "en-us";
                        localizationManager.SetLanguage(Language.Value);
                    }
                    if (GUILayout.Button(localizationManager.GetString("繁體中文"), buttonStyle))
                    {
                        Language.Value = "zh-tw";
                        localizationManager.SetLanguage(Language.Value);
                    }
                    if (GUILayout.Button(localizationManager.GetString("简体中文"), buttonStyle))
                    {
                        Language.Value = "zh-cn";
                        localizationManager.SetLanguage(Language.Value);
                    }

                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("GetAllMax"), buttonStyle))
                        GetAllMax();
                    if (GUILayout.Button(localizationManager.GetString("GetAllMaxWithoutActiveSkill"), buttonStyle))
                        GetAllMaxWithoutActiveSkill();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    isGetAll = GUILayout.Toggle(isGetAll, localizationManager.GetString("AutoGetAllMax"), toggleStyle);
                    isGetAllWithoutActiveSkill = GUILayout.Toggle(isGetAllWithoutActiveSkill, localizationManager.GetString("AutoGetAllMaxWithoutActiveSkill"), toggleStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("A2_S5_BossHorseman_Final"), buttonStyle))
                        HandleTeleportButtonClick("A2_S5_BossHorseman_Final", new Vector3(-4790, -2288f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A3_S5_BossGouMang_Final"), buttonStyle))
                        HandleTeleportButtonClick("A3_S5_BossGouMang_Final", new Vector3(-4430, -2288f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A4_S5_DaoTrapHouse_Final"), buttonStyle))
                        HandleTeleportButtonClick("A4_S5_DaoTrapHouse_Final", new Vector3(1833f, -3744f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A5_S5_JieChuanHall"), buttonStyle))
                    {
                        HandleTeleportButtonClick("A5_S5_JieChuanHall", new Vector3(-4784, -2288f, 0f));
                        ModifyFlag("c4a79371b6ba3ce47bbdda684236f7b5ItemData", 1); //(重要道具)04_截全毒藥 (ItemData)
                    }

                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("A7_S5_Boss_ButterFly"), buttonStyle))
                        HandleTeleportButtonClick("A7_S5_Boss_ButterFly", new Vector3(-2640f, -1104f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A9_S5_風氏"), buttonStyle))
                        HandleTeleportButtonClick("A9_S5_風氏", new Vector3(-2370f, -1264f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A10_S5_Boss_Jee"), buttonStyle))
                        Jee("A10_S5_Boss_Jee", new Vector3(-48f, -64f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A11_S0_Boss_YiGung_回蓬萊"), buttonStyle))
                        HandleTeleportButtonClick("A11_S0_Boss_YiGung_回蓬萊", new Vector3(-2686f, -1104f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A11_S0_Boss_YiGung"), buttonStyle))
                        HandleTeleportButtonClick("A11_S0_Boss_YiGung", new Vector3(-2686f, -1104f, 0f));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("赤虎－百長"), buttonStyle))
                        HandleTeleportButtonClick("A1_S2_ConnectionToElevator_Final", new Vector3(1820f, -4432f, 0f)); //赤虎刀校－百長
                    if (GUILayout.Button(localizationManager.GetString("赤虎－魁岩"), buttonStyle))
                        HandleTeleportButtonClick("A6_S1_AbandonMine_Remake_4wei", new Vector3(5151, -7488, 0f)); //赤虎刀校－魁岩
                    if (GUILayout.Button(localizationManager.GetString("赤虎－炎刃"), buttonStyle))
                        HandleTeleportButtonClick("A2_S6_LogisticCenter_Final", new Vector3(5030, -6768, 0f)); //赤虎刀校－炎刃
                    if (GUILayout.Button(localizationManager.GetString("赤虎－獵官"), buttonStyle))
                        PerformActionsAfterTeleport("A0_S9_AltarReturned", new Vector3(-95, -64, 0f)); //赤虎刀校－獵官： 從監獄脫逃後將能觸發神農支線任務， 使用神農給予的古礦坑鑰匙卡從古礦坑右上角返回桃花村。
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("步衛－角端"), buttonStyle))
                    {
                        //HandleTeleportButtonClick("A2_S2_ReactorRight_Final", new Vector3(-4690, -1968, 0f)); // 天綱步衛－角端
                        List<string> flags = new List<string> { 
                            "574d3e20-47c5-4841-a21c-121d7806ed6e_c3c3f30fb046d9743aea48eb8f4833bcScriptableDataBool",
                            "ff553e6df36c89644ae08124aaa2913eScriptableDataBool",
                            "5d67c34b-0553-482f-8e4d-dd4c02d0c359_c3c3f30fb046d9743aea48eb8f4833bcScriptableDataBool"};

                        PerformActionsAfterTeleport("A2_S2_ReactorRight_Final", new Vector3(-4690, -1968, 0f), flags);
                        if (Player.i != null)
                            Traverse.Create(Player.i).Method("SkipFooMiniGame").GetValue();
                            
                        //HandleTeleportButtonClick("A2_S2_ReactorRight_Final", new Vector3(-4642, -1968, 0f)); //天綱步衛－角端
                        //ModifyFlag("574d3e20-47c5-4841-a21c-121d7806ed6e_c3c3f30fb046d9743aea48eb8f4833bcScriptableDataBool", 1);
                        //ModifyFlag("ff553e6df36c89644ae08124aaa2913eScriptableDataBool", 1);
                        //ModifyFlag("5d67c34b-0553-482f-8e4d-dd4c02d0c359_c3c3f30fb046d9743aea48eb8f4833bcScriptableDataBool", 1);
                    }
                    if (GUILayout.Button(localizationManager.GetString("步衛－武槍"), buttonStyle))
                        PerformActionsAfterTeleport("A5_S4_CastleMid_Remake_5wei", new Vector3(4430, -4224, 0f)); //天綱步衛－武槍
                    if (GUILayout.Button(localizationManager.GetString("影者－水鬼"), buttonStyle))
                    {
                        HandleTeleportButtonClick("A3_S2_GreenHouse_Final", new Vector3(-4530, -1216, 0f)); //天綱影者－水鬼
                        if (Player.i != null)
                            Traverse.Create(Player.i).Method("UnlockChargedAttack").GetValue();

                        ModifyFlag("a4657cbd-5219-45fb-9401-3780b41e8cbe_efdc8e91e5eb76347b87b832ac07330cScriptableDataBool", 1); // 關閉水 A3_S2_GreenHouse_Final_[Variable] SimpleCutScenePlayeda4657cbd-5219-45fb-9401-3780b41e8cbe (ScriptableDataBool)
                    }
                    if (GUILayout.Button(localizationManager.GetString("影者－山鬼"), buttonStyle))
                        HandleTeleportButtonClick("A1_S3_InnerHumanDisposal_Final", new Vector3(-5590, -608, 0f)); //天綱影者－山鬼
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("法使－鐵焰"), buttonStyle))
                        HandleTeleportButtonClick("A4_S2_RouteToControlRoom_Final", new Vector3(-3950, -3040, 0f)); //天綱法使－鐵焰
                    if (GUILayout.Button(localizationManager.GetString("法使－幻仙"), buttonStyle))
                        HandleTeleportButtonClick("A7_S2_SectionF_MiniBossFight", new Vector3(-4004, -1888, 0f)); //法使-幻仙
                    if (GUILayout.Button(localizationManager.GetString("機兵－天守"), buttonStyle))
                    {
                        HandleTeleportButtonClick("A9_S1_Remake_4wei", new Vector3(-3330, 352, 0f)); //巨錘機兵－天守
                        ModifyFlag("a2dba9e5-61cf-453a-8981-efb081fb0b11_4256ef2ec22f942dc9f70607bb00391fScriptableDataBool", 1); // 跳過Butterfly Hack SimpleCutScenePlayeda2dba9e5-61cf-453a-8981-efb081fb0b11 (ScriptableDataBool)
                    }
                    if (GUILayout.Button(localizationManager.GetString("魂守－刺行"), buttonStyle))
                    {
                        HandleTeleportButtonClick("A10_S4_HistoryTomb_Left", new Vector3(-690, -368, 0f)); //魂守－刺行：完成三間密室內的考驗，此BOSS才會出現。
                        ModifyFlag("44bc69bd40a7f6d45a2b8784cc8ebbd1ScriptableDataBool", 1); //A10_SG1_Cave1_[Variable] 看過天尊棺材演出_科技天尊 (ScriptableDataBool)
                        ModifyFlag("118f725174ccdf5498af6386d4987482ScriptableDataBool", 1); //A10_SG2_Cave2_[Variable] 看過天尊棺材演出_經濟天尊 (ScriptableDataBool)
                        ModifyFlag("d7a444315eab0b74fb0ed1e8144edf73ScriptableDataBool", 1); //A10_SG3_Cave4_[Variable] 看過天尊棺材演出_軍事天尊 (ScriptableDataBool)
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("侍衛－隱月"), buttonStyle))
                        HandleTeleportButtonClick("A6_S3_Tutorial_And_SecretBoss_Remake", new Vector3(5457, -6288, 0f)); //天綱侍衛－隱月
                    if (GUILayout.Button(localizationManager.GetString("康回"), buttonStyle))
                        Kanghui("A5_S2_Jail_Remake_Final", new Vector3(-464f, -4624f, 0f)); //康回
                    if (GUILayout.Button(localizationManager.GetString("刑天"), buttonStyle))
                        HandleTeleportButtonClick("A4_S3_ControlRoom_Final", new Vector3(-4155, -5776f, 0f)); //刑天
                    if (GUILayout.Button(localizationManager.GetString("無頭刑天"), buttonStyle))
                        HandleTeleportButtonClick("A11_SG1_ShinTenRoom", new Vector3(-5827, -464f, 0f)); //無頭刑天
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("SupportWindow"), buttonStyle))
                        showSupportWindow = !showSupportWindow;
                    if (GUILayout.Button(localizationManager.GetString("ShowSupportWindowNoBackGround"), buttonStyle))
                        isShowSupportWindowNoBackGround = !isShowSupportWindowNoBackGround;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        isBossSpeed = GUILayout.Toggle(isBossSpeed, localizationManager.GetString("BossSpeed"), toggleStyle);
                        bossSpeedInput = GUILayout.TextField(bossSpeedInput, textFieldStyle);
                        float.TryParse(bossSpeedInput, out bossSpeed);
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    {
                        isAttackMult = GUILayout.Toggle(isAttackMult, localizationManager.GetString("Attack_Multiplier"), toggleStyle);
                        attackMultInput = GUILayout.TextField(attackMultInput, textFieldStyle);
                        float.TryParse(attackMultInput, out attackMult);
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    {
                        isInjeryMult = GUILayout.Toggle(isInjeryMult, localizationManager.GetString("Injury_Multiplier"), toggleStyle);
                        injeryMultInput = GUILayout.TextField(injeryMultInput, textFieldStyle);
                        float.TryParse(injeryMultInput, out injeryMult);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                if (GUILayout.Button(localizationManager.GetString("Skip"), buttonStyle))
                {
                    SkippableManager.Instance.TrySkip();
                }
                GUILayout.BeginHorizontal();
                {
                    #if DEBUG
                    {
                        if (GUILayout.Button(localizationManager.GetString("故意放棄進度 reset這個場景"), buttonStyle))
                        {
                            if (GameCore.Instance != null)
                                GameCore.Instance.DiscardUnsavedFlagsAndReset();
                        }
                        if (GUILayout.Button(localizationManager.GetString("Test"), buttonStyle))
                        {
                            var allStat = SaveManager.Instance.allStatData;
                            Logger.LogInfo(Traverse.Create(allStat.GetStat("ParryDuration").Stat).Field("BaseValue").GetValue<float>());
                            allStat.GetStat("ParryDuration").Stat.BaseValue = 0f;

                            //foreach (MonsterBase monsterBase in UnityEngine.Object.FindObjectsOfType<MonsterBase>())
                            //{
                            //    if (monsterBase.monsterStat.monsterLevel == MonsterLevel.Boss || monsterBase.monsterStat.monsterLevel == MonsterLevel.MiniBoss)
                            //        monsterBase.animator.speed = 4;
                            //}
                            //HandleTeleportButtonClick("A1_S2_ConnectionToElevator_Final", new Vector3(1820f, -4432f, 0f)); //赤虎刀校－百長
                            //HandleTeleportButtonClick("A2_S6_LogisticCenter_Final", new Vector3(4370, -6768, 0f)); //赤虎刀校－炎刃
                            //HandleTeleportButtonClick("A2_S2_ReactorRight_Final", new Vector3(-4642, -1968, 0f)); //天綱步衛－角端

                            //HandleTeleportButtonClick("A10_S4_HistoryTomb_Left", new Vector3(-690, -368, 0f)); //魂守－刺行：完成三間密室內的考驗，此BOSS才會出現。
                            //ModifyFlag("44bc69bd40a7f6d45a2b8784cc8ebbd1ScriptableDataBool", 1); //A10_SG1_Cave1_[Variable] 看過天尊棺材演出_科技天尊 (ScriptableDataBool)
                            //ModifyFlag("118f725174ccdf5498af6386d4987482ScriptableDataBool", 1); //A10_SG2_Cave2_[Variable] 看過天尊棺材演出_經濟天尊 (ScriptableDataBool)
                            //ModifyFlag("d7a444315eab0b74fb0ed1e8144edf73ScriptableDataBool", 1); //A10_SG3_Cave4_[Variable] 看過天尊棺材演出_軍事天尊 (ScriptableDataBool)

                            //HandleTeleportButtonClick("A3_S2_GreenHouse_Final", new Vector3(-4530, -1216, 0f)); //天綱影者－水鬼
                            //Traverse.Create(Player.i).Method("UnlockChargedAttack").GetValue();

                            //HandleTeleportButtonClick("A9_S1_Remake_4wei", new Vector3(-3330, 352, 0f)); //巨錘機兵－天守

                            //HandleTeleportButtonClick("A1_S3_InnerHumanDisposal_Final", new Vector3(-5590, -608, 0f)); //天綱影者－山鬼

                            //HandleTeleportButtonClick("A0_S9_AltarReturned", new Vector3(-95, -64, 0f)); //赤虎刀校－獵官： 從監獄脫逃後將能觸發神農支線任務， 使用神農給予的古礦坑鑰匙卡從古礦坑右上角返回桃花村。

                            //HandleTeleportButtonClick("A6_S3_Tutorial_And_SecretBoss_Remake", new Vector3(5457, -6288, 0f)); //天綱侍衛－隱月

                            //HandleTeleportButtonClick("A6_S1_AbandonMine_Remake_4wei", new Vector3(5151, -7488, 0f)); //赤虎刀校－魁岩

                            //HandleTeleportButtonClick("A4_S2_RouteToControlRoom_Final", new Vector3(-3950, -3040, 0f)); //天綱法使－鐵焰

                            //HandleTeleportButtonClick("A5_S4_CastleMid_Remake_5wei", new Vector3(4033, -4528, 0f)); //天綱步衛－武槍

                            //HandleTeleportButtonClick("A4_S3_ControlRoom_Final", new Vector3(-4155, -5776f, 0f)); //刑天

                            //HandleTeleportButtonClick("A11_SG1_ShinTenRoom", new Vector3(-5827, -464f, 0f)); //無頭刑天

                            //HandleTeleportButtonClick("A5_S2_Jail_Remake_Final", new Vector3(-464f, -4624f, 0f)); //康回

                            //foreach (PlayerAbilityModifyPackApplyAction playerAbilityModifyPackApplyAction in UnityEngine.Object.FindObjectsOfType<PlayerAbilityModifyPackApplyAction>())
                            //{
                            //    playerAbilityModifyPackApplyAction.ExitLevelAndDestroy(); //A5 Jail Debuff Pack 虛弱監獄 (PlayerAbilityScenarioModifyPack)
                            //}

                            //HandleTeleportButtonClick("A7_S2_SectionF_MiniBossFight", new Vector3(-4004, -1888, 0f)); //法使-幻仙


                            //ChangeSceneData data = new ChangeSceneData();
                            //data.sceneName = "A1_S2_ConnectionToElevator_Final";
                            //data.fadeColor = default(Color);
                            //GameCore.Instance.ChangeScene(data);

                            //TeleportPointData t = ScriptableObject.CreateInstance<TeleportPointData>();
                            //t.sceneName = "A2_S5_BossHorseman_Final";
                            //t.TeleportPosition = new Vector3(-5195f, -2288f, 0f);
                            //GameCore.Instance.TeleportToSavePoint(t);


                            //if(Player.i != null)
                            //{
                            //    //Player.i.transform.position = new Vector3(-5195f,-2288f,0f);
                            //}
                            //Traverse.Create(Player.i).Method("AddSkillPoint").GetValue();
                            //NotAtSavePoint();

                            //var dic = SaveManager.Instance.allFlags.flagDict;

                            //var d = new List<string>
                            //{
                            //    "af9cb112a715e4955afaa3e740f4fe5aSkillNodeData",
                            //    "261c03bb170884f0084f3d4a8c17f708SkillNodeData",
                            //    "9f05ad4510c4f4526bcf9facc75e1370SkillNodeData",
                            //    "d57a70d600fb34edbbfa503acf81b85eSkillNodeData",
                            //    "19b09ad0c66d84337826a5c0184625edSkillNodeData",
                            //    "d8cbeba2a689a422abdb956743a07891SkillNodeData"
                            //};

                            //foreach (var x in dic)
                            //{
                            //    if(x.Value.GetType().Name == "SkillNodeData")
                            //    {
                            //        if (d.Contains(x.Value.FinalSaveID))
                            //            ModifyFlag(x.Value.FinalSaveID, 1);
                            //    }
                            //}
                            //ModifyFlag("af9cb112a715e4955afaa3e740f4fe5aSkillNodeData", 1); //0_閃避 (SkillNodeData)
                            //ModifyFlag("261c03bb170884f0084f3d4a8c17f708SkillNodeData", 1); // 流派 Foo 1_一氣貫通 (SkillNodeData)
                            //ModifyFlag("9f05ad4510c4f4526bcf9facc75e1370SkillNodeData", 1); // 0_輕功 (SkillNodeData)
                            //ModifyFlag("d57a70d600fb34edbbfa503acf81b85eSkillNodeData", 1); // 0_輕功招式 (SkillNodeData)
                            //ModifyFlag("19b09ad0c66d84337826a5c0184625edSkillNodeData", 1); // 0_parry 格擋 (SkillNodeData)
                            //ModifyFlag("d8cbeba2a689a422abdb956743a07891SkillNodeData", 1); // 0_攻擊 (SkillNodeData)
                            //ModifyFlag("b3e48a60ad0b84648952dc21712b27c0SkillNodeData", 1); // Foo Power +1 內力提升 LV1 (SkillNodeData)
                        }
                    }
                    #endif
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
            GUI.DragWindow();
        }



        private void GetAllMax()
        {
            Logger.LogInfo("GetAllMax");
            SetAllMaxFlag();

            var skillTreeUI = UnityEngine.Object.FindObjectsOfType<SkillTreeUI>(true)[0];
            if(skillTreeUI != null)
            {
                foreach (var skillNode in skillTreeUI.allSkillNodes)
                {
                    skillNode.pluginCore.SkillAcquired();
                    skillNode.UpdateView();
                    skillNode.pluginCore.UnlockChildrenCheck();
                }
            }
            

            var player = Player.i;
            if(player != null)
            {
                Traverse.Create(player).Method("UnlockAll").GetValue();
                Traverse.Create(player).Method("SkipFooMiniGame").GetValue();
                Traverse.Create(player).Method("AddSkillPoint").GetValue();
                Traverse.Create(player).Method("AddMoney").GetValue();
                Traverse.Create(player).Method("GetAllJades").GetValue();
                Traverse.Create(player).Method("GetAllJadeSlots").GetValue();
                Traverse.Create(player.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("BaseValue").SetValue(500.0f);
                Traverse.Create(player.potion.potionMaxCountData.Stat).Field("BaseValue").SetValue(8);

                Player.i.RestoreEverything();
            }

            NotAtSavePoint();
        }

        private void NotAtSavePoint()
        {
            GameObject Jade = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/[Jade 玉] Multiple Collection Select Panel/[Condition] 在存檔點");
            GameObject Skill = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/[經絡]SkillTreeUI Manager/[Condition] 不在存檔點不能點技能");
            if (Jade != null)
                Jade.SetActive(false);
            if (Skill != null)
                Skill.SetActive(false);
        }

        private void GetAllMaxWithoutActiveSkill()
        {
            Logger.LogInfo("GetAllMaxWithoutActiveSkill");
            SetAllMaxFlag();

            var player = Player.i;
            if (player != null)
            {
                Traverse.Create(player).Method("UnlockAll").GetValue();
                //Traverse.Create(player).Method("UnlockButterfly").GetValue();
                //Traverse.Create(player).Method("UnlockArrow").GetValue();
                Traverse.Create(player).Method("SkipFooMiniGame").GetValue();
                Traverse.Create(player).Method("AddSkillPoint").GetValue();
                Traverse.Create(player).Method("AddMoney").GetValue();
                Traverse.Create(player).Method("GetAllJades").GetValue();
                Traverse.Create(player).Method("GetAllJadeSlots").GetValue();
                Traverse.Create(player.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("BaseValue").SetValue(500.0f);
                Traverse.Create(player.potion.potionMaxCountData.Stat).Field("BaseValue").SetValue(8);

                Player.i.RestoreEverything();
            }
            //var skillTreeUI = UnityEngine.Object.FindObjectsOfType<SkillTreeUI>(true)[0];
            //skillTreeUI.ResetAllSkillPoints();
            NotAtSavePoint();
        }

        private void SetAllMaxFlag()
        {
            ModifyFlag("d1e010f02b84fb14fa39a2b44f99a4beItemData", 1); //狀態欄_玄蝶
            ModifyFlag("a68fe303d0077264aa66218d3900f0edItemData", 1); //狀態欄_蒼弓
            ModifyFlag("b7a97935c391a324b803dc1fa542b769ItemData", 1); //狀態欄_藥斗

            ModifyFlag("2efd376b4493d40fca29f9e3d49669e9PlayerWeaponData", 1); //Bow
            ModifyFlag("7837bd6bb550641d8a9f30492603c5eePlayerWeaponData", 1); //穿雲箭
            ModifyFlag("2f7009a00edd57c4fa4332ffcd15396aPlayerWeaponData", 1); //穿雲箭_LV2
            ModifyFlag("9dfa4667af28b6a4da8c443c9814e40dPlayerWeaponData", 1); //穿雲箭_LV3
            ModifyFlag("ef8f7eb3bcd7b444f80d5da539f3b133PlayerWeaponData", 1); //爆破箭
            ModifyFlag("b4b36f48e6a6ec849a613f2fdeda1a2dPlayerWeaponData", 1); //爆破箭_LV2
            ModifyFlag("4b323612d5dc8bd49b3fd4508d7b485bPlayerWeaponData", 1); //爆破箭_LV3
            ModifyFlag("3949bc0edba197d459f5d2d7f15c72e0PlayerWeaponData", 1); //追蹤箭
            ModifyFlag("11df21b39de54f9479514d7135be8d57PlayerWeaponData", 1); //追蹤箭_LV2
            ModifyFlag("a9402e3a9e1e04f4488265f1c6d42641PlayerWeaponData", 1); //追蹤箭_LV3

            ModifyFlag("eb5ef12f4ef9e46eeb09809070d21db4PlayerAbilityData", 1); //MaxAmmo Lv1
            ModifyFlag("4f3107713e9dd43fc9968aa6579207c9PlayerAbilityData", 1); //MaxAmmo Lv2
            ModifyFlag("072576f6cb93e4921b287b4c50140e22PlayerAbilityData", 1); //MaxAmmo Lv3

            ModifyFlag("4e9b068d7b812a84b9f1b52efee467acPlayerAbilityData", 1); // Max health LV1
            ModifyFlag("b11ba30bd3a72eb49ae3d1746fb686b7PlayerAbilityData", 1); // Max health LV2
            ModifyFlag("d0a9876111d725d4298833323bb082d0PlayerAbilityData", 1); // Max health LV3
            ModifyFlag("1b8f6278e3ead824e8352349a8f7cb6dPlayerAbilityData", 1); // Max health LV4
            ModifyFlag("aa396485060a82d4db9ab2a1fb02c429PlayerAbilityData", 1); // Max health LV5
            ModifyFlag("a2f84651129789d468ad8fde13a54c4fPlayerAbilityData", 1); // Max health LV6
            ModifyFlag("3c1022c896f9ca44e9c588b76797e3d0PlayerAbilityData", 1); // Max health LV7
            ModifyFlag("aae70b5ca7663504eb9967182234dc6bPlayerAbilityData", 1); // Max health LV8
            //ModifyFlag("07274e0ddb6e91746adc0e583cddd430PlayerAbilityData", 1); // Max health LV9

            ModifyFlag("c3e9631e6805c704f8c3fb1d1d60d78fPlayerAbilityData", 1); // Potion value LV1
            ModifyFlag("960072fcea97cb8438297365d3db963cPlayerAbilityData", 1); // Potion value LV2
            ModifyFlag("cb11b23d6a0659f418937331d46de6fcPlayerAbilityData", 1); // Potion value LV3
            ModifyFlag("0c1ddf20ca0b26447895b50183aebae9PlayerAbilityData", 1); // Potion value LV4
            ModifyFlag("89f506003825f404eac747bbb19560ccPlayerAbilityData", 1); // Potion value LV5
            ModifyFlag("075eabd7421b58e43af25cc1c57e79e3PlayerAbilityData", 1); // Potion value LV6
            ModifyFlag("cf5080950d381d843b000c91175434bfPlayerAbilityData", 1); // Potion value LV7
            ModifyFlag("05b87ad6d7c226245b6e917ec21d3416PlayerAbilityData", 1); // Potion value LV8
        }

        private void ModifyFlag(string key, int value)
        {
            GameFlagBase gameFlagBase = null;
            if (key == null || !SaveManager.Instance.allFlags.flagDict.TryGetValue(key, out gameFlagBase))
                return;

            var valueBool = Convert.ToBoolean(value);
            //Logger.LogInfo(gameFlagBase.GetType().Name);
            switch (gameFlagBase.GetType().Name)
            {
                case "ItemData":
                    var itemData = gameFlagBase as ItemData;
                    if (itemData == null)
                        return;
                    if(valueBool)
                        itemData.PlayerPicked();
                    break;

                case "PlayerWeaponData":
                    var playerWeaponData = gameFlagBase as PlayerWeaponData;
                    if (playerWeaponData == null)
                        return;
                    if (valueBool)
                        playerWeaponData.PlayerPicked();
                    break;
                case "PlayerAbilityData":
                    var playerAbilityData = gameFlagBase as PlayerAbilityData;
                    if (playerAbilityData == null)
                        return;
                    if (valueBool)
                        playerAbilityData.PlayerPicked();
                    break;
                case "SkillNodeData":
                    var skillNodeData = gameFlagBase as SkillNodeData;
                    if (skillNodeData == null)
                        return;
                    if (valueBool)
                    {
                        //skillNodeData.equipped.CurrentValue = true;
                        //skillNodeData.acquired.CurrentValue = true;
                        //skillNodeData.viewed.CurrentValue = true;
                    }
                    break;
                case "ScriptableDataBool":
                    var scriptableDataBool = gameFlagBase as ScriptableDataBool;
                    Logger.LogInfo(scriptableDataBool.CurrentValue);
                    if (scriptableDataBool == null)
                        return;
                    if (valueBool)
                        scriptableDataBool.CurrentValue = true;
                    else
                        scriptableDataBool.CurrentValue = false;
                    break;
            }
        }

        private void PreocessGotoScene(string SceneName, TeleportPointData teleportPointData = null)
        {
            if (Player.i == null)
            {
                if (StartMenuLogic.Instance != null && SceneManager.GetActiveScene().name == "TitleScreenMenu")
                {
                    StartMenuLogic.Instance.StartGame(SceneName);
                    PrePrecoess(SceneName, teleportPointData).Forget();
                }
            }
            else
            {
                if (GameCore.Instance != null)
                {
                    if (GameCore.Instance.currentCoreState == GameCore.GameCoreState.Playing)
                    {
                        if(teleportPointData == null)
                            GameCore.Instance.GoToScene(SceneName);
                        else
                        {
                            GameCore.Instance.TeleportToSavePoint(teleportPointData);
                        }
                        GameCore.Instance.DiscardUnsavedFlagsAndReset();
                        checkMultiplier();
                        CheckGetAll();
                    }
                }
            }
        }

        private void CheckGetAll()
        {
            if (isGetAll)
                GetAllMax();
            else if (isGetAllWithoutActiveSkill)
                GetAllMaxWithoutActiveSkill();
        }

        private void HandleTeleportButtonClick(string sceneName, Vector3 teleportPosition)
        {
            TeleportPointData teleportPointData = CreateTeleportPointData(sceneName, teleportPosition);
            PreocessGotoScene(sceneName, teleportPointData);
        }

        private TeleportPointData CreateTeleportPointData(string sceneName, Vector3 position)
        {
            TeleportPointData teleportPointData = ScriptableObject.CreateInstance<TeleportPointData>();
            teleportPointData.sceneName = sceneName;
            teleportPointData.TeleportPosition = position;
            return teleportPointData;
        }
    }
}
