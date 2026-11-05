// Раскладка by mysik — исправляет текст, набранный не в той раскладке: ghbdtn → привет.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Mysik;
using WF = System.Windows.Forms;
using SD = System.Drawing;

[assembly: AssemblyTitle("Раскладка")]
[assembly: AssemblyDescription("Раскладка by mysik — исправление текста, набранного не в той раскладке")]
[assembly: AssemblyCompany("mysik")]
[assembly: AssemblyProduct("Раскладка by mysik")]
[assembly: AssemblyCopyright("by mysik")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Raskladka
{
    static class Program
    {
        [STAThread]
        static void Main() { Boot.Run("Раскладка", "RaskladkaByMysik_SingleInstance", () => new MainWindow()); }
    }

    // ------------------------------------------------------------------ WinAPI

    static class Native
    {
        public const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14;
        public const int WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
        public const int WM_LBUTTONDOWN = 0x201, WM_RBUTTONDOWN = 0x204, WM_MBUTTONDOWN = 0x207;
        public const uint LLKHF_INJECTED = 0x10;
        public const uint INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 2, KEYEVENTF_UNICODE = 4;
        public const int WM_INPUTLANGCHANGEREQUEST = 0x50;
        public static readonly IntPtr Mark = new IntPtr(0x4D59534B); // «MYSK» — наши собственные нажатия

        public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT { public uint vkCode; public uint scanCode; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT { public uint type; public InputUnion u; }

        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint n, INPUT[] inputs, int size);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr pid);
        [DllImport("user32.dll")] public static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32.dll")] public static extern int GetKeyboardLayoutList(int n, [Out] IntPtr[] list);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern short GetKeyState(int vk);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);

        public static int CurrentLang()
        {
            var hwnd = GetForegroundWindow();
            var hkl = GetKeyboardLayout(GetWindowThreadProcessId(hwnd, IntPtr.Zero));
            return (int)((long)hkl & 0xFFFF) & 0x3FF; // основной язык: 0x19 — русский, 0x09 — английский
        }

        public static IntPtr FindLayout(int primaryLang)
        {
            var list = new IntPtr[32];
            int n = GetKeyboardLayoutList(list.Length, list);
            for (int i = 0; i < n; i++)
                if (((int)((long)list[i] & 0xFFFF) & 0x3FF) == primaryLang) return list[i];
            return IntPtr.Zero;
        }

        public static void SwitchLayout(int primaryLang)
        {
            var hkl = FindLayout(primaryLang);
            if (hkl != IntPtr.Zero) PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
        }

        static INPUT Key(ushort vk, bool up)
        {
            var i = new INPUT { type = INPUT_KEYBOARD };
            i.u.ki = new KEYBDINPUT { wVk = vk, dwFlags = up ? KEYEVENTF_KEYUP : 0, dwExtraInfo = Mark };
            return i;
        }

        static INPUT Uni(char c, bool up)
        {
            var i = new INPUT { type = INPUT_KEYBOARD };
            i.u.ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE | (up ? KEYEVENTF_KEYUP : 0), dwExtraInfo = Mark };
            return i;
        }

        static void Send(List<INPUT> list)
        {
            if (list.Count > 0) SendInput((uint)list.Count, list.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        public static void Backspace(int n)
        {
            var l = new List<INPUT>();
            for (int i = 0; i < n; i++) { l.Add(Key(0x08, false)); l.Add(Key(0x08, true)); }
            Send(l);
        }

        public static void Type(string s)
        {
            var l = new List<INPUT>();
            foreach (char c in s) { l.Add(Uni(c, false)); l.Add(Uni(c, true)); }
            Send(l);
        }

        public static void Combo(ushort mod, ushort key)
        {
            Send(new List<INPUT> { Key(mod, false), Key(key, false), Key(key, true), Key(mod, true) });
        }

        public static bool Down(int vk) { return (GetAsyncKeyState(vk) & 0x8000) != 0; }
    }

    // ------------------------------------------------------------------ таблицы раскладок

    static class Layouts
    {
        // клавиша → (англ, англ+Shift, рус, рус+Shift)
        public static readonly Dictionary<int, string[]> Keys = new Dictionary<int, string[]>();
        static readonly Dictionary<char, char> EnToRu = new Dictionary<char, char>();
        static readonly Dictionary<char, char> RuToEn = new Dictionary<char, char>();

        static void K(int vk, string en, string enS, string ru, string ruS) { Keys[vk] = new[] { en, enS, ru, ruS }; }

        static Layouts()
        {
            const string enRow = "qwertyuiopasdfghjklzxcvbnm", ruRow = "йцукенгшщзфывапролдячсмить";
            for (int i = 0; i < enRow.Length; i++)
            {
                char e = enRow[i], r = ruRow[i];
                K(char.ToUpperInvariant(e), e.ToString(), char.ToUpperInvariant(e).ToString(), r.ToString(), char.ToUpper(r).ToString());
            }
            K(0xDB, "[", "{", "х", "Х");  // VK_OEM_4
            K(0xDD, "]", "}", "ъ", "Ъ");  // VK_OEM_6
            K(0xBA, ";", ":", "ж", "Ж");  // VK_OEM_1
            K(0xDE, "'", "\"", "э", "Э"); // VK_OEM_7
            K(0xBC, ",", "<", "б", "Б");  // VK_OEM_COMMA
            K(0xBE, ".", ">", "ю", "Ю");  // VK_OEM_PERIOD
            K(0xBF, "/", "?", ".", ",");  // VK_OEM_2
            K(0xC0, "`", "~", "ё", "Ё");  // VK_OEM_3
            K(0xDC, "\\", "|", "\\", "/"); // VK_OEM_5
            K(0xBD, "-", "_", "-", "_");
            K(0xBB, "=", "+", "=", "+");
            string enShift = "!@#$%^&*()", ruShift = "!\"№;%:?*()";
            for (int d = 0; d <= 9; d++)
            {
                int idx = d == 0 ? 9 : d - 1;
                K(0x30 + d, d.ToString(), enShift[idx].ToString(), d.ToString(), ruShift[idx].ToString());
            }
            K(0x20, " ", " ", " ", " ");

            // посимвольные таблицы для выделенного текста (только буквы и знаки, которые отличаются)
            foreach (var kv in Keys)
            {
                var v = kv.Value;
                if (kv.Key >= 0x30 && kv.Key <= 0x39) continue; // цифры одинаковые
                Pair(v[0], v[2]);
                Pair(v[1], v[3]);
            }
            // символы с цифровых клавиш, которые отличаются: @ # $ ^ & ↔ " № ; : ?
            Pair("@", "\""); Pair("#", "№"); Pair("$", ";"); Pair("^", ":"); Pair("&", "?");
        }

        static void Pair(string en, string ru)
        {
            if (en == ru) return;
            char e = en[0], r = ru[0];
            if (!EnToRu.ContainsKey(e)) EnToRu[e] = r;
            if (!RuToEn.ContainsKey(r)) RuToEn[r] = e;
        }

        static bool IsRu(char c) { c = char.ToLowerInvariant(c); return (c >= 'а' && c <= 'я') || c == 'ё'; }
        static bool IsEn(char c) { c = char.ToLowerInvariant(c); return c >= 'a' && c <= 'z'; }

        // Определяет, на каком языке набран текст, и переводит в другую раскладку.
        public static string Convert(string s, out bool toRussian)
        {
            int ru = s.Count(c => IsRu(c)), en = s.Count(c => IsEn(c));
            toRussian = en >= ru;
            var map = toRussian ? EnToRu : RuToEn;
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                char r;
                sb.Append(map.TryGetValue(c, out r) ? r : c);
            }
            return sb.ToString();
        }

        // Транслит: русский ⇄ латиница.
        static readonly string[][] Translit =
        {
            new[] { "щ", "sch" }, new[] { "ё", "yo" }, new[] { "ж", "zh" }, new[] { "х", "kh" }, new[] { "ц", "ts" },
            new[] { "ч", "ch" }, new[] { "ш", "sh" }, new[] { "ю", "yu" }, new[] { "я", "ya" }, new[] { "а", "a" },
            new[] { "б", "b" }, new[] { "в", "v" }, new[] { "г", "g" }, new[] { "д", "d" }, new[] { "е", "e" },
            new[] { "з", "z" }, new[] { "и", "i" }, new[] { "й", "y" }, new[] { "к", "k" }, new[] { "л", "l" },
            new[] { "м", "m" }, new[] { "н", "n" }, new[] { "о", "o" }, new[] { "п", "p" }, new[] { "р", "r" },
            new[] { "с", "s" }, new[] { "т", "t" }, new[] { "у", "u" }, new[] { "ф", "f" }, new[] { "ы", "y" },
            new[] { "ь", "'" }, new[] { "ъ", "" }, new[] { "э", "e" }
        };

        public static string Transliterate(string s)
        {
            int ru = s.Count(c => IsRu(c)), en = s.Count(c => IsEn(c));
            var sb = new StringBuilder();
            if (ru >= en && ru > 0)
            {
                foreach (char c in s)
                {
                    char low = char.ToLowerInvariant(c);
                    var pair = Translit.FirstOrDefault(p => p[0][0] == low);
                    if (pair == null) { sb.Append(c); continue; }
                    string t = pair[1];
                    if (char.IsUpper(c) && t.Length > 0) t = char.ToUpperInvariant(t[0]) + t.Substring(1);
                    sb.Append(t);
                }
                return sb.ToString();
            }
            const string vowels = "аеёиоуыэюя";
            for (int i = 0; i < s.Length;)
            {
                bool matched = false;
                foreach (var len in new[] { 3, 2, 1 })
                {
                    if (i + len > s.Length) continue;
                    string chunk = s.Substring(i, len), low = chunk.ToLowerInvariant();
                    var pair = Translit.FirstOrDefault(p => p[1] == low && p[1].Length > 0);
                    if (pair == null) continue;
                    string r = pair[0];
                    if (low == "y")  // «й» после гласной, в начале и в конце слова, иначе «ы»
                    {
                        bool wordStart = sb.Length == 0 || !char.IsLetter(sb[sb.Length - 1]);
                        bool afterVowel = !wordStart && vowels.IndexOf(char.ToLowerInvariant(sb[sb.Length - 1])) >= 0;
                        bool wordEnd = i + 1 >= s.Length || !char.IsLetter(s[i + 1]);
                        r = wordStart || afterVowel || wordEnd ? "й" : "ы";
                    }
                    if (low == "e" && (sb.Length == 0 || !char.IsLetter(sb[sb.Length - 1]))) r = "э";
                    if (char.IsUpper(chunk[0])) r = r.ToUpper();
                    sb.Append(r);
                    i += len;
                    matched = true;
                    break;
                }
                if (!matched) { sb.Append(s[i]); i++; }
            }
            return sb.ToString();
        }

        // Регистр: «пРИВЕТ» → «Привет», «ПРИВЕТ» → «привет», «привет» → «ПРИВЕТ».
        public static string ToggleCase(string s)
        {
            var letters = s.Where(c => char.IsLetter(c)).ToList();
            if (letters.Count == 0) return s;
            int upper = letters.Count(c => char.IsUpper(c));
            if (upper == letters.Count) return s.ToLower();
            if (upper == 0) return s.ToUpper();
            // похоже на включённый Caps Lock — инвертируем
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(char.IsUpper(c) ? char.ToLower(c) : char.IsLower(c) ? char.ToUpper(c) : c);
            return sb.ToString();
        }
    }

    // ------------------------------------------------------------------ перехват клавиатуры

    class Stroke { public int Vk; public bool Shift; public bool Caps; }

    class Engine : IDisposable
    {
        public int Preset;              // 0: Pause / Shift+Pause, 1: Shift-Shift / Ctrl-Ctrl
        public bool Enabled = true;
        public bool SwitchLayout = true;
        public event Action<string> Converted;  // текст результата, для статистики и подсказок
        public event Action<string> Problem;

        readonly List<Stroke> buffer = new List<Stroke>();
        bool afterSpace;
        IntPtr kbHook, msHook, lastWindow;
        readonly Native.HookProc kbProc, msProc;   // держим ссылки, чтобы сборщик мусора не убрал
        readonly System.Windows.Threading.Dispatcher ui;

        int tapVk;                 // для двойного нажатия Shift/Ctrl
        DateTime tapTime;
        bool tapClean;             // между нажатием и отпусканием не было других клавиш
        bool busy;

        public Engine(System.Windows.Threading.Dispatcher dispatcher)
        {
            ui = dispatcher;
            kbProc = OnKey;
            msProc = OnMouse;
            IntPtr mod = Native.GetModuleHandle(null);
            kbHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, kbProc, mod, 0);
            msHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, msProc, mod, 0);
        }

        public bool Hooked { get { return kbHook != IntPtr.Zero; } }

        public void Dispose()
        {
            if (kbHook != IntPtr.Zero) Native.UnhookWindowsHookEx(kbHook);
            if (msHook != IntPtr.Zero) Native.UnhookWindowsHookEx(msHook);
            kbHook = msHook = IntPtr.Zero;
        }

        IntPtr OnMouse(int code, IntPtr w, IntPtr l)
        {
            int m = w.ToInt32();
            if (code >= 0 && (m == Native.WM_LBUTTONDOWN || m == Native.WM_RBUTTONDOWN || m == Native.WM_MBUTTONDOWN))
                Reset();
            return Native.CallNextHookEx(msHook, code, w, l);
        }

        void Reset() { buffer.Clear(); afterSpace = false; }

        static bool IsShift(int vk) { return vk == 0x10 || vk == 0xA0 || vk == 0xA1; }
        static bool IsCtrl(int vk) { return vk == 0x11 || vk == 0xA2 || vk == 0xA3; }
        static bool IsAlt(int vk) { return vk == 0x12 || vk == 0xA4 || vk == 0xA5; }

        IntPtr OnKey(int code, IntPtr w, IntPtr l)
        {
            if (code < 0) return Native.CallNextHookEx(kbHook, code, w, l);
            var k = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(l, typeof(Native.KBDLLHOOKSTRUCT));
            if ((k.flags & Native.LLKHF_INJECTED) != 0 || k.dwExtraInfo == Native.Mark)
                return Native.CallNextHookEx(kbHook, code, w, l);

            int msg = w.ToInt32(), vk = (int)k.vkCode;
            bool down = msg == Native.WM_KEYDOWN || msg == Native.WM_SYSKEYDOWN;
            bool up = msg == Native.WM_KEYUP || msg == Native.WM_SYSKEYUP;
            bool ctrl = Native.Down(0x11), alt = Native.Down(0x12), shift = Native.Down(0x10);

            // всегда работающие сочетания: Ctrl+Alt+T и Ctrl+Alt+R
            if (down && ctrl && alt && (vk == 'T' || vk == 'R'))
            {
                if (Enabled) { Later(vk == 'T' ? 2 : 3); return (IntPtr)1; }
            }

            // --- двойное нажатие Shift/Ctrl (пресет 1)
            if (Preset == 1)
            {
                bool mod = IsShift(vk) || IsCtrl(vk);
                if (down && mod)
                {
                    int kind = IsShift(vk) ? 1 : 2;
                    if (tapVk != kind || (DateTime.Now - tapTime).TotalMilliseconds > 400) { tapVk = kind; tapTime = DateTime.MinValue; }
                    tapClean = true;
                }
                else if (up && mod && tapClean)
                {
                    int kind = IsShift(vk) ? 1 : 2;
                    if (tapVk == kind && tapTime != DateTime.MinValue && (DateTime.Now - tapTime).TotalMilliseconds < 400)
                    {
                        tapTime = DateTime.MinValue;
                        tapVk = 0;
                        if (Enabled) Later(kind == 1 ? 0 : 1);
                    }
                    else { tapVk = kind; tapTime = DateTime.Now; }
                }
                else if (down) { tapClean = false; tapVk = 0; }
            }

            // --- Pause / Shift+Pause (пресет 0)
            if (vk == 0x13 && Preset == 0 && Enabled)
            {
                if (down) Later(shift ? 1 : 0);
                return (IntPtr)1; // сам Pause программам не нужен
            }

            if (!down) return Native.CallNextHookEx(kbHook, code, w, l);

            // --- запоминаем набранное слово
            var fg = Native.GetForegroundWindow();
            if (fg != lastWindow) { Reset(); lastWindow = fg; }

            if (IsShift(vk) || vk == 0x14) return Native.CallNextHookEx(kbHook, code, w, l); // Shift, Caps Lock
            if ((ctrl || alt) && !(ctrl && alt)) { Reset(); return Native.CallNextHookEx(kbHook, code, w, l); } // сочетания клавиш

            if (vk == 0x08) { if (buffer.Count > 0) buffer.RemoveAt(buffer.Count - 1); }
            else if (vk == 0x20)
            {
                if (buffer.Count > 0) { buffer.Add(new Stroke { Vk = vk }); afterSpace = true; }
            }
            else if (Layouts.Keys.ContainsKey(vk))
            {
                if (afterSpace) { Reset(); }
                bool caps = (Native.GetKeyState(0x14) & 1) != 0;
                buffer.Add(new Stroke { Vk = vk, Shift = shift, Caps = caps });
                if (buffer.Count > 100) buffer.RemoveAt(0);
            }
            else Reset(); // Enter, стрелки, Tab, Home и т.п.

            return Native.CallNextHookEx(kbHook, code, w, l);
        }

        // Действие выполняем вне перехватчика: он должен отвечать мгновенно.
        void Later(int action) { ui.BeginInvoke(new Action(async () => await Run(action))); }

        async Task Run(int action)
        {
            if (busy) return;
            busy = true;
            try
            {
                // ждём, пока пользователь отпустит модификаторы, иначе Ctrl+C превратится в Ctrl+Shift+C
                for (int i = 0; i < 40 && (Native.Down(0x10) || Native.Down(0x11) || Native.Down(0x12)); i++) await Task.Delay(25);
                if (action == 0) ConvertLastWord();
                else await ConvertSelection(action);
            }
            catch (Exception ex) { if (Problem != null) Problem(ex.Message); }
            finally { busy = false; }
        }

        void ConvertLastWord()
        {
            if (buffer.Count == 0) { if (Problem != null) Problem("Нечего исправлять: напечатай слово и сразу нажми клавишу"); return; }
            bool nowRu = Native.CurrentLang() == 0x19;
            int col = nowRu ? 0 : 2;  // печатаем в другой раскладке
            var sb = new StringBuilder();
            foreach (var s in buffer)
            {
                var v = Layouts.Keys[s.Vk];
                bool letter = char.IsLetter(v[col][0]);           // Caps Lock влияет только на буквы
                bool upper = letter ? s.Shift ^ s.Caps : s.Shift;
                sb.Append(v[col + (upper ? 1 : 0)]);
            }
            string text = sb.ToString();
            Native.Backspace(buffer.Count);
            Native.Type(text);
            if (SwitchLayout) Native.SwitchLayout(nowRu ? 0x09 : 0x19);
            if (Converted != null) Converted(text.Trim());
        }

        async Task ConvertSelection(int action)
        {
            string saved = null;
            try { if (Clipboard.ContainsText()) saved = Clipboard.GetText(); } catch { }
            try { Clipboard.Clear(); } catch { }

            Native.Combo(0x11, (ushort)'C');
            string text = null;
            for (int i = 0; i < 10 && string.IsNullOrEmpty(text); i++)
            {
                await Task.Delay(40);
                try { if (Clipboard.ContainsText()) text = Clipboard.GetText(); } catch { }
            }
            if (string.IsNullOrEmpty(text))
            {
                Restore(saved);
                if (Problem != null) Problem("Сначала выдели текст");
                return;
            }

            string result;
            bool toRu = false;
            if (action == 1) result = Layouts.Convert(text, out toRu);
            else if (action == 2) result = Layouts.Transliterate(text);
            else result = Layouts.ToggleCase(text);

            if (result != text)
            {
                try { Clipboard.SetText(result); } catch { }
                await Task.Delay(30);
                Native.Combo(0x11, (ushort)'V');
                await Task.Delay(250);
                if (action == 1 && SwitchLayout) Native.SwitchLayout(toRu ? 0x19 : 0x09);
                if (Converted != null) Converted(result.Length > 40 ? result.Substring(0, 37) + "…" : result);
            }
            Restore(saved);
        }

        static void Restore(string saved)
        {
            try { if (saved != null) Clipboard.SetText(saved); else Clipboard.Clear(); } catch { }
        }
    }

    // ------------------------------------------------------------------ окно

    class MainWindow : ShellWindow
    {
        static readonly string IniPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Raskladka by mysik", "settings.ini");
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        Engine engine;
        WF.NotifyIcon tray;
        WF.ToolStripMenuItem trayEnabled;
        TextBlock statusTitle, statusSub, counterText, testHint;
        CheckBox swEnabled, swSwitchLayout, swAutostart;
        StackPanel presetList;
        TextBox testBox;
        readonly List<RadioButton> presetCards = new List<RadioButton>();
        int preset, counter;
        bool startHidden, reallyExit, hintShown, suppress;

        static readonly string[][] Presets =
        {
            new[] { "Pause", "Shift + Pause", "Классика, как в Punto Switcher" },
            new[] { "Shift дважды", "Ctrl дважды", "Для ноутбуков без клавиши Pause" }
        };

        public MainWindow()
        {
            LoadUi("Раскладка by mysik", 440, 760);
            startHidden = Environment.GetCommandLineArgs().Any(a => a == "--tray");
            if (startHidden) { Opacity = 0; ShowInTaskbar = false; }

            statusTitle = F<TextBlock>("StatusTitle");
            statusSub = F<TextBlock>("StatusSub");
            counterText = F<TextBlock>("CounterText");
            testHint = F<TextBlock>("TestHint");
            swEnabled = F<CheckBox>("SwEnabled");
            swSwitchLayout = F<CheckBox>("SwSwitchLayout");
            swAutostart = F<CheckBox>("SwAutostart");
            presetList = F<StackPanel>("PresetList");
            testBox = F<TextBox>("TestBox");

            LoadSettings();
            BuildPresets();
            BuildTray();

            swEnabled.Checked += (s, e) => SetEnabled(true);
            swEnabled.Unchecked += (s, e) => SetEnabled(false);
            swSwitchLayout.Checked += (s, e) => { if (!suppress) { engine.SwitchLayout = true; SaveSettings(); } };
            swSwitchLayout.Unchecked += (s, e) => { if (!suppress) { engine.SwitchLayout = false; SaveSettings(); } };
            swAutostart.Checked += (s, e) => SetAutostart(true);
            swAutostart.Unchecked += (s, e) => SetAutostart(false);
        }

        public override void Init()
        {
            engine = new Engine(Dispatcher) { Preset = preset };
            engine.Converted += t => { counter++; SaveSettings(); UpdateTexts(); };
            engine.Problem += t => { if (IsVisible && IsActive) Toast(t); };

            suppress = true;
            swEnabled.IsChecked = true;
            swSwitchLayout.IsChecked = true;
            swAutostart.IsChecked = AutostartEnabled();
            suppress = false;
            LoadSettings(); // повторно — уже с движком
            engine.SwitchLayout = swSwitchLayout.IsChecked == true;

            if (!engine.Hooked)
                Ask("Не удалось включить перехват клавиатуры", "Попробуй перезапустить программу. Иногда мешает антивирус.", "Понятно", null);

            UpdateTexts();
            if (startHidden) { Hide(); Opacity = 1; ShowInTaskbar = true; }
        }

        void BuildPresets()
        {
            var style = S("ChoiceCard");
            for (int i = 0; i < Presets.Length; i++)
            {
                int idx = i;
                var p = Presets[i];
                var sp = new StackPanel();
                var g = Columns(Star, Auto);
                g.Children.Add(Txt(p[2], 13.5, CText, true));
                sp.Children.Add(g);
                var keys = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
                keys.Children.Add(KeyChip(p[0]));
                keys.Children.Add(Txt("слово  ", 12, CSub, false));
                keys.Children.Add(KeyChip(p[1]));
                keys.Children.Add(Txt("выделенное", 12, CSub, false));
                foreach (UIElement c in keys.Children) if (c is TextBlock) ((TextBlock)c).VerticalAlignment = VerticalAlignment.Center;
                sp.Children.Add(keys);
                var rb = new RadioButton { Style = style, Content = sp, GroupName = "preset", IsChecked = i == preset };
                rb.Checked += (s, e) =>
                {
                    preset = idx;
                    if (engine != null) engine.Preset = idx;
                    SaveSettings();
                    UpdateTexts();
                };
                presetCards.Add(rb);
                presetList.Children.Add(rb);
            }
        }

        Border KeyChip(string text)
        {
            return new Border
            {
                Background = Br(CCard2), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 8, 4),
                Child = new TextBlock { Text = text, FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 12 }
            };
        }

        void UpdateTexts()
        {
            bool on = engine == null || engine.Enabled;
            statusTitle.Text = on ? "Работает" : "Выключено";
            statusSub.Text = on
                ? "Напечатал не в той раскладке — нажми " + Presets[preset][0]
                : "Включи, чтобы исправлять раскладку";
            counterText.Text = counter > 0 ? "Уже исправлено: " + Plural(counter, "раз", "раза", "раз") : "Работает во всех программах: браузере, Telegram, Word, играх.";
            testHint.Text = "Напечатай здесь ghbdtn vbh и нажми " + Presets[preset][0] + ". Выдели текст и нажми " + Presets[preset][1] + ".";
            if (trayEnabled != null) trayEnabled.Checked = on;
            if (tray != null) tray.Text = on ? "Раскладка by mysik — работает" : "Раскладка by mysik — выключена";
        }

        void SetEnabled(bool on)
        {
            if (suppress || engine == null) return;
            engine.Enabled = on;
            UpdateTexts();
        }

        // -------------------------------------------------- трей

        void BuildTray()
        {
            var menu = new WF.ContextMenuStrip();
            var open = new WF.ToolStripMenuItem("Открыть", null, (s, e) => ShowMain());
            open.Font = new SD.Font(open.Font, SD.FontStyle.Bold);
            trayEnabled = new WF.ToolStripMenuItem("Включено", null, (s, e) => swEnabled.IsChecked = swEnabled.IsChecked != true);
            menu.Items.Add(open);
            menu.Items.Add(trayEnabled);
            menu.Items.Add(new WF.ToolStripSeparator());
            menu.Items.Add(new WF.ToolStripMenuItem("Выход", null, (s, e) => { reallyExit = true; Close(); }));

            tray = new WF.NotifyIcon { ContextMenuStrip = menu, Visible = true, Text = "Раскладка by mysik" };
            try { tray.Icon = SD.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName); } catch { tray.Icon = SD.SystemIcons.Application; }
            tray.MouseClick += (s, e) => { if (e.Button == WF.MouseButtons.Left) ShowMain(); };
        }

        void ShowMain()
        {
            Show();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!reallyExit)
            {
                e.Cancel = true;
                Hide();
                if (!hintShown)
                {
                    hintShown = true;
                    try { tray.ShowBalloonTip(3000, "Раскладка работает в трее", "Выход — правый клик по значку у часов.", WF.ToolTipIcon.Info); } catch { }
                }
                return;
            }
            if (engine != null) engine.Dispose();
            tray.Visible = false;
            tray.Dispose();
            base.OnClosing(e);
        }

        // -------------------------------------------------- настройки

        void LoadSettings()
        {
            try
            {
                if (!File.Exists(IniPath)) return;
                foreach (var line in File.ReadAllLines(IniPath))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i), v = line.Substring(i + 1);
                    if (k == "preset") int.TryParse(v, out preset);
                    else if (k == "counter") int.TryParse(v, out counter);
                    else if (k == "switch" && engine != null) { suppress = true; swSwitchLayout.IsChecked = v == "1"; suppress = false; }
                }
                if (preset < 0 || preset >= Presets.Length) preset = 0;
            }
            catch { }
        }

        void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(IniPath));
                File.WriteAllLines(IniPath, new[]
                {
                    "preset=" + preset, "counter=" + counter, "switch=" + (swSwitchLayout.IsChecked == true ? "1" : "0")
                });
            }
            catch { }
        }

        static bool AutostartEnabled()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey))
                return k != null && k.GetValue("Raskladka by mysik") != null;
        }

        void SetAutostart(bool on)
        {
            if (suppress) return;
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (on) k.SetValue("Raskladka by mysik", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" --tray");
                    else k.DeleteValue("Raskladka by mysik", false);
                }
                Toast(on ? "Будет запускаться вместе с Windows" : "Автозапуск выключен");
            }
            catch (Exception ex) { Toast("Не получилось: " + ex.Message); }
        }
    }
}
