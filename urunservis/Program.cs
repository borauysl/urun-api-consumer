using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using urunservis;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClient<Worker>(client =>
                {
                    client.BaseAddress = new Uri("https://localhost:7205/");
                });

                services.AddHostedService<Worker>();
            });
}