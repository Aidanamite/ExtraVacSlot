﻿using HarmonyLib;
using SRML;
using SRML.Console;
using SRML.Config;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MonomiPark.SlimeRancher.DataModel;
using MonomiPark.SlimeRancher.Persist;
using SRML.Config.Attributes;

namespace ExtraVacSlot
{
    public class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{System.Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
        public static readonly int defaultSlots = AmmoSlotUI.MAX_SLOTS;
        public const string fileName = "interal config";
        public static float aspectRatio => (float)Screen.width / Screen.height;
        public static Dictionary<string, string> nameReplace = new Dictionary<string, string> // Used for dynamically generating KeyCode nicknames
        {
            ["Alpha"] = "",
            ["Keypad"] = "Num",
            ["Plus"] = "+",
            ["Minus"] = "-",
            ["Divide"] = "/",
            ["Multiply"] = "*",
            ["Period"] = ".",
            ["Equals"] = "=",
            ["Left"] = "Lf",
            ["Right"] = "Rt",
            ["Windows"] = "Win",
            ["Control"] = "Ctrl"
        };
        public static Dictionary<string, KeyCode> nickname = new Dictionary<string, KeyCode> // Stores a list of nicknames for the KeyCodes
        {
            [";"] = KeyCode.Semicolon,
            ["'"] = KeyCode.Quote,
            ["/"] = KeyCode.Slash,
            ["\\"] = KeyCode.Backslash,
            ["Ins"] = KeyCode.Insert,
            ["Del"] = KeyCode.Delete,
            ["PgDn"] = KeyCode.PageDown,
            ["PgUp"] = KeyCode.PageUp,
            ["`"] = KeyCode.BackQuote,
            ["Left"] = KeyCode.LeftArrow,
            ["Right"] = KeyCode.RightArrow,
            ["Up"] = KeyCode.UpArrow,
            ["Down"] = KeyCode.DownArrow,
            ["("] = KeyCode.LeftParen,
            ["["] = KeyCode.LeftBracket,
            ["{"] = KeyCode.LeftCurlyBracket,
            [")"] = KeyCode.RightParen,
            ["]"] = KeyCode.RightBracket,
            ["}"] = KeyCode.RightCurlyBracket
        };
        public static List<string> autoComplete = new List<string>() { "reset" };
        public Main()
        {
            SRML.Config.Parsing.ParserRegistry.RegisterParser(new keybindDictionaryParser());
        }
        public override void PreLoad()
        {
            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (nickname.ContainsValue(k))
                    continue;
                bool flag = false;
                string name = k.ToString();
                foreach (var pair in nameReplace)
                    if (name.Contains(pair.Key))
                    {
                        name = name.Replace(pair.Key, pair.Value);
                        flag = true;
                    }
                if (flag)
                    nickname.Add(name, k);
                autoComplete.Add(k.ToString());
            }
            foreach (var name in nickname.Keys)
                autoComplete.Add(name);
            HarmonyInstance.PatchAll();
            Console.RegisterCommand(new ChangeSlotsCommand());
            Console.RegisterCommand(new SetSlotBindCommand());
            //Console.RegisterCommand(new iCommand());
        }
        public static void Log(string message) => Console.Log($"[{modName}]: " + message);
        public static void LogError(string message) => Console.LogError($"[{modName}]: " + message);
        public static void LogWarning(string message) => Console.LogWarning($"[{modName}]: " + message);
        public static void LogSuccess(string message) => Console.LogSuccess($"[{modName}]: " + message);
        public static void Insert<T>(ref T[] array, T value, int index)
        {
            var list = array.ToList();
            list.Insert(index, value);
            array = list.ToArray();
        }
        public static void InsertRange<T>(ref T[] array, T[] value, int index)
        {
            var list = array.ToList();
            list.InsertRange(index, value);
            array = list.ToArray();
        }

        public static void SaveConfig()
        {
            SRMod mod = SRModLoader.GetModForAssembly(modAssembly);
            SRMod.ForceModContext(mod);
            mod.Configs.Find((x) => x.FileName.ToLower() == fileName.ToLower()).SaveToFile();
            SRMod.ClearModContext();
        }

        public static string GetRectTree(RectTransform rect, string prefix = " -")
        {
            var s = prefix + "Object: " + rect.name;
            foreach (var c in rect.GetComponents<Component>())
                s += " | " + c.GetType().Name;
            s += $"\n{prefix}Rect: {rect.rect}\n{prefix}Position: {rect.position}\n{prefix}Local Position: {rect.localPosition}\n{prefix}Local Scale: {rect.localScale}\n{prefix}Size Delta: {rect.sizeDelta}\n{prefix}Anchor Min: {rect.anchorMin}\n{prefix}Anchor Max: {rect.anchorMax}\n{prefix}Offset Min: {rect.offsetMin}\n{prefix}Offset Max: {rect.offsetMax}";
            foreach (RectTransform r in rect)
                s += "\n" + GetRectTree(r, prefix + "--");
            return s;
        }
    }

    static class ExtentionMethods {
        public static List<T> ToList<T>(this T[] array) => new List<T>(array);
        public static bool TryParseKeyCode(this string value, out KeyCode key) => Main.nickname.TryGetValue(value, out key) || KeyCode.TryParse(value, out key);
        public static string GetName(this KeyCode key)
        {
            foreach (var pair in Main.nickname)
                if (pair.Value == key)
                    return pair.Key;
            return key.ToString();
        }
        public static T Find<T>(this IEnumerable<T> set, System.Predicate<T> predicate)
        {
            foreach (var value in set) if (predicate(value)) return value;
            return default(T);
        }
        public static float GetChildWidth(this RectTransform main, bool excludeFirst = false)
        {
            var r = main.GetChildSize(excludeFirst);
            return r.y - r.x;
        }
        public static Vector2 GetChildSize(this RectTransform main, bool excludeFirst = false)
        {
            Vector2 s = new Vector2(main.rect.x, main.rect.x + main.rect.width);
            if (excludeFirst)
                s = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
            foreach (RectTransform rect in main)
            {
                if (!rect.gameObject.activeSelf)
                    continue;
                var c = rect.GetChildSize();
                s = new Vector2(Mathf.Min(c.x + rect.localPosition.x, s.x), Mathf.Max(c.y + rect.localPosition.x, s.y));
            }
            return s;
        }
        public static float Lerp(this float current, float target, float change) => (current == target) ? current : ((current < target) ? Mathf.Min(current + change,target) : Mathf.Max(current - change, target));
        public static T AddOrGetComponent<T>(this MonoBehaviour obj) where T : Component
        {
            var c = obj.GetComponent<T>();
            if (c)
                return c;
            return obj.gameObject.AddComponent<T>();
        }
        public static Rect GetSize(this RectTransform transform)
        {
            var pRect = ((RectTransform)transform.parent).rect;
            var x = transform.anchorMin.x * pRect.width + transform.offsetMin.x;
            var y = transform.anchorMin.y * pRect.height + transform.offsetMin.y;
            return new Rect(x, y,
                transform.anchorMax.x * pRect.width + transform.offsetMax.x - x,
                transform.anchorMax.y * pRect.height + transform.offsetMax.y - y);
        }
    }
    
    [HarmonyPatch(typeof(PlayerState), "Reset")]
    class Patch_PlayerState_Reset
    {
        public static bool called = false;
        public static void Prefix() => called = true;
    }

    [HarmonyPatch(typeof(Ammo), MethodType.Constructor, new System.Type[] { typeof(HashSet<Identifiable.Id>), typeof(int), typeof(int), typeof(System.Predicate<Identifiable.Id>[]), typeof(System.Func<Identifiable.Id, int, int>) })]
    class Patch_Ammo_ctor
    {
        public static void Prefix(ref int numSlots, ref int usableSlots, ref System.Predicate<Identifiable.Id>[] slotPreds)
        {
            if (Patch_PlayerState_Reset.called)
            {
                Patch_PlayerState_Reset.called = false;
                for (int i = 0; i < Config.slots - numSlots; i++)
                {
                    Main.Insert(ref slotPreds, slotPreds[0], 0);
                };
                usableSlots += Config.slots - numSlots;
                numSlots = Config.slots;
            }
        }
    }

    [HarmonyPatch(typeof(MonomiPark.SlimeRancher.SavedGame), "AmmoDataToSlots", new System.Type[] {typeof(Dictionary<PlayerState.AmmoMode, List<AmmoDataV02>>) })]
    class Patch_SavedGame_AmmoDataToSlots
    {
        public static void Prefix(Dictionary<PlayerState.AmmoMode, List<AmmoDataV02>> ammo)
        {
            var data = ammo[PlayerState.AmmoMode.DEFAULT];
            for (int i = data.Count; i > Config.slots; i--)
                data.RemoveAt(Config.slots - 1);
            for (int i = data.Count; i < Config.slots; i++)
                data.Insert(data.Count - 1, new AmmoDataV02() { id = Identifiable.Id.NONE, count = 0, emotionData = new SlimeEmotionDataV02() });
        }
    }

    [HarmonyPatch(typeof(PlayerModel), "ApplyUpgrade")]
    class Patch_PlayerModel_ApplyUpgrade
    {
        public static bool Prefix(PlayerModel __instance, PlayerState.Upgrade upgrade)
        {
            if (upgrade == PlayerState.Upgrade.LIQUID_SLOT)
            {
                var ammo = __instance.ammoDict[PlayerState.AmmoMode.DEFAULT];
                ammo.IncreaseUsableSlots(ammo.slots.Length);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(AmmoSlotUI), "Awake")]
    class Patch_AmmoSlotUI_Awake
    {
        public static void Prefix(AmmoSlotUI __instance)
        {
            __instance.AddOrGetComponent<AnimationHandler>();
            var mask = __instance.AddOrGetComponent<RectMask2D>();
            mask.enabled = true;
            var layout = __instance.GetComponent<HorizontalLayoutGroup>();
            layout.padding.left = (int)(mask.rectTransform.rect.height * 0.2f);
            layout.padding.right = layout.padding.left;
            mask.rectTransform.sizeDelta *= 2.5f;
            var l = mask.rectTransform.offsetMin.y;
            mask.rectTransform.localPosition -= new Vector3(0, l, 0);
            mask.rectTransform.sizeDelta += new Vector2(0, l);
            layout.childAlignment = TextAnchor.LowerLeft;
            var extraSlots = Config.slots - __instance.lastSlotCounts.Length;
            var sU = new AmmoSlotUI.Slot[extraSlots];
            for (int i = extraSlots - 1; i >= 0; i--)
            {
                var newSlot = GameObject.Instantiate(__instance.transform.Find("Ammo Slot 1").gameObject, __instance.transform, false);
                newSlot.name = "Ammo Slot ?";
                newSlot.transform.SetSiblingIndex(0);
                sU[i] =  new AmmoSlotUI.Slot()
                {
                    anim = newSlot.GetComponent<Animator>(),
                    back = newSlot.transform.Find("Ammo Slot").Find("Behind").GetComponent<Image>(),
                    bar = newSlot.transform.Find("Ammo Slot").GetComponent<StatusBar>(),
                    front = newSlot.transform.Find("Ammo Slot").Find("Frame").GetComponent<Image>(),
                    icon = newSlot.transform.Find("Icon").GetComponent<Image>(),
                    keyBinding = newSlot.transform.Find("Keybinding").gameObject,
                    label = newSlot.transform.Find("Label").GetComponent<TMPro.TMP_Text>()
                };
                Main.Insert(ref __instance.lastSlotIds, __instance.lastSlotIds[0], 0);
            }
            Main.InsertRange(ref __instance.lastSlotCounts, new int[extraSlots], 0);
            Main.InsertRange(ref __instance.lastSlotMaxAmmos, new int[extraSlots], 0);
            var keyText = new List<XlateKeyText>();
            for (int i = 0; i < Main.defaultSlots; i++)
                keyText.Add(__instance.slots[i].keyBinding.GetComponentInChildren<XlateKeyText>());
            Main.InsertRange(ref __instance.slots, sU, 0);
            for (int i = 0; i < __instance.slots.Length; i++)
            {
                var key = __instance.slots[i].keyBinding.GetComponentInChildren<XlateKeyText>();
                if (i < keyText.Count)
                    CopySettings(key, keyText[i]);
                else if (Patch_XlateKeyText_OnKeysChanged.custom.ContainsKey(key))
                    Patch_XlateKeyText_OnKeysChanged.custom[key] = i;
                else
                    Patch_XlateKeyText_OnKeysChanged.custom.Add(key, i);
            }

        }
        public static void CopySettings(XlateKeyText target, XlateKeyText source)
        {
            target.key = source.key;
            target.inputKey = source.inputKey;
            target.bundlePath = source.bundlePath;
            target.bundle = source.bundle;
        }
    }

    class AnimationHandler : MonoBehaviour
    {
        RectTransform rect;
        RectTransform parent;
        RectTransform healthBar;
        AmmoSlotUI ui;
        public RectTransform Parent => parent;
        HorizontalLayoutGroup group;
        public HorizontalLayoutGroup LayoutGroup => group;
        public RectTransform RectTransform => rect;
        public float TargetOffset = 0;
        float current = 0;
        public void Start()
        {
            rect = GetComponent<RectTransform>();
            parent = (RectTransform)transform.parent;
            healthBar = (RectTransform)parent.Find("Health Meter");
            group = GetComponent<HorizontalLayoutGroup>();
            ui = GetComponent<AmmoSlotUI>();
        }
        public void Update()
        {
            var childWidth = RectTransform.GetChildWidth(true);
            var selfWidth = parent.rect.width - healthBar.offsetMax.x;
            var selfWidthAlt = parent.rect.width - healthBar.offsetMax.x * 2;
            var targetWidth = childWidth > selfWidthAlt ? selfWidth : selfWidthAlt;
            if (childWidth > targetWidth && ui.player.Ammo.ammoModel.usableSlots > 1)
            {
                LayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                var percent = ui.player.Ammo.GetSelectedAmmoIdx() / (ui.player.Ammo.ammoModel.usableSlots - 1f);
                //Main.Log("Data: ( " + childWidth + ", " + selfWidth + ", " + Mathf.Round(percent * 100) + "% )");
                TargetOffset = Mathf.Max(childWidth - targetWidth, 0) * percent;
            }
            if (childWidth <= targetWidth && LayoutGroup.childAlignment != TextAnchor.MiddleCenter)
            {
                TargetOffset = 0;
                LayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            }
            if (rect.rect.width != targetWidth)
            {
                var x = childWidth > selfWidthAlt ? (parent.rect.width - targetWidth / 2) / parent.rect.width : 0.5f;
                rect.sizeDelta = new Vector2(targetWidth, rect.sizeDelta.y);
                rect.anchorMin = new Vector2(x, 0);
                rect.anchorMax = new Vector2(x, 0);
            }
            if (TargetOffset != current)
            {
                current = current.Lerp(TargetOffset, Time.deltaTime * Mathf.Max(1, Mathf.Abs(current - TargetOffset)) * rect.rect.width / 100);
            }
            group.SetLayoutHorizontal();
            foreach (Transform obj in rect)
                obj.localPosition -= new Vector3(current, obj.localPosition.y, obj.localPosition.z);

        }
    }

    [HarmonyPatch(typeof(XlateKeyText), "OnKeysChanged")]
    class Patch_XlateKeyText_OnKeysChanged
    {
        public static Dictionary<XlateKeyText, int> custom = new Dictionary<XlateKeyText, int>();
        public static bool Prefix(XlateKeyText __instance)
        {
            if (!custom.ContainsKey(__instance))
                return true;
            if (__instance.text)
                __instance.text.text = Config.GetCustomBind(custom[__instance]);
            if (__instance.meshText)
                __instance.meshText.text = Config.GetCustomBind(custom[__instance]);
            return false;
        }
        public static void Update(int slot = -1)
        {
            foreach (var pair in custom)
                if (pair.Key && (slot == -1 || pair.Value == slot))
                {
                    var text = Config.GetCustomBind(pair.Value);
                    if (pair.Key.text)
                        pair.Key.text.text = text;
                    if (pair.Key.meshText)
                        pair.Key.meshText.text = text;
                }
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum), "UpdateSlotForInputs")]
    class Patch_WeaponVacuum_UpdateSlotForInputs
    {
        public static void Postfix(WeaponVacuum __instance)
        {
            foreach (var pair in Config.binds)
                if (Input.GetKeyDown(pair.Value) && __instance.player.Ammo.SetAmmoSlot(pair.Key))
                {
                    __instance.PlayTransientAudio(__instance.vacAmmoSelectCue);
                    __instance.vacAnimator.SetTrigger(__instance.animSwitchSlotsId);
                }
        }
    }

    /*class iCommand : ConsoleCommand
    {
        public override string Usage => "info";
        public override string ID => "info";
        public override string Description => "";
        public override bool Execute(string[] args)
        {
            Main.Log(Main.GetRectTree(Object.FindObjectOfType<AmmoSlotUI>().transform.parent.parent as RectTransform));// SRSingleton<HudUI>.Instance.transform.localScale
            return true;
        }
    }*/

    class ChangeSlotsCommand : ConsoleCommand
    {
        public override string Usage => "extraslots [count]";
        public override string ID => "extraslots";
        public override string Description => "gets or sets the number of extra slots";
        public override bool Execute(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Main.Log("Slot count is " + Config.slots + " (" + Main.defaultSlots + " default + " + Config.extraSlots + " extra)" );
                return true;
            }
            if (!Levels.isMainMenu())
            {
                Main.LogError("This command can only be used on the main menu");
                return true;
            }
            if (!int.TryParse(args[0], out int v))
            {
                Main.LogError(args[0] + " failed to parse as a number");
                return false;
            }
            if (v < 0)
            {
                Main.LogError("Value cannot be less than 0");
                return true;
            }
            Config.extraSlots = v;
            Main.SaveConfig();
            Main.LogSuccess("Slot count has been changed to " + Config.slots);
            return true;
        }
    }

    class SetSlotBindCommand : ConsoleCommand
    {
        public override string Usage => "bindslot <slot index> [key]";
        public override string ID => "bindslot";
        public override string Description => "gets or sets the key bound to the specified slot";
        public override bool Execute(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Main.Log("Not enough arguments");
                return false;
            }
            if (args[0] == "reset")
            {
                Config.ResetBinds();
                Main.SaveConfig();
                Main.LogSuccess("Binds have been reset to default");
                return true;
            }
            if (!int.TryParse(args[0], out int v))
            {
                Main.LogError(args[0] + " failed to parse as a number");
                return true;
            }
            if (v < 0)
            {
                Main.LogError("Value cannot be less than 0");
                return true;
            }
            if (args.Length >= 2)
            {
                if (args[1] == "reset")
                {
                    Config.ResetBinds(v);
                    Main.LogSuccess("Bind has been reset to default");
                }
                else if (args[1].TryParseKeyCode(out KeyCode k))
                {
                    Config.SetBind(v, k);
                    Main.LogSuccess("Bind has been updated");
                }
                else
                {
                    Main.LogError(args[1] + " failed to parse as a key");
                    return true;
                }
                Main.SaveConfig();
            }
            else
                Main.Log("Bind for slot " + v + " is " + Config.GetCustomBind(v));
            return true;
        }
        public override List<string> GetAutoComplete(int argIndex, string argText)
        {
            if (argIndex == 0)
            {
                var l = new List<string>() { "reset" };
                for (int i = Main.defaultSlots; i < Config.slots; i++)
                    l.Add(i.ToString());
                return l;
            }
            if (argIndex == 1)
                return Main.autoComplete;
            return base.GetAutoComplete(argIndex, argText);
        }
    }

    [ConfigFile(Main.fileName)]
    public static class Config
    {
        static Config()
        {
            ResetBinds();
        }
        public static int extraSlots = 3;
        public static int slots => Main.defaultSlots + extraSlots;
        public static Dictionary<int, KeyCode> binds = new Dictionary<int, KeyCode>();
        public static void SetBind(int slot, KeyCode key, bool Update = true)
        {
            if (key == KeyCode.None)
                RemoveBind(slot, false);
            else if (binds.ContainsKey(slot))
                binds[slot] = key;
            else
                binds.Add(slot, key);
            if (Update)
                Patch_XlateKeyText_OnKeysChanged.Update(slot);
        }
        public static KeyCode GetBind(int slot)
        {
            if (binds.ContainsKey(slot))
                return binds[slot];
            else
                return KeyCode.None;
        }
        public static void RemoveBind(int slot, bool Update = true)
        {
            if (binds.ContainsKey(slot))
                binds.Remove(slot);
            if (Update)
                Patch_XlateKeyText_OnKeysChanged.Update(slot);
        }
        public static string GetCustomBind(int slot)
        {
            if (binds.ContainsKey(slot))
                return binds[slot].GetName();
            return "?";
        }
        public static void ResetBinds(params int[] slots)
        {
            if (slots == null || slots.Length == 0)
            {
                binds.Clear();
                for (int i = Main.defaultSlots; i <= 9; i++)
                    binds.Add(i, KeyCode.Alpha1 + (i == 9 ? -1 : i) + i);
            }
            else
                foreach (var slot in slots)
                    SetBind(slot, (slot >= Main.defaultSlots && slot <= 9) ? KeyCode.Alpha1 + (slot == 9 ? -1 : slot) : KeyCode.None, false);
            Patch_XlateKeyText_OnKeysChanged.Update();
        }
    }

    public class keybindDictionaryParser : SRML.Config.Parsing.IStringParser
    {
        public object ParseObject(string value)
        {
            var d = new Dictionary<int, KeyCode>();
            if (value == "")
                return d;
            var pairs = value.Split(';');
            foreach (var pair in pairs)
            {
                if (pair == "")
                    continue;
                var data = pair.Split('=');
                try
                {
                    d.Add(int.Parse(data[0]), (KeyCode)int.Parse(data[1]));
                } catch { }
            }
            return d;
        }
        public string EncodeObject(object value)
        {
            if (!type.IsInstanceOfType(value))
                return "";
            var data = "";
            foreach (var pair in value as Dictionary<int, KeyCode>)
                data += (data == "" ? "" : ";") + pair.Key + "=" + (int)pair.Value;
            return data;
        }
        public string GetUsageString() => type.Name;
        public System.Type ParsedType => type;
        static System.Type type = typeof(Dictionary<int, KeyCode>);
    }
}