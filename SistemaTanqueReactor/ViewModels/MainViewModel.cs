using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaTanqueReactor.Views;

namespace SistemaTanqueReactor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private string _currentPageTitle = "Dashboard";
    [ObservableProperty] private DateTime _fechaActual = DateTime.Now;

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
            case "Registro":
                // Planilla mensual (antes "Historial")
                CurrentPageTitle = "Registro mensual";
                var regVm = _services.GetRequiredService<HistorialMensualViewModel>();
                CurrentView = new HistorialMensualView { DataContext = regVm };
                _ = regVm.CargarAsync();
                break;
            case "Historial":
                // Lista completa de registros con editar/eliminar
                CurrentPageTitle = "Historial de registros";
                var histVm = _services.GetRequiredService<HistorialViewModel>();
                CurrentView = new HistorialView { DataContext = histVm };
                _ = histVm.CargarDatosAsync();
                break;
            case "Otros":
                CurrentPageTitle = "Otros · Lotes y Operarios";
                CurrentView = new MaestrosView();
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
}
