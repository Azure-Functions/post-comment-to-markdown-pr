using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(PostCommentToMarkdownPR.Startup))]
namespace PostCommentToMarkdownPR
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var context = builder.GetContext();
            var config = new ConfigurationBuilder()
                .SetBasePath(context.ApplicationRootPath)
                .AddEnvironmentVariables()                  // Prod Azure variables
                .Build();

            builder.Services.Configure<PostCommentSettings>(config.GetSection("PostCommentSettings"));
            builder.Services.AddOptions();
        }
    }
}