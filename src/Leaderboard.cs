using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DailyQuest
{
    public sealed class LeaderboardGameCount
    {
        public string key;
        public string name;
        public int daily;
        public int weekly;
    }
    public sealed class LeaderboardSnapshot
    {
        public List<LeaderboardGameCount> games = new List<LeaderboardGameCount>();
        public int DailyTotal { get { return games.Sum(g => g.daily); } }
        public int WeeklyTotal { get { return games.Sum(g => g.weekly); } }
        public static LeaderboardSnapshot Create(Journal journal)
        {
            var result = new LeaderboardSnapshot();
            var definitions = journal.Games.Concat(journal.WeeklyGames).ToDictionary(g => g.Id);
            var byKey = new Dictionary<string, LeaderboardGameCount>(StringComparer.Ordinal);
            var counted = new HashSet<string>(StringComparer.Ordinal);
            Action<GameResult, bool> add = delegate(GameResult completed, bool weekly)
            {
                if (completed == null || !completed.Done || String.IsNullOrWhiteSpace(completed.CompletedUtc)) return;
                var signature = (weekly ? "weekly|" : "daily|") + completed.Id + "|" + completed.CompletedUtc;
                if (!counted.Add(signature)) return;
                Game definition;
                definitions.TryGetValue(completed.Id, out definition);
                var label = definition != null && !String.IsNullOrWhiteSpace(definition.GroupName) ? definition.GroupName :
                    !String.IsNullOrWhiteSpace(completed.GroupName) ? completed.GroupName : definition != null ? definition.Name : completed.Name;
                label = (label ?? "Game").Trim();
                if (label.Length > 50) label = label.Substring(0, 50);
                var key = Key(label);
                LeaderboardGameCount count;
                if (!byKey.TryGetValue(key, out count)) { count = new LeaderboardGameCount { key = key, name = label }; byKey.Add(key, count); }
                if (weekly) count.weekly++; else count.daily++;
            };
            foreach (var day in journal.Days) foreach (var game in day.Games) add(game, false);
            foreach (var record in journal.DailyUtcRecords) add(record.Result, false);
            foreach (var record in journal.WeeklyRecords) add(record.Result, true);
            result.games = byKey.Values.OrderBy(g => g.name, StringComparer.OrdinalIgnoreCase).ToList();
            return result;
        }
        static string Key(string label)
        {
            var slug = Regex.Replace(label.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            if (slug.Length > 40) slug = slug.Substring(0, 40).TrimEnd('-');
            if (slug.Length > 0) return slug;
            using (var sha = SHA256.Create()) return "game-" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(label))).Replace("-", "").Substring(0, 12).ToLowerInvariant();
        }
    }
    public sealed class LeaderboardIdentity
    {
        public string nickname;
        public string inviteCode;
        public static LeaderboardIdentity Load(string folder)
        {
            var path = Path.Combine(folder, "leaderboard-identity.dat");
            if (!File.Exists(path)) return new LeaderboardIdentity();
            var json = Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
            return new JavaScriptSerializer().Deserialize<LeaderboardIdentity>(json) ?? new LeaderboardIdentity();
        }
        public void Save(string folder)
        {
            var bytes = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(this));
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            var path = Path.Combine(folder, "leaderboard-identity.dat");
            var temp = path + ".pending";
            File.WriteAllBytes(temp, protectedBytes);
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }
    public static class LeaderboardUpload
    {
        const string ConfigurationUrl = "https://nullwren.github.io/WrenFeed/data/service.json";
        static string Read(HttpWebRequest request)
        {
            request.Timeout = 12000;
            request.ReadWriteTimeout = 12000;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) return reader.ReadToEnd();
        }
        public static Task SendAsync(string nickname, string inviteCode, LeaderboardSnapshot snapshot)
        {
            return Task.Run(delegate
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var configRequest = (HttpWebRequest)WebRequest.Create(ConfigurationUrl);
                configRequest.Headers[HttpRequestHeader.CacheControl] = "no-cache";
                var config = new JavaScriptSerializer().DeserializeObject(Read(configRequest)) as Dictionary<string, object>;
                if (config == null || !config.ContainsKey("version") || Convert.ToInt32(config["version"]) != 1) throw new InvalidDataException("The leaderboard service configuration is invalid.");
                var endpoint = config.ContainsKey("endpoint") ? config["endpoint"] as string : null;
                if (String.IsNullOrWhiteSpace(endpoint)) throw new InvalidOperationException("Leaderboard uploads are not open yet. Your local progress is safe; try again after the service is announced.");
                Uri baseUri;
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out baseUri) || baseUri.Scheme != Uri.UriSchemeHttps || !baseUri.Host.EndsWith(".workers.dev", StringComparison.OrdinalIgnoreCase) || baseUri.AbsolutePath != "/") throw new InvalidDataException("The leaderboard service address is invalid.");
                var body = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(new { version = 1, nickname = nickname, inviteCode = inviteCode, games = snapshot.games }));
                if (body.Length > 16384) throw new InvalidOperationException("This tally is too large to upload. Please contact the leaderboard owner.");
                var request = (HttpWebRequest)WebRequest.Create(new Uri(baseUri, "upload"));
                request.Method = "POST"; request.ContentType = "application/json"; request.ContentLength = body.Length;
                request.Timeout = 12000; request.ReadWriteTimeout = 12000;
                using (var stream = request.GetRequestStream()) stream.Write(body, 0, body.Length);
                try { Read(request); }
                catch (WebException error)
                {
                    var response = error.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == HttpStatusCode.Unauthorized) throw new InvalidOperationException("That invite code was not accepted.");
                    if (response != null && (int)response.StatusCode == 429) throw new InvalidOperationException("Please wait a minute before uploading again.");
                    throw new InvalidOperationException("The leaderboard could not receive your totals. Check your connection and try again.", error);
                }
            });
        }
    }
}
