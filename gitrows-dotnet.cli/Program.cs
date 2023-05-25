namespace gitrows_dotnet.cli 
{
    public class Program 
    {
        public static async Task Main()
        {
            var client = new Gitrows();
            var result = await client.Get();
        }
    }
}