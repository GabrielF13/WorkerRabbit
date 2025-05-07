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

                // Configura��o do MongoDB (se as configura��es estiverem presentes)
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
                        // Continuar sem MongoDB
                    }
                }
                else
                {
                    Console.WriteLine("String de conex�o do MongoDB n�o configurada");
                }

                // Registrar o servi�o worker
                services.AddHostedService<Worker>();
            })
            .UseWindowsService(); 
}