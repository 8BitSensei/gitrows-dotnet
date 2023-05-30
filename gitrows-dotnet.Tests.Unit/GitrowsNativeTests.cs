using Xunit;

namespace gitrows_dotnet.Tests.Unit
{
    public class GitrowsNativeTests
    {
        [Fact]
        public async void GitrowsGet_ValidData_ValidReturn()
        {
            var sut = new GitrowsNative();

            var result = await sut.Pull("@github/gitrows/data/iris.json");

        }
    }
}