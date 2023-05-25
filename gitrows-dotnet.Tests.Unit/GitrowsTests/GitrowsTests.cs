using Xunit;

namespace gitrows_dotnet.Tests.Unit.GitrowsTests
{
    public class GitrowsTests
    {
        [Fact]
        public async void GitrowsGet_ValidData_ValidReturn()
        {
            var sut = new GitrowsJS();

            var result = await sut.Get();

            result.Equals(8);
        }
    }
}