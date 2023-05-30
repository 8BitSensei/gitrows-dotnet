namespace gitrows_dotnet.cli
{
    public class Program 
    {
        public static async Task Main()
        {
            var client = new GitrowsNative();
            var result = await client.Get("@github/gitrows/data/iris.json", new Dictionary<string, string> { { "sepalLength", "gte:5" } }, "pull");
            Console.Write(result.Item1);
        }
    }
}