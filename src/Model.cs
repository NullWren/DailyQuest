using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DailyQuest
{
    [DataContract] public class QuestLine
    {
        [DataMember] public string Id = Guid.NewGuid().ToString("N");
        [DataMember] public string Text = "";
        [DataMember] public bool Checkable;
    }
    [DataContract] public class Game
    {
        [DataMember] public string Id = Guid.NewGuid().ToString("N");
        [DataMember] public string Name = "";
        [DataMember] public string Description = "";
        [DataMember] public string GroupName = "";
        [DataMember] public string DailyReset = "local";
        [DataMember] public int DailyHourUtc;
        [DataMember] public int DailyMinuteUtc;
        [DataMember] public int DailyIntervalHours = 24;
        [DataMember] public List<QuestLine> Lines = new List<QuestLine>();
        [DataMember] public bool Archived;
        [DataMember] public string WeeklyReset = "fixed";
        [DataMember] public int FixedWeekdayUtc;
        [DataMember] public int FixedHourUtc;
        [DataMember] public int FixedMinuteUtc;
    }
    [DataContract] public class LineResult
    {
        [DataMember] public string Id;
        [DataMember] public string Text;
        [DataMember] public bool Checkable;
        [DataMember] public bool Done;
        [DataMember] public string CompletedUtc;
    }
    [DataContract] public class GameResult
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public string Description;
        [DataMember] public string GroupName;
        [DataMember] public List<LineResult> Lines = new List<LineResult>();
        [DataMember] public bool Done;
        [DataMember] public string CompletedUtc;
        [DataMember] public bool Archived;
    }
    [DataContract] public class DayRecord
    {
        [DataMember] public string Date;
        [DataMember] public List<GameResult> Games = new List<GameResult>();
    }
    [DataContract] public class WeeklyRecord
    {
        [DataMember] public string Id = Guid.NewGuid().ToString("N");
        [DataMember] public string GameId;
        [DataMember] public string CycleStartUtc;
        [DataMember] public GameResult Result;
    }
    [DataContract] public class Preferences
    {
        [DataMember] public string Theme = "quest";
        [DataMember] public bool Animate = true;
        [DataMember] public bool AlwaysOnTop;
        [DataMember] public double WindowOpacity = 1.0;
        [DataMember] public bool BackgroundOnlyOpacity;
        [DataMember] public double Width = 430;
        [DataMember] public double Height = 720;
    }
    [DataContract] public class Journal
    {
        [DataMember] public int Version = 4;
        [DataMember] public List<Game> Games = new List<Game>();
        [DataMember] public List<DayRecord> Days = new List<DayRecord>();
        [DataMember] public List<Game> WeeklyGames = new List<Game>();
        [DataMember] public List<WeeklyRecord> WeeklyRecords = new List<WeeklyRecord>();
        [DataMember] public List<WeeklyRecord> DailyUtcRecords = new List<WeeklyRecord>();
        [DataMember] public Preferences Settings = new Preferences();

        public static string DateKey(DateTime local) { return local.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); }
        public DayRecord Today(DateTime local)
        {
            var key = DateKey(local);
            var day = Days.FirstOrDefault(d => d.Date == key);
            if (day == null)
            {
                day = new DayRecord { Date = key };
                Days.Add(day);
                foreach (var game in Games.Where(g => !g.Archived && g.DailyReset == "local")) day.Games.Add(Snapshot(game));
            }
            return day;
        }
        static GameResult Snapshot(Game game)
        {
            return new GameResult { Id = game.Id, Name = game.Name, Description = game.Description, GroupName = game.GroupName,
                Lines = game.Lines.Select(l => new LineResult { Id = l.Id, Text = l.Text, Checkable = l.Checkable }).ToList() };
        }
        public static DateTime DailyCycleStart(Game game, DateTime utc)
        {
            utc = utc.ToUniversalTime();
            var start = utc.Date.AddHours(game.DailyHourUtc).AddMinutes(game.DailyMinuteUtc);
            return start > utc ? start.AddDays(-1) : start;
        }
        public WeeklyRecord CurrentDailyUtc(Game game, DateTime utc)
        {
            var start = DailyCycleStart(game, utc);
            var latest = DailyUtcRecords.LastOrDefault(r => r.GameId == game.Id);
            if (latest == null || DateTime.Parse(latest.CycleStartUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime() != start)
            {
                latest = new WeeklyRecord { GameId = game.Id, CycleStartUtc = start.ToString("o"), Result = Snapshot(game) };
                DailyUtcRecords.Add(latest);
            }
            return latest;
        }
        public static DateTime NextDailyIntervalReset(Game game, WeeklyRecord record)
        {
            if (record == null || record.Result == null || !record.Result.Done || String.IsNullOrEmpty(record.Result.CompletedUtc)) return DateTime.MaxValue;
            return DateTime.Parse(record.Result.CompletedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime().AddHours(game.DailyIntervalHours);
        }
        public WeeklyRecord CurrentDailyInterval(Game game, DateTime utc)
        {
            utc = utc.ToUniversalTime();
            var latest = DailyUtcRecords.LastOrDefault(r => r.GameId == game.Id);
            if (latest == null || latest.Result.Done && utc >= NextDailyIntervalReset(game, latest))
            {
                latest = new WeeklyRecord { GameId = game.Id, CycleStartUtc = (latest == null ? utc : NextDailyIntervalReset(game, latest)).ToString("o"), Result = Snapshot(game) };
                DailyUtcRecords.Add(latest);
            }
            return latest;
        }
        public WeeklyRecord CurrentTimedDaily(Game game, DateTime utc)
        {
            return game.DailyReset == "utc" ? CurrentDailyUtc(game, utc) : CurrentDailyInterval(game, utc);
        }
        public GameResult CurrentDaily(Game game, DateTime local, DateTime utc)
        {
            return game.DailyReset == "local" ? Today(local).Games.First(g => g.Id == game.Id) : CurrentTimedDaily(game, utc).Result;
        }
        static GameResult Updated(Game game, GameResult old, DateTime utc)
        {
            var fresh = Snapshot(game);
            if (old == null) return fresh;
            fresh.Done = old.Done; fresh.CompletedUtc = old.CompletedUtc;
            foreach (var line in fresh.Lines)
            {
                var prior = old.Lines.FirstOrDefault(l => l.Id == line.Id && l.Checkable && line.Checkable);
                if (prior != null) { line.Done = prior.Done; line.CompletedUtc = prior.CompletedUtc; }
            }
            if (fresh.Lines.Any(l => l.Checkable)) SetDone(fresh, fresh.Lines.Where(l => l.Checkable).All(l => l.Done), utc);
            return fresh;
        }
        public void SaveGame(Game game, DateTime local, DateTime utc)
        {
            ValidateDailySchedule(game);
            var day = Today(local);
            var index = Games.FindIndex(g => g.Id == game.Id);
            var previous = index < 0 ? null : Games[index];
            var oldRecord = DailyUtcRecords.LastOrDefault(r => r.GameId == game.Id);
            var old = previous == null ? null : previous.DailyReset != "local" ? (oldRecord == null ? null : oldRecord.Result) : day.Games.FirstOrDefault(g => g.Id == game.Id);
            if (index < 0) Games.Add(game); else Games[index] = game;
            if (game.DailyReset != "local")
            {
                if (previous != null && previous.DailyReset != game.DailyReset)
                {
                    var start = game.DailyReset == "utc" ? DailyCycleStart(game, utc) : utc.ToUniversalTime();
                    DailyUtcRecords.Add(new WeeklyRecord { GameId = game.Id, CycleStartUtc = start.ToString("o"), Result = Updated(game, old, utc) });
                }
                else
                {
                    var record = CurrentTimedDaily(game, utc);
                    record.Result = Updated(game, record.Result, utc);
                }
                if (game.DailyReset == "after-completion") CurrentDailyInterval(game, utc);
            }
            else
            {
                var current = day.Games.FirstOrDefault(g => g.Id == game.Id);
                var fresh = Updated(game, old ?? current, utc);
                if (current == null) day.Games.Add(fresh); else day.Games[day.Games.IndexOf(current)] = fresh;
            }
        }
        public void Archive(string id, DateTime local)
        {
            Games.First(g => g.Id == id).Archived = true;
            var utcRecord = DailyUtcRecords.LastOrDefault(r => r.GameId == id);
            var result = Games.First(g => g.Id == id).DailyReset != "local" ? (utcRecord == null ? null : utcRecord.Result) : Today(local).Games.FirstOrDefault(g => g.Id == id);
            if (result != null) result.Archived = true;
        }
        public void Restore(string id, DateTime local, DateTime utc)
        {
            var game = Games.First(g => g.Id == id); game.Archived = false; SaveGame(game, local, utc);
        }
        static void SetDone(GameResult game, bool done, DateTime utc)
        {
            if (game.Done != done || (done && game.CompletedUtc == null)) game.CompletedUtc = done ? utc.ToString("o") : null;
            game.Done = done;
        }
        public void ToggleGame(string id, bool done, DateTime local, DateTime utc)
        {
            var game = CurrentDaily(Games.First(g => g.Id == id), local, utc);
            SetDone(game, done, utc);
            foreach (var line in game.Lines.Where(l => l.Checkable))
            {
                if (line.Done != done) line.CompletedUtc = done ? utc.ToString("o") : null;
                line.Done = done;
            }
        }
        public void ToggleLine(string gameId, string lineId, bool done, DateTime local, DateTime utc)
        {
            var game = CurrentDaily(Games.First(g => g.Id == gameId), local, utc);
            var line = game.Lines.First(l => l.Id == lineId && l.Checkable);
            line.Done = done; line.CompletedUtc = done ? utc.ToString("o") : null;
            SetDone(game, game.Lines.Where(l => l.Checkable).All(l => l.Done), utc);
        }
        public void Move(string id, int delta)
        {
            var active = Games.Where(g => !g.Archived).ToList();
            int from = active.FindIndex(g => g.Id == id), to = from + delta;
            if (from < 0 || to < 0 || to >= active.Count) return;
            int a = Games.IndexOf(active[from]), b = Games.IndexOf(active[to]);
            var swap = Games[a]; Games[a] = Games[b]; Games[b] = swap;
        }
        public static DateTime FixedCycleStart(Game game, DateTime utc)
        {
            utc = utc.ToUniversalTime();
            var start = utc.Date.AddHours(game.FixedHourUtc).AddMinutes(game.FixedMinuteUtc);
            int days = ((int)start.DayOfWeek - game.FixedWeekdayUtc + 7) % 7;
            start = start.AddDays(-days);
            if (start > utc) start = start.AddDays(-7);
            return start;
        }
        public static DateTime NextWeeklyReset(Game game, WeeklyRecord record)
        {
            if (game.WeeklyReset == "completion-weekday")
            {
                if (record == null || record.Result == null || !record.Result.Done) return DateTime.MaxValue;
                return DateTime.Parse(record.Result.CompletedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime().Date.AddDays(7);
            }
            return DateTime.Parse(record.CycleStartUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime().AddDays(7);
        }
        public WeeklyRecord CurrentWeekly(Game game, DateTime utc)
        {
            utc = utc.ToUniversalTime();
            var latest = WeeklyRecords.LastOrDefault(r => r.GameId == game.Id);
            bool fresh = latest == null || (game.WeeklyReset == "fixed" && DateTime.Parse(latest.CycleStartUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime() != FixedCycleStart(game, utc)) || (game.WeeklyReset == "completion-weekday" && latest.Result.Done && utc >= NextWeeklyReset(game, latest));
            if (fresh)
            {
                latest = new WeeklyRecord { GameId = game.Id, CycleStartUtc = (game.WeeklyReset == "fixed" ? FixedCycleStart(game, utc) : utc.Date).ToString("o"), Result = Snapshot(game) };
                WeeklyRecords.Add(latest);
            }
            return latest;
        }
        public void SaveWeeklyGame(Game game, DateTime utc)
        {
            ValidateWeeklySchedule(game);
            var index = WeeklyGames.FindIndex(g => g.Id == game.Id);
            if (index < 0) WeeklyGames.Add(game); else WeeklyGames[index] = game;
            var current = CurrentWeekly(game, utc);
            var old = current.Result;
            var fresh = Snapshot(game);
            fresh.Done = old.Done; fresh.CompletedUtc = old.CompletedUtc;
            foreach (var line in fresh.Lines)
            {
                var prior = old.Lines.FirstOrDefault(l => l.Id == line.Id && l.Checkable && line.Checkable);
                if (prior != null) { line.Done = prior.Done; line.CompletedUtc = prior.CompletedUtc; }
            }
            if (fresh.Lines.Any(l => l.Checkable)) SetDone(fresh, fresh.Lines.Where(l => l.Checkable).All(l => l.Done), utc);
            current.Result = fresh;
        }
        public void ToggleWeeklyGame(string id, bool done, DateTime utc)
        {
            var result = CurrentWeekly(WeeklyGames.First(g => g.Id == id), utc).Result;
            SetDone(result, done, utc);
            foreach (var line in result.Lines.Where(l => l.Checkable)) { line.Done = done; line.CompletedUtc = done ? utc.ToUniversalTime().ToString("o") : null; }
        }
        public void ToggleWeeklyLine(string gameId, string lineId, bool done, DateTime utc)
        {
            var result = CurrentWeekly(WeeklyGames.First(g => g.Id == gameId), utc).Result;
            var line = result.Lines.First(l => l.Id == lineId && l.Checkable);
            line.Done = done; line.CompletedUtc = done ? utc.ToUniversalTime().ToString("o") : null;
            SetDone(result, result.Lines.Where(l => l.Checkable).All(l => l.Done), utc);
        }
        public void ArchiveWeekly(string id, DateTime utc)
        {
            var game = WeeklyGames.First(g => g.Id == id); game.Archived = true;
            CurrentWeekly(game, utc).Result.Archived = true;
        }
        public void RestoreWeekly(string id, DateTime utc)
        {
            var game = WeeklyGames.First(g => g.Id == id); game.Archived = false; SaveWeeklyGame(game, utc);
            CurrentWeekly(game, utc).Result.Archived = false;
        }
        public void MoveWeekly(string id, int delta)
        {
            var active = WeeklyGames.Where(g => !g.Archived).ToList();
            int from = active.FindIndex(g => g.Id == id), to = from + delta;
            if (from < 0 || to < 0 || to >= active.Count) return;
            int a = WeeklyGames.IndexOf(active[from]), b = WeeklyGames.IndexOf(active[to]);
            var swap = WeeklyGames[a]; WeeklyGames[a] = WeeklyGames[b]; WeeklyGames[b] = swap;
        }
        public static void ValidateWeeklySchedule(Game game)
        {
            if (game.WeeklyReset != "fixed" && game.WeeklyReset != "completion-weekday" || game.FixedWeekdayUtc < 0 || game.FixedWeekdayUtc > 6 || game.FixedHourUtc < 0 || game.FixedHourUtc > 23 || game.FixedMinuteUtc < 0 || game.FixedMinuteUtc > 59) throw new InvalidDataException("Invalid weekly reset rule.");
        }
        public static void ValidateDailySchedule(Game game)
        {
            if (game.DailyReset != "local" && game.DailyReset != "utc" && game.DailyReset != "after-completion" || game.DailyHourUtc < 0 || game.DailyHourUtc > 23 || game.DailyMinuteUtc < 0 || game.DailyMinuteUtc > 59 || game.DailyIntervalHours < 1 || game.DailyIntervalHours > 8760) throw new InvalidDataException("Invalid daily reset rule.");
        }
    }
    public sealed class Storage : IDisposable
    {
        public readonly string Folder;
        public string DataPath { get { return Path.Combine(Folder, "journal.json"); } }
        public string BackupPath { get { return Path.Combine(Folder, "journal.backup.json"); } }
        FileStream instanceLock;
        public string RecoveryNotice;
        public Storage(string folder)
        {
            Folder = Path.GetFullPath(folder); Directory.CreateDirectory(Folder);
            try { instanceLock = new FileStream(Path.Combine(Folder, "journal.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { throw new IOException("WrenFeed is already open, or its data folder is locked. Close the other window and try again."); }
        }
        public static byte[] Encode(Journal journal)
        {
            using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(Journal)).WriteObject(stream, journal); return stream.ToArray(); }
        }
        public static Journal Decode(byte[] bytes)
        {
            Journal journal;
            using (var stream = new MemoryStream(bytes)) journal = (Journal)new DataContractJsonSerializer(typeof(Journal)).ReadObject(stream);
            if (journal == null || (journal.Version != 1 && journal.Version != 2 && journal.Version != 3 && journal.Version != 4)) throw new InvalidDataException("This journal uses an unsupported version. Keep the original file and use the matching app version.");
            if (journal.Games == null || journal.Days == null || journal.Settings == null) throw new InvalidDataException("The journal is missing required data.");
            if (journal.Settings.WindowOpacity == 0) journal.Settings.WindowOpacity = 1.0; // Journals saved before the opacity setting.
            if (Double.IsNaN(journal.Settings.WindowOpacity) || Double.IsInfinity(journal.Settings.WindowOpacity) || journal.Settings.WindowOpacity < 0.6 || journal.Settings.WindowOpacity > 1.0) throw new InvalidDataException("Invalid window opacity setting.");
            if (journal.Version == 1) { journal.WeeklyGames = new List<Game>(); journal.WeeklyRecords = new List<WeeklyRecord>(); }
            if (journal.WeeklyGames == null || journal.WeeklyRecords == null) throw new InvalidDataException("The journal is missing weekly data.");
            if (journal.Version < 3) journal.DailyUtcRecords = new List<WeeklyRecord>();
            if (journal.DailyUtcRecords == null) throw new InvalidDataException("The journal is missing UTC daily history.");
            journal.Version = 4;
            var ids = new HashSet<string>();
            foreach (var game in journal.Games.Concat(journal.WeeklyGames))
            {
                if (game == null || String.IsNullOrWhiteSpace(game.Id) || !ids.Add(game.Id) || String.IsNullOrWhiteSpace(game.Name) || game.Lines == null) throw new InvalidDataException("Invalid game data.");
                if (game.GroupName == null) game.GroupName = "";
                if (game.DailyReset == null) game.DailyReset = "local";
                if (game.DailyIntervalHours == 0 && game.DailyReset != "after-completion") game.DailyIntervalHours = 24;
                if (journal.WeeklyGames.Contains(game)) Journal.ValidateWeeklySchedule(game);
                else Journal.ValidateDailySchedule(game);
                var lines = new HashSet<string>();
                foreach (var line in game.Lines) if (line == null || String.IsNullOrWhiteSpace(line.Id) || !lines.Add(line.Id) || line.Text == null) throw new InvalidDataException("Invalid quest line.");
            }
            var dates = new HashSet<string>();
            foreach (var day in journal.Days)
            {
                DateTime parsed;
                if (day == null || !DateTime.TryParseExact(day.Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed) || !dates.Add(day.Date) || day.Games == null) throw new InvalidDataException("Invalid daily history.");
                var gameIds = new HashSet<string>();
                foreach (var game in day.Games)
                {
                    if (game == null || String.IsNullOrWhiteSpace(game.Id) || !gameIds.Add(game.Id) || game.Name == null || game.Lines == null) throw new InvalidDataException("Invalid history entry.");
                    if (game.GroupName == null) game.GroupName = "";
                    var lineIds = new HashSet<string>();
                    foreach (var line in game.Lines) if (line == null || String.IsNullOrWhiteSpace(line.Id) || !lineIds.Add(line.Id) || line.Text == null) throw new InvalidDataException("Invalid history line.");
                }
            }
            var recordIds = new HashSet<string>();
            foreach (var record in journal.WeeklyRecords.Concat(journal.DailyUtcRecords))
            {
                DateTime start;
                if (record == null || String.IsNullOrWhiteSpace(record.Id) || !recordIds.Add(record.Id) || !ids.Contains(record.GameId) || !DateTime.TryParse(record.CycleStartUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out start) || start.Kind != DateTimeKind.Utc || record.Result == null || record.Result.Id != record.GameId || record.Result.Lines == null) throw new InvalidDataException("Invalid scheduled history.");
                if (record.Result.GroupName == null) record.Result.GroupName = "";
                var lineIds = new HashSet<string>();
                foreach (var line in record.Result.Lines) if (line == null || String.IsNullOrWhiteSpace(line.Id) || !lineIds.Add(line.Id) || line.Text == null) throw new InvalidDataException("Invalid weekly history line.");
            }
            return journal;
        }
        public Journal Load()
        {
            if (!File.Exists(DataPath))
            {
                if (File.Exists(BackupPath)) return Recover(new IOException("Main journal is missing."));
                return new Journal();
            }
            try { return Decode(File.ReadAllBytes(DataPath)); }
            catch (InvalidDataException) { throw; } // Never overwrite an unknown schema or structurally invalid journal.
            catch (Exception error)
            {
                if (!(error is SerializationException) && !(error is System.Xml.XmlException) && !(error is IOException)) throw;
                return Recover(error);
            }
        }
        Journal Recover(Exception original)
        {
            if (!File.Exists(BackupPath)) throw new InvalidDataException("Could not read your journal. Its original file has been preserved. " + original.Message);
            var journal = Decode(File.ReadAllBytes(BackupPath));
            if (File.Exists(DataPath)) File.Copy(DataPath, Path.Combine(Folder, "journal.unreadable-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff") + ".json"));
            File.Copy(BackupPath, DataPath, true);
            RecoveryNotice = "Recovered the last backup. The unreadable original was preserved in your data folder. Your most recent change may need to be repeated.";
            return journal;
        }
        public void Save(Journal journal)
        {
            var bytes = Encode(journal);
            string temp = Path.Combine(Folder, "journal.pending.json");
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            if (File.Exists(DataPath)) File.Replace(temp, DataPath, BackupPath, true); else File.Move(temp, DataPath);
        }
        public void Dispose() { if (instanceLock != null) instanceLock.Dispose(); }
    }
}
