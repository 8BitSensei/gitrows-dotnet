using gitrows_dotnet.utils;
using System.Net;

namespace gitrows_dotnet
{
    public class GitrowsNative
    {
        
        const string _branch = "HEAD";
        const string _message = "GitRows API Post (https://gitrows.com)";
        const bool _strict = false;
        
        readonly string[] _allowed = { "server", "ns", "owner", "repo", "branch", "path", "user", "token", "message", "author", "csv", "type", "columns", "strict", "default" };
        
        string _user = string.Empty;
        string _token = string.Empty;
        string _ns = "github";

        Author _author = new Author();
        Csv _csv = new Csv();
        Repository _repo = new Repository();
        File _file = new File();

        static HttpClient _sharedClient = new();

        public async Task<(object, HttpStatusCode)> Get(string path, string query, string method) 
        {
            _repo = new Repository();
            _file = new File();

            var task = await Task.Run(() =>
            {
                //TODO Parse path

                //TODO if invalid return 400

                //TODO set query if present

                //TODO construct URL

                //TODO Construct class level repository and file 

                //TODO Get results from PullOrFetch() then parse data with queries

                return (string.Empty, HttpStatusCode.OK);
            });

            return task;
        }

        public async Task<(object, HttpStatusCode)> Pull(string url) 
        {
            var task = await Task.Run(async () =>
            {
                //TODO Parse path
                var pathData = GitPath.Parse(url);
                if (pathData == null)
                    return (string.Empty, HttpStatusCode.BadRequest);

                //TODO set data to class level option

                //TODO Validate options

                //TODO set auth if available

                //TODO construct URL

                //TODO GET with headers then return as json
                var response = await _sharedClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    _repo.Private = false;
                    var data = await response.Content.ReadAsStringAsync();
                    return (data, response.StatusCode);
                }

                return (string.Empty, HttpStatusCode.OK);
            });

            return task;
        }

        //TODO Add headers as optional arg
        private async Task<(object?, HttpStatusCode)> PullOrFetch(string url, string method) 
        {
            if (method.Equals("pull")) 
            {
                var pullResponse = await Pull(url);
                _repo.Private = true;
                return pullResponse;
            }

            var response = await _sharedClient.GetAsync(url);
            if (response.IsSuccessStatusCode) 
            {
                _repo.Private = false;
                var data = await response.Content.ReadAsStringAsync();
                return (data, response.StatusCode);
            }

            //Request was not succesfull, attempt with tokens
            if (!string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_token) && _ns.Equals("github")) 
            {
                var pullResponse = await Pull(url);
                _repo.Private = true;
                return pullResponse;
            }

            return (null, response.StatusCode);
        }

        private void SetOptions(object obj) 
        {
        
        }

        class Author 
        {
            public string? Name { get; set; } = "GitRows";
            public string? Email { get; set; } = "api@gitrows.com";
        }

        class Csv 
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
            public Uri? Url { get; set; }
            public string? PathData { get; set; }
            public string? Owner { get; set; }
            public string? Ns { get; set; }
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
            unknown   
        }

        class Count 
        {
            public int Total { get; set; }
            public int Query { get; set; }
        }

        
    }
}
