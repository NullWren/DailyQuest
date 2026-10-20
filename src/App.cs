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
                    MessageBox.Show("The action could not be completed.\n\n" + e.Exception.Message, "Daily Quest", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                };
                app.Run(new QuestWindow(storage, journal));
            }
            catch (Exception error) { MessageBox.Show(error.Message, "Daily Quest could not open", MessageBoxButton.OK, MessageBoxImage.Warning); }
            finally { if (storage != null) storage.Dispose(); }
        }
    }
    public static class UI
    {
        public static Brush Brush(string key) { return (Brush)Application.Current.FindResource(key); }
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
        public static Border Card(UIElement child) { return new Border { Background = Brush("Panel"), BorderBrush = Brush("Edge"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(14), Child = child }; }
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
        Journal journal;
        readonly DockPanel root = new DockPanel();
        readonly StackPanel cards = new StackPanel();
        readonly TextBlock heading = UI.Text("", 24, "Ink");
        readonly TextBlock summary = UI.Text("", 12, "Muted");
        readonly TextBlock footer = UI.Text("Saved on this device · resets at midnight", 11, "Muted");
        readonly Border fill = new Border { Height = 3, HorizontalAlignment = HorizontalAlignment.Left };
        readonly Border track = new Border { Height = 3 };
        readonly StackPanel dateArea = new StackPanel();
        readonly Button todayButton;
        readonly Button historyButton;
        readonly Button addButton;
        readonly DispatcherTimer clock = new DispatcherTimer();
        string activeDate;
        bool history;
        DateTime historyDate = DateTime.Today;
        double fraction;
        public QuestWindow(Storage storage, Journal journal)
        {
            SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
            this.storage = storage; this.journal = journal;
            Title = "Daily Quest";
            Width = Math.Max(350, Math.Min(journal.Settings.Width, SystemParameters.WorkArea.Width));
            Height = Math.Max(440, Math.Min(journal.Settings.Height, SystemParameters.WorkArea.Height - 35));
            MinWidth = 350; MinHeight = 440; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = journal.Settings.AlwaysOnTop;
            root.Margin = new Thickness(20, 18, 20, 16); Content = root;
            var top = UI.Stack(); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
            var masthead = new DockPanel();
            var settings = UI.Button("⚙", OpenSettings); settings.FontSize = 17; settings.Width = 34; settings.Padding = new Thickness(0); settings.ToolTip = "Settings (Ctrl+,)"; AutomationProperties.SetName(settings, "Settings");
            DockPanel.SetDock(settings, Dock.Right); masthead.Children.Add(settings);
            var brand = UI.Text("◇  D A I L Y  Q U E S T", 11, "Accent"); brand.VerticalAlignment = VerticalAlignment.Center; masthead.Children.Add(brand); top.Children.Add(masthead);
            heading.FontFamily = new FontFamily("Georgia"); heading.Margin = new Thickness(0, 17, 0, 6); top.Children.Add(heading);
            summary.Margin = new Thickness(0, 0, 0, 12); top.Children.Add(summary);
            track.Background = UI.Brush("Edge"); fill.Background = UI.Brush("Accent"); track.Child = fill; top.Children.Add(track); track.SizeChanged += delegate { fill.Width = Math.Max(0, track.ActualWidth * fraction); };
            var tabs = new Grid { Margin = new Thickness(0, 16, 0, 14) };
            tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            todayButton = UI.Button("Today", delegate { history = false; Render(null); }); todayButton.Margin = new Thickness(0, 0, 6, 0);
            historyButton = UI.Button("History", delegate { history = true; historyDate = DateTime.Today; Render(null); }); historyButton.Margin = new Thickness(0, 0, 10, 0);
            addButton = UI.Button("+ Game", delegate { EditGame(null); }); UI.Primary(addButton); addButton.ToolTip = "Add game (Ctrl+N)";
            Grid.SetColumn(historyButton, 1); Grid.SetColumn(addButton, 2); tabs.Children.Add(todayButton); tabs.Children.Add(historyButton); tabs.Children.Add(addButton); top.Children.Add(tabs);
            top.Children.Add(dateArea);
            var bottom = new Border { BorderBrush = UI.Brush("Edge"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 12, 0, 0), Margin = new Thickness(0, 12, 0, 0), Child = footer };
            DockPanel.SetDock(bottom, Dock.Bottom); root.Children.Add(bottom);
            var scroll = new ScrollViewer { Content = cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; root.Children.Add(scroll);
            activeDate = Journal.DateKey(DateTime.Now); journal.Today(DateTime.Now); storage.Save(journal);
            clock.Interval = TimeSpan.FromSeconds(3); clock.Tick += delegate { RefreshDay(); }; clock.Start();
            Activated += delegate { RefreshDay(); };
            Closing += delegate
            {
                clock.Stop();
                try { if (WindowState == WindowState.Normal) { journal.Settings.Width = Width; journal.Settings.Height = Height; } storage.Save(journal); }
                catch (Exception e) { MessageBox.Show(this, "Could not save window preferences. Previously saved progress is still on disk.\n\n" + e.Message, "Daily Quest"); }
            };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (Keyboard.Modifiers != ModifierKeys.Control) return;
                if (e.Key == Key.N) { EditGame(null); e.Handled = true; }
                if (e.Key == Key.H) { history = !history; Render(null); e.Handled = true; }
                if (e.Key == Key.OemComma) { OpenSettings(); e.Handled = true; }
            };
            Render(null);
            Loaded += delegate { if (storage.RecoveryNotice != null) MessageBox.Show(this, storage.RecoveryNotice, "Journal recovered", MessageBoxButton.OK, MessageBoxImage.Warning); };
        }
        bool RefreshDay()
        {
            string date = Journal.DateKey(DateTime.Now);
            if (date == activeDate) return false;
            if (Change(delegate { journal.Today(DateTime.Now); }, null)) { activeDate = date; footer.Text = "A new day. Your next quests await."; }
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
            todayButton.BorderBrush = UI.Brush(history ? "Edge" : "Accent"); historyButton.BorderBrush = UI.Brush(history ? "Accent" : "Edge");
            addButton.Visibility = history ? Visibility.Collapsed : Visibility.Visible;
            var day = journal.Days.FirstOrDefault(d => d.Date == Journal.DateKey(history ? historyDate : DateTime.Now));
            var entries = day == null ? new System.Collections.Generic.List<GameResult>() : day.Games.Where(g => history || !g.Archived).ToList();
            if (!history) entries = entries.OrderBy(g => journal.Games.FindIndex(t => t.Id == g.Id)).ToList();
            int done = entries.Count(g => g.Done);
            fraction = entries.Count == 0 ? 0 : (double)done / entries.Count; fill.Width = Math.Max(0, track.ActualWidth * fraction);
            fill.Background = UI.Brush(entries.Count > 0 && done == entries.Count ? "Success" : "Accent");
            heading.Text = history ? "The adventure so far" : "Your daily quests";
            summary.Text = (history ? historyDate : DateTime.Today).ToString("ddd, MMM d") + "   ·   " + done + " / " + entries.Count + " games complete";
            footer.Text = "Saved on this device · resets at midnight";
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
                var title = UI.Text(history ? "No quests recorded" : "Every adventure starts small", 18, "Ink"); title.FontFamily = new FontFamily("Georgia"); title.TextAlignment = TextAlignment.Center; title.Margin = new Thickness(0, 14, 0, 10); empty.Children.Add(title);
                var copy = UI.Text(history ? "There is no saved checklist for this date. Days when the app was closed are not marked as completed." : "Add a game and the little daily tasks worth coming back for.", 13, "Muted"); copy.TextAlignment = TextAlignment.Center; empty.Children.Add(copy);
                if (!history) { var add = UI.Button("+ Add your first game", delegate { EditGame(null); }); UI.Primary(add); add.Margin = new Thickness(0, 22, 0, 0); add.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(add); }
                cards.Children.Add(UI.Card(empty));
            }
            foreach (var game in entries) cards.Children.Add(GameCard(game, flash == game.Id));
            if (!history && entries.Count > 0 && done == entries.Count)
            {
                var finished = UI.Text("✦  Daily quests complete. Well played.", 12, "Success"); finished.Margin = new Thickness(0, 10, 0, 8); finished.TextAlignment = TextAlignment.Center; cards.Children.Add(finished);
            }
        }
        Border GameCard(GameResult game, bool flash)
        {
            var content = UI.Stack(); var header = new DockPanel();
            if (!history)
            {
                var menuButton = UI.Button("···", delegate { }); menuButton.Padding = new Thickness(5, 0, 5, 0); menuButton.MinHeight = 26; menuButton.Width = 29; menuButton.FontSize = 17; menuButton.ToolTip = "Edit or reorder " + game.Name; AutomationProperties.SetName(menuButton, "Options for " + game.Name);
                var menu = new ContextMenu();
                Action<string, Action> item = delegate(string label, Action action) { var mi = new MenuItem { Header = label }; mi.Click += delegate { action(); }; menu.Items.Add(mi); };
                item("Edit game", delegate { EditGame(journal.Games.First(g => g.Id == game.Id)); });
                item("Move up", delegate { Change(delegate { journal.Move(game.Id, -1); }, null); });
                item("Move down", delegate { Change(delegate { journal.Move(game.Id, 1); }, null); });
                menu.Items.Add(new Separator());
                item("Archive game", delegate
                {
                    if (MessageBox.Show(this, "Remove “" + game.Name + "” from your daily list?\n\nIts history will stay saved. You can restore it in Settings.", "Archive game", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Change(delegate { journal.Archive(game.Id, DateTime.Now); }, null);
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
                if (Change(delegate { journal.ToggleGame(game.Id, done, DateTime.Now, DateTime.UtcNow); }, done ? game.Id : null)) footer.Text = done ? "Quest complete. Nicely done." : "Quest reopened.";
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
                            Change(delegate { journal.ToggleLine(game.Id, lineId, lineCheck.IsChecked == true, DateTime.Now, DateTime.UtcNow); }, game.Id);
                        };
                        subs.Children.Add(lineCheck);
                    }
                    else { var note = UI.Text("·  " + line.Text, 12, "Muted"); note.Margin = new Thickness(0, 6, 0, 6); subs.Children.Add(note); }
                }
                content.Children.Add(new Border { Margin = new Thickness(29, 10, 0, 0), BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = UI.Brush("Edge"), Padding = new Thickness(12, 0, 0, 0), Child = subs });
            }
            if (game.Archived) { var archived = UI.Text("Archived from daily list", 10, "Muted"); archived.Margin = new Thickness(29, 8, 0, 0); content.Children.Add(archived); }
            var card = UI.Card(content); card.Margin = new Thickness(0, 0, 0, 10);
            if (game.Done) card.BorderBrush = new SolidColorBrush(Color.FromRgb(64, 89, 78));
            if (flash && journal.Settings.Animate)
            {
                var brush = new SolidColorBrush(((SolidColorBrush)card.BorderBrush).Color); card.BorderBrush = brush;
                brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(((SolidColorBrush)UI.Brush("Accent")).Color, brush.Color, TimeSpan.FromMilliseconds(650)));
            }
            return card;
        }
        void EditGame(Game game)
        {
            var editor = new GameEditor(this, game);
            if (editor.ShowDialog() == true)
            {
                history = false;
                Change(delegate { journal.SaveGame(editor.Result, DateTime.Now, DateTime.UtcNow); }, null);
            }
        }
        void OpenSettings()
        {
            var dialog = new Window(); UI.SetupDialog(dialog, this, "Daily Quest · Settings", 450, 580);
            var stack = UI.Stack(); stack.Margin = new Thickness(24);
            var title = UI.Text("Make yourself at home", 22, "Ink"); title.FontFamily = new FontFamily("Georgia"); stack.Children.Add(title);
            var intro = UI.Text("A quiet little companion for your daily games.", 12, "Muted"); intro.Margin = new Thickness(0, 8, 0, 20); stack.Children.Add(intro);
            var pin = new CheckBox { Content = "Keep this window on top", IsChecked = journal.Settings.AlwaysOnTop, Margin = new Thickness(0, 0, 0, 10) };
            pin.Click += delegate { if (Change(delegate { journal.Settings.AlwaysOnTop = pin.IsChecked == true; }, null)) Topmost = journal.Settings.AlwaysOnTop; else pin.IsChecked = journal.Settings.AlwaysOnTop; };
            var animate = new CheckBox { Content = "Subtle completion animations", IsChecked = journal.Settings.Animate };
            animate.Click += delegate { if (!Change(delegate { journal.Settings.Animate = animate.IsChecked == true; }, null)) animate.IsChecked = journal.Settings.Animate; };
            stack.Children.Add(pin); stack.Children.Add(animate);
            stack.Children.Add(UI.Label("DAILY RESET")); stack.Children.Add(UI.Text("Local midnight. Progress saves immediately, and previous days remain in History.", 12, "Muted"));
            stack.Children.Add(UI.Label("YOUR DATA")); stack.Children.Add(UI.Text("Stored only on this PC. No account or internet needed. A previous-save backup is kept automatically.", 12, "Muted"));
            var buttons = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
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
                if (archived.Children.Count == 0) archived.Children.Add(UI.Text("No archived games.", 12, "Muted"));
            }; renderArchived();
            var close = UI.Button("Done", delegate { dialog.Close(); }); close.HorizontalAlignment = HorizontalAlignment.Right; close.Margin = new Thickness(0, 22, 0, 0); UI.Primary(close); stack.Children.Add(close);
            dialog.Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            dialog.ShowDialog();
        }
        void Export(Window owner)
        {
            var picker = new SaveFileDialog { Filter = "Daily Quest backup (*.json)|*.json", FileName = "DailyQuest-" + DateTime.Today.ToString("yyyy-MM-dd") + ".json", OverwritePrompt = true };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                if (IsLiveFile(picker.FileName)) throw new IOException("Choose a location outside the live journal files.");
                File.WriteAllBytes(picker.FileName, Storage.Encode(journal)); MessageBox.Show(owner, "Your games, settings, and daily history have been exported.", "Backup exported");
            } catch (Exception e) { MessageBox.Show(owner, e.Message, "Export failed"); }
        }
        bool IsLiveFile(string path)
        {
            return String.Equals(Path.GetFullPath(path), storage.DataPath, StringComparison.OrdinalIgnoreCase) || String.Equals(Path.GetFullPath(path), storage.BackupPath, StringComparison.OrdinalIgnoreCase);
        }
        void Import(Window owner)
        {
            var picker = new OpenFileDialog { Filter = "Daily Quest backup (*.json)|*.json", CheckFileExists = true };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                var restored = Storage.Decode(File.ReadAllBytes(picker.FileName));
                if (MessageBox.Show(owner, "Replace your current journal with this backup?\n\n" + restored.Games.Count(g => !g.Archived) + " active games · " + restored.Days.Count + " recorded days\n\nA separate copy of your current journal will be kept in the data folder.", "Restore backup", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                File.WriteAllBytes(Path.Combine(storage.Folder, "before-restore-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff") + ".json"), Storage.Encode(journal));
                if (Change(delegate { journal = restored; journal.Today(DateTime.Now); }, null)) { Topmost = journal.Settings.AlwaysOnTop; owner.Close(); }
            } catch (Exception e) { MessageBox.Show(owner, e.Message, "Restore failed"); }
        }
    }
    public sealed class GameEditor : Window
    {
        public Game Result;
        readonly string gameId;
        readonly TextBox name;
        readonly TextBox description;
        readonly StackPanel rows = UI.Stack();
        readonly System.Collections.Generic.List<LineEditor> lines = new System.Collections.Generic.List<LineEditor>();
        public GameEditor(Window owner, Game game)
        {
            gameId = game == null ? Guid.NewGuid().ToString("N") : game.Id;
            UI.SetupDialog(this, owner, game == null ? "Daily Quest · Add game" : "Daily Quest · Edit game", 510, 630);
            var root = new DockPanel { Margin = new Thickness(24) }; Content = root;
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            var cancel = UI.Button("Cancel", delegate { DialogResult = false; }); cancel.Margin = new Thickness(0, 0, 8, 0); cancel.IsCancel = true; actions.Children.Add(cancel);
            var save = UI.Button("Save game", Save); UI.Primary(save); save.IsDefault = true; actions.Children.Add(save); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var stack = UI.Stack();
            var title = UI.Text(game == null ? "A new daily quest" : "Refine your quest", 23, "Ink"); title.FontFamily = new FontFamily("Georgia"); stack.Children.Add(title);
            stack.Children.Add(UI.Label("GAME NAME")); name = UI.Field(game == null ? "" : game.Name, "Game name", 80); stack.Children.Add(name);
            stack.Children.Add(UI.Label("SHORT DESCRIPTION  ·  OPTIONAL")); description = UI.Field(game == null ? "" : game.Description, "Short description", 220); description.TextWrapping = TextWrapping.Wrap; description.MinHeight = 60; description.AcceptsReturn = true; stack.Children.Add(description);
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
            if (String.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show(this, "Give this game a name first.", "Game name required"); name.Focus(); return; }
            Result = new Game { Id = gameId, Name = name.Text.Trim(), Description = description.Text.Trim(), Lines = lines.Where(l => !String.IsNullOrWhiteSpace(l.Text.Text)).Select(l => new QuestLine { Id = l.Id, Text = l.Text.Text.Trim(), Checkable = l.Checkable.IsChecked == true }).ToList() };
            DialogResult = true;
        }
        sealed class LineEditor { public string Id; public TextBox Text; public CheckBox Checkable; }
    }
}
