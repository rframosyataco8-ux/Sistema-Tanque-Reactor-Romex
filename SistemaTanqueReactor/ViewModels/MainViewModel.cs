using System.Windows.Input;
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
                NavigateToNuevaCarga();
                break;
            case "Historial":
                NavigateToHistorial();
                break;
            case "Maestros":
                CurrentPageTitle = "Maestros";
                CurrentView = new TextBlockPlaceholder("Módulo de Maestros (Lotes, Operarios, Insumos) - Próximamente");
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

    private void NavigateToNuevaCarga()
    {
        CurrentPageTitle = "Nueva Carga";
        var vm = _services.GetRequiredService<NuevaCargaViewModel>();
        CurrentView = new NuevaCargaView { DataContext = vm };
        _ = vm.InicializarAsync();
    }

    private void NavigateToHistorial()
    {
        CurrentPageTitle = "Historial de Cargas";
        var vm = _services.GetRequiredService<HistorialViewModel>();
        CurrentView = new HistorialView { DataContext = vm };
        _ = vm.CargarDatosAsync();
    }
}

// Placeholder simple para módulos pendientes
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
