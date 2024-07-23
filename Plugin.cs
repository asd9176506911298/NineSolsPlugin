using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public bool isSpeed = false;
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
            float height = Screen.height * 0.6f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
        }

        private void Start()
        {
            
        }

        void OnScreenSizeChanged(float width, float height)
        {
            width *= 0.5f;
            height *= 0.6f;
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

        private void Update()
        {
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            {
                // Update lastScreenSize with the current screen dimensions
                lastScreenSize.x = Screen.width;
                lastScreenSize.y = Screen.height;


                // Call a method or raise an event indicating screen size change
                OnScreenSizeChanged(lastScreenSize.x, lastScreenSize.y);
            }

            if (isFov)
            {
                if ((fov - Input.GetAxis("Mouse ScrollWheel") * 30f > 0) && (fov - Input.GetAxis("Mouse ScrollWheel") * 30f < 180))
                    fov -= Input.GetAxis("Mouse ScrollWheel") * 30f;
            }

            if (showMenu || Input.GetKey(MouseTeleportKey.Value))
                Cursor.visible = true;

            if (Input.GetKey(MouseTeleportKey.Value))
            {
                var cheatManagerInstance = CheatManager.Instance;
                if (cheatManagerInstance != null &&  Player.i != null)
                {
                    Traverse.Create(cheatManagerInstance).Method("DropPlayerAtMousePosition").GetValue();
                }
            }

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
                if(Player.i != null && Player.i.GetHealth != null)
                    Player.i.GetHealth.isInvincibleVote.Vote(Player.i.gameObject, true);
            }
            else
            {
                if (Player.i != null && Player.i.GetHealth != null)
                    Player.i.GetHealth.isInvincibleVote.Vote(Player.i.gameObject, false);
            }

            

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
                isInvincible = GUILayout.Toggle(isInvincible, localizationManager.GetString("invincible"), toggleStyle);
                isOneHitKill = GUILayout.Toggle(isOneHitKill, localizationManager.GetString("OHK"), toggleStyle);
                isFov = GUILayout.Toggle(isFov, localizationManager.GetString("FOV"), toggleStyle);
                fov = GUILayout.HorizontalSlider(fov, 1f, 180f, GUILayout.Width(200));
                isSpeed = GUILayout.Toggle(isSpeed, localizationManager.GetString("Timer"), toggleStyle);
                speedInput = GUILayout.TextField(speedInput, textFieldStyle);
                float.TryParse(speedInput, out speed);
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
                if (GUILayout.Button(localizationManager.GetString("UnlockAll"), buttonStyle))
                {
                    if (Player.i != null)
                    {
                        var player = Player.i;
                        Traverse.Create(player).Method("UnlockAll").GetValue();
                        Traverse.Create(player).Method("AddSkillPoint").GetValue();
                        Traverse.Create(player).Method("AddMoney").GetValue();
                        Traverse.Create(player).Method("GetAllJades").GetValue();
                        Traverse.Create(player).Method("GetAllJadeSlots").GetValue();
                        Traverse.Create(player.mainAbilities.PlayerMaxJadePowerStat.Stat).Field("_value").SetValue(500.0f);
                    }

                    GameObject Jade = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/[Jade 玉] Multiple Collection Select Panel/[Condition] 在存檔點");
                    GameObject JadeSlot = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/[Jade 玉] Multiple Collection Select Panel/[Condition] 算力還沒滿/Max JadePower");
                    GameObject Skill = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/[經絡]SkillTreeUI Manager/[Condition] 不在存檔點不能點技能");
                    if (Jade != null)
                        Jade.SetActive(false);
                    if (Skill != null)
                        Skill.SetActive(false);
                }
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("A2_S5_BossHorseman_Final"), buttonStyle))
                        GotoScene("A2_S5_BossHorseman_Final");
                    if (GUILayout.Button(localizationManager.GetString("A3_S5_BossGouMang_Final"), buttonStyle))
                        GotoScene("A3_S5_BossGouMang_Final");
                    if (GUILayout.Button(localizationManager.GetString("A4_S5_DaoTrapHouse_Final"), buttonStyle))
                        GotoScene("A4_S5_DaoTrapHouse_Final");
                    if (GUILayout.Button(localizationManager.GetString("A5_S5_JieChuanHall"), buttonStyle))
                        GotoScene("A5_S5_JieChuanHall");
                    
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(localizationManager.GetString("A7_S5_Boss_ButterFly"), buttonStyle))
                        GotoScene("A7_S5_Boss_ButterFly");
                    if (GUILayout.Button(localizationManager.GetString("A9_S5_風氏"), buttonStyle))
                        GotoScene("A9_S5_風氏");
                    if (GUILayout.Button(localizationManager.GetString("A10_S5_Boss_Jee"), buttonStyle))
                        GotoScene("A10_S5_Boss_Jee");
                    if (GUILayout.Button(localizationManager.GetString("A11_S0_Boss_YiGung_回蓬萊"), buttonStyle))
                        GotoScene("A11_S0_Boss_YiGung_回蓬萊");
                }
                GUILayout.EndHorizontal();


                    if (GUILayout.Button(localizationManager.GetString("Skip"), buttonStyle))
                    SkippableManager.Instance.TrySkip();
            }

            GUILayout.EndArea();
            GUI.DragWindow();
        }

        private void GotoScene(string SceneName)
        {
            if (Player.i == null)
            {
                if (StartMenuLogic.Instance != null && SceneManager.GetActiveScene().name == "TitleScreenMenu")
                {
                    StartMenuLogic.Instance.StartGame(SceneName);
                }
            }
            else
            {
                if (GameCore.Instance != null)
                {
                    if (GameCore.Instance.currentCoreState == GameCore.GameCoreState.Playing)
                    {
                        GameCore.Instance.GoToScene(SceneName);
                        GameCore.Instance.DiscardUnsavedFlagsAndReset();
                    }
                }
            }
        }
    }
}
