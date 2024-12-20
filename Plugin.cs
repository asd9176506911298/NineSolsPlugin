﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using RCGFSM.PlayerAbility;
using BepInEx.Logging;
using System.Linq;
using _3_Script.UI.TitleScreenMenuUI;
using static SceneConnectionPoint;
using Auto.Utils;
using System.Security.AccessControl;
using RCGMaker.Runtime.Character;

namespace NineSolsPlugin
{
    [BepInPlugin("NineSols.Yukikaco.plugin", "Nine Sols Cheat Menu Made By Yuki.kaco", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        private static ManualLogSource logger;

        private Harmony harmony;

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
        private ConfigEntry<KeyCode> saveKey;
        private ConfigEntry<KeyCode> loadKey;
        private ConfigEntry<string> Language;
        public ConfigEntry<bool> isEnableConsole;

        private bool showMenu = false;
        private bool hasBossRushVersion = false;
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
        public bool isSitAtSavePoint = false;
        public bool isCPU = false;
        private bool previousIsBossSpeed;
        private bool previousIsAttackMult;
        private bool previousIsInjeryMult;
        private bool previousIsCpu;
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

        GameObject attack = null;
        GameObject attack2 = null;
        GameObject attack3 = null;

        public bool showSupportWindow = false;
        public string SupportText = "test";
        private bool isShowSupportWindowNoBackGround = false;

        SaveSlotMetaData data = null;
        byte[] dataByte;
        string flagJson = null;
        Vector3 tmpPos;
        Vector2 vel;
        string sceneName;
        Vector3 HuanxianPos = Vector3.zero;
        bool isHuanxianPosSet = false;


        private void Awake()
        {
            RCGLifeCycle.DontDestroyForever(gameObject);
            Debug.Log("九日修改器");
            Instance = this;
            logger = Logger;

            MenuToggleKey = Config.Bind<KeyCode>("Menu", "MenuToggleKey", KeyCode.F3, "Open Cheat Menu ShortCut\n開啟選單快捷鍵\n开启选单热键");
            SpeedToggleKey = Config.Bind<KeyCode>("Menu", "SpeedToggleKey", KeyCode.F4, "Timer ShortCut\n加速快捷鍵\n加速热键");
            FovToggleKey = Config.Bind<KeyCode>("Menu", "FOVToggleKey", KeyCode.F5, "FOV ShortCut\nFOV快捷鍵\nFOV热键");
            MouseTeleportKey = Config.Bind<KeyCode>("Menu", "MouseTeleportKey", KeyCode.F2, "Mouse Move Character ShortCut\n滑鼠移動快捷鍵\n滑鼠移动热键");
            SkipKey = Config.Bind<KeyCode>("Menu", "SkipKey", KeyCode.LeftControl, "Skip ShortCut\n跳過快捷鍵\n跳過热键");
            saveKey = Config.Bind<KeyCode>("Menu", "SaveKey", KeyCode.F11, "Save Current State\n儲存當前資料\n暂存当前资料");
            loadKey = Config.Bind<KeyCode>("Menu", "LoadKey", KeyCode.F12, "Load Tmp State\n讀取暫存資料\n读取暂存资料");
            isEnableConsole = Config.Bind<bool>("Menu", "isEnableConsole", true, "Is Enable Console? F1 Open Console\n是否開啟控制台 F1開啟控制台\n是否开启控制台 F1开启控制台");
            Language = Config.Bind<string>("Menu", "MenuLanguage", "en-us", "Menu Language\n選單語言\n选单语言\nen-us, zh-tw, zh-cn");

            localizationManager = new LocalizationManager();
            localizationManager.SetLanguage(Language.Value);

            Harmony.CreateAndPatchAll(typeof(Patch));
            harmony = new Harmony("MonsterBasePatcher");

            // Check if UpdateAnimatorSpeed exists in MonsterBase and apply patch if it does
            var method = AccessTools.Method(typeof(MonsterBase), "UpdateAnimatorSpeed");

            if (method != null)
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(MonsterBasePatcher), nameof(MonsterBasePatcher.UpdateAnimatorSpeed)));
                Logger.LogInfo("UpdateAnimatorSpeed patch applied.");
            }
            else
            {
                Logger.LogInfo("UpdateAnimatorSpeed method not found. Skipping patch.");
            }

            // Initialize window size based on screen dimensions
            float width = Screen.width * 0.6f;
            float height = Screen.height * 0.92f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            supportRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height / 6);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            previousIsBossSpeed = isBossSpeed;
            previousBossSpeed = bossSpeed;

            previousIsAttackMult = isAttackMult;
            previousAttackMult = attackMult;

            previousIsInjeryMult = isInjeryMult;
            previousInjeryMult = injeryMult;

            previousIsCpu = isCPU;
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
            try
            {

            KillAllEnemiesExcept(MonsterLevel.MiniBoss);
            }catch(Exception e)
            {
                Logger.LogInfo($"Exp {e}");
            }
        }

        async UniTask checkMove()
        {
            // Wait until the player is instantiated and on the ground
            while (Player.i == null || Player.i.moveVec.x == 0)
            {
                await UniTask.Yield();
            }
        }

        private async UniTask PrePrecoess(string sceneName, TeleportPointData teleportPointData,bool isMemory = false)
        {
            Logger.LogInfo("PrePrecoessPrePrecoess");

            if (isMemory)
                await WaitForSceneLoad("VR_Challenge_Hub");

            await WaitForEnterGame();

            GameCore.Instance.DiscardUnsavedFlagsAndReset();


            if (teleportPointData != null)
                checkTeleportToSavePoint(teleportPointData);
            //GameCore.Instance.TeleportToSavePoint(teleportPointData);

            await WaitForSceneLoad(sceneName);

            //checkTeleportToSavePoint(teleportPointData);
            // Now the scene is loaded, run the appropriate method
            GameCore.Instance.SetReviveSavePoint(teleportPointData);
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

        private async UniTask WaitForHuanXiaoPos()
        {
            while (!isHuanxianPosSet)
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
            width *= 0.6f;
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
            var method = AccessTools.Method(typeof(MonsterBase), "UpdateAnimatorSpeed");

            if (method != null)
            {
                harmony.UnpatchSelf();
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Your code here
            Logger.LogInfo("Scene loaded: " + scene.name);
            if(scene.name == "TitleScreenMenu")
            {
                if(GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Button Layout/MainMenuButton_MemoryOfBattle"))
                {
                    hasBossRushVersion = true;
                }
            }

            if (scene.name == "A7_S2_SectionF_MiniBossFight")
            {
                if (GameObject.Find("StealthMonster_Flying Teleport Wizard_MiniBoss 幻象區平行宇宙版 Variant"))
                {
                    HuanxianPos = GameObject.Find("StealthMonster_Flying Teleport Wizard_MiniBoss 幻象區平行宇宙版 Variant").GetComponent<MonsterBase>().HomePos;
                    if(HuanxianPos != Vector3.zero)
                    {
                        isHuanxianPosSet = true;
                    }
                }           
            }

            //NotAtSavePoint();
            Logger.LogInfo("hasBossRushVersion: " + hasBossRushVersion);
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

            if (Input.GetKeyDown(saveKey.Value))
            {
                SaveCurState();
            }

            if (Input.GetKeyDown(loadKey.Value))
            {
                LoadSaveState();
            }

            if (Input.GetKeyDown(SkipKey.Value))
            {
                Skip();
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

            if (isCPU != previousIsCpu)
                setCpu();

            previousIsCpu = isCPU;
        }

        void setCpu()
        {
            Logger.LogInfo($"CPU {isCPU}");
            var p = Player.i;
            if(isCPU)
                Traverse.Create(p.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("BaseValue").SetValue(500.0f);
            else
                Traverse.Create(p.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("BaseValue").SetValue(2.0f);
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
                    var monsterStatField = typeof(MonsterBase).GetField("monsterStat")
                                       ?? typeof(MonsterBase).GetField("_monsterStat");
                    if(monsterStatField != null)
                    {
                        var monsterStat = monsterStatField.GetValue(monsterBase) as MonsterStat; // Assuming MonsterStat is the type of the field
                        if (monsterStat.monsterLevel == MonsterLevel.Boss || monsterStat.monsterLevel == MonsterLevel.MiniBoss)
                            monsterBase.animator.speed = speed;
                    }
                    
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
                bool flag = monsterBase != null
                            && monsterBase.IsAlive()
                            && monsterBase.isActiveAndEnabled
                            && monsterBase.gameObject.activeInHierarchy
                            && monsterBase.gameObject.activeSelf;

                // Get the monsterStat field using reflection to handle both old and new versions
                var monsterStatField = typeof(MonsterBase).GetField("monsterStat")
                                       ?? typeof(MonsterBase).GetField("_monsterStat");
                
                if (monsterStatField != null)
                {
                    var monsterStat = monsterStatField.GetValue(monsterBase) as MonsterStat; // Assuming MonsterStat is the type of the field
                    if (monsterStat != null && monsterStat.monsterLevel == killType && flag)
                    {
                        Logger.LogInfo($"{monsterBase.name} {monsterStat.monsterLevel}");
                        monsterBase.DieSelfDesctruction();
                    }
                }
                else
                {
                    Logger.LogWarning("No valid monsterStat field found on MonsterBase.");
                }
            }
        }


        private void Update()
        {
            CheckScreenSize();
            ProcessShortCut();
            TickLogic();
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
                        isSpeed = GUILayout.Toggle(isSpeed, ShowKey(SpeedToggleKey) + localizationManager.GetString("Timer"), toggleStyle);
                        speedInput = GUILayout.TextField(speedInput, textFieldStyle);
                        float.TryParse(speedInput, out speed);
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    {
                        isFov = GUILayout.Toggle(isFov, ShowKey(FovToggleKey) + localizationManager.GetString("FOV"), toggleStyle);
                        fov = GUILayout.HorizontalSlider(fov, 1f, 180f, GUILayout.Width(200));
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    {
                        isSitAtSavePoint = GUILayout.Toggle(isSitAtSavePoint, localizationManager.GetString("isSitAtSavePoint"), toggleStyle);
                        isCPU = GUILayout.Toggle(isCPU, localizationManager.GetString("isCPU"), toggleStyle);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(ShowKey(SkipKey) + localizationManager.GetString("Skip"), buttonStyle))
                    {
                        Skip();
                    }
                    if (GUILayout.Button(localizationManager.GetString("FullBright"), buttonStyle))
                        FullLight();
                }
                GUILayout.EndHorizontal();
                
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
                    if (GUILayout.Button(localizationManager.GetString("SkillPoint0"), buttonStyle))
                        GameCore.Instance.playerGameData.SkillPointLeft = 0;
                    if (GUILayout.Button(localizationManager.GetString("Gold0"), buttonStyle))
                        GameCore.Instance.playerGameData.CurrentGold = 0;
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
                    {
                        HuanXianTeleport();

                           
                    }
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

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("Enable_Jailed_Weak_Status"), buttonStyle))
                        ModifyFlag("df6a9a9f7748f4baba6207afdf10ea31PlayerAbilityScenarioModifyPack", 1);
                    if (GUILayout.Button(localizationManager.GetString("Disable_Jailed_Weak_Status"), buttonStyle))
                        ModifyFlag("df6a9a9f7748f4baba6207afdf10ea31PlayerAbilityScenarioModifyPack", 0);
                    

                    if (GUILayout.Button(ShowKey(saveKey) + localizationManager.GetString("Save_CurState"), buttonStyle))
                    {
                        SaveCurState();
                        //flagJson = GameFlagManager.FlagsToJson(SaveManager.Instance.allFlags);
                        //Logger.LogInfo(flagJson);
                    }

                    if (GUILayout.Button(ShowKey(loadKey) + localizationManager.GetString("Load_SaveState"), buttonStyle))
                    {
                        //GameFlagManager.Instance.LoadFlagsFromJson(flagJson, SaveManager.Instance.allFlags, TestMode.Build);
                        LoadSaveState();

                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("DiscardFlagsAndReset"), buttonStyle))
                    {
                        if (GameCore.Instance != null)
                            GameCore.Instance.DiscardUnsavedFlagsAndReset();
                        Traverse.Create(Player.i.potion.potionMaxCountData.Stat).Field("BaseValue").SetValue(2f);
                        Player.i.RestoreEverything();
                    }

                    if (GUILayout.Button(localizationManager.GetString("DevModeConfig"), buttonStyle))
                    {
                        SaveManager.Instance.allFlags.AllFlagAwake(TestMode.EditorDevelopment);
                        SaveManager.Instance.allFlags.AllFlagInitStartAndEquip();
                    }
                }
                GUILayout.EndHorizontal();
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
                            GameFlagBase gameFlagBase = null;
                            //key: c4a79371b6ba3ce47bbdda684236f7b5ItemData value:$(重要道具)04_截全毒藥(ItemData)
                            SaveManager.Instance.allFlags.flagDict.TryGetValue("c57a121df65b847a7873d7eb644a80d0ReceiveItemData", out gameFlagBase);


                            var data = gameFlagBase as GameFlagDescriptable;
                            
                            //Logger.LogInfo(gameFlagBase);
                            foreach (var gameFlagDescriptable in Resources.FindObjectsOfTypeAll<GameFlagDescriptable>())
                            {
                                Logger.LogInfo(gameFlagDescriptable);
                                //Logger.LogInfo($"{x.FinalSaveID} {x} summary:{Traverse.Create(x).Field("summary").GetValue<string>()} description:{Traverse.Create(x).Field("description").GetValue<string>()}");
                                //if(Traverse.Create(x).Field("summary").GetValue<string>() != "" && Traverse.Create(x).Field("description").GetValue<string>() != "")
                                if (gameFlagDescriptable.promptViewed.CurrentValue || gameFlagDescriptable.IsImportantObject)
                                {
                                    SingletonBehaviour<UIManager>.Instance.ShowGetDescriptablePrompt(gameFlagDescriptable);
                                }
                                else if (true)
                                {
                                    if (gameFlagDescriptable is ItemData)
                                    {
                                        SingletonBehaviour<UIManager>.Instance.ShowDescriptableNitification(gameFlagDescriptable);
                                    }
                                    else
                                    {
                                        SingletonBehaviour<UIManager>.Instance.ShowGetDescriptablePrompt(gameFlagDescriptable);
                                    }
                                }
                            }

                            //SingletonBehaviour<UIManager>.Instance.ShowGetDescriptablePrompt(data);

                            //Logger.LogInfo(GameObject.Find("AG_S2/Room/Prefab/Treasure Chests 寶箱/LootProvider 刺蝟玉/0_DropPickable Bag FSM/ItemProvider/DropPickable FSM Prototype/--[States]/FSM/[State] Picking/[Action] GetItem"));
                            //var pickItemData = GameObject.Find("AG_S2/Room/Prefab/Treasure Chests 寶箱/LootProvider 刺蝟玉/0_DropPickable Bag FSM/ItemProvider/DropPickable FSM Prototype/--[States]/FSM/[State] Picking/[Action] GetItem").GetComponent<PickItemAction>();
                            //pickItemData.pickItemData = data;
                            //Traverse.Create(pickItemData).Field("itemProvider").Field("item").SetValue(data);
                            //Traverse.Create(GameObject.Find("AG_S2/Room/Prefab/Treasure Chests 寶箱/LootProvider 刺蝟玉/0_DropPickable Bag FSM/ItemProvider/DropPickable FSM Prototype/--[States]/FSM/[State] Picking/[Action] GetItem").GetComponent<PickItemAction>()).Field("itemProvider").Field("item").SetValue(itemData);

                            //Logger.LogInfo(Traverse.Create(Player.i.potion.potionMaxCountData.Stat).Field("BaseValue").GetValue());
                            //ModifyFlag("23168073bb271184b86dc9601f989db3MerchandiseData", 1); //咒滅化緣
                            //ModifyFlag("8ff1633b861daf549b6ceefe7c2c7a1cMerchandiseData", 1); //咒滅化生
                            //ModifyFlag("ab52d2383f0a50c40913616dbd0efe94MerchandiseData", 1); //咒滅化息_二階
                            //Traverse.Create(Player.i).Method("GetAllJades").GetValue();
                            //Logger.LogInfo(Traverse.Create(Player.i.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("BaseValue").GetValue());
                            //SaveManager.Instance.allFlags.AllFlagAwake(TestMode.EditorDevelopment);
                            //bool result = ((!isFromAsset) ? (await ReadFlagsInSave(path)) : ReadFlagsFromResource(path));
                            //SaveManager.Instance.allFlags.AllFlagInitStartAndEquip();
                            //HandleTeleportButtonClick("A7_S2_SectionF_MiniBossFight", new Vector3(7773, 444, 0f)); //法使-幻仙
                            //HandleTeleportButtonClick("A7_S2_SectionF_MiniBossFight", new Vector3(-3776f, -1760f, 0f)); //法使-幻仙
                            //-4004 - -3776 228 
                            // -1880 - -1760 120
                            //Logger.LogInfo(AccessTools.Method(typeof(MonsterBase), "UpdateAnimatorSpeed"));
                            //var parentObject = GameObject.Find("treeroot");
                            //if (parentObject != null)
                            //{
                            //    var icons = parentObject.GetComponentsInChildren<Transform>(true)
                            //                            .Where(t => t.name == "Icon")
                            //                            .Select(t => t.gameObject);

                            //    foreach (var icon in icons)
                            //    {
                            //        icon.SetActive(false);
                            //        Logger.LogInfo(icon);
                            //    }

                            //    var olds = parentObject.GetComponentsInChildren<Transform>(true)
                            //                            .Where(t => t.name == "old")
                            //                            .Select(t => t.gameObject);

                            //    foreach (var old in olds)
                            //    {
                            //        old.SetActive(true);
                            //        var enables = parentObject.GetComponentsInChildren<Transform>(true)
                            //                            .Where(t => t.name == "EnabledImage")
                            //                            .Select(t => t.gameObject);
                            //        foreach (var enable in enables)
                            //        {
                            //            enable.SetActive(false);
                            //        }

                            //        var disables = parentObject.GetComponentsInChildren<Transform>(true)
                            //                            .Where(t => t.name == "DisabledImage")
                            //                            .Select(t => t.gameObject);
                            //        foreach (var disable in disables)
                            //        {
                            //            disable.SetActive(true);
                            //        }
                            //        //Logger.LogInfo(old);
                            //    }
                            //}
                            //else
                            //{
                            //    Logger.LogInfo("GameObject 'viewUI' not found.");
                            //}
                            //Logger.LogInfo(ShowKey(SpeedToggleKey));
                            //GameCore.Instance.SetReviveSavePoint(CreateTeleportPointData(SceneManager.GetActiveScene().name, new Vector3(Player.i.transform.position.x, Player.i.transform.position.y, Player.i.transform.position.z)));
                            //Player.i.RespawnAtSavePoint();
                            //SceneConnectionPoint.ChangeSceneData changeSceneData = GameCore.Instance.FetchReviveData();
                            //GameCore.Instance.ChangeScene(changeSceneData).Forget();

                            //foreach (var skippable in FindObjectsOfType<MonoBehaviour>().OfType<ISkippable>().ToArray())
                            //{
                            //    Logger.LogInfo($"{skippable} {skippable.CanSkip}");
                            //    skippable.TrySkip();
                            //}

                            //foreach (ISkippable item in FindObjectsOfType<MonoBehaviour>().OfType<ISkippable>())
                            //{
                            //    try
                            //    {
                            //        if (item.CanSkip)
                            //        {
                            //            item.TrySkip();
                            //        }
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        // Handle exceptions gracefully
                            //        Debug.LogError($"Error trying to skip item: {ex.Message}");
                            //    }
                            //}
                            //HandleTeleportButtonClick("A1_S2_ConnectionToElevator_Final", new Vector3(75, -3408f, 0f)); //赤虎刀校－百長s
                            //HandleTeleportButtonClick("A4_S4_Container_Final", new Vector3(1968f, -8768f, 0f));
                            //var allStat = SaveManager.Instance.allStatData;
                            //Logger.LogInfo(Traverse.Create(allStat.GetStat("ParryDuration").Stat).Field("BaseValue").GetValue<float>());
                            //allStat.GetStat("ParryDuration").Stat.BaseValue = 0f;

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
                //Traverse.Create(player.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("BaseValue").SetValue(500.0f);
                Traverse.Create(player.potion.potionMaxCountData.Stat).Field("BaseValue").SetValue(8);

                Player.i.RestoreEverything();
            }

            //NotAtSavePoint();
        }

        private void NotAtSavePoint()
        {
            return;
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
                //Traverse.Create(player.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("BaseValue").SetValue(500.0f);
                Traverse.Create(player.potion.potionMaxCountData.Stat).Field("BaseValue").SetValue(8);

                Player.i.RestoreEverything();
            }
            //var skillTreeUI = UnityEngine.Object.FindObjectsOfType<SkillTreeUI>(true)[0];
            //skillTreeUI.ResetAllSkillPoints();
            //NotAtSavePoint();
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

            ModifyFlag("23168073bb271184b86dc9601f989db3MerchandiseData", 1); //咒滅化緣
            ModifyFlag("8ff1633b861daf549b6ceefe7c2c7a1cMerchandiseData", 1); //咒滅化生
            ModifyFlag("ab52d2383f0a50c40913616dbd0efe94MerchandiseData", 1); //咒滅化息_二階
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
                case "PlayerAbilityScenarioModifyPack":
                    var playerAbilityScenarioModifyPack = gameFlagBase as PlayerAbilityScenarioModifyPack;
                    Logger.LogInfo(playerAbilityScenarioModifyPack.IsActivated);
                    if (playerAbilityScenarioModifyPack == null)
                        return;
                    if (valueBool)
                        playerAbilityScenarioModifyPack.ApplyOverriding(playerAbilityScenarioModifyPack);
                    else
                        playerAbilityScenarioModifyPack.RevertApply(playerAbilityScenarioModifyPack);
                    break;
                case "MerchandiseData":
                    var merchandiseData = gameFlagBase as MerchandiseData;
                    Logger.LogInfo(merchandiseData.IsAcquired);
                    if (merchandiseData == null)
                        return;
                    if (valueBool)
                        merchandiseData.item.PlayerPicked();
                    break;
                    
            }
        }

        private async UniTask checkVersionStartGame(string name, TeleportPointData teleportPointData)
        {
            try
            {
                var StartMemoryChallenge = typeof(StartMenuLogic).GetMethod("StartMemoryChallenge");

                if (StartMemoryChallenge != null)
                {
                    //StartMenuLogic.Instance.StartMemoryChallenge();

                    // Check if the StartGame method exists in StartMenuLogic
                        // Old version code: Call StartGame if it exists
                    string sceneName = name; // Replace with the actual scene name
                    StartMemoryChallenge.Invoke(StartMenuLogic.Instance, new object[] { });
                    Logger.LogInfo("Successfully called StartGame with scene name2222: " + sceneName);
                    PrePrecoess(name, teleportPointData, true);
                }
                else
                {
                    string sceneName = name; // Replace with the actual scene name
                    typeof(StartMenuLogic).GetMethod("StartGame").Invoke(StartMenuLogic.Instance, new object[] { sceneName });
                    //startGameMethod.Invoke(startMenuLogicInstance, new object[] { sceneName });
                    Logger.LogInfo("Successfully called StartGame with scene name1111: " + sceneName);
                    await WaitForSceneLoad(sceneName);

                    await WaitForEnterGame();
                    PrePrecoess(name, teleportPointData);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking or invoking StartGame: {ex.Message}");
            }
        }

        private void PreocessGotoScene(string SceneName, TeleportPointData teleportPointData = null)
        {
            Logger.LogInfo("PreocessGotoScene");
            if (Player.i == null)
            {
                if (StartMenuLogic.Instance != null && SceneManager.GetActiveScene().name == "TitleScreenMenu")
                {
                    //StartMenuLogic.Instance.StartGame(SceneName);
                    checkVersionStartGame(SceneName, teleportPointData);
                    //PrePrecoess(SceneName, teleportPointData).Forget();
                    Logger.LogInfo("PrePrecoess");
                }
            }
            else
            {
                if (GameCore.Instance != null)
                {
                    Logger.LogInfo($"{GameCore.Instance} {GameCore.Instance.currentCoreState}");
                    if (GameCore.Instance.currentCoreState == GameCore.GameCoreState.Playing)
                    {
                        if (teleportPointData == null)
                            GameCore.Instance.GoToScene(SceneName);
                        else
                        {
                            //GameCore.Instance.TeleportToSavePoint(teleportPointData);
                            checkTeleportToSavePoint(teleportPointData);
                            GameCore.Instance.SetReviveSavePoint(teleportPointData);
                        }
                        GameCore.Instance.DiscardUnsavedFlagsAndReset();
                        checkMultiplier();
                        CheckGetAll();
                    }
                }
            }
        }

        private void checkTeleportToSavePoint(TeleportPointData teleportPointData)
        {
            try
            {
                var teleportMethod = typeof(GameCore).GetMethod("TeleportToSavePoint");

                if (teleportMethod != null)
                {
                    ParameterInfo[] parameters = teleportMethod.GetParameters();
                    if (parameters.Length == 2)
                    {
                        Logger.LogInfo("Invoking old version of TeleportToSavePoint.");
                        teleportMethod.Invoke(GameCore.Instance, new object[] { teleportPointData, false });
                    }
                    else if (parameters.Length == 3)
                    {
                        Logger.LogInfo("Invoking new version of TeleportToSavePoint.");
                        teleportMethod.Invoke(GameCore.Instance, new object[] { teleportPointData, false, 0f });
                    }
                    else
                    {
                        Logger.LogError("Unexpected parameter count in TeleportToSavePoint method.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking or invoking TeleportToSavePoint: {ex.Message}");
            }
        }

        private void CheckGetAll()
        {
            if (isGetAll)
                GetAllMax();
            else if (isGetAllWithoutActiveSkill)
                GetAllMaxWithoutActiveSkill();
        }

        private async UniTask HuanXianTeleport()
        {
            if (!isHuanxianPosSet)
            {
                HandleTeleportButtonClick("A7_S2_SectionF_MiniBossFight",Vector3.zero);
                await WaitForHuanXiaoPos();
                await WaitForEnterGame();
            }
                var huanPos = new Vector3(HuanxianPos.x - 228, HuanxianPos.y - 120, HuanxianPos.z);
                Logger.LogInfo($"1111 {huanPos} {HuanxianPos}");
                HandleTeleportButtonClick("A7_S2_SectionF_MiniBossFight", huanPos); //法使-幻仙
            
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

        public static void LogInfo(string message)
        {
            logger.LogInfo(message);
        }

        public static void LogError(string message)
        {
            logger.LogError(message);
        }

        void Skip()
        {
            foreach (ISkippable item in FindObjectsOfType<MonoBehaviour>().OfType<ISkippable>())
            {
                try
                {
                    if (item.CanSkip)
                    {
                        item.TrySkip();
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions gracefully
                    Debug.LogError($"Error trying to skip item: {ex.Message}");
                }
            }
        }

        String ShowKey(ConfigEntry<KeyCode> code)
        {
            return $"[{code.BoxedValue.ToString()}]";
        }

        void SaveCurState()
        {
            data = GameCore.Instance.playerGameData.SaveMetaData();
            dataByte = GameFlagManager.FlagsToBinary(SaveManager.Instance.allFlags);
            //flagJson = GameFlagManager.FlagsToJson(SaveManager.Instance.allFlags);
            tmpPos = Player.i.transform.position;
            vel = Player.i.Velocity;
            sceneName = GameCore.Instance.gameLevel.gameObject.scene.name;
        }

        void LoadSaveState()
        {
            GameFlagManager.LoadFlagsFromBinarySave(dataByte, SaveManager.Instance.allFlags, TestMode.Build);
            SaveManager.Instance.allFlags.AllFlagInitStartAndEquip();
            GameCore.Instance.ResetLevel();
            if (sceneName != SceneManager.GetActiveScene().name)
            {
                var teleportToSavePoint = CreateTeleportPointData(sceneName, tmpPos);
                checkTeleportToSavePoint(teleportToSavePoint);

            }
            Player.i.transform.position = tmpPos;
            Player.i.Velocity = vel;
        }
    }
}
