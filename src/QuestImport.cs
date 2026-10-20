using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace DailyQuest
{
    public sealed class ImportedQuest
    {
        public bool Weekly;
        public Game Game;
    }

    public sealed class QuestImportPlan
    {
        public readonly List<ImportedQuest> ToAdd = new List<ImportedQuest>();
        public readonly List<ImportedQuest> Skipped = new List<ImportedQuest>();

        public static QuestImportPlan Create(Journal journal, IEnumerable<ImportedQuest> entries)
        {
            var plan = new QuestImportPlan();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var game in journal.Games) keys.Add(Key(false, game));
            foreach (var game in journal.WeeklyGames) keys.Add(Key(true, game));
            foreach (var entry in entries)
            {
                if (keys.Add(Key(entry.Weekly, entry.Game))) plan.ToAdd.Add(entry);
                else plan.Skipped.Add(entry);
            }
            return plan;
        }

        static string Key(bool weekly, Game game)
        {
            return (weekly ? "weekly" : "daily") + "\u001f" + (game.GroupName ?? "").Trim() + "\u001f" + game.Name.Trim();
        }

        public void Apply(Journal journal, DateTime local, DateTime utc)
        {
            foreach (var entry in ToAdd)
            {
                if (entry.Weekly) journal.SaveWeeklyGame(entry.Game, utc);
                else journal.SaveGame(entry.Game, local, utc);
            }
        }
    }

    public static class QuestImport
    {
        static readonly HashSet<string> RootKeys = Keys("format", "version", "quests");
        static readonly HashSet<string> QuestKeys = Keys("frequency", "game", "name", "description", "reset", "lines");
        static readonly HashSet<string> LineKeys = Keys("text", "checkbox");
        static readonly HashSet<string> ResetKeys = Keys("type", "timeUtc", "weekdayUtc", "hours");

        static HashSet<string> Keys(params string[] keys) { return new HashSet<string>(keys, StringComparer.Ordinal); }
        static Dictionary<string, object> Object(object value, string where)
        {
            var result = value as Dictionary<string, object>;
            if (result == null) throw new InvalidDataException(where + " must be an object.");
            return result;
        }
        static object[] Array(object value, string where)
        {
            var result = value as object[];
            if (result == null) throw new InvalidDataException(where + " must be an array.");
            return result;
        }
        static void CheckKeys(Dictionary<string, object> value, HashSet<string> allowed, string where)
        {
            foreach (var key in value.Keys)
                if (!allowed.Contains(key)) throw new InvalidDataException(where + " has an unknown field: " + key + ".");
        }
        static string String(Dictionary<string, object> value, string key, string where, int max, bool required)
        {
            object item;
            if (!value.TryGetValue(key, out item))
            {
                if (required) throw new InvalidDataException(where + " needs \"" + key + "\".");
                return "";
            }
            var text = item as string;
            if (text == null) throw new InvalidDataException(where + "." + key + " must be text.");
            text = text.Trim();
            if (required && text.Length == 0 || text.Length > max) throw new InvalidDataException(where + "." + key + " must be " + (required ? "1–" : "0–") + max + " characters.");
            return text;
        }
        static int Number(Dictionary<string, object> value, string key, string where)
        {
            object item;
            if (!value.TryGetValue(key, out item) || !(item is int)) throw new InvalidDataException(where + "." + key + " must be a whole number.");
            return (int)item;
        }
        static void Only(Dictionary<string, object> value, string where, params string[] allowed)
        {
            var keys = Keys(allowed);
            foreach (var key in value.Keys) if (!keys.Contains(key)) throw new InvalidDataException(where + " cannot use \"" + key + "\" with this reset type.");
        }
        static void Time(string text, string where, out int hour, out int minute)
        {
            hour = minute = 0;
            if (text.Length != 5 || text[2] != ':' || !Int32.TryParse(text.Substring(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out hour) || !Int32.TryParse(text.Substring(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out minute) || hour > 23 || minute > 59)
                throw new InvalidDataException(where + " must use UTC HH:MM, for example 00:00 or 17:30.");
        }

        public static List<ImportedQuest> Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > 1024 * 1024) throw new InvalidDataException("Choose a nonempty quest file smaller than 1 MB.");
            object parsed;
            try
            {
                string json = new UTF8Encoding(false, true).GetString(bytes);
                if (json.Length > 0 && json[0] == '\uFEFF') json = json.Substring(1);
                parsed = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024, RecursionLimit = 32 }.DeserializeObject(json);
            }
            catch (Exception error) { throw new InvalidDataException("The quest file is not valid UTF-8 JSON: " + error.Message, error); }
            var root = Object(parsed, "File"); CheckKeys(root, RootKeys, "File");
            if (String(root, "format", "File", 40, true) != "wrenfeed-quests" || Number(root, "version", "File") != 1)
                throw new InvalidDataException("Use WrenFeed quest format version 1, as shown in import/template.json.");
            object rawQuests;
            if (!root.TryGetValue("quests", out rawQuests)) throw new InvalidDataException("File needs a quests array.");
            var quests = Array(rawQuests, "File.quests");
            if (quests.Length == 0 || quests.Length > 500) throw new InvalidDataException("Include between 1 and 500 quests.");
            var result = new List<ImportedQuest>();
            for (int index = 0; index < quests.Length; index++)
            {
                string where = "Quest " + (index + 1);
                var source = Object(quests[index], where); CheckKeys(source, QuestKeys, where);
                string frequency = String(source, "frequency", where, 6, true);
                if (frequency != "daily" && frequency != "weekly") throw new InvalidDataException(where + ".frequency must be daily or weekly.");
                var game = new Game { GroupName = String(source, "game", where, 80, false), Name = String(source, "name", where, 80, true), Description = String(source, "description", where, 220, false) };
                object rawReset;
                if (source.TryGetValue("reset", out rawReset))
                {
                    var reset = Object(rawReset, where + ".reset"); CheckKeys(reset, ResetKeys, where + ".reset");
                    string type = String(reset, "type", where + ".reset", 32, true);
                    if (frequency == "daily")
                    {
                        if (type == "local-midnight") { Only(reset, where + ".reset", "type"); game.DailyReset = "local"; }
                        else if (type == "fixed-utc") { Only(reset, where + ".reset", "type", "timeUtc"); game.DailyReset = "utc"; Time(String(reset, "timeUtc", where + ".reset", 5, true), where + ".reset.timeUtc", out game.DailyHourUtc, out game.DailyMinuteUtc); }
                        else if (type == "hours-after-completion") { Only(reset, where + ".reset", "type", "hours"); game.DailyReset = "after-completion"; game.DailyIntervalHours = Number(reset, "hours", where + ".reset"); }
                        else throw new InvalidDataException(where + " has an invalid daily reset type.");
                    }
                    else
                    {
                        if (type == "fixed-utc")
                        {
                            Only(reset, where + ".reset", "type", "weekdayUtc", "timeUtc"); game.WeeklyReset = "fixed";
                            string day = String(reset, "weekdayUtc", where + ".reset", 9, true);
                            DayOfWeek weekday;
                            if (!Enum.TryParse(day, true, out weekday) || !Enum.IsDefined(typeof(DayOfWeek), weekday)) throw new InvalidDataException(where + ".reset.weekdayUtc must be a weekday name, such as Sunday.");
                            game.FixedWeekdayUtc = (int)weekday;
                            Time(String(reset, "timeUtc", where + ".reset", 5, true), where + ".reset.timeUtc", out game.FixedHourUtc, out game.FixedMinuteUtc);
                        }
                        else if (type == "seven-days-after-completion") { Only(reset, where + ".reset", "type"); game.WeeklyReset = "completion-weekday"; }
                        else throw new InvalidDataException(where + " has an invalid weekly reset type.");
                    }
                }
                else if (frequency == "weekly") throw new InvalidDataException(where + " needs a weekly reset rule.");
                object rawLines;
                if (source.TryGetValue("lines", out rawLines))
                {
                    var lines = Array(rawLines, where + ".lines");
                    if (lines.Length > 100) throw new InvalidDataException(where + " has more than 100 lines.");
                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        string lineWhere = where + ".lines[" + lineIndex + "]";
                        var line = Object(lines[lineIndex], lineWhere); CheckKeys(line, LineKeys, lineWhere);
                        object checkbox;
                        if (!line.TryGetValue("checkbox", out checkbox) || !(checkbox is bool)) throw new InvalidDataException(lineWhere + ".checkbox must be true or false.");
                        game.Lines.Add(new QuestLine { Text = String(line, "text", lineWhere, 160, true), Checkable = (bool)checkbox });
                    }
                }
                if (frequency == "weekly") Journal.ValidateWeeklySchedule(game); else Journal.ValidateDailySchedule(game);
                result.Add(new ImportedQuest { Weekly = frequency == "weekly", Game = game });
            }
            return result;
        }
    }
}
