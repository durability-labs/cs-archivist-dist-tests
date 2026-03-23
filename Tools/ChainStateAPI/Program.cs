using ChainStateAPI.Services;
using Logging;

namespace ChainStateAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var log = new TimestampPrefixer(
                new LogSplitter(
                    new FileLog(Path.Combine("logs", "chainstateapi")),
                    new ConsoleLog()
                )
            );

            var deploymentService = new DeploymentService(log);
            builder.Services.AddSingleton<ILog>(log);
            builder.Services.AddSingleton<IDeploymentService>(deploymentService);

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();

            deploymentService.Initialize();

            app.MapControllers();
            app.Run();
        }
    }
}
