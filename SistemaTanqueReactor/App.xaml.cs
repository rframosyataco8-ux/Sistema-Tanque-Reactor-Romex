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

        // Configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TanqueReactorContext>(options =>
            options.UseSqlServer(connectionString));

        // Services
        services.AddScoped<CargaService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<NuevaCargaViewModel>();
        services.AddTransient<HistorialViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        // Verificar conexión a BD al iniciar
        try
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TanqueReactorContext>();
            context.Database.CanConnect();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo conectar a la base de datos.\n\nVerifica:\n1. Que SQL Server esté corriendo\n2. Que hayas ejecutado el script CreateDatabase.sql\n3. La cadena de conexión en appsettings.json\n\nError: {ex.Message}",
                "Error de conexión",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
