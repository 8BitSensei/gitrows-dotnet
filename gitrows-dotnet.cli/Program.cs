using System.Reflection.Metadata.Ecma335;

namespace gitrows_dotnet.cli 
{
    public class Program 
    {
        public static async Task Main()
        {
            var client = new GitrowsNative();
            var result = await client.Get("@github/gitrows/data/iris.json", (x => (x.GetType().GetProperty("species")) == "virginica"), "pull");
            Console.Write(result.Item1);
        }
    }
}