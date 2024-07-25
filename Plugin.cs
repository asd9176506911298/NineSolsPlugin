using BepInEx;
using BepInEx.Configuration;
using EasyEditor;
using HarmonyLib;
using RCGMaker.Core;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Linefy.PolygonalMesh;
using System.Collections.Generic;
using System.Reflection;
using RCGMaker.Runtime.Character;
using System.Collections;
using Cysharp.Threading.Tasks;
using XInputDotNetPure;
using static SceneConnectionPoint;

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
        public bool isInfiniteChi = false;
        public bool isInfinitePotion = false;
        public bool isInfiniteAmmo = false;
        public float fov = 68f;
        public float speed = 2f;
        private string speedInput = "2";
        private Rect windowRect;

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
            float height = Screen.height * 0.7f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
        }

        private void Start()
        {
            
        }

        private async UniTaskVoid PrePrecoess(string sceneName, TeleportPointData teleportPointData)
        {
            
            await WaitForSceneLoad(sceneName);

            await WaitForEnterGame();
            
            GameCore.Instance.DiscardUnsavedFlagsAndReset();
            if(teleportPointData != null)
                GameCore.Instance.TeleportToSavePoint(teleportPointData);
            // Now the scene is loaded, run the appropriate method
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
            height *= 0.7f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
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

            toggleStyle.fontSize = toggleSize;
            toggleStyle.padding = new RectOffset(toggleSize * 2, toggleSize * 2, toggleSize / 2, toggleSize / 2);
            textFieldStyle.fontSize = textFieldSize;
            buttonStyle.fontSize = buttonSize;
            buttonStyle.padding = new RectOffset(buttonSize / 3, buttonSize / 3, buttonSize / 3, buttonSize / 3);
            titleStyle.fontSize = Mathf.RoundToInt(basetitleSize * scaleFactor);
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
                Cursor.visible = true;

            if (isFov)
            {
                if ((fov - Input.GetAxis("Mouse ScrollWheel") * 30f > 0) && (fov - Input.GetAxis("Mouse ScrollWheel") * 30f < 180))
                    fov -= Input.GetAxis("Mouse ScrollWheel") * 30f;
            }

            if (isSpeed)
            {
                if (speed > 0)
                    TimePauseManager.GlobalSimulationSpeed = speed;
            }
            else
            {
                TimePauseManager.GlobalSimulationSpeed = 1f;
            }

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

            if (isInfiniteChi)
            {
                if(Player.i != null && Player.i.chiContainer != null){
                    Player.i.chiContainer.GainFull();
                }
            }

            if (isInfinitePotion)
            {
                if (Player.i != null && Player.i.potion != null)
                {
                    Player.i.potion.GainFull();
                }
            }

            if (isInfiniteAmmo)
            {
                if (Player.i != null && Player.i.ammo != null)
                {
                    Player.i.ammo.GainFull();
                }
            }
        }

        private void Update()
        {
            CheckScreenSize();
            ProcessShortCut();
            TickLogic();

            if (Input.GetKeyDown(KeyCode.Insert))
            {

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
                //            Logger.LogInfo($"key:{x.Key} value:{x.Value} bool:{scriptableDataBool.isValid}");
                //            break;
                //        case "InterestPointData":
                //            var interestPointData = x.Value as InterestPointData;
                //            Logger.LogInfo($"key:{x.Key} value:{x.Value} bool:{interestPointData.IsSolved}");
                //            break;
                //                //var scriptableDataBool = x.Value as ScriptableDataBool;
                //                //Logger.LogInfo($"key:{x.Key} value:{x.Value} bool:{scriptableDataBool.isValid}");
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
                float scaleFactor = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
                int toggleSize = Mathf.RoundToInt(baseToggleSize * scaleFactor);
                int buttonSize = Mathf.RoundToInt(baseButtonSize * scaleFactor);
                int textFieldSize = Mathf.RoundToInt(baseTextFieldSize * scaleFactor);

                toggleStyle.fontSize = toggleSize;
                toggleStyle.padding = new RectOffset(toggleSize * 2, toggleSize * 2, toggleSize / 2, toggleSize / 2);
                textFieldStyle.fontSize = textFieldSize;
                buttonStyle.fontSize = buttonSize;
                buttonStyle.padding = new RectOffset(buttonSize / 3, buttonSize / 3, buttonSize / 3, buttonSize / 3);
                titleStyle.fontSize = Mathf.RoundToInt(basetitleSize * scaleFactor);
                isInit = true;
            }

            if (showMenu)
            {
                windowRect = GUI.Window(156789, windowRect, DoMyWindow, localizationManager.GetString("title"), titleStyle);
            }
        }

        public void DoMyWindow(int windowID)
        {
            GUILayout.BeginArea(new Rect(10, 20, windowRect.width - 20, windowRect.height - 30));
            {
                GUILayout.BeginHorizontal();
                {
                    isInvincible = GUILayout.Toggle(isInvincible, localizationManager.GetString("invincible"), toggleStyle);
                    isOneHitKill = GUILayout.Toggle(isOneHitKill, localizationManager.GetString("OHK"), toggleStyle);
                    isFastLearnSkill = GUILayout.Toggle(isFastLearnSkill, localizationManager.GetString("isFastLearnSkill"), toggleStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    isInfiniteChi = GUILayout.Toggle(isInfiniteChi, localizationManager.GetString("isInfiniteChi"), toggleStyle);
                    isInfinitePotion = GUILayout.Toggle(isInfinitePotion, localizationManager.GetString("isInfinitePotion"), toggleStyle);
                    isInfiniteAmmo = GUILayout.Toggle(isInfiniteAmmo, localizationManager.GetString("isInfiniteAmmo"), toggleStyle);
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
                    isGetAll = GUILayout.Toggle(isGetAll, localizationManager.GetString("Boss Apply GetAllMax"), toggleStyle);
                    isGetAllWithoutActiveSkill = GUILayout.Toggle(isGetAllWithoutActiveSkill, localizationManager.GetString("Boss Apply GetAllMaxWithoutActiveSkill"), toggleStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("A2_S5_BossHorseman_Final"), buttonStyle))
                        HandleTeleportButtonClick("A2_S5_BossHorseman_Final", new Vector3(-5195f, -2288f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A3_S5_BossGouMang_Final"), buttonStyle))
                        HandleTeleportButtonClick("A3_S5_BossGouMang_Final", new Vector3(-4287f, -2288f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A4_S5_DaoTrapHouse_Final"), buttonStyle))
                        HandleTeleportButtonClick("A4_S5_DaoTrapHouse_Final", new Vector3(1833f, -3744f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A5_S5_JieChuanHall"), buttonStyle))
                        HandleTeleportButtonClick("A5_S5_JieChuanHall", new Vector3(-4500f, -2288f, 0f));

                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("A7_S5_Boss_ButterFly"), buttonStyle))
                        HandleTeleportButtonClick("A7_S5_Boss_ButterFly", new Vector3(-2640f, -1104f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A9_S5_風氏"), buttonStyle))
                        HandleTeleportButtonClick("A9_S5_風氏", new Vector3(-2340f, -1264f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A10_S5_Boss_Jee"), buttonStyle))
                        HandleTeleportButtonClick("A10_S5_Boss_Jee", new Vector3(-90f, -64f, 0f));
                    if (GUILayout.Button(localizationManager.GetString("A11_S0_Boss_YiGung_回蓬萊"), buttonStyle))
                        HandleTeleportButtonClick("A11_S0_Boss_YiGung_回蓬萊", new Vector3(-2686f, -1104f, 0f));
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button(localizationManager.GetString("Skip"), buttonStyle))
                {
                    SkippableManager.Instance.TrySkip();
                }
                if (GUILayout.Button(localizationManager.GetString("故意放棄進度 reset這個場景"), buttonStyle))
                {
                    if (GameCore.Instance != null)
                        GameCore.Instance.DiscardUnsavedFlagsAndReset();
                }
                if (GUILayout.Button(localizationManager.GetString("Test"), buttonStyle))
                {
                    //HandleTeleportButtonClick("A1_S2_ConnectionToElevator_Final", new Vector3(1820f, -4432f, 0f)); //赤虎刀校－百長
                    //HandleTeleportButtonClick("A2_S6_LogisticCenter_Final", new Vector3(4370, -6768, 0f)); //赤虎刀校－炎刃
                    //HandleTeleportButtonClick("A2_S2_ReactorRight_Final", new Vector3(-4642, -1968, 0f)); //天綱步衛－角端

                    //HandleTeleportButtonClick("A10_S4_HistoryTomb_Left", new Vector3(-690, -368, 0f)); //魂守－刺行：完成三間密室內的考驗，此BOSS才會出現。
                    //ModifyFlag("44bc69bd40a7f6d45a2b8784cc8ebbd1ScriptableDataBool", 1); //A10_SG1_Cave1_[Variable] 看過天尊棺材演出_科技天尊 (ScriptableDataBool)
                    //ModifyFlag("118f725174ccdf5498af6386d4987482ScriptableDataBool", 1); //A10_SG2_Cave2_[Variable] 看過天尊棺材演出_經濟天尊 (ScriptableDataBool)
                    //ModifyFlag("d7a444315eab0b74fb0ed1e8144edf73ScriptableDataBool", 1); //A10_SG3_Cave4_[Variable] 看過天尊棺材演出_軍事天尊 (ScriptableDataBool)

                    //HandleTeleportButtonClick("A3_S2_GreenHouse_Final", new Vector3(-4530, -1216, 0f)); //天綱影者－水鬼
                    //Traverse.Create(Player.i).Method("UnlockChargedAttack").GetValue();

                    //HandleTeleportButtonClick("A9_S1_Remake_4wei", new Vector3(-3330, 352, 0f)); //巨錘機兵－

                    //HandleTeleportButtonClick("A9_S1_Remake_4wei", new Vector3(-3330, 352, 0f)); //巨錘機兵－

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
                Traverse.Create(player.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("_value").SetValue(500.0f);
                Traverse.Create(player.potion.potionMaxCountData.Stat).Field("_value").SetValue(8);

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
                Traverse.Create(player.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("_value").SetValue(500.0f);
                Traverse.Create(player.potion.potionMaxCountData.Stat).Field("_value").SetValue(8);

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
