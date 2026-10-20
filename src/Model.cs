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
        [DataMember] public List<QuestLine> Lines = new List<QuestLine>();
        [DataMember] public bool Archived;
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
    [DataContract] public class Preferences
    {
        [DataMember] public string Theme = "quest";
        [DataMember] public bool Animate = true;
        [DataMember] public bool AlwaysOnTop;
        [DataMember] public double Width = 430;
        [DataMember] public double Height = 720;
    }
    [DataContract] public class Journal
    {
        [DataMember] public int Version = 1;
        [DataMember] public List<Game> Games = new List<Game>();
        [DataMember] public List<DayRecord> Days = new List<DayRecord>();
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
                foreach (var game in Games.Where(g => !g.Archived)) day.Games.Add(Snapshot(game));
            }
            return day;
        }
        static GameResult Snapshot(Game game)
        {
            return new GameResult { Id = game.Id, Name = game.Name, Description = game.Description,
                Lines = game.Lines.Select(l => new LineResult { Id = l.Id, Text = l.Text, Checkable = l.Checkable }).ToList() };
        }
        public void SaveGame(Game game, DateTime local, DateTime utc)
        {
            var day = Today(local);
            var index = Games.FindIndex(g => g.Id == game.Id);
            if (index < 0) Games.Add(game); else Games[index] = game;
            var old = day.Games.FirstOrDefault(g => g.Id == game.Id);
            var fresh = Snapshot(game);
            if (old != null)
            {
                fresh.Done = old.Done; fresh.CompletedUtc = old.CompletedUtc;
                foreach (var line in fresh.Lines)
                {
                    var prior = old.Lines.FirstOrDefault(l => l.Id == line.Id && l.Checkable && line.Checkable);
                    if (prior != null) { line.Done = prior.Done; line.CompletedUtc = prior.CompletedUtc; }
                }
                if (fresh.Lines.Any(l => l.Checkable)) SetDone(fresh, fresh.Lines.Where(l => l.Checkable).All(l => l.Done), utc);
                day.Games[day.Games.IndexOf(old)] = fresh;
            }
            else day.Games.Add(fresh);
        }
        public void Archive(string id, DateTime local)
        {
            Games.First(g => g.Id == id).Archived = true;
            var result = Today(local).Games.FirstOrDefault(g => g.Id == id);
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
            var game = Today(local).Games.First(g => g.Id == id);
            SetDone(game, done, utc);
            foreach (var line in game.Lines.Where(l => l.Checkable))
            {
                if (line.Done != done) line.CompletedUtc = done ? utc.ToString("o") : null;
                line.Done = done;
            }
        }
        public void ToggleLine(string gameId, string lineId, bool done, DateTime local, DateTime utc)
        {
            var game = Today(local).Games.First(g => g.Id == gameId);
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
            catch (IOException) { throw new IOException("Daily Quest is already open, or its data folder is locked. Close the other window and try again."); }
        }
        public static byte[] Encode(Journal journal)
        {
            using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(Journal)).WriteObject(stream, journal); return stream.ToArray(); }
        }
        public static Journal Decode(byte[] bytes)
        {
            Journal journal;
            using (var stream = new MemoryStream(bytes)) journal = (Journal)new DataContractJsonSerializer(typeof(Journal)).ReadObject(stream);
            if (journal == null || journal.Version != 1) throw new InvalidDataException("This journal uses an unsupported version. Keep the original file and use the matching app version.");
            if (journal.Games == null || journal.Days == null || journal.Settings == null) throw new InvalidDataException("The journal is missing required data.");
            var ids = new HashSet<string>();
            foreach (var game in journal.Games)
            {
                if (game == null || String.IsNullOrWhiteSpace(game.Id) || !ids.Add(game.Id) || String.IsNullOrWhiteSpace(game.Name) || game.Lines == null) throw new InvalidDataException("Invalid game data.");
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
                    var lineIds = new HashSet<string>();
                    foreach (var line in game.Lines) if (line == null || String.IsNullOrWhiteSpace(line.Id) || !lineIds.Add(line.Id) || line.Text == null) throw new InvalidDataException("Invalid history line.");
                }
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
