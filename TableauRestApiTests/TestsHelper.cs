using Microsoft.Extensions.Configuration;

namespace TableauRestApiTests
{
    public static class TestsHelper
    {
        public static IConfigurationRoot GetIConfigurationRoot()
        {
            return new ConfigurationBuilder()
                //.SetBasePath(outputPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets("3223a6c2-2965-4811-915f-84031feb10c6") 
                .Build();
        }

        //Follow the link below for guidance on how to use appsettings and secrets in Unit Test projects
        //https://weblog.west-wind.com/posts/2018/Feb/18/Accessing-Configuration-in-NET-Core-Test-Projects
        
    }
}
