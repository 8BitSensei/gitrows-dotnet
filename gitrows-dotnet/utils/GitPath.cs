using System.Text.RegularExpressions;

namespace gitrows_dotnet.utils
{
    public static class GitPath
    {
        static Regex _pathRegex = new Regex("^(?:(?:(?:(?:@)(?<ns>[\\w]+)\\/)?(?:(?<owner>[\\w-]+)?\\/)(?<repo>[\\w\\-\\.]+)(?:(?:#)(?<branch>[\\w-]+))?)|(?:\\.))\\/?(?<path>[\\w\\-\\/.]*(?:\\.([\\w]{2,4}))|[\\w\\/]*)?(?:\\/)?([\\w]+)?");
        static Regex _urlRegex = new Regex("https?:\\/\\/[\\w\\.]*(?<ns>github|gitlab)[\\w]*.com\\/(?<owner>[\\w-]+)\\/(?<repo>[\\w\\-\\.]+)\\/(?:(?:-\\/)?(?:blob\\/|raw\\/)?(?<branch>[\\w]+)\\/)(?<path>[\\w\\/\\-\\.]+.(?:json|csv))");
        
        public static GroupCollection? Parse(string path)
        {
            var isUrl = Uri.TryCreate(path, UriKind.Absolute, out var url);
            if (isUrl) 
                return ParseUrl(path);

            return ParsePath(path);
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
    }
}
