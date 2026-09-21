using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTanqueReactor.Data;
using SistemaTanqueReactor.Services;
using SistemaTanqueReactor.ViewModels;
using SistemaTanqueReactor.Views;

namespace SistemaTanqueReactor;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TanqueReactorContext>(options =>
            options.UseSqlServer(connectionString));

        // Services
        services.AddScoped<CargaService>();
        services.AddSingleton<ProduccionService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<NuevaCargaViewModel>();
        services.AddTransient<HistorialViewModel>();
        services.AddTransient<NuevoRegistroViewModel>();
        services.AddTransient<HistorialMensualViewModel>();

        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        try
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TanqueReactorContext>();
            context.Database.CanConnect();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo conectar a la base de datos.\n\nVerifica:\n1. Que SQL Server esté corriendo\n2. Que hayas ejecutado los scripts SQL\n3. La cadena de conexión en appsettings.json\n\nError: {ex.Message}",
                "Error de conexión",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
