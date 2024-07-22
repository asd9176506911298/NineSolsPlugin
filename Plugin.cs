using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace NineSolsPlugin
{
    [BepInPlugin("NineSols.Yukikaco.plugin", "Nine Sols Cheat Menu Made By Yuki.kaco", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }

        private LocalizationManager localizationManager;

        private ConfigEntry<KeyCode> MenuToggleKey;
        private ConfigEntry<KeyCode> SpeedToggleKey;
        private ConfigEntry<KeyCode> MouseTeleportKey;
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
            localizationManager = new LocalizationManager();
            RCGLifeCycle.DontDestroyForever(gameObject);
            Debug.Log("九日修改器");
            Instance = this;

            MenuToggleKey = Config.Bind<KeyCode>("Menu", "MenuToggleKey", KeyCode.F3, "Open Cheat Menu ShortCut");
            SpeedToggleKey = Config.Bind<KeyCode>("Menu", "SpeedToggleKey", KeyCode.F4, "Timer ShortCut");
            MouseTeleportKey = Config.Bind<KeyCode>("Menu", "MouseTeleportKey", KeyCode.F2, "Mouse Move Character ShortCut");
            isEnableConsole = Config.Bind<bool>("Menu", "isEnableConsole", true, "Is Enable Console? F1 Open Console");

            Harmony.CreateAndPatchAll(typeof(Patch));

            // Initialize window size based on screen dimensions
            float width = Screen.width * 0.3f;
            float height = Screen.height * 0.3f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
        }

        private void OnDestory()
        {
            Harmony.UnpatchAll();
        }

        private void Update()
        {
            if(isFov)
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

            if (isSpeed)
            {
                if (speed > 0)
                    TimePauseManager.GlobalSimulationSpeed = speed;
            }
            else
            {
                TimePauseManager.GlobalSimulationSpeed = 1f;
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
            if (showMenu)
            {
                windowRect = GUI.Window(156789, windowRect, DoMyWindow, localizationManager.GetString("title"));
            }
        }

        public void DoMyWindow(int windowID)
        {
            GUILayout.BeginArea(new Rect(10, 20, windowRect.width - 20, windowRect.height - 30));
            {
                isInvincible = GUILayout.Toggle(isInvincible, localizationManager.GetString("invincible"));
                isOneHitKill = GUILayout.Toggle(isOneHitKill, localizationManager.GetString("OHK"));
                isFov = GUILayout.Toggle(isFov, localizationManager.GetString("FOV"));
                fov = GUILayout.HorizontalSlider(fov, 1f, 180f, GUILayout.Width(200));
                isSpeed = GUILayout.Toggle(isSpeed, localizationManager.GetString("Timer"));
                speedInput = GUILayout.TextField(speedInput);
                float.TryParse(speedInput, out speed);
                if (GUILayout.Button(localizationManager.GetString("FullBright")))
                    FullLight();
            }
            GUILayout.EndArea();
            GUI.DragWindow();
        }
    }
}
