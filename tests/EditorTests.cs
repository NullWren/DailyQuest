using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using DailyQuest;

class EditorTests
{
    static IEnumerable<DependencyObject> Walk(DependencyObject root)
    {
        yield return root;
        foreach (object child in LogicalTreeHelper.GetChildren(root))
            if (child is DependencyObject) foreach (var descendant in Walk((DependencyObject)child)) yield return descendant;
    }
    [STAThread] static void Main()
    {
        var app = new Application();
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DailyQuest.Theme.xaml")) app.Resources = (ResourceDictionary)XamlReader.Load(stream);
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var owner = new Window { Opacity = 0, ShowInTaskbar = false, ShowActivated = false }; owner.Show();
        var editor = new GameEditor(owner, null) { Opacity = 0, ShowActivated = false };
        Exception failure = null;
        editor.Loaded += delegate
        {
            try
            {
                var controls = Walk(editor).ToList();
                controls.OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Game name").Text = "  Editor test  ";
                controls.OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Short description").Text = "A short description";
                controls.OfType<Button>().Single(b => (string)b.Content == "+ Add line").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                controls = Walk(editor).ToList();
                controls.OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Line text").Text = "An individual task";
                controls.OfType<CheckBox>().Single().IsChecked = true;
                controls.OfType<Button>().Single(b => (string)b.Content == "Save game").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception e) { failure = e; editor.Close(); }
        };
        bool? saved = editor.ShowDialog();
        if (failure != null) throw failure;
        if (saved != true || editor.Result.Name != "Editor test" || editor.Result.Description != "A short description" || editor.Result.Lines.Count != 1 || !editor.Result.Lines[0].Checkable || editor.Result.Lines[0].Text != "An individual task") throw new Exception("Editor failed to save form data.");
        Console.WriteLine("PASS Editor adds a game, description, and checkable line using its actual controls");
        var revised = new GameEditor(owner, editor.Result) { Opacity = 0, ShowActivated = false };
        revised.Loaded += delegate
        {
            try
            {
                var controls = Walk(revised).ToList();
                if (controls.OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Line text").Text != "An individual task") throw new Exception("Editor did not reload the line.");
                controls.OfType<CheckBox>().Single().IsChecked = false;
                controls.OfType<Button>().Single(b => (string)b.Content == "Save game").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception e) { failure = e; revised.Close(); }
        };
        saved = revised.ShowDialog();
        if (failure != null) throw failure;
        if (saved != true || revised.Result.Id != editor.Result.Id || revised.Result.Lines[0].Id != editor.Result.Lines[0].Id || revised.Result.Lines[0].Checkable) throw new Exception("Edit did not preserve IDs and convert task to note.");
        Console.WriteLine("PASS Editor reopens saved game, keeps stable IDs, and converts task to plain note");
        var weekly = new GameEditor(owner, null, true) { Opacity = 0, ShowActivated = false };
        weekly.Loaded += delegate
        {
            try
            {
                var controls = Walk(weekly).ToList();
                controls.OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Game name").Text = "Tears of Guthix";
                controls.OfType<ComboBox>().Single(c => AutomationProperties.GetName(c) == "Weekly reset rule").SelectedIndex = 1;
                controls.OfType<Button>().Single(b => (string)b.Content == "Save game").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception e) { failure = e; weekly.Close(); }
        };
        saved = weekly.ShowDialog();
        if (failure != null) throw failure;
        if (saved != true || weekly.Result.WeeklyReset != "completion-weekday") throw new Exception("Weekly editor did not save completion-based rule.");
        Console.WriteLine("PASS Weekly editor saves completion-based UTC reset rule");
        var fixedWeekly = new GameEditor(owner, weekly.Result, true) { Opacity = 0, ShowActivated = false };
        fixedWeekly.Loaded += delegate
        {
            try
            {
                var controls = Walk(fixedWeekly).ToList();
                controls.OfType<ComboBox>().Single(c => AutomationProperties.GetName(c) == "Weekly reset rule").SelectedIndex = 0;
                controls.OfType<ComboBox>().Single(c => AutomationProperties.GetName(c) == "Fixed reset day UTC").SelectedIndex = (int)DayOfWeek.Thursday;
                controls.OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Fixed reset time UTC").Text = "18:30";
                controls.OfType<Button>().Single(b => (string)b.Content == "Save game").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception e) { failure = e; fixedWeekly.Close(); }
        };
        saved = fixedWeekly.ShowDialog();
        if (failure != null) throw failure;
        if (saved != true || fixedWeekly.Result.WeeklyReset != "fixed" || fixedWeekly.Result.FixedWeekdayUtc != (int)DayOfWeek.Thursday || fixedWeekly.Result.FixedHourUtc != 18 || fixedWeekly.Result.FixedMinuteUtc != 30) throw new Exception("Weekly editor did not save fixed schedule.");
        Console.WriteLine("PASS Weekly editor saves fixed UTC day and time");
        var windowJournal = new Journal();
        var sample = new Game { Name = "Weekly window test", WeeklyReset = "completion-weekday" };
        windowJournal.SaveWeeklyGame(sample, DateTime.UtcNow);
        using (var storage = new Storage(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-weekly-ui-" + Guid.NewGuid().ToString("N"))))
        {
            var window = new QuestWindow(storage, windowJournal) { Opacity = 0, ShowActivated = false };
            window.Show();
            Walk(window).OfType<Button>().Single(b => (string)b.Content == "Weekly").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (!Walk(window).OfType<TextBlock>().Any(t => t.Text == "Your weekly quests") || !Walk(window).OfType<TextBlock>().Any(t => t.Text == "Weekly window test")) throw new Exception("Weekly tab did not show saved game.");
            var complete = Walk(window).OfType<CheckBox>().Single(c => AutomationProperties.GetName(c) == "Complete Weekly window test");
            complete.IsChecked = true; complete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (!windowJournal.CurrentWeekly(sample, DateTime.UtcNow).Result.Done) throw new Exception("Weekly game checkbox did not save completion.");
            Console.WriteLine("PASS Weekly tab renders and saves game completion");
            window.Close();
        }
        owner.Close(); app.Shutdown();
    }
}
