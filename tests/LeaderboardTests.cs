using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DailyQuest;

class LeaderboardTests
{
    static void Assert(bool okay, string name) { if (!okay) throw new Exception(name); Console.WriteLine("PASS " + name); }
    static GameResult Result(Game game, string completed)
    {
        return new GameResult { Id = game.Id, Name = game.Name, GroupName = "", Done = true, CompletedUtc = completed, Lines = new List<LineResult>() };
    }
    static void Main(string[] args)
    {
        var osrs = new Game { Name = "Tears of Guthix", GroupName = "OSRS" };
        var forma = new Game { Name = "Forma crafting", GroupName = "Warframe", DailyReset = "after-completion" };
        var birdhouse = new Game { Name = "Birdhouse run" };
        var journal = new Journal(); journal.Games.AddRange(new[] { forma, birdhouse }); journal.WeeklyGames.Add(osrs);
        journal.Days.Add(new DayRecord { Date = "2026-10-18", Games = new List<GameResult> { Result(birdhouse, "2026-10-18T15:00:00Z") } });
        journal.Days.Add(new DayRecord { Date = "2026-10-19", Games = new List<GameResult> { Result(birdhouse, "2026-10-19T15:00:00Z"), Result(forma, "2026-10-19T17:00:00Z") } });
        journal.DailyUtcRecords.Add(new WeeklyRecord { GameId = forma.Id, CycleStartUtc = "2026-10-18T17:00:00Z", Result = Result(forma, "2026-10-19T17:00:00Z") });
        journal.DailyUtcRecords.Add(new WeeklyRecord { GameId = forma.Id, CycleStartUtc = "2026-10-19T17:00:00Z", Result = Result(forma, "2026-10-19T20:00:00Z") });
        journal.WeeklyRecords.Add(new WeeklyRecord { GameId = osrs.Id, CycleStartUtc = "2026-10-19T00:00:00Z", Result = Result(osrs, "2026-10-19T21:00:00Z") });
        var tally = LeaderboardSnapshot.Create(journal);
        Assert(tally.DailyTotal == 4 && tally.WeeklyTotal == 1, "All recorded daily and weekly cycles counted without duplicate conversion");
        Assert(tally.games.Single(g => g.name == "Warframe").daily == 2, "Rolling daily cycles on one date both count");
        Assert(tally.games.Single(g => g.name == "OSRS").weekly == 1, "Current group combines older history under its game label");
        Assert(tally.games.Single(g => g.name == "Birdhouse run").daily == 2, "Ungrouped quest name is its game label");
        var folder = args[0]; Directory.CreateDirectory(folder);
        var secret = new LeaderboardIdentity { nickname = "Wren", inviteCode = new string('z', 32) };
        secret.Save(folder);
        var encoded = File.ReadAllText(Path.Combine(folder, "leaderboard-identity.dat"));
        Assert(!encoded.Contains(secret.inviteCode), "Saved invite code is not plaintext");
        var restored = LeaderboardIdentity.Load(folder);
        Assert(restored.nickname == secret.nickname && restored.inviteCode == secret.inviteCode, "Windows account can reopen its encrypted invite code");
    }
}
