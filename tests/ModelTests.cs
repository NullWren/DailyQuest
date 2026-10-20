using System;
using System.IO;
using System.Linq;
using DailyQuest;

class ModelTests
{
    static int count;
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); count++; Console.WriteLine("PASS " + message); }
    static DateTime Local(int day, int hour) { return new DateTime(2026, 10, day, hour, 0, 0, DateTimeKind.Local); }
    static DateTime Utc = new DateTime(2026, 10, 20, 5, 0, 0, DateTimeKind.Utc);
    static Game Game()
    {
        return new Game { Name = "Test game", Description = "Quick daily run", Lines = new System.Collections.Generic.List<QuestLine> {
            new QuestLine { Text = "First task", Checkable = true }, new QuestLine { Text = "Second task", Checkable = true }, new QuestLine { Text = "A plain note" } } };
    }
    static void Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]); Directory.CreateDirectory(root);
        var journal = new Journal(); var game = Game(); var now = Local(19, 23);
        journal.SaveGame(game, now, Utc); var day = journal.Today(now); var result = day.Games.Single();
        Assert(!result.Done && result.Lines.All(l => !l.Done), "New quests start incomplete");
        journal.ToggleLine(game.Id, game.Lines[0].Id, true, now, Utc);
        Assert(!result.Done, "One subtask leaves game incomplete");
        journal.ToggleLine(game.Id, game.Lines[1].Id, true, now, Utc);
        Assert(result.Done && result.CompletedUtc != null, "All checkable subtasks complete game; notes ignored");
        journal.ToggleLine(game.Id, game.Lines[0].Id, false, now, Utc);
        Assert(!result.Done && result.CompletedUtc == null, "Unchecking a subtask reopens the game");
        journal.ToggleGame(game.Id, true, now, Utc);
        Assert(result.Lines.Where(l => l.Checkable).All(l => l.Done), "Game completion checks every subtask");
        Assert(!result.Lines.Last().Done, "Notes never become completion tasks");
        journal.ToggleGame(game.Id, false, now, Utc);
        Assert(result.Lines.All(l => !l.Done && l.CompletedUtc == null), "Reopening game clears its subtask completions");
        journal.ToggleGame(game.Id, true, now, Utc);
        var tomorrow = journal.Today(Local(20, 0));
        Assert(tomorrow.Date == "2026-10-20" && !tomorrow.Games.Single().Done, "Local midnight creates fresh checklist");
        Assert(day.Games.Single().Done && day.Games.Single().Lines[0].Done, "Midnight preserves previous day and subtask history");
        Assert(!Object.ReferenceEquals(day.Games[0], tomorrow.Games[0]), "Daily snapshots are independent objects");
        var edited = Storage.Decode(Storage.Encode(journal)).Games.Single(); edited.Name = "Renamed game"; edited.Lines[0].Text = "Edited task";
        journal.SaveGame(edited, Local(20, 2), Utc);
        Assert(day.Games[0].Name == "Test game" && day.Games[0].Lines[0].Text == "First task", "Editing keeps past names and task descriptions");
        Assert(tomorrow.Games[0].Name == "Renamed game", "Editing updates current day");
        journal.ToggleGame(game.Id, true, Local(20, 2), Utc);
        edited.Lines.Add(new QuestLine { Text = "Added task", Checkable = true }); journal.SaveGame(edited, Local(20, 2), Utc);
        Assert(!tomorrow.Games[0].Done && tomorrow.Games[0].Lines[0].Done, "Adding a new task reopens game and preserves existing progress");
        journal.Archive(game.Id, Local(20, 2));
        Assert(tomorrow.Games[0].Archived && !day.Games[0].Archived, "Archiving preserves old snapshots and current progress");
        Assert(journal.Today(Local(21, 0)).Games.Count == 0, "Archived games do not recur tomorrow");
        journal.Restore(game.Id, Local(21, 1), Utc);
        Assert(journal.Today(Local(21, 1)).Games.Count == 1 && !journal.Games[0].Archived, "Archived game can be restored");
        journal.Today(Local(24, 10));
        Assert(!journal.Days.Any(d => d.Date == "2026-10-22" || d.Date == "2026-10-23"), "Closed days are not fabricated as completed");
        Assert(journal.Today(Local(19, 10)).Games[0].Done, "Returning to an existing local date retains that day's state");
        var simple = new Game { Name = "Simple game", Description = "One short match" }; journal.SaveGame(simple, now, Utc); journal.ToggleGame(simple.Id, true, now, Utc);
        Assert(journal.Today(now).Games.First(g => g.Id == simple.Id).Done, "Game with no subtasks can be completed");
        simple.Description = "Edited note"; journal.SaveGame(simple, now, Utc);
        Assert(journal.Today(now).Games.First(g => g.Id == simple.Id).Done, "Editing notes preserves simple game completion");
        journal.Move(simple.Id, -1); Assert(journal.Games[0].Id == simple.Id, "Game ordering persists independently of history");
        var cloned = Storage.Decode(Storage.Encode(journal)); Assert(cloned.Days.Count == journal.Days.Count && cloned.Games[0].Id == simple.Id, "JSON round trip retains history and stable IDs");
        var weekly = new Game { Name = "Tears of Guthix", WeeklyReset = "completion-weekday", Lines = new System.Collections.Generic.List<QuestLine> { new QuestLine { Text = "Collect tears", Checkable = true }, new QuestLine { Text = "Claim XP", Checkable = true } } };
        var sundayLate = new DateTime(2026, 10, 18, 23, 45, 0, DateTimeKind.Utc);
        journal.SaveWeeklyGame(weekly, sundayLate);
        var firstWeekly = journal.CurrentWeekly(weekly, sundayLate);
        journal.ToggleWeeklyLine(weekly.Id, weekly.Lines[0].Id, true, sundayLate);
        Assert(!firstWeekly.Result.Done, "Weekly subtask alone does not complete quest");
        journal.ToggleWeeklyGame(weekly.Id, true, sundayLate);
        Assert(firstWeekly.Result.Lines.All(l => l.Done), "Completing weekly game checks all subtasks");
        Assert(Journal.NextWeeklyReset(weekly, firstWeekly) == new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), "Completion-weekday resets at UTC midnight seven dates later");
        Assert(journal.CurrentWeekly(weekly, new DateTime(2026, 10, 24, 23, 59, 0, DateTimeKind.Utc)).Id == firstWeekly.Id, "Weekly quest stays completed until exact UTC reset");
        var secondWeekly = journal.CurrentWeekly(weekly, new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc));
        Assert(secondWeekly.Id != firstWeekly.Id && !secondWeekly.Result.Done && firstWeekly.Result.Done, "Weekly rollover preserves completed history and resets progress");
        var fixedGame = new Game { Name = "Fixed event", WeeklyReset = "fixed", FixedWeekdayUtc = (int)DayOfWeek.Thursday, FixedHourUtc = 18, FixedMinuteUtc = 30 };
        Assert(Journal.FixedCycleStart(fixedGame, new DateTime(2026, 10, 22, 18, 29, 0, DateTimeKind.Utc)) == new DateTime(2026, 10, 15, 18, 30, 0, DateTimeKind.Utc), "Fixed weekly event waits until configured UTC time");
        Assert(Journal.FixedCycleStart(fixedGame, new DateTime(2026, 10, 22, 18, 30, 0, DateTimeKind.Utc)) == new DateTime(2026, 10, 22, 18, 30, 0, DateTimeKind.Utc), "Fixed weekly event resets at configured UTC instant");
        journal.SaveWeeklyGame(fixedGame, new DateTime(2026, 10, 22, 18, 29, 0, DateTimeKind.Utc));
        var oldFixed = journal.CurrentWeekly(fixedGame, new DateTime(2026, 10, 22, 18, 29, 0, DateTimeKind.Utc));
        journal.ToggleWeeklyGame(fixedGame.Id, true, new DateTime(2026, 10, 22, 18, 29, 0, DateTimeKind.Utc));
        Assert(!journal.CurrentWeekly(fixedGame, new DateTime(2026, 10, 22, 18, 30, 0, DateTimeKind.Utc)).Result.Done && oldFixed.Result.Done, "Fixed weekly rollover archives prior completion");
        var weeklyCopy = Storage.Decode(Storage.Encode(journal));
        Assert(weeklyCopy.WeeklyRecords.Count == journal.WeeklyRecords.Count && weeklyCopy.WeeklyGames.Count == 2, "Weekly schedules and history survive JSON round trip");
        weeklyCopy.ArchiveWeekly(weekly.Id, new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc));
        Assert(weeklyCopy.WeeklyGames.First(g => g.Id == weekly.Id).Archived && firstWeekly.Result.Done, "Archiving weekly quest preserves completed history");
        weeklyCopy.RestoreWeekly(weekly.Id, new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc));
        Assert(!weeklyCopy.WeeklyGames.First(g => g.Id == weekly.Id).Archived, "Archived weekly quest can be restored");
        var legacy = new Journal { Version = 1 }; var migrated = Storage.Decode(Storage.Encode(legacy));
        Assert(migrated.Version == 2 && migrated.WeeklyGames.Count == 0 && migrated.WeeklyRecords.Count == 0, "Version 1 daily journal migrates to weekly-capable version 2");
        bool refused = false; cloned.Version = 99; try { Storage.Decode(Storage.Encode(cloned)); } catch (InvalidDataException) { refused = true; }
        Assert(refused, "Future schema versions are refused without overwrite");
        using (var store = new Storage(root))
        {
            store.Save(journal); Assert(File.Exists(store.DataPath), "First save writes journal");
            journal.Games[0].Name = "Updated"; store.Save(journal);
            Assert(Storage.Decode(File.ReadAllBytes(store.BackupPath)).Games[0].Name != "Updated", "Atomic save keeps previous-save backup");
            Assert(store.Load().Games[0].Name == "Updated", "Progress survives reopening storage");
            bool locked = false; try { using (var second = new Storage(root)) {} } catch (IOException) { locked = true; }
            Assert(locked, "Second instance cannot overwrite active journal");
            File.WriteAllText(store.DataPath, "{broken"); var recovered = store.Load();
            Assert(recovered.Games[0].Name != "Updated" && store.RecoveryNotice != null, "Unreadable journal recovers previous backup");
            Assert(Directory.GetFiles(root, "journal.unreadable-*.json").Length == 1, "Unreadable original retained for recovery");
        }
        using (var store = new Storage(root)) Assert(store.Load().Days.Count == journal.Days.Count, "Lock released on close and recovered journal persists");
        // UI fixture uses its own directory, never the user's journal.
        var demo = new Journal(); var demoNow = DateTime.Now;
        var g1 = new Game { Name = "Guild Wars 2", Description = "A little progress in Tyria", Lines = new System.Collections.Generic.List<QuestLine> { new QuestLine { Text = "Daily objectives", Checkable = true }, new QuestLine { Text = "Gather at the home instance", Checkable = true }, new QuestLine { Text = "Keep it short — about 10 minutes" } } };
        var g2 = new Game { Name = "Final Fantasy XIV", Description = "One roulette, then back to the adventure" };
        var g3 = new Game { Name = "Honkai: Star Rail", Description = "Daily training + a quick energy spend" };
        demo.SaveGame(g1, demoNow.AddDays(-1), Utc); demo.SaveGame(g2, demoNow.AddDays(-1), Utc); demo.SaveGame(g3, demoNow.AddDays(-1), Utc);
        demo.ToggleGame(g1.Id, true, demoNow.AddDays(-1), Utc); demo.ToggleGame(g2.Id, true, demoNow.AddDays(-1), Utc);
        demo.Today(demoNow); demo.ToggleLine(g1.Id, g1.Lines[0].Id, true, demoNow, Utc); demo.ToggleGame(g3.Id, true, demoNow, Utc);
        using (var fixture = new Storage(Path.Combine(root, "ui-fixture"))) fixture.Save(demo);
        Console.WriteLine(count + " assertions passed. UI fixture: " + Path.Combine(root, "ui-fixture"));
    }
}
