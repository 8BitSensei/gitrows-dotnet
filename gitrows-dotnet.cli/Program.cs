namespace gitrows_dotnet.cli
{
    public class Program 
    {
        public static async Task Main()
        {
            var client = new Gitrows(user: "", token: "");
            var result = await client.Delete("@github/8BitSensei/Templum-Data#testing/data/templum_sites.json", new Dictionary<string, string> { {"index", "eq:2" } });
        }
    }
}