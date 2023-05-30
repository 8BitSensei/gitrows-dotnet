using gitrows_dotnet.utils;
using System.Dynamic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gitrows_dotnet
{
    public class GitrowsNative
    {
        string _server;
        Repository _repo;
        string _branch;
        string _user;
        string _token;
        string _message;
        Author _author;
        Csv _csv;
        string _types;
        bool _strict;
        string _default;

        File _file = new File();
        static HttpClient _sharedClient = new();

        public GitrowsNative(string? server = null, string ns = "github", string? owner = null, string? repo = null, string branch = "HEAD", string? path = null, string? user = null, string? token = null, string message = "Gitrows-Dotnet API Post", Author? author = null, Csv? csv = null, string? types = null, bool strict = false, string? @default = null)
        {
            _server = string.IsNullOrEmpty(server) ? string.Empty : server;
            _repo = new Repository(string.IsNullOrEmpty(path) ? string.Empty : path, string.IsNullOrEmpty(owner) ? string.Empty : owner, ns);
            _branch = branch;
            _user = string.IsNullOrEmpty(user) ? string.Empty : user;
            _token = string.IsNullOrEmpty(token) ? string.Empty : token;
            _message = message;
            _author = author == null ? new Author() : author;
            _csv = csv == null ? new Csv() : csv;
            _types = string.IsNullOrEmpty(types) ? string.Empty : types;
            _strict = strict;
            _default = string.IsNullOrEmpty(@default) ? string.Empty : @default;
        }

        public async Task<(string?, HttpStatusCode)> Get(string path, Dictionary<string, string>? query = null, string method = "fetch")
        {
            var pathData = GitPath.Parse(path);
            if (pathData == null)
                return (string.Empty, HttpStatusCode.BadRequest);

            pathData.TryGetValue("path", out var pathGroup);
            if (pathGroup == null)
                return (string.Empty, HttpStatusCode.BadRequest);

            if (!GitPath.IsValid(pathData))
                return (string.Empty, HttpStatusCode.BadRequest);

            _repo.PathData = pathGroup.Value;

            var resource = Path.GetFileName(_repo.PathData);
            /*if (!string.IsNullOrEmpty(resource))
            {
                query ??= new ExpandoObject();
                query.Id = resource;
            }*/

            var constructedUrl = GitPath.ToUrl(path);
            if (string.IsNullOrEmpty(constructedUrl))
                return (string.Empty, HttpStatusCode.BadRequest);

            var result = await PullOrFetch(constructedUrl, method);
            if (!string.IsNullOrEmpty(result.Item1))
            {
                pathData.TryGetValue("type", out var type);
                var parsableObject = await Parse(result.Item1, type!.Value);
                if (parsableObject != null && query != null)
                {
                    var filtered = Filter(parsableObject, query);
                    var serialised = JsonSerializer.Serialize(filtered, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    return (serialised, HttpStatusCode.OK);
                }
            }

            return (result.Item1, HttpStatusCode.OK);
        }

        public async Task<HttpStatusCode> Put(string path, object data)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> Update(string path, object data, object? filter = null)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> Replace(string path, object data)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> Delete(string path, object? filter = null)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> Types(string path, object? filter = null)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> Create(string path, object? data = null)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> Drop(string path)
        {
            throw new NotImplementedException();
        }

        public async Task<(string, HttpStatusCode)> Pull(string url)
        {
            var pathData = GitPath.Parse(url);
            if (pathData == null)
                return (string.Empty, HttpStatusCode.BadRequest);

            pathData.TryGetValue("path", out var pathGroup);
            if (pathGroup == null)
                return (string.Empty, HttpStatusCode.BadRequest);

            if (!GitPath.IsValid(pathData))
                return (string.Empty, HttpStatusCode.BadRequest);

            //TODO set pathData to class level option

            if (!string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_repo.Ns))
                _sharedClient.DefaultRequestHeaders.Add("Authorization", $"{_user}:{_token}");

            _sharedClient.DefaultRequestHeaders.Add("User-Agent", "gitrows-dotnet");
            var constructedUrl = GitPath.ToApi(url);

            //TODO This can be improved, too much handing around
            var response = await _sharedClient.GetAsync(constructedUrl);
            if (response.IsSuccessStatusCode)
            {
                var x = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(x))
                    return (string.Empty, HttpStatusCode.NoContent);

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(x));
                var gitResponse = await JsonSerializer.DeserializeAsync<GitResponse>(stream);
                if (gitResponse == null || string.IsNullOrEmpty(gitResponse.Content))
                    return (string.Empty, HttpStatusCode.NoContent);

                return (gitResponse.Content, response.StatusCode);
            }

            return (string.Empty, HttpStatusCode.OK);
        }

        //TODO Add headers as optional arg
        public async Task<(string?, HttpStatusCode)> PullOrFetch(string url, string method)
        {
            if (method.Equals("pull"))
            {
                var pullResponse = await Pull(url);
                _repo.Private = true;

                var decoded = Convert.FromBase64String(pullResponse.Item1);
                var str = Encoding.Default.GetString(decoded);

                return (str, pullResponse.Item2);
            }

            _sharedClient.DefaultRequestHeaders.Add("User-Agent", "gitrows-dotnet");
            var response = await _sharedClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                _repo.Private = false;
                var data = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(data))
                    return (string.Empty, HttpStatusCode.NoContent);

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                var gitResponse = await JsonSerializer.DeserializeAsync<GitResponse>(stream);
                if (gitResponse == null || string.IsNullOrEmpty(gitResponse.Content))
                    return (string.Empty, HttpStatusCode.NoContent);

                return (gitResponse.Content, response.StatusCode);
            }

            //Request was not succesfull, attempt with tokens
            if (!string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_token) && _repo.Ns!.Equals("github"))
            {
                var pullResponse = await Pull(url);
                _repo.Private = true;
                return pullResponse;
            }

            return (null, response.StatusCode);
        }

        private List<ExpandoObject> Filter(List<ExpandoObject> data, Dictionary<string, string> query)
        {
            var conditionActions = new List<Func<ExpandoObject, bool>>();
            var agregateActions = new List<Func<List<ExpandoObject>, List<ExpandoObject>>>();
            for (var i = 0; i < query.Count; i++)
            {
                var statement = query.ElementAt(i);
                var property = statement.Key;
                var condition = statement.Value;

                // Agregate functions - not supported
                if (property.ElementAt(0).Equals("$"))
                {

                }
                // Filter conditons
                else
                {
                    var split = condition.Split(':');
                    if (split.Count() < 2)
                        break;

                    var @operator = split[0];
                    var value = split[1];
                    var valueType = FindType(value);

                    switch (@operator)
                    {
                        case "eq":
                            if (valueType == typeof(int))
                                conditionActions.Add(x => 
                                x.TryParsePropertyAsInt(property, out var asInt) && asInt == int.Parse(value));
                            else if (valueType == typeof(string))
                                conditionActions.Add(x =>
                                    (x as IDictionary<string, object>)[property].ToString().Equals(value));
                            break;
                        case "not":
                            if (valueType == typeof(int))
                                conditionActions.Add(x =>
                                    (int)(x as IDictionary<string, object>)[property] != int.Parse(value));
                            else if (valueType == typeof(string))
                                conditionActions.Add(x =>
                                    (string)(x as IDictionary<string, object>)[property] != value);
                            break;
                        case "lt":
                            if (valueType == typeof(int))
                                conditionActions.Add(x =>
                                    (int)(x as IDictionary<string, object>)[property] < int.Parse(value));
                            break;
                        case "lte":
                            if (valueType == typeof(int))
                                conditionActions.Add(x =>
                                    (int)(x as IDictionary<string, object>)[property] <= int.Parse(value));
                            break;
                        case "gt":
                            if (valueType == typeof(int))
                                conditionActions.Add(x =>
                                    (int)(x as IDictionary<string, object>)[property] > int.Parse(value));
                            break;
                        case "gte":
                            if (valueType == typeof(int))
                                conditionActions.Add(x =>
                                {
                                    var a = double.Parse((x as IDictionary<string, object>)[property].ToString());
                                    var b = int.Parse(value);
                                    if (a >= b)
                                        return true;

                                    return false;
                                });
                            break;
                        case "starts":
                            if (valueType == typeof(string))
                                conditionActions.Add(x =>
                                    ((string)(x as IDictionary<string, object>)[property]).StartsWith(value));
                            break;
                        case "contains":
                            if (valueType == typeof(string))
                                conditionActions.Add(x =>
                                    ((string)(x as IDictionary<string, object>)[property]).Contains(value));
                            break;
                        case "ends":
                            if (valueType == typeof(string))
                                conditionActions.Add(x =>
                                    ((string)(x as IDictionary<string, object>)[property]).EndsWith(value));
                            break;
                        default:
                            break;
                    }
                }
            }

            var tmp = new List<ExpandoObject>();
            foreach (var action in conditionActions)
            {
                foreach (var datum in data)
                {
                    if (action(datum))
                        tmp.Add(datum);
                }
            }

            return tmp;
        }

        private Type FindType(string value)
        {
            if (int.TryParse(value, out var _))
                return typeof(int);

            return typeof(string);
        }

        private async Task<List<ExpandoObject>> Parse(string data, string type)
        {
            switch (type)
            {
                case "json":
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                    var jsonObject = await JsonSerializer.DeserializeAsync<List<ExpandoObject>>(stream);
                    if (jsonObject == null)
                        return new List<ExpandoObject>();

                    return jsonObject;
                default:
                    return new List<ExpandoObject>();
            }
        }

        class GitResponse
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        public class Author
        {
            public string Name { get; set; } = "GitRows";
            public string Email { get; set; } = "api@gitrows.com";
        }

        public class Csv
        {
            public char Delimiter { get; set; } = ',';
        }

        class Meta
        {
            public Repository? Repository { get; set; }
            public File? File { get; set; }
            public int Count { get; set; }
        }

        class Repository
        {
            public Repository(string pathData, string owner, string ns)
            {
                PathData = pathData;
                Owner = owner;
                Ns = ns;
            }

            public Uri? Url { get; set; }
            public string PathData { get; set; }
            public string Owner { get; set; }
            public string Ns { get; set; }
            public bool Private { get; set; }
        }

        class File
        {
            public FileType Type { get; set; }

            //No idea yet what this is
            public object? Mime { get; set; }
        }

        enum FileType
        {
            json,
            csv,
            yaml
        }

        class Count
        {
            public int Total { get; set; }
            public int Query { get; set; }
        }


    }
}
