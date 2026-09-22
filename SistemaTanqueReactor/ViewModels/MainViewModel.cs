using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaTanqueReactor.Services;
using SistemaTanqueReactor.Views;

namespace SistemaTanqueReactor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private string _currentPageTitle = "Dashboard";
    [ObservableProperty] private DateTime _fechaActual = DateTime.Now;
    [ObservableProperty] private string _langLabel = "ES";

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
        LangLabel = Loc.T("ui.lang");
        Loc.LanguageChanged += () =>
        {
            LangLabel = Loc.T("ui.lang");
            // Reaplicar título de página actual
            RefreshTitle();
        };
        NavigateToDashboard();
    }

    private void RefreshTitle()
    {
        // Títulos según vista actual (aproximado por título previo)
        if (CurrentPageTitle.Contains("Dashboard", StringComparison.OrdinalIgnoreCase) ||
            CurrentPageTitle.Contains("Producción", StringComparison.OrdinalIgnoreCase))
            CurrentPageTitle = Loc.T("ui.dashboard");
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        Loc.Toggle();
        LangLabel = Loc.T("ui.lang");
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
                CurrentPageTitle = Loc.T("ui.registro");
                var regVm = _services.GetRequiredService<HistorialMensualViewModel>();
                CurrentView = new HistorialMensualView { DataContext = regVm };
                _ = regVm.CargarAsync();
                break;
            case "Historial":
                CurrentPageTitle = Loc.T("ui.historial");
                var histVm = _services.GetRequiredService<HistorialViewModel>();
                CurrentView = new HistorialView { DataContext = histVm };
                _ = histVm.CargarDatosAsync();
                break;
            case "Otros":
                CurrentPageTitle = Loc.T("ui.otros");
                CurrentView = new MaestrosView();
                break;
        }
    }

    private void NavigateToDashboard()
    {
        CurrentPageTitle = Loc.T("ui.dashboard");
        var vm = _services.GetRequiredService<DashboardViewModel>();
        CurrentView = new DashboardView { DataContext = vm };
        _ = vm.CargarDatosAsync();
    }

    private void NavigateToNuevoRegistro()
    {
        CurrentPageTitle = Loc.T("ui.nuevo");
        var vm = _services.GetRequiredService<NuevoRegistroViewModel>();
        CurrentView = new NuevoRegistroView { DataContext = vm };
        _ = vm.InicializarAsync();
    }
}
