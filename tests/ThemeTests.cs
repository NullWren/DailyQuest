using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using DailyQuest;

class ThemeTests
{
    static int count;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); count++; Console.WriteLine("PASS " + message); }
    static IEnumerable<DependencyObject> Walk(DependencyObject root)
    {
        yield return root;
        foreach (object child in LogicalTreeHelper.GetChildren(root)) if (child is DependencyObject) foreach (var descendant in Walk((DependencyObject)child)) yield return descendant;
    }
    static void Reject(string json, string message)
    {
        try { ThemeManager.Parse(Encoding.UTF8.GetBytes(json)); throw new Exception("Accepted invalid theme: " + message); }
        catch (InvalidDataException) { Assert(true, message); }
    }
    static void Capture(QuestWindow window, string path)
    {
        window.UpdateLayout();
        window.Opacity = 1;
        int width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth)), height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(path)) encoder.Save(file);
        window.Opacity = 0;
    }
    [STAThread] static void Main(string[] args)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DailyQuest.Theme.xaml")) app.Resources = (ResourceDictionary)XamlReader.Load(stream);
        var neon = ThemeManager.Parse(File.ReadAllBytes(args[0]));
        var ledger = ThemeManager.Parse(File.ReadAllBytes(args[1]));
        var template = ThemeManager.Parse(File.ReadAllBytes(args[2]));
        var additions = args.Skip(4).Select(path => ThemeManager.Parse(File.ReadAllBytes(path))).ToList();
        Assert(neon.Id == "neon-aviary" && neon.Decoration == "circuit" && neon.DisplayFont == "Consolas", "Neon theme parses circuit look and display font");
        Assert(ledger.Id == "adventurers-ledger" && ledger.Decoration == "filigree" && ledger.Mascot == "wren", "Ledger theme enables ornate cards and small wren");
        Assert(template.Id == "my-theme" && template.Decoration == "none", "Repository theme template is valid");
        Assert(additions.Select(t => t.Id).SequenceEqual(new[] { "anime-noir", "g-units", "sphynx-nook", "pixel-perch", "anime-graffiti", "the-flock" }), "Six additional theme presets parse and use distinct IDs");
        Assert(additions[0].Mascot == "manga-eye" && additions[1].Mascot == "chain-wren" && additions[2].Mascot == "sphynx" && additions[3].Mascot == "pixel-wren" && additions[4].Mascot == "graffiti-wing" && additions[5].Mascot == "flock", "New themes select original built-in emblems");
        var original = ThemeManager.Brush("Canvas").Color;
        ThemeManager.Apply(neon);
        Assert(ThemeManager.Brush("Canvas").Color == neon.Colors["Canvas"] && ((SolidColorBrush)app.Resources["Canvas"]).Color == neon.Colors["Canvas"], "Theme changes live UI and WPF style colors");
        ThemeManager.Apply(ledger);
        Assert(ThemeManager.Brush("Canvas").Color == ledger.Colors["Canvas"] && ((FontFamily)app.Resources["BodyFont"]).Source == "Georgia", "Switching themes changes colors and body font");
        ThemeManager.Apply(ThemeManager.Default);
        Assert(ThemeManager.Brush("Canvas").Color == original, "Default restores original palette");
        string root = Path.GetFullPath(args[3]);
        Directory.CreateDirectory(root);
        using (var storage = new Storage(root))
        {
            var manager = new ThemeManager(storage.Folder);
            manager.Install(args[0]); manager.Install(args[1]); foreach (var path in args.Skip(4)) manager.Install(path);
            Assert(manager.Installed().Count == 8 && manager.Find("neon-aviary").Name == "Neon Aviary", "All eight optional theme files install locally and can be found by ID");
            Assert(manager.Find("not-installed").Id == ThemeManager.DefaultId, "Missing optional theme falls back to Default");
            var journal = new Journal(); journal.SaveGame(new Game { Name = "Theme test quest" }, DateTime.Now, DateTime.UtcNow);
            var window = new QuestWindow(storage, journal) { Opacity = 0, ShowActivated = false }; window.Show();
            Assert(window.WindowStyle == WindowStyle.None && window.AllowsTransparency && Walk(window).OfType<Thumb>().Count() == 8 && Walk(window).OfType<Button>().Any(b => AutomationProperties.GetName(b) == "Close WrenFeed"), "Custom quest-log title bar keeps close and edge-resize controls");
            double previousWidth = window.Width;
            var rightGrip = Walk(window).OfType<Thumb>().Single(t => Grid.GetRow(t) == 1 && Grid.GetColumn(t) == 2);
            rightGrip.RaiseEvent(new DragDeltaEventArgs(20, 0) { RoutedEvent = Thumb.DragDeltaEvent });
            Assert(Math.Abs(window.Width - previousWidth - 20) < 0.001, "Right edge grip resizes the custom window");
            Exception failure = null;
            app.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                try
                {
                    var dialog = app.Windows.Cast<Window>().Single(w => w.Title == "WrenFeed · Settings");
                    Assert(dialog.ResizeMode == ResizeMode.NoResize && dialog.WindowState == WindowState.Normal, "Modal settings cannot hide behind the quest log via minimize");
                    dialog.WindowState = WindowState.Minimized;
                    Assert(dialog.WindowState == WindowState.Normal, "Unexpected minimize requests restore the settings window");
                    var opacity = Walk(dialog).OfType<Slider>().Single(s => AutomationProperties.GetName(s) == "Window opacity");
                    opacity.Value = 80;
                    Assert(Math.Abs(window.Opacity - 0.8) < 0.001 && Math.Abs(journal.Settings.WindowOpacity - 0.8) < 0.001, "Whole-window visibility slider applies and saves immediately");
                    var backgroundMode = Walk(dialog).OfType<RadioButton>().Single(r => AutomationProperties.GetName(r) == "Fade backgrounds only");
                    Assert(Walk(dialog).OfType<RadioButton>().Where(r => r.GroupName == "OpacityMode").All(r => Object.ReferenceEquals(r.Foreground, UI.Brush("Ink"))), "Visibility choices use readable theme text colors");
                    backgroundMode.IsChecked = true;
                    Assert(journal.Settings.BackgroundOnlyOpacity && window.Opacity == 1, "Background-only mode keeps quest text at full opacity");
                    Assert(Walk(window).OfType<Border>().Any(b => b.Background is SolidColorBrush && ((SolidColorBrush)b.Background).Color == ThemeManager.Current.Colors["Panel"] && Math.Abs(((SolidColorBrush)b.Background).Opacity - Math.Pow(0.8, 8)) < 0.001), "Quest-card tint no longer stacks heavily over the translucent window");
                    opacity.Value = 20;
                    Assert(Math.Abs(journal.Settings.WindowOpacity - 0.2) < 0.001 && Walk(window).OfType<Border>().Any(b => b.Background is SolidColorBrush && ((SolidColorBrush)b.Background).Color == ThemeManager.Current.Colors["Canvas"] && Math.Abs(((SolidColorBrush)b.Background).Opacity - 0.2) < 0.001), "Background-only visibility reaches 20 percent");
                    opacity.Value = 100;
                    var list = Walk(dialog).OfType<ComboBox>().Single(c => AutomationProperties.GetName(c) == "Theme");
                    list.SelectedItem = list.Items.Cast<ComboBoxItem>().Single(i => ((ThemeSpec)i.Tag).Id == "adventurers-ledger");
                    Assert(journal.Settings.Theme == "adventurers-ledger" && ThemeManager.Current.Id == "adventurers-ledger", "Settings saves and applies selected theme immediately");
                    Assert(Walk(window).OfType<Canvas>().Any(c => (string)c.ToolTip == "A little wren keeping watch" && c.Visibility == Visibility.Visible), "Ledger shows the small wren in the app header");
                    opacity.Value = 20;
                    Assert(Walk(window).OfType<TextBlock>().Any(t => t.Text == "Theme test quest" && t.Opacity == 1 && t.Effect is DropShadowEffect) && window.Opacity == 1, "Ledger quest text stays opaque with a contrast halo in background-only mode");
                    var leaderboardButton = Walk(dialog).OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Leaderboard upload…");
                    app.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
                    {
                        var review = app.Windows.Cast<Window>().Single(w => w.Title == "WrenFeed · Leaderboard upload");
                        Assert(Walk(review).OfType<TextBlock>().Any(t => t.Text.Contains("No completed quests")) && !Walk(review).OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Upload totals").IsEnabled, "Upload review opens without networking and refuses an empty tally");
                        review.Close();
                    }));
                    leaderboardButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    opacity.Value = 100;
                    Capture(window, Path.Combine(root, "ledger.png"));
                    list.SelectedItem = list.Items.Cast<ComboBoxItem>().Single(i => ((ThemeSpec)i.Tag).Id == "neon-aviary");
                    Capture(window, Path.Combine(root, "neon.png"));
                    foreach (var theme in additions)
                    {
                        list.SelectedItem = list.Items.Cast<ComboBoxItem>().Single(i => ((ThemeSpec)i.Tag).Id == theme.Id);
                        Assert(ThemeManager.Current.Id == theme.Id && Walk(window).OfType<Canvas>().Any(c => c.ToolTip != null && c.Visibility == Visibility.Visible && c.ToolTip.ToString().Contains(theme.Mascot == "sphynx" ? "sphynx" : theme.Mascot == "chain-wren" ? "medallion" : theme.Mascot == "pixel-wren" ? "pixel" : theme.Mascot == "flock" ? "flock" : "emblem")), theme.Name + " shows its own small emblem");
                        if (theme.Id == "pixel-perch") Assert(((CornerRadius)app.Resources["ControlCornerRadius"]).TopLeft == 0 && Walk(window).OfType<Border>().Any(b => b.BorderThickness.Left == 2 && b.CornerRadius.TopLeft == 0), "Pixel theme uses square controls and cards");
                        if (theme.Id == "the-flock") Assert(Walk(window).OfType<Canvas>().Any(c => c.ToolTip != null && c.ToolTip.ToString().StartsWith("The flock:") && c.Children.OfType<Canvas>().Count() == 3), "Flock header contains the G wren, red wren, and sphynx together");
                        Capture(window, Path.Combine(root, theme.Id + ".png"));
                    }
                    list.SelectedIndex = 0;
                    Assert(journal.Settings.Theme == ThemeManager.DefaultId && ThemeManager.Current.Id == ThemeManager.DefaultId, "Settings can restore Default without altering quests");
                    dialog.Close();
                }
                catch (Exception e) { failure = e; foreach (Window dialog in app.Windows.Cast<Window>().Where(w => w.Title == "WrenFeed · Settings").ToList()) dialog.Close(); }
            }));
            var settings = Walk(window).OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Settings");
            settings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (failure != null) throw failure;
            var saved = Storage.Decode(File.ReadAllBytes(storage.DataPath));
            Assert(journal.Games.Count == 1 && saved.Settings.Theme == ThemeManager.DefaultId && saved.Settings.BackgroundOnlyOpacity && saved.Settings.WindowOpacity == 1, "Theme and visibility switching leave quest data intact and persist selection");
            window.Close();
        }
        string valid = File.ReadAllText(args[2]);
        Reject(valid.Replace("\"Canvas\": \"#12151C\"", "\"Canvas\": \"not-a-color\""), "Malformed color is rejected");
        Reject(valid.Replace("\"Muted\": \"#ACB1BE\"", "\"Muted\": \"#151921\""), "Unreadable muted text is rejected");
        Reject(valid.Replace("\"decoration\": \"none\"", "\"decoration\": \"run-xaml\""), "Executable-style decoration is rejected");
        Reject(valid.Replace("\"id\": \"my-theme\"", "\"id\": \"../escape\""), "Theme IDs cannot escape local folder");
        app.Shutdown();
        Console.WriteLine("Theme assertions passed: " + count);
    }
}
