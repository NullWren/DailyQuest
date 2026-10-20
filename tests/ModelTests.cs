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
        weekly.GroupName = "OSRS"; fixedGame.GroupName = "OSRS";
        journal.SaveWeeklyGame(weekly, new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc));
        journal.SaveWeeklyGame(fixedGame, new DateTime(2026, 10, 22, 18, 30, 0, DateTimeKind.Utc));
        Assert(journal.CurrentWeekly(weekly, new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc)).Result.GroupName == "OSRS" && journal.CurrentWeekly(fixedGame, new DateTime(2026, 10, 22, 18, 30, 0, DateTimeKind.Utc)).Result.GroupName == "OSRS", "Independent weekly rules can share one game group");
        Assert(firstWeekly.Result.GroupName == "" && oldFixed.Result.GroupName == "", "Grouping does not rewrite past history");
        var utcDaily = new Game { Name = "Sortie", GroupName = "Warframe", DailyReset = "utc", DailyHourUtc = 17 };
        var beforeReset = new DateTime(2026, 10, 20, 16, 59, 0, DateTimeKind.Utc);
        journal.SaveGame(utcDaily, beforeReset.ToLocalTime(), beforeReset);
        var utcOld = journal.CurrentDailyUtc(utcDaily, beforeReset);
        journal.ToggleGame(utcDaily.Id, true, beforeReset.ToLocalTime(), beforeReset);
        Assert(Journal.DailyCycleStart(utcDaily, beforeReset) == new DateTime(2026, 10, 19, 17, 0, 0, DateTimeKind.Utc), "UTC daily waits until its configured reset hour");
        var afterReset = beforeReset.AddMinutes(1);
        var utcNew = journal.CurrentDailyUtc(utcDaily, afterReset);
        Assert(utcNew.Id != utcOld.Id && !utcNew.Result.Done && utcOld.Result.Done, "UTC daily rollover preserves history and resets only that quest");
        Assert(!journal.Today(afterReset.ToLocalTime()).Games.Any(g => g.Id == utcDaily.Id), "UTC daily quest is not duplicated in local daily snapshots");
        var utcCopy = Storage.Decode(Storage.Encode(journal));
        Assert(utcCopy.DailyUtcRecords.Count == 2 && utcCopy.Games.First(g => g.Id == utcDaily.Id).GroupName == "Warframe", "UTC daily schedules and game groups survive JSON round trip");
        var forma = new Game { Name = "Forma", GroupName = "Warframe", DailyReset = "after-completion", DailyIntervalHours = 23 };
        // Keep the next hour on the same local date on both developer PCs and UTC CI runners.
        var builtAt = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Local).ToUniversalTime();
        journal.SaveGame(forma, builtAt.ToLocalTime(), builtAt);
        var firstForma = journal.CurrentDailyInterval(forma, builtAt);
        journal.ToggleGame(forma.Id, true, builtAt.ToLocalTime(), builtAt);
        Assert(Journal.NextDailyIntervalReset(forma, firstForma) == builtAt.AddHours(23), "Forma becomes ready 23 hours after completion");
        Assert(journal.CurrentDailyInterval(forma, builtAt.AddHours(22).AddMinutes(59)).Id == firstForma.Id, "Rolling quest stays completed until the exact interval ends");
        Assert(journal.CurrentDailyInterval(forma, builtAt.AddHours(23)).Id != firstForma.Id && !journal.CurrentDailyInterval(forma, builtAt.AddHours(23)).Result.Done && firstForma.Result.Done, "Rolling reset opens a new quest and preserves the previous completion");
        var secondForma = journal.CurrentDailyInterval(forma, builtAt.AddHours(23));
        Assert(journal.CurrentDailyInterval(forma, builtAt.AddDays(4)).Id == secondForma.Id, "Uncompleted rolling quests do not fabricate missed cycles");
        var reopened = Storage.Decode(Storage.Encode(journal));
        var reopenedForma = reopened.Games.First(g => g.Id == forma.Id);
        Assert(reopenedForma.DailyIntervalHours == 23 && reopened.CurrentDailyInterval(reopenedForma, builtAt.AddDays(4)).Id == secondForma.Id, "Rolling interval and current cycle survive reopening");
        reopened.ToggleGame(forma.Id, true, builtAt.AddDays(4).ToLocalTime(), builtAt.AddDays(4));
        reopened.ToggleGame(forma.Id, false, builtAt.AddDays(4).ToLocalTime(), builtAt.AddDays(4));
        Assert(Journal.NextDailyIntervalReset(reopenedForma, reopened.CurrentDailyInterval(reopenedForma, builtAt.AddDays(5))) == DateTime.MaxValue, "Reopening a rolling quest clears its countdown");
        var hourly = new Game { Name = "Hourly", DailyReset = "after-completion", DailyIntervalHours = 1 };
        journal.SaveGame(hourly, builtAt.ToLocalTime(), builtAt);
        journal.ToggleGame(hourly.Id, true, builtAt.ToLocalTime(), builtAt);
        var firstHourly = journal.CurrentDailyInterval(hourly, builtAt);
        var secondHourly = journal.CurrentDailyInterval(hourly, builtAt.AddHours(1));
        journal.ToggleGame(hourly.Id, true, builtAt.AddHours(1).ToLocalTime(), builtAt.AddHours(1));
        Assert(Journal.DateKey(builtAt.ToLocalTime()) == Journal.DateKey(builtAt.AddHours(1).ToLocalTime()) && firstHourly.Id != secondHourly.Id && secondHourly.Result.Done, "Rolling quest can have two completed cycles on one local date");
        var defaultRolling = new Game { Name = "Default interval", DailyReset = "after-completion" };
        journal.SaveGame(defaultRolling, builtAt.ToLocalTime(), builtAt);
        journal.ToggleGame(defaultRolling.Id, true, builtAt.ToLocalTime(), builtAt);
        Assert(Journal.NextDailyIntervalReset(defaultRolling, journal.CurrentDailyInterval(defaultRolling, builtAt)) == builtAt.AddHours(24), "New rolling quests default to 24 hours");
        var changedInterval = Storage.Decode(Storage.Encode(journal)).Games.First(g => g.Id == defaultRolling.Id);
        changedInterval.DailyIntervalHours = 23;
        journal.SaveGame(changedInterval, builtAt.AddHours(23).ToLocalTime(), builtAt.AddHours(23));
        Assert(journal.CurrentDailyInterval(changedInterval, builtAt.AddHours(23)).Result.Done == false && journal.DailyUtcRecords.First(r => r.GameId == defaultRolling.Id).Result.Done, "Shortening an interval opens a due quest without erasing its completed cycle");
        var localToRolling = new Game { Name = "Change rule" };
        journal.SaveGame(localToRolling, builtAt.ToLocalTime(), builtAt);
        journal.ToggleGame(localToRolling.Id, true, builtAt.ToLocalTime(), builtAt);
        localToRolling = Storage.Decode(Storage.Encode(journal)).Games.First(g => g.Id == localToRolling.Id);
        localToRolling.DailyReset = "after-completion";
        journal.SaveGame(localToRolling, builtAt.AddHours(1).ToLocalTime(), builtAt.AddHours(1));
        Assert(journal.CurrentDailyInterval(localToRolling, builtAt.AddHours(1)).Result.Done, "Switching a completed local quest to rolling preserves its current completion");
        var legacy = new Journal { Version = 1 }; var migrated = Storage.Decode(Storage.Encode(legacy));
        Assert(migrated.Version == 4 && migrated.WeeklyGames.Count == 0 && migrated.WeeklyRecords.Count == 0 && migrated.DailyUtcRecords.Count == 0, "Version 1 daily journal migrates to version 4");
        var legacyWeekly = new Journal { Version = 2 }; var migratedWeekly = Storage.Decode(Storage.Encode(legacyWeekly));
        Assert(migratedWeekly.Version == 4 && migratedWeekly.DailyUtcRecords.Count == 0, "Version 2 weekly journal migrates without losing history");
        var legacyGrouped = new Journal { Version = 3 }; legacyGrouped.Games.Add(new Game { Name = "Old daily", DailyIntervalHours = 0 });
        var migratedGrouped = Storage.Decode(Storage.Encode(legacyGrouped));
        Assert(migratedGrouped.Version == 4 && migratedGrouped.Games[0].DailyIntervalHours == 24, "Version 3 journal gains a safe 24-hour default");
        var oldVisibility = new Journal(); oldVisibility.Settings.WindowOpacity = 0;
        Assert(Storage.Decode(Storage.Encode(oldVisibility)).Settings.WindowOpacity == 1, "Older journals gain fully opaque window default");
        var customVisibility = new Journal(); customVisibility.Settings.WindowOpacity = 0.7; customVisibility.Settings.BackgroundOnlyOpacity = true;
        var savedVisibility = Storage.Decode(Storage.Encode(customVisibility));
        Assert(Math.Abs(savedVisibility.Settings.WindowOpacity - 0.7) < 0.001 && savedVisibility.Settings.BackgroundOnlyOpacity, "Background-only visibility choice survives JSON round trip");
        customVisibility.Settings.WindowOpacity = 0.2;
        bool refusedVisibility = false; try { Storage.Decode(Storage.Encode(customVisibility)); } catch (InvalidDataException) { refusedVisibility = true; }
        Assert(refusedVisibility, "Unusable window visibility is rejected");
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
