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
        owner.Close(); app.Shutdown();
    }
}
