using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

namespace gitrows_dotnet.utils
{
    public static class GitPath
    {
        // See https://regex101.com/r/mWxhnm/1
        static Regex _pathRegex = new Regex("^(?:(?:(?:(?:@)(?<ns>[\\w]+)\\/)?(?:(?<owner>[\\w-]+)?\\/)(?<repo>[\\w\\-\\.]+)(?:(?:#)(?<branch>[\\w-]+))?)|(?:\\.))\\/?(?<path>[\\w\\-\\/.]*(?:\\.(?<type>[\\w]{2,4}))|[\\w\\/]*)?(?:\\/)?");
        // See https://regex101.com/r/RZneHI/1
        static Regex _urlRegex = new Regex("https?:\\/\\/[\\w\\.]*(?<ns>github|gitlab)[\\w]*.com\\/(?<owner>[\\w-]+)\\/(?<repo>[\\w\\-\\.]+)\\/(?:(?:-\\/)?(?:blob\\/|raw\\/)?(?<branch>[\\w\\-]+)\\/)(?<path>[\\w\\-\\/.]*(?:\\.(?<type>[\\w]{2,4}))|[\\w\\/]*)?(?:\\/)?");

        readonly static string[] _allowedTypes = { "csv", "json", "yaml" };
        readonly static string[] _mandatoryGroups = { "ns", "owner", "repo", "path" };

        public static GroupCollection? Parse(string path)
        {
            var isUrl = Uri.TryCreate(path, UriKind.Absolute, out var url);
            if (isUrl) 
                return ParseUrl(path);

            return ParsePath(path);
        }

        public static string? ToApi(string path) 
        {
            var groupCollection = Parse(path);
            if (groupCollection == null) 
                return null;

            if (!IsValid(groupCollection))
                return null;

            groupCollection.TryGetValue("ns", out var ns);
            groupCollection.TryGetValue("owner", out var owner);
            groupCollection.TryGetValue("repo", out var repo);
            groupCollection.TryGetValue("path", out var groupPath);
            if (ns!.Value.Equals("github")) 
                return $"https://api.github.com/repos/{owner!.Value}/{repo!.Value}/contents/{groupPath!.Value}";

            //TODO Gitlab


            return null;
        }

        public static string? ToUrl(string path, bool raw = false)
        {
            var groupCollection = Parse(path);
            if (groupCollection == null)
                return null;

            if (!IsValid(groupCollection))
                return null;

            //tOD better error handling
            groupCollection.TryGetValue("ns", out var groupNs);
            var ns = groupNs?.Value;
            groupCollection.TryGetValue("owner", out var groupOwner);
            var owner = groupOwner?.Value;
            groupCollection.TryGetValue("repo", out var groupRepo);
            var repo = groupRepo?.Value;
            groupCollection.TryGetValue("path", out var groupPath);
            var urlPath = groupPath?.Value;
            groupCollection.TryGetValue("branch", out var groupBranch);
            var branch = groupBranch?.Value;
            if (string.IsNullOrEmpty(branch))
                branch = "master";

            string server;
            string format;
            if (ns!.Equals("github")) 
            {
                server = raw ? "raw.githubusercontent.com" : "github.com";
                format = raw ? string.Empty : "blob/";
                return $"https://{server}/{owner}/{repo}/{format}{branch}/{urlPath}";
            }

            server = "gitlab.com";
            format = raw ? "raw" : "blob";
            return $"https://{server}/{owner}/{repo}/-/{format}/{branch}/{path}";
        }

        public static GroupCollection? ParseUrl(string path) 
        {
            var match = _urlRegex.Match(path);
            if (match == null)
                return null;

            return match.Groups;
        }

        public static GroupCollection? ParsePath(string path)
        {
            var match = _pathRegex.Match(path);
            if (match == null)
                return null;

            return match.Groups;
        }

        public static bool IsValid(GroupCollection groupCollection) 
        {
            groupCollection.TryGetValue("type", out var typeGroup);
            if (typeGroup == null)
                return false;

            var type = typeGroup.Value;
            if (!_allowedTypes.Contains(type))
                return false;

            for (var i = 0; i < _mandatoryGroups.Length; i++) 
            {
                groupCollection.TryGetValue(_mandatoryGroups[i], out var tmp);
                if (tmp == null)
                    return false;
            }

            return true;
        }
    }
}
