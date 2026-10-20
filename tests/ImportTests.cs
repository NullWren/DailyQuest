using System;
using System.IO;
using System.Linq;
using System.Text;
using DailyQuest;

class ImportTests
{
    static int count;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); count++; Console.WriteLine("PASS " + message); }
    static byte[] Bytes(string text) { return Encoding.UTF8.GetBytes(text); }
    static void Reject(string json, string message)
    {
        try { QuestImport.Parse(Bytes(json)); throw new Exception("Accepted invalid import: " + message); }
        catch (InvalidDataException) { Assert(true, message); }
    }
    static void Main(string[] args)
    {
        var imported = QuestImport.Parse(File.ReadAllBytes(args[0]));
        Assert(imported.Count == 5 && imported.Count(e => e.Weekly) == 2, "Repository template covers both tabs");
        Assert(imported[0].Game.GroupName == "Example game" && imported[0].Game.Description.Length > 0 && imported[0].Game.Lines.Count == 2 && imported[0].Game.Lines[0].Checkable && !imported[0].Game.Lines[1].Checkable, "Import retains group, description, checkbox and note lines");
        Assert(imported[0].Game.DailyReset == "local" && imported[1].Game.DailyReset == "utc" && imported[1].Game.DailyHourUtc == 0 && imported[2].Game.DailyReset == "after-completion" && imported[2].Game.DailyIntervalHours == 23, "Import covers all daily reset rules");
        Assert(imported[3].Game.WeeklyReset == "fixed" && imported[3].Game.FixedWeekdayUtc == 1 && imported[4].Game.WeeklyReset == "completion-weekday", "Import covers both weekly reset rules");
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var journal = new Journal();
        var existing = new Game { Name = imported[0].Game.Name.ToUpperInvariant(), GroupName = "example GAME" };
        journal.SaveGame(existing, now.ToLocalTime(), now);
        journal.ToggleGame(existing.Id, true, now.ToLocalTime(), now);
        var repeat = new ImportedQuest { Weekly = true, Game = new Game { Name = imported[3].Game.Name, GroupName = imported[3].Game.GroupName } };
        var plan = QuestImportPlan.Create(journal, imported.Concat(new[] { repeat }));
        Assert(plan.ToAdd.Count == 4 && plan.Skipped.Count == 2, "Existing and within-file duplicates are skipped case-insensitively");
        plan.Apply(journal, now.ToLocalTime(), now);
        Assert(journal.Games.Count == 3 && journal.WeeklyGames.Count == 2 && journal.Today(now.ToLocalTime()).Games.Single(g => g.Id == existing.Id).Done, "Additive import preserves existing completion");
        Assert(journal.Games.SelectMany(g => g.Lines).Select(l => l.Id).Distinct().Count() == journal.Games.SelectMany(g => g.Lines).Count(), "Imported lines receive distinct IDs");
        Assert(QuestImportPlan.Create(journal, imported).ToAdd.Count == 0, "Repeated import adds no copies");
        Assert(Storage.Decode(Storage.Encode(journal)).WeeklyGames.Count == 2, "Imported quests survive journal round trip");
        string prefix = "{\"format\":\"wrenfeed-quests\",\"version\":1,\"quests\":[";
        string suffix = "]}";
        Reject(prefix + "{\"frequency\":\"daily\",\"name\":\"A\",\"descripton\":\"typo\"}" + suffix, "Unknown fields are rejected rather than silently ignored");
        Reject(prefix + "{\"frequency\":\"daily\",\"name\":\"A\",\"reset\":{\"type\":\"hours-after-completion\",\"hours\":0}}" + suffix, "Out-of-range interval is rejected");
        Reject(prefix + "{\"frequency\":\"weekly\",\"name\":\"A\",\"reset\":{\"type\":\"fixed-utc\",\"weekdayUtc\":\"Monday\",\"timeUtc\":\"25:00\"}}" + suffix, "Invalid UTC time is rejected");
        Reject(prefix + "{\"frequency\":\"weekly\",\"name\":\"A\"}" + suffix, "Weekly reset is required");
        Reject(prefix + "{\"frequency\":\"daily\",\"name\":\"A\",\"lines\":[{\"text\":\"B\",\"checkbox\":\"yes\"}]}" + suffix, "Checkbox must be a boolean");
        Reject(prefix + "{\"frequency\":\"daily\",\"name\":\"A\"},{\"frequency\":\"daily\",\"name\":\"B\",\"reset\":{\"type\":\"fixed-utc\",\"timeUtc\":\"bad\"}}" + suffix, "One invalid quest rejects the whole file");
        Console.WriteLine("Import assertions passed: " + count);
    }
}
