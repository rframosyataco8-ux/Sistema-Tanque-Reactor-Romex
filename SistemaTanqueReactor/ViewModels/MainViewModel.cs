using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaTanqueReactor.Views;

namespace SistemaTanqueReactor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentPageTitle = "Dashboard";

    [ObservableProperty]
    private DateTime _fechaActual = DateTime.Now;

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
        NavigateToDashboard();
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        switch (page)
        {
            case "Dashboard":
                NavigateToDashboard();
                break;
            case "NuevaCarga":
                NavigateToNuevoRegistro();
                break;
            case "Historial":
                NavigateToHistorialMensual();
                break;
            case "Maestros":
                CurrentPageTitle = "Maestros";
                CurrentView = new TextBlockPlaceholder("Módulo de Maestros - Próximamente");
                break;
        }
    }

    private void NavigateToDashboard()
    {
        CurrentPageTitle = "Dashboard";
        var vm = _services.GetRequiredService<DashboardViewModel>();
        CurrentView = new DashboardView { DataContext = vm };
        _ = vm.CargarDatosAsync();
    }

    private void NavigateToNuevoRegistro()
    {
        CurrentPageTitle = "Nuevo Registro";
        var vm = _services.GetRequiredService<NuevoRegistroViewModel>();
        CurrentView = new NuevoRegistroView { DataContext = vm };
        _ = vm.InicializarAsync();
    }

    private void NavigateToHistorialMensual()
    {
        CurrentPageTitle = "Historial Mensual";
        var vm = _services.GetRequiredService<HistorialMensualViewModel>();
        CurrentView = new HistorialMensualView { DataContext = vm };
        _ = vm.CargarAsync();
    }
}

public class TextBlockPlaceholder : System.Windows.Controls.TextBlock
{
    public TextBlockPlaceholder(string text)
    {
        Text = text;
        FontSize = 18;
        HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        VerticalAlignment = System.Windows.VerticalAlignment.Center;
        Foreground = System.Windows.Media.Brushes.Gray;
    }
}
