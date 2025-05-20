using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
 
namespace SystemHTTPListener
{
    public class UrlRule
    {
        [JsonPropertyName("host")]
        public List<Dictionary<string, HostRuleDetails>> Host { get; set; } = new List<Dictionary<string, HostRuleDetails>>();
    }

    public class HostRuleDetails
    {
        [JsonPropertyName("allow")]
        public bool Allow { get; set; }

        [JsonPropertyName("path")]
        public List<string> Path { get; set; } = new List<string>();
    }
    public class UrlBlocker
    {
        private static UrlRule _rules;
        private static DateTime _lastLoadTime;
        private static readonly object _lock = new object();
        private static readonly string _ruleFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RULE.json");

        public static void LoadRules()
        {
            lock (_lock)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    string json = File.ReadAllText(_ruleFilePath);
                    _rules = JsonSerializer.Deserialize<UrlRule>(json, options);
                    _lastLoadTime = DateTime.Now;
                    Console.WriteLine("规则加载成功");
                    Console.WriteLine($"共{_rules.Host.Count}条规则");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"规则加载失败: {ex.Message}");
                    _rules = new UrlRule();
                }
            }
        }

        public static bool ShouldBlockRequest(string url)
        {
            if (_rules == null || File.GetLastWriteTime(_ruleFilePath) > _lastLoadTime)
            {
                LoadRules();
            }

            if (_rules == null || !_rules.Host.Any()) return false;

            try
            {
                Uri uri = new Uri(url);
                string host = uri.Host;
                string path = uri.AbsolutePath.ToLower();
                string query = uri.Query.ToLower();

                foreach (var hostRule in _rules.Host)
                {
                    foreach (var rule in hostRule)
                    {
                        string pattern = ConvertWildcardToRegex(rule.Key);
                        if (Regex.IsMatch(host, pattern, RegexOptions.IgnoreCase))
                        {
                            if (rule.Value.Allow == false)
                            {
                                return true;
                            }

                            if (rule.Value.Path != null && rule.Value.Path.Any())
                            {
                                foreach (var blockedPath in rule.Value.Path)
                                {
                                    string pathPattern = ConvertPathWildcardToRegex(blockedPath);
                                    if (Regex.IsMatch(path, pathPattern, RegexOptions.IgnoreCase) ||
                                        Regex.IsMatch(query, pathPattern, RegexOptions.IgnoreCase))
                                    {
                                        return true;
                                    }
                                }
                            }
                            return false;
                        }
                    }
                }
            }
            catch (UriFormatException)
            {
                return true;
            }

            return false;
        }
        private static string ConvertWildcardToRegex(string pattern)
        {
            string regexPattern = Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".");

            if (regexPattern.StartsWith("^.*\\."))
            {
                string withoutSubdomain = regexPattern.Substring(4);
                regexPattern = $"(^{withoutSubdomain}|{regexPattern})";
            }

            if (!regexPattern.StartsWith("^")) regexPattern = "^" + regexPattern;
            if (!regexPattern.EndsWith("$")) regexPattern = regexPattern + "$";

            return regexPattern;
        }

        private static string ConvertPathWildcardToRegex(string pathPattern)
        {
            string regexPattern = Regex.Escape(pathPattern.ToLower())
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".");

            if (regexPattern.StartsWith(".*/"))
            {
                regexPattern = string.Concat("^.*", regexPattern.AsSpan(2));
            }
            else if (regexPattern.EndsWith("/.*"))
            {
                regexPattern = regexPattern.Substring(0, regexPattern.Length - 3) + "/.*$";
            }
            if (!regexPattern.StartsWith("^")) regexPattern = "^" + regexPattern;
            if (!regexPattern.EndsWith("$")) regexPattern = regexPattern + "$";

            return regexPattern;
        }
    }
}
