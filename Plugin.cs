using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace NineSolsPlugin
{
    [BepInPlugin("NineSols.Yukikaco.plugin", "Nine Sols 作弊選單 Made By Yuki.kaco", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }

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
        public float speed = 1f;
        private string speedInput = "1";
        private Rect windowRect;

        private void Awake()
        {
            Debug.Log("九日修改器");
            Instance = this;

            MenuToggleKey = Config.Bind<KeyCode>("Menu", "MenuToggleKey", KeyCode.F3, "開啟選單快捷鍵");
            SpeedToggleKey = Config.Bind<KeyCode>("Menu", "SpeedToggleKey", KeyCode.F4, "加速快捷鍵");
            MouseTeleportKey = Config.Bind<KeyCode>("Menu", "MouseTeleportKey", KeyCode.F2, "滑鼠移動快捷鍵");
            isEnableConsole = Config.Bind<bool>("Menu", "isEnableConsole", true, "是否開啟控制台 F1開啟控制台");

            Harmony.CreateAndPatchAll(typeof(Patch));

            // Initialize window size based on screen dimensions
            float width = Screen.width * 0.3f;
            float height = Screen.height * 0.3f;
            windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
        }

        private void Update()
        {
            if (Input.GetKey(MouseTeleportKey.Value))
            {
                var cheatManagerInstance = CheatManager.Instance;
                if (cheatManagerInstance != null)
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
                windowRect = GUI.Window(156789, windowRect, DoMyWindow, "Nine Sols 作弊選單 Made By Yuki.kaco");
            }
        }

        public void DoMyWindow(int windowID)
        {
            GUILayout.BeginArea(new Rect(10, 20, windowRect.width - 20, windowRect.height - 30));
            {
                isInvincible = GUILayout.Toggle(isInvincible, "無敵");
                isOneHitKill = GUILayout.Toggle(isOneHitKill, "一擊必殺");
                isFov = GUILayout.Toggle(isFov, "調整視野距離");
                fov = GUILayout.HorizontalSlider(fov, 1f, 170f, GUILayout.Width(200));
                isSpeed = GUILayout.Toggle(isSpeed, "加速");
                speedInput = GUILayout.TextField(speedInput);
                float.TryParse(speedInput, out speed);
                if (GUILayout.Button("開啟/關閉 地圖亮(關閉特效)"))
                    FullLight();
            }
            GUILayout.EndArea();
            GUI.DragWindow();
        }
    }
}
