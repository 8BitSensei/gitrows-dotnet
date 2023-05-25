using Jering.Javascript.NodeJS;

namespace gitrows_dotnet
{
    public class GitrowsJS
    {
        public async Task<string> Get()
        {
            string javascriptModule = @"
            const Gitrows = require('./gitrows');
            const gitrows = new Gitrows();
            let path = '@github/gitrows/data/iris.json';
            module.exports = (callback, x) => {  // Module must export a function that takes a callback as its first parameter
                gitrows.get(x)
                    .then((data) => {
                        console.log(data);
                    })
                    .catch( (error) => {
                    });
            }";

            // Invoke javascript
            string result = await StaticNodeJSService.InvokeFromStringAsync<string>(javascriptModule, args: new object[] { "@github/gitrows/data/iris.json" });

            return result;
        }
    }
}