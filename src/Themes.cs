using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;

namespace DailyQuest
{
    public sealed class ThemeSpec
    {
        public string Id;
        public string Name;
        public string Description;
        public string BodyFont;
        public string DisplayFont;
        public string Decoration;
        public string Mark;
        public string Mascot;
        public Dictionary<string, Color> Colors;
    }

    public sealed class ThemeManager
    {
        public const string DefaultId = "quest";
        public static readonly string[] ColorKeys = { "Canvas", "Panel", "Field", "Edge", "Ink", "Muted", "Accent", "Success" };
        static readonly string[] DefaultColors = { "#12151C", "#1C202A", "#151921", "#363D49", "#F0EBDD", "#ACB1BE", "#D7B77A", "#91C7AA" };
        static readonly HashSet<string> RootKeys = new HashSet<string>(new[] { "format", "version", "id", "name", "description", "colors", "fonts", "decoration", "mark", "mascot" }, StringComparer.Ordinal);
        static readonly HashSet<string> FontKeys = new HashSet<string>(new[] { "body", "display" }, StringComparer.Ordinal);
        public static readonly ThemeSpec Default = BuildDefault();
        public static ThemeSpec Current = Default;
        static readonly Dictionary<string, SolidColorBrush> LiveBrushes = Default.Colors.ToDictionary(pair => pair.Key, pair => new SolidColorBrush(pair.Value), StringComparer.Ordinal);
        public readonly string Folder;

        public static SolidColorBrush Brush(string key) { return LiveBrushes[key]; }

        static ThemeSpec BuildDefault()
        {
            var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
            for (int i = 0; i < ColorKeys.Length; i++) colors.Add(ColorKeys[i], (Color)ColorConverter.ConvertFromString(DefaultColors[i]));
            return new ThemeSpec { Id = DefaultId, Name = "Default", Description = "The original dark quest log.", BodyFont = "Segoe UI", DisplayFont = "Georgia", Decoration = "none", Mark = "◇", Mascot = "none", Colors = colors };
        }
        public ThemeManager(string dataFolder)
        {
            Folder = Path.Combine(dataFolder, "themes");
            Directory.CreateDirectory(Folder);
        }
        static Dictionary<string, object> Object(object value, string where)
        {
            var obj = value as Dictionary<string, object>;
            if (obj == null) throw new InvalidDataException(where + " must be an object.");
            return obj;
        }
        static void Keys(Dictionary<string, object> obj, IEnumerable<string> allowed, string where)
        {
            foreach (var key in obj.Keys) if (!allowed.Contains(key)) throw new InvalidDataException(where + " has an unknown field: " + key + ".");
        }
        static string Text(Dictionary<string, object> obj, string key, int max)
        {
            object raw;
            if (!obj.TryGetValue(key, out raw) || !(raw is string)) throw new InvalidDataException("Theme needs text field \"" + key + "\".");
            var value = ((string)raw).Trim();
            if (value.Length == 0 || value.Length > max) throw new InvalidDataException("Theme field \"" + key + "\" must be 1–" + max + " characters.");
            return value;
        }
        static Color ColorField(Dictionary<string, object> obj, string key)
        {
            var text = Text(obj, key, 7);
            if (!Regex.IsMatch(text, "^#[0-9A-Fa-f]{6}$")) throw new InvalidDataException("Theme color " + key + " must use #RRGGBB.");
            return (Color)ColorConverter.ConvertFromString(text);
        }
        static double Luminance(Color color)
        {
            double[] channels = { color.R / 255.0, color.G / 255.0, color.B / 255.0 };
            for (int i = 0; i < channels.Length; i++) channels[i] = channels[i] <= 0.04045 ? channels[i] / 12.92 : Math.Pow((channels[i] + 0.055) / 1.055, 2.4);
            return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
        }
        static double Contrast(Color a, Color b)
        {
            double x = Luminance(a), y = Luminance(b);
            return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
        }
        public static ThemeSpec Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > 65536) throw new InvalidDataException("Choose a nonempty theme file smaller than 64 KB.");
            object parsed;
            try
            {
                string json = new UTF8Encoding(false, true).GetString(bytes);
                if (json.Length > 0 && json[0] == '\uFEFF') json = json.Substring(1);
                parsed = new JavaScriptSerializer { MaxJsonLength = 65536, RecursionLimit = 8 }.DeserializeObject(json);
            }
            catch (Exception error) { throw new InvalidDataException("The theme is not valid UTF-8 JSON: " + error.Message, error); }
            var root = Object(parsed, "Theme"); Keys(root, RootKeys, "Theme");
            if (Text(root, "format", 40) != "wrenfeed-theme") throw new InvalidDataException("Use WrenFeed theme format version 1.");
            object version;
            if (!root.TryGetValue("version", out version) || !(version is int) || (int)version != 1) throw new InvalidDataException("Use WrenFeed theme format version 1.");
            var theme = new ThemeSpec { Id = Text(root, "id", 40), Name = Text(root, "name", 60), Description = Text(root, "description", 160), Decoration = Text(root, "decoration", 16), Mark = Text(root, "mark", 4) };
            theme.Mascot = root.ContainsKey("mascot") ? Text(root, "mascot", 10) : "none";
            if (!Regex.IsMatch(theme.Id, "^[a-z][a-z0-9-]*$") || theme.Id == DefaultId) throw new InvalidDataException("Theme id must use lowercase letters, numbers, and hyphens, and cannot be \"quest\".");
            if (!new[] { "none", "circuit", "filigree", "manga", "chain", "whiskers", "pixel" }.Contains(theme.Decoration)) throw new InvalidDataException("Unknown theme decoration.");
            if (!new[] { "none", "wren", "chain-wren", "sphynx", "pixel-wren", "manga-eye" }.Contains(theme.Mascot)) throw new InvalidDataException("Unknown theme mascot.");
            object rawFonts, rawColors;
            if (!root.TryGetValue("fonts", out rawFonts) || !root.TryGetValue("colors", out rawColors)) throw new InvalidDataException("Theme needs fonts and colors objects.");
            var fonts = Object(rawFonts, "fonts"); Keys(fonts, FontKeys, "fonts");
            theme.BodyFont = Text(fonts, "body", 60); theme.DisplayFont = Text(fonts, "display", 60);
            if (theme.BodyFont.IndexOfAny(new[] { '#', '/', '\\' }) >= 0 || theme.DisplayFont.IndexOfAny(new[] { '#', '/', '\\' }) >= 0) throw new InvalidDataException("Use installed Windows font family names, not font files or paths.");
            var colors = Object(rawColors, "colors"); Keys(colors, ColorKeys, "colors");
            theme.Colors = ColorKeys.ToDictionary(key => key, key => ColorField(colors, key), StringComparer.Ordinal);
            foreach (var background in new[] { "Canvas", "Panel", "Field" })
            {
                if (Contrast(theme.Colors["Ink"], theme.Colors[background]) < 4.5 || Contrast(theme.Colors["Muted"], theme.Colors[background]) < 4.5)
                    throw new InvalidDataException("Ink and Muted need readable contrast against Canvas, Panel, and Field (at least 4.5:1).");
            }
            if (Contrast(theme.Colors["Accent"], theme.Colors["Canvas"]) < 4.5 || Contrast(theme.Colors["Success"], theme.Colors["Canvas"]) < 4.5 || Contrast(theme.Colors["Canvas"], theme.Colors["Accent"]) < 4.5)
                throw new InvalidDataException("Accent and Success need readable contrast against Canvas (at least 4.5:1).");
            return theme;
        }
        public List<ThemeSpec> Installed()
        {
            var themes = new List<ThemeSpec>();
            foreach (var path in Directory.GetFiles(Folder, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                try { var theme = Parse(File.ReadAllBytes(path)); if (Path.GetFileName(path).Equals(theme.Id + ".json", StringComparison.OrdinalIgnoreCase) && !themes.Any(t => t.Id == theme.Id)) themes.Add(theme); }
                catch (Exception) { /* An invalid optional file must not stop the app opening. */ }
            }
            return themes;
        }
        public ThemeSpec Find(string id) { return id == DefaultId ? Default : Installed().FirstOrDefault(t => t.Id == id) ?? Default; }
        public ThemeSpec Install(string path)
        {
            var theme = Parse(File.ReadAllBytes(path));
            var destination = Path.Combine(Folder, theme.Id + ".json");
            if (!String.Equals(Path.GetFullPath(path), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) File.Copy(path, destination, true);
            return theme;
        }
        public static void Apply(ThemeSpec theme)
        {
            var resources = Application.Current.Resources;
            foreach (var key in ColorKeys)
            {
                LiveBrushes[key].Color = theme.Colors[key];
                resources[key] = new SolidColorBrush(theme.Colors[key]);
            }
            resources["BodyFont"] = new FontFamily(theme.BodyFont);
            resources["DisplayFont"] = new FontFamily(theme.DisplayFont);
            resources["ControlCornerRadius"] = new CornerRadius(theme.Decoration == "pixel" ? 0 : theme.Decoration == "whiskers" ? 9 : theme.Decoration == "manga" ? 2 : 5);
            Current = theme;
        }
    }
}
