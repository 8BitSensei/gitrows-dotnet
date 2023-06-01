using gitrows_dotnet.utils;
using System.Dynamic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

            //var resource = Path.GetFileName(_repo.PathData);

            var constructedUrl = GitPath.ToUrl(path, true);
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

        public async Task<HttpStatusCode> Delete(string path, Dictionary<string, string>? query = null)
        {
            var pathData = GitPath.Parse(path);
            if (pathData == null)
                return HttpStatusCode.BadRequest;

            if (query == null)
                return HttpStatusCode.NotModified;

            pathData.TryGetValue("type", out var typeGroup);
            if (typeGroup == null)
                return HttpStatusCode.BadRequest;

            var results = await PullOrFetch(path, "pull");
            var data = await Parse(results.Item1, typeGroup.Value);
            data = Filter(data, query);

            var pushResults = await Push(path, data);
            return pushResults;
        }

        public async Task<HttpStatusCode> Types(string path, object? filter = null)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> Push(string path, object? data = null)
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

        //TODO base 64 decode in here
        public async Task<(string, HttpStatusCode)> Pull(string url)
        {
            var pathData = GitPath.Parse(url);
            if (pathData == null)
                return (string.Empty, HttpStatusCode.BadRequest);

           if(!pathData.TryGetValue("path", out _))
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
                var data = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(data))
                    return (string.Empty, HttpStatusCode.NoContent);

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                var gitResponse = await JsonSerializer.DeserializeAsync<GitResponse>(stream);
                if (gitResponse == null || string.IsNullOrEmpty(gitResponse.Content))
                    return (string.Empty, HttpStatusCode.NoContent);

                return (gitResponse.Content, response.StatusCode);
            }

            return (string.Empty, response.StatusCode);
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

                return (data, response.StatusCode);
            }

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

                if (property.StartsWith("$"))
                {
                    switch (property)
                    {
                        case "$count":
                            agregateActions.Add(x =>
                            {
                                dynamic countObject = new ExpandoObject();
                                countObject.Result = x.Count;
                                return new List<ExpandoObject>
                                {
                                    countObject
                                };
                            });
                            break;
                        case "$avg":
                            agregateActions.Add(x =>
                            {
                                if (x.TryGetAverageAsInt(condition, out var asInt))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asInt;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                if (x.TryGetAverageAsDouble(condition, out var asDouble))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asDouble;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                return new List<ExpandoObject>();
                            });
                            break;
                        case "$sum":
                            agregateActions.Add(x =>
                            {
                                if (x.TryGetSumAsInt(condition, out var asInt))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asInt;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                if (x.TryGetSumAsDouble(condition, out var asDouble))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asDouble;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                return new List<ExpandoObject>();
                            });
                            break;
                        case "$min":
                            agregateActions.Add(x =>
                            {
                                if (x.TryGetMinAsInt(condition, out var asInt))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asInt;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                if (x.TryGetMinAsDouble(condition, out var asDouble))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asDouble;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                return new List<ExpandoObject>();
                            });
                            break;
                        case "$max":
                            agregateActions.Add(x =>
                            {
                                if (x.TryGetMaxAsInt(condition, out var asInt))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asInt;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                if (x.TryGetMaxAsDouble(condition, out var asDouble))
                                {
                                    dynamic countObject = new ExpandoObject();
                                    countObject.Result = asDouble;
                                    return new List<ExpandoObject>
                                    {
                                        countObject
                                    };
                                }

                                return new List<ExpandoObject>();
                            });
                            break;
                        case "$select":
                            agregateActions.Add(x =>
                            {
                                if (x.TrySelectProperties(condition, out var selected))
                                    return selected;

                                return new List<ExpandoObject>();
                            });
                            break;
                        case "$order":
                            break;
                        case "$limit":
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var split = condition.Split(':');
                    if (split.Count() < 2)
                        break;

                    var @operator = split[0];
                    var value = split[1];

                    switch (@operator)
                    {
                        case "eq":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsInt(property, out var asInt) && asInt == int.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsDouble(property, out var asDouble) && asDouble == double.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsString(property, out var asString) && asString.Equals(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "not":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsInt(property, out var asInt) && asInt != int.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsDouble(property, out var asDouble) && asDouble != double.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsString(property, out var asString) && !asString.Equals(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "lt":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsInt(property, out var asInt) && asInt < int.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsDouble(property, out var asDouble) && asDouble < double.Parse(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "lte":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsInt(property, out var asInt) && asInt <= int.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsDouble(property, out var asDouble) && asDouble <= double.Parse(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "gt":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsInt(property, out var asInt) && asInt > int.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsDouble(property, out var asDouble) && asDouble > double.Parse(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "gte":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsInt(property, out var asInt) && asInt >= int.Parse(value))
                                    return true;
                                if (x.TryParsePropertyAsDouble(property, out var asDouble) && asDouble >= double.Parse(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "starts":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsString(property, out var asString) && asString.StartsWith(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "contains":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsString(property, out var asString) && asString.Contains(value))
                                    return true;

                                return false;
                            });
                            break;
                        case "ends":
                            conditionActions.Add(x =>
                            {
                                if (x.TryParsePropertyAsString(property, out var asString) && asString.EndsWith(value))
                                    return true;

                                return false;
                            });
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

            if (tmp.Count > 0) 
                data = tmp;

            foreach (var action in agregateActions)
                data = action(data);

            return data;
        }

        private static async Task<List<ExpandoObject>> Parse(string data, string type)
        {
            switch (type)
            {
                case "json":
                    MemoryStream stream;
                    if (data.StartsWith('[')) 
                    {
                        stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                        var jsonList = await JsonSerializer.DeserializeAsync<List<ExpandoObject>>(stream);
                        if (jsonList == null)
                            return new List<ExpandoObject>();

                        return jsonList;
                    }

                    stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                    var jsonObject = await JsonSerializer.DeserializeAsync<ExpandoObject>(stream);
                    if (jsonObject == null)
                        return new List<ExpandoObject>();

                    return new List<ExpandoObject>{jsonObject};
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
