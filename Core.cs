using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using ScheduleOne.ItemFramework;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(UniversalStackLimit.Core), "Stack Limit Plus MONO", "1.1.0", "BlessingMds")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace UniversalStackLimit
{
    public class Core : MelonMod
    {
        private static int _globalStackLimit = 40; //default
        private static bool _menuVisible = false;
        private static Rect _menuRect = new Rect(20, 20, 310, 170);
        private static bool _sceneLoaded = false;
        private static bool _hasInitialized = false;
        private static int _frameCounter = 0;
        private const int CHECK_INTERVAL = 60;

        private static readonly string ConfigFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "StackLimitPlus.txt");

        private static readonly Color _greenColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        private static readonly Color _yellowColor = new Color(1f, 0.9f, 0.2f, 1f);
        private static Texture2D _whiteTexture;

        private static HarmonyLib.Harmony _harmony;

        public override void OnInitializeMelon()
        {
            _harmony = new HarmonyLib.Harmony("com.blessingmds.universalstacklimit");
            _harmony.PatchAll();

            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();

            LoadStackLimit();
            LoggerInstance.Msg($"Stack Limit Plus MONO loaded! Press F6 for menu.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                LoggerInstance.Msg("Main scene loaded. Reloading user preference and resetting item update...");
                _sceneLoaded = true;
                _hasInitialized = false;
                LoadStackLimit();
                ItemDefinitionConstructorPatch.AllItems.Clear();
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F6))
                _menuVisible = !_menuVisible;

            if (_sceneLoaded && !_hasInitialized)
            {
                _frameCounter++;
                if (_frameCounter >= CHECK_INTERVAL)
                {
                    _frameCounter = 0;

                    if (IsPlayerLoaded())
                    {
                        FindAndUpdateAllItems();
                        _hasInitialized = true;
                        LoggerInstance.Msg("Local player detected. Items updated with user preference: " + _globalStackLimit);
                    }
                }
            }
        }

        private bool IsPlayerLoaded()
        {
            try
            {
                var localPlayerObj = GameObject.Find("Player_Local");
                if (localPlayerObj != null && localPlayerObj.activeInHierarchy)
                {
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error checking if player is loaded: {ex.Message}");
                return false;
            }
        }

        private static void FindAndUpdateAllItems()
        {
            var resourceItems = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            MelonLogger.Msg($"Found {resourceItems.Length} items");

            int newItemsAdded = 0;
            foreach (var item in resourceItems)
            {
                if (item == null) continue;
                if (!ItemDefinitionConstructorPatch.AllItems.Contains(item))
                {
                    ItemDefinitionConstructorPatch.AllItems.Add(item);
                    newItemsAdded++;
                    UpdateStackLimit(item);
                }
            }
            UpdateAllDefinitions();
        }

        public override void OnGUI()
        {
            if (!_menuVisible) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _menuRect = GUI.Window(0, _menuRect, (GUI.WindowFunction)DrawMenuWindow, "");
        }

        private static void DrawMenuWindow(int windowId)
        {
            GUI.color = _greenColor;
            GUI.Label(new Rect(10, 10, 230, 25), "StackLimit", new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            });

            GUI.color = _yellowColor;
            GUI.Label(new Rect(110, 10, 50, 25), "Plus", new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            });

            GUI.color = Color.white;
            DrawLine(new Rect(10, 35, _menuRect.width - 20, 1), Color.gray);

            GUI.Label(new Rect(10, 45, 150, 20), "Current Stack Limit:");
            GUI.Label(new Rect(150, 45, 100, 20), _globalStackLimit.ToString(), new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight
            });

            _globalStackLimit = (int)GUI.HorizontalSlider(new Rect(10, 70, _menuRect.width - 20, 20), _globalStackLimit, 1, 9999);

            string limitText = GUI.TextField(new Rect(10, 95, 70, 20), _globalStackLimit.ToString());
            if (int.TryParse(limitText, out int newLimit) && newLimit > 0)
                _globalStackLimit = newLimit;

            GUI.Label(new Rect(85, 95, 70, 20), "Presets:");
            if (GUI.Button(new Rect(135, 95, 35, 20), "10"))
                UpdateStackLimit(10);
            if (GUI.Button(new Rect(175, 95, 35, 20), "40"))
                UpdateStackLimit(40);
            if (GUI.Button(new Rect(215, 95, 35, 20), "99"))
                UpdateStackLimit(99);
            if (GUI.Button(new Rect(255, 95, 45, 20), "999"))
                UpdateStackLimit(999);

            if (GUI.Button(new Rect(10, 125, 30, 30), "-"))
            {
                if (_globalStackLimit > 1)
                    UpdateStackLimit(_globalStackLimit - 1);
            }

            if (GUI.Button(new Rect(45, 125, 30, 30), "+"))
            {
                if (_globalStackLimit < 9999)
                    UpdateStackLimit(_globalStackLimit + 1);
            }

            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 1f);
            if (GUI.Button(new Rect(95, 125, 100, 30), "Update"))
            {
                UpdateAllDefinitions();
                SaveStackLimit();
                MelonLogger.Msg($"Stack limit updated to {_globalStackLimit}");
            }

            GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            if (GUI.Button(new Rect(205, 125, 85, 30), "Close"))
            {
                _menuVisible = false;
            }

            GUI.backgroundColor = Color.white;

            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(0, _menuRect.height - 22, _menuRect.width, 20),
                $"F6 to toggle • {ItemDefinitionConstructorPatch.AllItems.Count} items",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 });

            GUI.color = Color.white;
            GUI.DragWindow();
        }

        private static void DrawLine(Rect rect, Color color)
        {
            Color savedColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = savedColor;
        }

        private static void UpdateStackLimit(int newLimit)
        {
            _globalStackLimit = newLimit;
            SaveStackLimit();
        }

        private static void UpdateStackLimit(ItemDefinition item)
        {
            if (item == null) return;
            try
            {
                var stackLimitField = AccessTools.Field(item.GetType(), "StackLimit");
                if (stackLimitField != null)
                {
                    stackLimitField.SetValue(item, _globalStackLimit);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Update failed: {ex.Message}");
            }
        }

        private static void SaveStackLimit()
        {
            try
            {
                File.WriteAllText(ConfigFilePath, _globalStackLimit.ToString());
                MelonLogger.Msg($"Stack limit saved to {ConfigFilePath}");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to save stack limit: {ex.Message}");
            }
        }

        private static void LoadStackLimit()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string content = File.ReadAllText(ConfigFilePath);
                    if (int.TryParse(content, out int savedLimit))
                    {
                        _globalStackLimit = Mathf.Clamp(savedLimit, 1, 9999);
                        MelonLogger.Msg($"Loaded stack limit from preference file: {_globalStackLimit}");
                    }
                }
                else
                {
                    SaveStackLimit();
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to load stack limit: {ex.Message}");
            }
        }

        public static void UpdateAllDefinitions()
        {
            int totalUpdated = 0;

            foreach (var item in ItemDefinitionConstructorPatch.AllItems)
            {
                if (item == null) continue;
                try
                {
                    var stackLimitField = AccessTools.Field(item.GetType(), "StackLimit");
                    if (stackLimitField != null)
                    {
                        stackLimitField.SetValue(item, _globalStackLimit);
                        totalUpdated++;
                    }
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Error($"Update failed: {ex.Message}");
                }
            }

            MelonLogger.Msg($"Updated {totalUpdated} items");
        }

        [HarmonyPatch(typeof(ItemDefinition))]
        public class ItemDefinitionConstructorPatch
        {
            public static List<ItemDefinition> AllItems = new List<ItemDefinition>();

            [HarmonyPostfix]
            [HarmonyPatch(MethodType.Constructor)]
            static void OnItemDefinitionCreated(ItemDefinition __instance)
            {
                if (__instance == null) return;

                var stackLimitField = AccessTools.Field(typeof(ItemDefinition), "StackLimit");
                if (stackLimitField != null)
                {
                    stackLimitField.SetValue(__instance, Core._globalStackLimit);
                }

                if (!AllItems.Contains(__instance))
                {
                    AllItems.Add(__instance);
                }
            }
        }
    }
}
