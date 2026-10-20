using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DailyQuest
{
    public static class Program
    {
        [STAThread] public static void Main(string[] args)
        {
            Storage storage = null;
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyQuest");
                if (args.Length == 2 && args[0] == "--data-dir") folder = Path.GetFullPath(args[1]);
                storage = new Storage(folder);
                var journal = storage.Load();
                var app = new Application();
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DailyQuest.Theme.xaml")) app.Resources = (ResourceDictionary)XamlReader.Load(stream);
                app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    MessageBox.Show("The action could not be completed.\n\n" + e.Exception.Message, "WrenFeed", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                };
                app.Run(new QuestWindow(storage, journal));
            }
            catch (Exception error) { MessageBox.Show(error.Message, "WrenFeed could not open", MessageBoxButton.OK, MessageBoxImage.Warning); }
            finally { if (storage != null) storage.Dispose(); }
        }
    }
    public static class UI
    {
        public static Brush Brush(string key) { return ThemeManager.Brush(key); }
        public static void Display(TextBlock text) { text.SetResourceReference(TextBlock.FontFamilyProperty, "DisplayFont"); }
        public static TextBlock Text(string text, double size, string color)
        {
            return new TextBlock { Text = text, FontSize = size, Foreground = Brush(color), TextWrapping = TextWrapping.Wrap };
        }
        public static Button Button(string text, Action action)
        {
            var b = new Button { Content = text }; AutomationProperties.SetName(b, text);
            b.Click += delegate { action(); }; return b;
        }
        public static void Primary(Button button) { button.Background = Brush("Accent"); button.Foreground = Brush("Canvas"); button.BorderBrush = Brush("Accent"); button.FontWeight = FontWeights.SemiBold; }
        public static StackPanel Stack() { return new StackPanel(); }
        public static TextBlock Label(string text) { var label = Text(text, 12, "Muted"); label.Margin = new Thickness(0, 16, 0, 7); return label; }
        public static TextBox Field(string text, string name, int max)
        {
            var field = new TextBox { Text = text ?? "", MaxLength = max }; AutomationProperties.SetName(field, name); return field;
        }
        public static Border Card(UIElement child)
        {
            var decoration = ThemeManager.Current.Decoration;
            var card = new Border { Background = Brush("Panel"), BorderBrush = Brush("Edge"), BorderThickness = new Thickness(decoration == "filigree" ? 2 : 1), CornerRadius = new CornerRadius(decoration == "none" ? 8 : decoration == "circuit" ? 4 : 3), Padding = new Thickness(14) };
            if (decoration == "none") { card.Child = child; return card; }
            var panel = new StackPanel();
            var trim = new DockPanel { Margin = new Thickness(0, -5, 0, 10) };
            var mark = Text(decoration == "circuit" ? "◇ ── ◇ ─────" : "✦  ───  ✦  ───", 11, "Accent"); mark.Opacity = 0.8; mark.HorizontalAlignment = HorizontalAlignment.Right;
            trim.Children.Add(mark); panel.Children.Add(trim); panel.Children.Add(child); card.Child = panel;
            return card;
        }
        public static Canvas CuteWren()
        {
            var bird = new Canvas { Width = 43, Height = 33, Margin = new Thickness(0, 0, 10, 0), ToolTip = "A little wren keeping watch" };
            Action<System.Windows.Shapes.Shape, double, double> add = delegate(System.Windows.Shapes.Shape shape, double left, double top) { Canvas.SetLeft(shape, left); Canvas.SetTop(shape, top); bird.Children.Add(shape); };
            add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 12,20 L 1,11 L 5,23 L 14,26 Z"), Fill = Brush("Accent") }, 0, 0);
            add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 20,25 L 18,30 M 27,24 L 29,30 M 9,30 L 36,30"), Stroke = Brush("Ink"), StrokeThickness = 1.4, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round }, 0, 0);
            add(new System.Windows.Shapes.Ellipse { Width = 26, Height = 19, Fill = Brush("Accent") }, 10, 10);
            add(new System.Windows.Shapes.Ellipse { Width = 18, Height = 14, Fill = Brush("Panel") }, 15, 14);
            add(new System.Windows.Shapes.Ellipse { Width = 14, Height = 10, Fill = Brush("Success"), Opacity = 0.8 }, 11, 13);
            add(new System.Windows.Shapes.Ellipse { Width = 17, Height = 17, Fill = Brush("Accent") }, 23, 3);
            add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 36,11 L 43,14 L 36,16 Z"), Fill = Brush("Success") }, 0, 0);
            add(new System.Windows.Shapes.Ellipse { Width = 2.8, Height = 2.8, Fill = Brush("Ink") }, 33, 9);
            return bird;
        }
        public static void SetupDialog(Window dialog, Window owner, string title, double width, double height)
        {
            dialog.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
            dialog.Owner = owner; dialog.Title = title; dialog.Width = width; dialog.Height = Math.Min(height, SystemParameters.WorkArea.Height - 50);
            dialog.MinWidth = Math.Min(width, 380); dialog.MinHeight = 350; dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.ShowInTaskbar = false;
            dialog.PreviewKeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Escape) dialog.Close(); };
        }
    }
    public sealed class QuestWindow : Window
    {
        readonly Storage storage;
        readonly ThemeManager themes;
        Journal journal;
        readonly DockPanel root = new DockPanel();
        readonly StackPanel cards = new StackPanel();
        readonly TextBlock heading = UI.Text("", 24, "Ink");
        readonly TextBlock summary = UI.Text("", 12, "Muted");
        readonly TextBlock footer = UI.Text("Saved on this device · resets at midnight", 11, "Muted");
        readonly TextBlock brand = UI.Text("◇  W R E N F E E D", 11, "Accent");
        readonly TextBlock headerOrnament = UI.Text("", 11, "Accent");
        readonly Canvas wrenBadge = UI.CuteWren();
        readonly Border fill = new Border { Height = 3, HorizontalAlignment = HorizontalAlignment.Left };
        readonly Border track = new Border { Height = 3 };
        readonly StackPanel dateArea = new StackPanel();
        readonly Button todayButton;
        readonly Button weeklyButton;
        readonly Button historyButton;
        readonly Button addButton;
        readonly DispatcherTimer clock = new DispatcherTimer();
        string activeDate;
        bool history;
        bool weekly;
        DateTime historyDate = DateTime.Today;
        double fraction;
        public QuestWindow(Storage storage, Journal journal)
        {
            SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
            this.storage = storage; this.journal = journal;
            themes = new ThemeManager(storage.Folder);
            ThemeManager.Apply(themes.Find(journal.Settings.Theme));
            Title = "WrenFeed";
            Width = Math.Max(350, Math.Min(journal.Settings.Width, SystemParameters.WorkArea.Width));
            Height = Math.Max(440, Math.Min(journal.Settings.Height, SystemParameters.WorkArea.Height - 35));
            MinWidth = 350; MinHeight = 440; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = journal.Settings.AlwaysOnTop;
            root.Margin = new Thickness(20, 18, 20, 16); Content = root;
            var top = UI.Stack(); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
            var masthead = new DockPanel();
            var settings = UI.Button("⚙", OpenSettings); settings.FontSize = 17; settings.Width = 34; settings.Padding = new Thickness(0); settings.ToolTip = "Settings (Ctrl+,)"; AutomationProperties.SetName(settings, "Settings");
            DockPanel.SetDock(settings, Dock.Right); masthead.Children.Add(settings);
            DockPanel.SetDock(wrenBadge, Dock.Right); masthead.Children.Add(wrenBadge);
            brand.VerticalAlignment = VerticalAlignment.Center; masthead.Children.Add(brand); top.Children.Add(masthead);
            headerOrnament.Margin = new Thickness(0, 7, 0, 0); headerOrnament.TextAlignment = TextAlignment.Center; top.Children.Add(headerOrnament);
            UI.Display(heading); heading.Margin = new Thickness(0, 17, 0, 6); top.Children.Add(heading);
            RefreshThemeChrome();
            summary.Margin = new Thickness(0, 0, 0, 12); top.Children.Add(summary);
            track.Background = UI.Brush("Edge"); fill.Background = UI.Brush("Accent"); track.Child = fill; top.Children.Add(track); track.SizeChanged += delegate { fill.Width = Math.Max(0, track.ActualWidth * fraction); };
            var tabs = new Grid { Margin = new Thickness(0, 16, 0, 14) };
            tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            todayButton = UI.Button("Today", delegate { history = false; weekly = false; Render(null); }); todayButton.Margin = new Thickness(0, 0, 6, 0);
            weeklyButton = UI.Button("Weekly", delegate { history = false; weekly = true; Render(null); }); weeklyButton.Margin = new Thickness(0, 0, 6, 0);
            historyButton = UI.Button("History", delegate { history = true; weekly = false; historyDate = DateTime.Today; Render(null); }); historyButton.Margin = new Thickness(0, 0, 10, 0);
            addButton = UI.Button("+ Game", delegate { EditGame(null); }); UI.Primary(addButton); addButton.ToolTip = "Add game (Ctrl+N)";
            Grid.SetColumn(weeklyButton, 1); Grid.SetColumn(historyButton, 2); Grid.SetColumn(addButton, 3); tabs.Children.Add(todayButton); tabs.Children.Add(weeklyButton); tabs.Children.Add(historyButton); tabs.Children.Add(addButton); top.Children.Add(tabs);
            top.Children.Add(dateArea);
            var bottom = new Border { BorderBrush = UI.Brush("Edge"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 12, 0, 0), Margin = new Thickness(0, 12, 0, 0), Child = footer };
            DockPanel.SetDock(bottom, Dock.Bottom); root.Children.Add(bottom);
            var scroll = new ScrollViewer { Content = cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; root.Children.Add(scroll);
            activeDate = Journal.DateKey(DateTime.Now); journal.Today(DateTime.Now); foreach (var game in journal.Games.Where(g => !g.Archived && g.DailyReset != "local")) journal.CurrentTimedDaily(game, DateTime.UtcNow); foreach (var game in journal.WeeklyGames.Where(g => !g.Archived)) journal.CurrentWeekly(game, DateTime.UtcNow); storage.Save(journal);
            clock.Interval = TimeSpan.FromSeconds(3); clock.Tick += delegate { RefreshDay(); }; clock.Start();
            Activated += delegate { RefreshDay(); };
            Closing += delegate
            {
                clock.Stop();
                try { if (WindowState == WindowState.Normal) { journal.Settings.Width = Width; journal.Settings.Height = Height; } storage.Save(journal); }
                catch (Exception e) { MessageBox.Show(this, "Could not save window preferences. Previously saved progress is still on disk.\n\n" + e.Message, "WrenFeed"); }
            };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (Keyboard.Modifiers != ModifierKeys.Control) return;
                if (e.Key == Key.N) { EditGame(null); e.Handled = true; }
                if (e.Key == Key.H) { history = !history; if (history) weekly = false; Render(null); e.Handled = true; }
                if (e.Key == Key.OemComma) { OpenSettings(); e.Handled = true; }
            };
            Render(null);
            Loaded += delegate { if (storage.RecoveryNotice != null) MessageBox.Show(this, storage.RecoveryNotice, "Journal recovered", MessageBoxButton.OK, MessageBoxImage.Warning); };
        }
        bool RefreshDay()
        {
            string date = Journal.DateKey(DateTime.Now);
            bool dailyChanged = date != activeDate;
            bool timedDailyChanged = journal.Games.Where(g => !g.Archived && g.DailyReset != "local").Any(g =>
            {
                var current = journal.DailyUtcRecords.LastOrDefault(r => r.GameId == g.Id);
                return current == null || (g.DailyReset == "utc" ? DateTime.Parse(current.CycleStartUtc).ToUniversalTime() != Journal.DailyCycleStart(g, DateTime.UtcNow) : current.Result.Done && DateTime.UtcNow >= Journal.NextDailyIntervalReset(g, current));
            });
            bool weeklyChanged = journal.WeeklyGames.Where(g => !g.Archived).Any(g =>
            {
                var current = journal.WeeklyRecords.LastOrDefault(r => r.GameId == g.Id);
                return current == null || (g.WeeklyReset == "fixed" && DateTime.Parse(current.CycleStartUtc).ToUniversalTime() != Journal.FixedCycleStart(g, DateTime.UtcNow)) || (g.WeeklyReset == "completion-weekday" && current.Result.Done && DateTime.UtcNow >= Journal.NextWeeklyReset(g, current));
            });
            if (!dailyChanged && !timedDailyChanged && !weeklyChanged) return false;
            if (Change(delegate { journal.Today(DateTime.Now); foreach (var game in journal.Games.Where(g => !g.Archived && g.DailyReset != "local")) journal.CurrentTimedDaily(game, DateTime.UtcNow); foreach (var game in journal.WeeklyGames.Where(g => !g.Archived)) journal.CurrentWeekly(game, DateTime.UtcNow); }, null)) { activeDate = date; footer.Text = "New quests are ready."; }
            return true;
        }
        bool Change(Action action, string flash)
        {
            var before = Storage.Encode(journal);
            try { action(); storage.Save(journal); Render(flash); return true; }
            catch (Exception e)
            {
                journal = Storage.Decode(before); Render(null);
                MessageBox.Show(this, "This change was not saved. Please try again.\n\n" + e.Message, "Could not save", MessageBoxButton.OK, MessageBoxImage.Warning); return false;
            }
        }
        void Render(string flash)
        {
            cards.Children.Clear(); dateArea.Children.Clear();
            todayButton.BorderBrush = UI.Brush(!history && !weekly ? "Accent" : "Edge"); weeklyButton.BorderBrush = UI.Brush(weekly ? "Accent" : "Edge"); historyButton.BorderBrush = UI.Brush(history ? "Accent" : "Edge");
            addButton.Visibility = history ? Visibility.Collapsed : Visibility.Visible;
            if (weekly) { RenderWeekly(flash); return; }
            var day = journal.Days.FirstOrDefault(d => d.Date == Journal.DateKey(history ? historyDate : DateTime.Now));
            var entries = day == null ? new System.Collections.Generic.List<GameResult>() : day.Games.Where(g => history || !g.Archived).ToList();
            if (history)
            {
                entries.AddRange(journal.DailyUtcRecords.Where(r => r.Result != null && (r.Result.Done && r.Result.CompletedUtc != null ? DateTime.Parse(r.Result.CompletedUtc).ToLocalTime().Date : DateTime.Parse(r.CycleStartUtc).ToLocalTime().Date) == historyDate.Date).Select(r => r.Result));
                entries = entries.GroupBy(g => g.Id + "|" + (g.CompletedUtc ?? "open")).Select(g => g.First()).ToList();
            }
            else
            {
                entries = journal.Games.Where(g => !g.Archived).Select(g =>
                {
                    var record = journal.DailyUtcRecords.LastOrDefault(r => r.GameId == g.Id);
                    return g.DailyReset != "local" ? (record == null ? null : record.Result) : (day == null ? null : day.Games.FirstOrDefault(r => r.Id == g.Id));
                }).Where(r => r != null).ToList();
            }
            int done = entries.Count(g => g.Done);
            fraction = entries.Count == 0 ? 0 : (double)done / entries.Count; fill.Width = Math.Max(0, track.ActualWidth * fraction);
            fill.Background = UI.Brush(entries.Count > 0 && done == entries.Count ? "Success" : "Accent");
            heading.Text = history ? "The adventure so far" : "Your daily quests";
            summary.Text = (history ? historyDate : DateTime.Today).ToString("ddd, MMM d") + "   ·   " + done + " / " + entries.Count + " quests complete";
            footer.Text = "Saved on this device · each quest has its own reset";
            if (history)
            {
                var dateRow = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
                var prev = UI.Button("‹", delegate { historyDate = historyDate.AddDays(-1); Render(null); }); prev.ToolTip = "Previous day"; AutomationProperties.SetName(prev, "Previous day"); prev.Margin = new Thickness(0, 0, 8, 0);
                var next = UI.Button("›", delegate { historyDate = historyDate.AddDays(1); Render(null); }); next.IsEnabled = historyDate.Date < DateTime.Today; next.ToolTip = "Next day"; AutomationProperties.SetName(next, "Next day"); next.Margin = new Thickness(8, 0, 0, 0);
                DockPanel.SetDock(prev, Dock.Left); DockPanel.SetDock(next, Dock.Right); dateRow.Children.Add(prev); dateRow.Children.Add(next);
                var date = new DatePicker { SelectedDate = historyDate, DisplayDateEnd = DateTime.Today, VerticalContentAlignment = VerticalAlignment.Center, FontSize = 13, Foreground = UI.Brush("Ink"), Background = UI.Brush("Field"), BorderBrush = UI.Brush("Edge") };
                AutomationProperties.SetName(date, "History date");
                date.SelectedDateChanged += delegate { if (date.SelectedDate.HasValue) { historyDate = date.SelectedDate.Value.Date; Render(null); } };
                dateRow.Children.Add(date); dateArea.Children.Add(dateRow);
                var note = UI.Text("A saved snapshot of this day · read only", 11, "Muted"); note.Margin = new Thickness(0, 0, 0, 12); dateArea.Children.Add(note);
            }
            if (entries.Count == 0)
            {
                var empty = UI.Stack(); empty.Margin = new Thickness(10, 30, 10, 30);
                var symbol = UI.Text("◇", 38, "Accent"); symbol.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(symbol);
                var title = UI.Text(history ? "No quests recorded" : "Every adventure starts small", 18, "Ink"); UI.Display(title); title.TextAlignment = TextAlignment.Center; title.Margin = new Thickness(0, 14, 0, 10); empty.Children.Add(title);
                var copy = UI.Text(history ? "There is no saved checklist for this date. Days when the app was closed are not marked as completed." : "Add a game and the little daily tasks worth coming back for.", 13, "Muted"); copy.TextAlignment = TextAlignment.Center; empty.Children.Add(copy);
                if (!history) { var add = UI.Button("+ Add your first game", delegate { EditGame(null); }); UI.Primary(add); add.Margin = new Thickness(0, 22, 0, 0); add.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(add); }
                cards.Children.Add(UI.Card(empty));
            }
            RenderQuestGroups(entries, flash);
            if (!history && entries.Count > 0 && done == entries.Count)
            {
                var finished = UI.Text("✦  Daily quests complete. Well played.", 12, "Success"); finished.Margin = new Thickness(0, 10, 0, 8); finished.TextAlignment = TextAlignment.Center; cards.Children.Add(finished);
            }
        }
        void RenderWeekly(string flash)
        {
            var entries = journal.WeeklyGames.Where(g => !g.Archived).Select(g => new { Game = g, Record = journal.WeeklyRecords.LastOrDefault(r => r.GameId == g.Id) }).Where(x => x.Record != null).ToList();
            int done = entries.Count(x => x.Record.Result.Done);
            fraction = entries.Count == 0 ? 0 : (double)done / entries.Count; fill.Width = Math.Max(0, track.ActualWidth * fraction);
            fill.Background = UI.Brush(entries.Count > 0 && done == entries.Count ? "Success" : "Accent");
            heading.Text = "Your weekly quests";
            summary.Text = done + " / " + entries.Count + " quests complete";
            footer.Text = "Saved on this device · weekly resets use UTC";
            addButton.Content = "+ Game";
            if (entries.Count == 0)
            {
                var empty = UI.Stack(); empty.Margin = new Thickness(10, 30, 10, 30);
                var title = UI.Text("A new weekly adventure", 18, "Ink"); UI.Display(title); title.TextAlignment = TextAlignment.Center; empty.Children.Add(title);
                var copy = UI.Text("Add a weekly game and choose when its quest resets.", 13, "Muted"); copy.Margin = new Thickness(0, 10, 0, 16); copy.TextAlignment = TextAlignment.Center; empty.Children.Add(copy);
                var add = UI.Button("+ Add a weekly game", delegate { EditGame(null); }); UI.Primary(add); add.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(add);
                cards.Children.Add(UI.Card(empty));
            }
            RenderQuestGroups(entries.Select(e => e.Record.Result).ToList(), flash);
            var past = journal.WeeklyRecords.Where(r => r.Result.Done && !entries.Any(e => e.Record.Id == r.Id)).OrderByDescending(r => r.Result.CompletedUtc).Take(8).ToList();
            if (past.Count > 0)
            {
                var title = UI.Text("PREVIOUS WEEKLY QUESTS", 11, "Accent"); title.Margin = new Thickness(0, 16, 0, 10); cards.Children.Add(title);
                foreach (var record in past)
                {
                    var at = DateTime.Parse(record.Result.CompletedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();
                    var note = UI.Text("✓  " + record.Result.Name + "  ·  " + at.ToString("MMM d, yyyy"), 12, "Muted"); note.Margin = new Thickness(3, 0, 0, 9); cards.Children.Add(note);
                }
            }
        }
        void RenderQuestGroups(System.Collections.Generic.List<GameResult> entries, string flash)
        {
            var shown = new System.Collections.Generic.HashSet<GameResult>();
            foreach (var entry in entries)
            {
                if (!shown.Add(entry)) continue;
                string group = (entry.GroupName ?? "").Trim();
                if (group.Length == 0) { cards.Children.Add(GameCard(entry, flash == entry.Id)); continue; }
                var members = entries.Where(e => String.Equals((e.GroupName ?? "").Trim(), group, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var member in members) shown.Add(member);
                var content = UI.Stack();
                var header = new DockPanel { Margin = new Thickness(0, 0, 0, 9) };
                if (!history)
                {
                    var add = UI.Button("+ Quest", delegate { EditGame(null, group); }); add.FontSize = 11; add.Padding = new Thickness(7, 2, 7, 2); add.ToolTip = "Add a quest to " + group;
                    DockPanel.SetDock(add, Dock.Right); header.Children.Add(add);
                }
                var title = UI.Text(group, 17, "Accent"); title.FontWeight = FontWeights.SemiBold; header.Children.Add(title); content.Children.Add(header);
                for (int i = 0; i < members.Count; i++)
                {
                    if (i > 0) content.Children.Add(new Border { BorderBrush = UI.Brush("Edge"), BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 11, 0, 11) });
                    content.Children.Add(GameRow(members[i]));
                }
                var card = UI.Card(content); card.Margin = new Thickness(0, 0, 0, 10);
                if (members.All(m => m.Done)) card.BorderBrush = CompletedBorder();
                if (members.Any(m => m.Id == flash)) Flash(card);
                cards.Children.Add(card);
            }
        }
        void Flash(Border card)
        {
            if (!journal.Settings.Animate) return;
            var brush = new SolidColorBrush(((SolidColorBrush)card.BorderBrush).Color); card.BorderBrush = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(((SolidColorBrush)UI.Brush("Accent")).Color, brush.Color, TimeSpan.FromMilliseconds(650)));
        }
        Border GameCard(GameResult game, bool flash)
        {
            var card = UI.Card(GameRow(game)); card.Margin = new Thickness(0, 0, 0, 10);
            if (game.Done) card.BorderBrush = CompletedBorder();
            if (flash) Flash(card);
            return card;
        }
        static SolidColorBrush CompletedBorder()
        {
            return ThemeManager.Current.Decoration == "none" ? new SolidColorBrush(Color.FromRgb(64, 89, 78)) : new SolidColorBrush(ThemeManager.Current.Colors["Success"]);
        }
        void RefreshThemeChrome()
        {
            brand.Text = ThemeManager.Current.Mark + "  W R E N F E E D";
            wrenBadge.Visibility = ThemeManager.Current.Mascot == "wren" ? Visibility.Visible : Visibility.Collapsed;
            headerOrnament.Visibility = ThemeManager.Current.Decoration == "none" ? Visibility.Collapsed : Visibility.Visible;
            headerOrnament.Text = ThemeManager.Current.Decoration == "circuit" ? "── ◇ ────── ◇ ──" : "✦  ───  ◇  ───  ✦";
        }
        StackPanel GameRow(GameResult game)
        {
            var content = UI.Stack(); var header = new DockPanel();
            if (!history)
            {
                var menuButton = UI.Button("···", delegate { }); menuButton.Padding = new Thickness(5, 0, 5, 0); menuButton.MinHeight = 26; menuButton.Width = 29; menuButton.FontSize = 17; menuButton.ToolTip = "Edit or reorder " + game.Name; AutomationProperties.SetName(menuButton, "Options for " + game.Name);
                var menu = new ContextMenu();
                Action<string, Action> item = delegate(string label, Action action) { var mi = new MenuItem { Header = label }; mi.Click += delegate { action(); }; menu.Items.Add(mi); };
                item("Edit quest", delegate { EditGame((weekly ? journal.WeeklyGames : journal.Games).First(g => g.Id == game.Id)); });
                item("Move up", delegate { Change(delegate { if (weekly) journal.MoveWeekly(game.Id, -1); else journal.Move(game.Id, -1); }, null); });
                item("Move down", delegate { Change(delegate { if (weekly) journal.MoveWeekly(game.Id, 1); else journal.Move(game.Id, 1); }, null); });
                menu.Items.Add(new Separator());
                item("Archive quest", delegate
                {
                    if (MessageBox.Show(this, "Remove “" + game.Name + "” from your " + (weekly ? "weekly" : "daily") + " list?\n\nIts history will stay saved. You can restore it in Settings.", "Archive quest", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Change(delegate { if (weekly) journal.ArchiveWeekly(game.Id, DateTime.UtcNow); else journal.Archive(game.Id, DateTime.Now); }, null);
                });
                menuButton.Click += delegate { menu.PlacementTarget = menuButton; menu.IsOpen = true; };
                DockPanel.SetDock(menuButton, Dock.Right); header.Children.Add(menuButton);
            }
            var name = UI.Text(game.Name, 16, game.Done ? "Success" : "Ink"); name.FontWeight = FontWeights.SemiBold;
            var check = new CheckBox { Content = name, IsChecked = game.Done, IsHitTestVisible = !history, Focusable = !history };
            AutomationProperties.SetName(check, "Complete " + game.Name);
            check.Click += delegate
            {
                if (RefreshDay()) return;
                bool done = check.IsChecked == true;
                if (Change(delegate { if (weekly) journal.ToggleWeeklyGame(game.Id, done, DateTime.UtcNow); else journal.ToggleGame(game.Id, done, DateTime.Now, DateTime.UtcNow); }, done ? game.Id : null)) footer.Text = done ? "Quest complete. Nicely done." : "Quest reopened.";
            };
            header.Children.Add(check); content.Children.Add(header);
            if (!String.IsNullOrWhiteSpace(game.Description)) { var desc = UI.Text(game.Description, 12, "Muted"); desc.Margin = new Thickness(29, 4, 0, 3); content.Children.Add(desc); }
            if (game.Lines.Count > 0)
            {
                var subs = UI.Stack();
                foreach (var line in game.Lines)
                {
                    if (line.Checkable)
                    {
                        var lineCheck = new CheckBox { Content = UI.Text(line.Text, 12, line.Done ? "Success" : "Muted"), IsChecked = line.Done, Margin = new Thickness(0, 2, 0, 2), IsHitTestVisible = !history, Focusable = !history };
                        AutomationProperties.SetName(lineCheck, game.Name + ": " + line.Text);
                        string lineId = line.Id;
                        lineCheck.Click += delegate
                        {
                            if (RefreshDay()) return;
                            Change(delegate { if (weekly) journal.ToggleWeeklyLine(game.Id, lineId, lineCheck.IsChecked == true, DateTime.UtcNow); else journal.ToggleLine(game.Id, lineId, lineCheck.IsChecked == true, DateTime.Now, DateTime.UtcNow); }, game.Id);
                        };
                        subs.Children.Add(lineCheck);
                    }
                    else { var note = UI.Text("·  " + line.Text, 12, "Muted"); note.Margin = new Thickness(0, 6, 0, 6); subs.Children.Add(note); }
                }
                content.Children.Add(new Border { Margin = new Thickness(29, 10, 0, 0), BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = UI.Brush("Edge"), Padding = new Thickness(12, 0, 0, 0), Child = subs });
            }
            if (game.Archived) { var archived = UI.Text("Archived quest", 10, "Muted"); archived.Margin = new Thickness(29, 8, 0, 0); content.Children.Add(archived); }
            if (!history)
            {
                var template = (weekly ? journal.WeeklyGames : journal.Games).First(g => g.Id == game.Id);
                string rule = weekly ? template.WeeklyReset == "completion-weekday" ? "7 days after completion · 00:00 UTC" : "Every " + ((DayOfWeek)template.FixedWeekdayUtc) + " · " + template.FixedHourUtc.ToString("00") + ":" + template.FixedMinuteUtc.ToString("00") + " UTC" : template.DailyReset == "utc" ? "Daily · " + template.DailyHourUtc.ToString("00") + ":" + template.DailyMinuteUtc.ToString("00") + " UTC" : template.DailyReset == "after-completion" ? template.DailyIntervalHours + " hours after completion" : "Daily · local midnight";
                var detail = UI.Text(rule, 11, "Accent"); detail.Margin = new Thickness(29, 8, 0, 0); content.Children.Add(detail);
                if (!weekly && template.DailyReset == "after-completion" && game.Done)
                {
                    var record = journal.DailyUtcRecords.Last(r => r.GameId == game.Id);
                    var reset = Journal.NextDailyIntervalReset(template, record).ToLocalTime();
                    var next = UI.Text("Ready again: " + reset.ToString("ddd, MMM d · h:mm tt") + " local", 11, "Muted"); next.Margin = new Thickness(29, 4, 0, 0); content.Children.Add(next);
                }
                if (weekly && game.Done)
                {
                    var record = journal.WeeklyRecords.Last(r => r.GameId == game.Id);
                    var reset = Journal.NextWeeklyReset(template, record).ToLocalTime();
                    var next = UI.Text("Next quest: " + reset.ToString("ddd, MMM d · h:mm tt") + " local", 11, "Muted"); next.Margin = new Thickness(29, 4, 0, 0); content.Children.Add(next);
                }
            }
            if (history && game.Done && !String.IsNullOrEmpty(game.CompletedUtc))
            {
                var at = DateTime.Parse(game.CompletedUtc).ToLocalTime();
                var when = UI.Text("Completed " + at.ToString("h:mm tt") + " local", 10, "Muted"); when.Margin = new Thickness(29, 8, 0, 0); content.Children.Add(when);
            }
            return content;
        }
        void EditGame(Game game)
        {
            EditGame(game, null);
        }
        void EditGame(Game game, string group)
        {
            var editor = new GameEditor(this, game, weekly, group);
            if (editor.ShowDialog() == true)
            {
                history = false;
                Change(delegate { if (weekly) journal.SaveWeeklyGame(editor.Result, DateTime.UtcNow); else journal.SaveGame(editor.Result, DateTime.Now, DateTime.UtcNow); }, null);
            }
        }
        void OpenSettings()
        {
            var dialog = new Window(); UI.SetupDialog(dialog, this, "WrenFeed · Settings", 450, 580);
            var stack = UI.Stack(); stack.Margin = new Thickness(24);
            var title = UI.Text("Make yourself at home", 22, "Ink"); UI.Display(title); stack.Children.Add(title);
            var intro = UI.Text("A quiet little companion for your daily games.", 12, "Muted"); intro.Margin = new Thickness(0, 8, 0, 20); stack.Children.Add(intro);
            var pin = new CheckBox { Content = "Keep this window on top", IsChecked = journal.Settings.AlwaysOnTop, Margin = new Thickness(0, 0, 0, 10) };
            pin.Click += delegate { if (Change(delegate { journal.Settings.AlwaysOnTop = pin.IsChecked == true; }, null)) Topmost = journal.Settings.AlwaysOnTop; else pin.IsChecked = journal.Settings.AlwaysOnTop; };
            var animate = new CheckBox { Content = "Subtle completion animations", IsChecked = journal.Settings.Animate };
            animate.Click += delegate { if (!Change(delegate { journal.Settings.Animate = animate.IsChecked == true; }, null)) animate.IsChecked = journal.Settings.Animate; };
            stack.Children.Add(pin); stack.Children.Add(animate);
            stack.Children.Add(UI.Label("APPEARANCE"));
            var themeList = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(themeList, "Theme"); stack.Children.Add(themeList);
            var themeInfo = UI.Text("", 11, "Muted"); themeInfo.Margin = new Thickness(0, 6, 0, 5); stack.Children.Add(themeInfo);
            bool fillingThemes = false;
            Action fillThemes = delegate
            {
                fillingThemes = true; themeList.Items.Clear();
                var available = new System.Collections.Generic.List<ThemeSpec> { ThemeManager.Default }; available.AddRange(themes.Installed());
                foreach (var option in available) themeList.Items.Add(new ComboBoxItem { Content = option.Name, Tag = option });
                themeList.SelectedIndex = available.FindIndex(t => t.Id == journal.Settings.Theme);
                themeInfo.Text = available.Any(t => t.Id == journal.Settings.Theme) ? ((ThemeSpec)((ComboBoxItem)themeList.SelectedItem).Tag).Description : "The selected theme file is missing. Default is showing until you install it again or choose another theme.";
                fillingThemes = false;
            };
            fillThemes();
            themeList.SelectionChanged += delegate
            {
                if (fillingThemes || themeList.SelectedItem == null) return;
                var selected = (ThemeSpec)((ComboBoxItem)themeList.SelectedItem).Tag;
                if (selected.Id == journal.Settings.Theme) return;
                if (Change(delegate { journal.Settings.Theme = selected.Id; }, null))
                {
                    ThemeManager.Apply(selected); RefreshThemeChrome(); Render(null);
                    themeInfo.Text = selected.Description;
                }
                else fillThemes();
            };
            var installTheme = UI.Button("Install theme file…", delegate
            {
                var picker = new OpenFileDialog { Filter = "WrenFeed theme (*.json)|*.json", CheckFileExists = true, Title = "Choose a WrenFeed theme" };
                var suppliedThemes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themes");
                if (Directory.Exists(suppliedThemes)) picker.InitialDirectory = suppliedThemes;
                if (picker.ShowDialog(dialog) != true) return;
                try
                {
                    var candidate = ThemeManager.Parse(File.ReadAllBytes(picker.FileName));
                    var destination = Path.Combine(themes.Folder, candidate.Id + ".json");
                    if (File.Exists(destination) && !String.Equals(Path.GetFullPath(picker.FileName), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase) && MessageBox.Show(dialog, "Replace the installed “" + candidate.Name + "” theme file?", "Replace theme", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    var installed = themes.Install(picker.FileName);
                    fillThemes();
                    if (installed.Id == journal.Settings.Theme) { ThemeManager.Apply(installed); RefreshThemeChrome(); Render(null); themeInfo.Text = installed.Description; }
                    foreach (ComboBoxItem item in themeList.Items) if (((ThemeSpec)item.Tag).Id == installed.Id) { themeList.SelectedItem = item; break; }
                }
                catch (Exception error) { MessageBox.Show(dialog, error.Message, "Theme install failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
            });
            installTheme.Margin = new Thickness(0, 5, 0, 0); installTheme.HorizontalAlignment = HorizontalAlignment.Left; stack.Children.Add(installTheme);
            var themeLocation = UI.Text("Optional theme files stay in " + themes.Folder, 10, "Muted"); themeLocation.TextWrapping = TextWrapping.Wrap; themeLocation.Margin = new Thickness(0, 6, 0, 0); stack.Children.Add(themeLocation);
            stack.Children.Add(UI.Label("DAILY RESET")); stack.Children.Add(UI.Text("Each quest can reset at local midnight, a fixed UTC time, or a chosen number of hours after completion. Progress saves immediately, and earlier completions remain in History.", 12, "Muted"));
            stack.Children.Add(UI.Label("WEEKLY RESET")); stack.Children.Add(UI.Text("Choose a fixed UTC day and time, or the same UTC weekday seven days after completion. Weekly completion history appears on the Weekly tab.", 12, "Muted"));
            stack.Children.Add(UI.Label("YOUR DATA")); stack.Children.Add(UI.Text("Stored only on this PC. No account or internet needed. A previous-save backup is kept automatically.", 12, "Muted"));
            var buttons = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
            var questImport = UI.Button("Import quests", delegate { ImportQuests(dialog); }); questImport.Margin = new Thickness(0, 0, 8, 8); buttons.Children.Add(questImport);
            var export = UI.Button("Export backup", delegate { Export(dialog); }); export.Margin = new Thickness(0, 0, 8, 8); buttons.Children.Add(export);
            var import = UI.Button("Restore backup", delegate { Import(dialog); }); import.Margin = new Thickness(0, 0, 0, 8); buttons.Children.Add(import); stack.Children.Add(buttons);
            var location = UI.Text(storage.DataPath, 10, "Muted"); location.TextWrapping = TextWrapping.Wrap; stack.Children.Add(location);
            stack.Children.Add(UI.Label("ARCHIVED GAMES"));
            var archived = UI.Stack(); stack.Children.Add(archived);
            Action renderArchived = null;
            renderArchived = delegate
            {
                archived.Children.Clear();
                foreach (var game in journal.Games.Where(g => g.Archived).ToList())
                {
                    var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) }; string id = game.Id;
                    var restore = UI.Button("Restore", delegate { if (Change(delegate { journal.Restore(id, DateTime.Now, DateTime.UtcNow); }, null)) renderArchived(); }); DockPanel.SetDock(restore, Dock.Right); row.Children.Add(restore);
                    var label = UI.Text(game.Name, 12, "Ink"); label.Margin = new Thickness(0, 5, 8, 0); row.Children.Add(label); archived.Children.Add(row);
                }
                foreach (var game in journal.WeeklyGames.Where(g => g.Archived).ToList())
                {
                    var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) }; string id = game.Id;
                    var restore = UI.Button("Restore", delegate { if (Change(delegate { journal.RestoreWeekly(id, DateTime.UtcNow); }, null)) renderArchived(); }); DockPanel.SetDock(restore, Dock.Right); row.Children.Add(restore);
                    var label = UI.Text(game.Name + " · Weekly", 12, "Ink"); label.Margin = new Thickness(0, 5, 8, 0); row.Children.Add(label); archived.Children.Add(row);
                }
                if (archived.Children.Count == 0) archived.Children.Add(UI.Text("No archived games.", 12, "Muted"));
            }; renderArchived();
            var close = UI.Button("Done", delegate { dialog.Close(); }); close.HorizontalAlignment = HorizontalAlignment.Right; close.Margin = new Thickness(0, 22, 0, 0); UI.Primary(close); stack.Children.Add(close);
            dialog.Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            dialog.ShowDialog();
        }
        void Export(Window owner)
        {
            var picker = new SaveFileDialog { Filter = "WrenFeed backup (*.json)|*.json", FileName = "WrenFeed-" + DateTime.Today.ToString("yyyy-MM-dd") + ".json", OverwritePrompt = true };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                if (IsLiveFile(picker.FileName)) throw new IOException("Choose a location outside the live journal files.");
                File.WriteAllBytes(picker.FileName, Storage.Encode(journal)); MessageBox.Show(owner, "Your games, settings, and history have been exported.", "Backup exported");
            } catch (Exception e) { MessageBox.Show(owner, e.Message, "Export failed"); }
        }
        bool IsLiveFile(string path)
        {
            return String.Equals(Path.GetFullPath(path), storage.DataPath, StringComparison.OrdinalIgnoreCase) || String.Equals(Path.GetFullPath(path), storage.BackupPath, StringComparison.OrdinalIgnoreCase);
        }
        void ImportQuests(Window owner)
        {
            var picker = new OpenFileDialog { Filter = "WrenFeed quest list (*.json)|*.json", CheckFileExists = true, Title = "Choose a WrenFeed quest list" };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                var plan = QuestImportPlan.Create(journal, QuestImport.Parse(File.ReadAllBytes(picker.FileName)));
                var preview = new Window(); UI.SetupDialog(preview, owner, "WrenFeed · Import quests", 510, 560);
                var layout = new DockPanel { Margin = new Thickness(22) }; preview.Content = layout;
                var summaryText = UI.Text(plan.ToAdd.Count + " quests to add · " + plan.Skipped.Count + " duplicates to skip", 17, "Ink");
                summaryText.Margin = new Thickness(0, 0, 0, 12); DockPanel.SetDock(summaryText, Dock.Top); layout.Children.Add(summaryText);
                var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
                var cancel = UI.Button("Cancel", delegate { preview.DialogResult = false; }); cancel.IsCancel = true; cancel.Margin = new Thickness(0, 0, 8, 0); actions.Children.Add(cancel);
                var add = UI.Button("Add quests", delegate { preview.DialogResult = true; }); add.IsEnabled = plan.ToAdd.Count > 0; UI.Primary(add); actions.Children.Add(add);
                DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions);
                var details = UI.Stack();
                foreach (var entry in plan.ToAdd)
                {
                    var label = UI.Text((entry.Weekly ? "Weekly" : "Daily") + " · " + (String.IsNullOrEmpty(entry.Game.GroupName) ? "Ungrouped" : entry.Game.GroupName) + " · " + entry.Game.Name, 13, "Ink");
                    label.Margin = new Thickness(0, 0, 0, 9); details.Children.Add(label);
                }
                foreach (var entry in plan.Skipped)
                {
                    var label = UI.Text("Skip duplicate · " + (entry.Weekly ? "Weekly" : "Daily") + " · " + (String.IsNullOrEmpty(entry.Game.GroupName) ? "Ungrouped" : entry.Game.GroupName) + " · " + entry.Game.Name, 12, "Muted");
                    label.Margin = new Thickness(0, 0, 0, 9); details.Children.Add(label);
                }
                layout.Children.Add(new ScrollViewer { Content = details, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
                if (preview.ShowDialog() != true) return;
                if (Change(delegate { plan.Apply(journal, DateTime.Now, DateTime.UtcNow); }, null))
                {
                    history = false;
                    weekly = plan.ToAdd.All(q => q.Weekly);
                    Render(null);
                    owner.Close();
                    MessageBox.Show(this, "Added " + plan.ToAdd.Count + " quests. Skipped " + plan.Skipped.Count + " duplicates.", "Quests imported");
                }
            }
            catch (Exception e) { MessageBox.Show(owner, e.Message, "Quest import failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        void Import(Window owner)
        {
            var picker = new OpenFileDialog { Filter = "WrenFeed backup (*.json)|*.json", CheckFileExists = true };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                var restored = Storage.Decode(File.ReadAllBytes(picker.FileName));
                if (MessageBox.Show(owner, "Replace your current journal with this backup?\n\n" + restored.Games.Count(g => !g.Archived) + " daily games · " + restored.WeeklyGames.Count(g => !g.Archived) + " weekly games · " + restored.Days.Count + " recorded days\n\nA separate copy of your current journal will be kept in the data folder.", "Restore backup", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                File.WriteAllBytes(Path.Combine(storage.Folder, "before-restore-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff") + ".json"), Storage.Encode(journal));
                if (Change(delegate { journal = restored; journal.Today(DateTime.Now); foreach (var game in journal.Games.Where(g => !g.Archived && g.DailyReset != "local")) journal.CurrentTimedDaily(game, DateTime.UtcNow); foreach (var game in journal.WeeklyGames.Where(g => !g.Archived)) journal.CurrentWeekly(game, DateTime.UtcNow); }, null)) { Topmost = journal.Settings.AlwaysOnTop; owner.Close(); }
            } catch (Exception e) { MessageBox.Show(owner, e.Message, "Restore failed"); }
        }
    }
    public sealed class GameEditor : Window
    {
        public Game Result;
        readonly string gameId;
        readonly TextBox name;
        readonly TextBox groupName;
        readonly TextBox description;
        readonly bool isWeekly;
        ComboBox resetRule;
        ComboBox resetDay;
        TextBox resetTime;
        StackPanel fixedOptions;
        ComboBox dailyResetRule;
        TextBox dailyResetTime;
        StackPanel dailyUtcOptions;
        TextBox dailyIntervalHours;
        StackPanel dailyIntervalOptions;
        readonly StackPanel rows = UI.Stack();
        readonly System.Collections.Generic.List<LineEditor> lines = new System.Collections.Generic.List<LineEditor>();
        public GameEditor(Window owner, Game game) : this(owner, game, false) { }
        public GameEditor(Window owner, Game game, bool weekly) : this(owner, game, weekly, null) { }
        public GameEditor(Window owner, Game game, bool weekly, string group)
        {
            isWeekly = weekly;
            gameId = game == null ? Guid.NewGuid().ToString("N") : game.Id;
            UI.SetupDialog(this, owner, game == null ? "WrenFeed · Add quest" : "WrenFeed · Edit quest", 510, weekly ? 760 : 730);
            var root = new DockPanel { Margin = new Thickness(24) }; Content = root;
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            var cancel = UI.Button("Cancel", delegate { DialogResult = false; }); cancel.Margin = new Thickness(0, 0, 8, 0); cancel.IsCancel = true; actions.Children.Add(cancel);
            var save = UI.Button("Save quest", Save); UI.Primary(save); save.IsDefault = true; actions.Children.Add(save); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var stack = UI.Stack();
            var title = UI.Text(game == null ? (weekly ? "A new weekly quest" : "A new daily quest") : "Refine your quest", 23, "Ink"); UI.Display(title); stack.Children.Add(title);
            stack.Children.Add(UI.Label("QUEST NAME")); name = UI.Field(game == null ? "" : game.Name, "Quest name", 80); stack.Children.Add(name);
            stack.Children.Add(UI.Label("GAME GROUP  ·  OPTIONAL")); groupName = UI.Field(game == null ? group : game.GroupName, "Game group", 80); stack.Children.Add(groupName);
            var groupHint = UI.Text("Use the same group for related quests, such as OSRS. Each quest keeps its own reset and history.", 11, "Muted"); groupHint.Margin = new Thickness(0, 5, 0, 0); stack.Children.Add(groupHint);
            stack.Children.Add(UI.Label("SHORT DESCRIPTION  ·  OPTIONAL")); description = UI.Field(game == null ? "" : game.Description, "Short description", 220); description.TextWrapping = TextWrapping.Wrap; description.MinHeight = 60; description.AcceptsReturn = true; stack.Children.Add(description);
            if (!weekly)
            {
                stack.Children.Add(UI.Label("DAILY RESET RULE"));
                dailyResetRule = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(dailyResetRule, "Daily reset rule");
                dailyResetRule.Items.Add("Local midnight"); dailyResetRule.Items.Add("Fixed UTC time"); dailyResetRule.Items.Add("Hours after completion");
                dailyResetRule.SelectedIndex = game != null && game.DailyReset == "after-completion" ? 2 : game != null && game.DailyReset == "utc" ? 1 : 0; stack.Children.Add(dailyResetRule);
                dailyUtcOptions = UI.Stack(); dailyUtcOptions.Children.Add(UI.Label("RESET TIME (HH:MM UTC)"));
                dailyResetTime = UI.Field(game == null ? "00:00" : game.DailyHourUtc.ToString("00") + ":" + game.DailyMinuteUtc.ToString("00"), "Daily reset time UTC", 5); dailyUtcOptions.Children.Add(dailyResetTime);
                stack.Children.Add(dailyUtcOptions);
                dailyIntervalOptions = UI.Stack(); dailyIntervalOptions.Children.Add(UI.Label("HOURS AFTER COMPLETION"));
                dailyIntervalHours = UI.Field(game == null || game.DailyIntervalHours == 0 ? "24" : game.DailyIntervalHours.ToString(), "Hours after completion", 5); dailyIntervalOptions.Children.Add(dailyIntervalHours);
                var intervalHint = UI.Text("Complete the quest when you start the timer. For Forma, use 23 hours if you check it after starting the next build.", 11, "Muted"); intervalHint.Margin = new Thickness(0, 7, 0, 0); dailyIntervalOptions.Children.Add(intervalHint);
                stack.Children.Add(dailyIntervalOptions);
                dailyResetRule.SelectionChanged += delegate { dailyUtcOptions.Visibility = dailyResetRule.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed; dailyIntervalOptions.Visibility = dailyResetRule.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed; };
                dailyUtcOptions.Visibility = dailyResetRule.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
                dailyIntervalOptions.Visibility = dailyResetRule.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (weekly)
            {
                stack.Children.Add(UI.Label("WEEKLY RESET RULE"));
                resetRule = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(resetRule, "Weekly reset rule");
                resetRule.Items.Add("Fixed UTC day and time"); resetRule.Items.Add("Seven days after completion (UTC weekday)");
                resetRule.SelectedIndex = game != null && game.WeeklyReset == "completion-weekday" ? 1 : 0; stack.Children.Add(resetRule);
                fixedOptions = UI.Stack();
                fixedOptions.Children.Add(UI.Label("RESET DAY (UTC)")); resetDay = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(resetDay, "Fixed reset day UTC");
                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek))) resetDay.Items.Add(day.ToString());
                resetDay.SelectedIndex = game == null ? 0 : game.FixedWeekdayUtc; fixedOptions.Children.Add(resetDay);
                fixedOptions.Children.Add(UI.Label("RESET TIME (HH:MM UTC)")); resetTime = UI.Field(game == null ? "00:00" : game.FixedHourUtc.ToString("00") + ":" + game.FixedMinuteUtc.ToString("00"), "Fixed reset time UTC", 5); fixedOptions.Children.Add(resetTime);
                stack.Children.Add(fixedOptions);
                var ruleHint = UI.Text("For Tears of Guthix, choose “Seven days after completion.” It becomes available at 00:00 UTC on the same weekday next week.", 11, "Muted"); ruleHint.Margin = new Thickness(0, 8, 0, 0); stack.Children.Add(ruleHint);
                resetRule.SelectionChanged += delegate { fixedOptions.Visibility = resetRule.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed; };
                fixedOptions.Visibility = resetRule.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            stack.Children.Add(UI.Label("ADDITIONAL LINES  ·  OPTIONAL"));
            var info = UI.Text("Use a line for a note or a task. Enable “Checkbox” for tasks you want to check off individually.", 12, "Muted"); info.Margin = new Thickness(0, 0, 0, 12); stack.Children.Add(info); stack.Children.Add(rows);
            if (game != null) foreach (var line in game.Lines) AddLine(line);
            var add = UI.Button("+ Add line", delegate { var line = AddLine(null); line.Text.Focus(); }); add.HorizontalAlignment = HorizontalAlignment.Left; add.Margin = new Thickness(0, 8, 0, 0); stack.Children.Add(add);
            var hint = UI.Text("Completing every checkable task completes the game. Checking the game completes all its tasks.", 11, "Muted"); hint.Margin = new Thickness(0, 20, 0, 0); stack.Children.Add(hint);
            root.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Loaded += delegate { name.Focus(); name.SelectAll(); };
        }
        LineEditor AddLine(QuestLine line)
        {
            var editor = new LineEditor { Id = line == null ? Guid.NewGuid().ToString("N") : line.Id };
            var row = UI.Stack(); row.Margin = new Thickness(0, 0, 0, 10);
            editor.Text = UI.Field(line == null ? "" : line.Text, "Line text", 160); editor.Text.TextWrapping = TextWrapping.Wrap; row.Children.Add(editor.Text);
            var options = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
            var remove = UI.Button("Remove", delegate { rows.Children.Remove(row); lines.Remove(editor); }); remove.FontSize = 11; remove.Padding = new Thickness(8, 2, 8, 2); remove.MinHeight = 27; DockPanel.SetDock(remove, Dock.Right); options.Children.Add(remove);
            editor.Checkable = new CheckBox { Content = UI.Text("Checkbox", 11, "Muted"), IsChecked = line != null && line.Checkable }; options.Children.Add(editor.Checkable); row.Children.Add(options);
            rows.Children.Add(row); lines.Add(editor); return editor;
        }
        void Save()
        {
            if (String.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show(this, "Give this quest a name first.", "Quest name required"); name.Focus(); return; }
            int hour = 0, minute = 0;
            if (isWeekly && resetRule.SelectedIndex == 0)
            {
                var parts = resetTime.Text.Split(':');
                if (parts.Length != 2 || parts[0].Length != 2 || parts[1].Length != 2 || !int.TryParse(parts[0], out hour) || !int.TryParse(parts[1], out minute) || hour > 23 || minute > 59 || hour < 0 || minute < 0)
                { MessageBox.Show(this, "Enter a UTC reset time in 24-hour HH:MM format, such as 00:00 or 18:30.", "Invalid reset time"); resetTime.Focus(); return; }
            }
            int dailyHour = 0, dailyMinute = 0;
            if (!isWeekly && dailyResetRule.SelectedIndex == 1)
            {
                var parts = dailyResetTime.Text.Split(':');
                if (parts.Length != 2 || parts[0].Length != 2 || parts[1].Length != 2 || !int.TryParse(parts[0], out dailyHour) || !int.TryParse(parts[1], out dailyMinute) || dailyHour > 23 || dailyMinute > 59 || dailyHour < 0 || dailyMinute < 0)
                { MessageBox.Show(this, "Enter a UTC reset time in 24-hour HH:MM format.", "Invalid reset time"); dailyResetTime.Focus(); return; }
            }
            int intervalHours = 24;
            if (!isWeekly && dailyResetRule.SelectedIndex == 2 && (!int.TryParse(dailyIntervalHours.Text.Trim(), out intervalHours) || intervalHours < 1 || intervalHours > 8760))
            { MessageBox.Show(this, "Enter a whole number of hours from 1 to 8760.", "Invalid reset interval"); dailyIntervalHours.Focus(); return; }
            Result = new Game { Id = gameId, Name = name.Text.Trim(), GroupName = groupName.Text.Trim(), Description = description.Text.Trim(), Lines = lines.Where(l => !String.IsNullOrWhiteSpace(l.Text.Text)).Select(l => new QuestLine { Id = l.Id, Text = l.Text.Text.Trim(), Checkable = l.Checkable.IsChecked == true }).ToList(), DailyReset = !isWeekly && dailyResetRule.SelectedIndex == 2 ? "after-completion" : !isWeekly && dailyResetRule.SelectedIndex == 1 ? "utc" : "local", DailyHourUtc = dailyHour, DailyMinuteUtc = dailyMinute, DailyIntervalHours = intervalHours, WeeklyReset = isWeekly && resetRule.SelectedIndex == 1 ? "completion-weekday" : "fixed", FixedWeekdayUtc = isWeekly ? resetDay.SelectedIndex : 0, FixedHourUtc = hour, FixedMinuteUtc = minute };
            DialogResult = true;
        }
        sealed class LineEditor { public string Id; public TextBox Text; public CheckBox Checkable; }
    }
}
