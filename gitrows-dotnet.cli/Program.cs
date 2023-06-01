namespace gitrows_dotnet.cli
{
    public class Program 
    {
        public static async Task Main()
        {
            var client = new GitrowsNative();
            var result = await client.Delete("@github/gitrows/data/iris.json", new Dictionary<string, string> { { "sepalLength", "gt:6" } });
        }
    }
}