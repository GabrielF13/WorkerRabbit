using MongoDB.Driver;
using WorkerRabbit.Services.Interfaces;
using WorkerRabbit.Services;
using WorkerRabbit;

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
                services.AddSingleton<IEmailService, EmailService>();

                var mongoConnectionString = hostContext.Configuration["MongoDB:ConnectionString"];
                if (!string.IsNullOrEmpty(mongoConnectionString))
                {
                    try
                    {
                        services.AddSingleton<IMongoClient>(sp =>
                        {
                            return new MongoClient(mongoConnectionString);
                        });
                        Console.WriteLine("MongoDB client configurado com sucesso");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao configurar MongoDB: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("String de conexão do MongoDB não configurada");
                }

                services.AddHostedService<Worker>();
            })
            .UseWindowsService(); 
}