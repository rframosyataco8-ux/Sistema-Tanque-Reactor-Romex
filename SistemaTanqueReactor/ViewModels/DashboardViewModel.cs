using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Services;

namespace SistemaTanqueReactor.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly CargaService _cargaService;

    [ObservableProperty]
    private int _totalCargasHoy;

    [ObservableProperty]
    private decimal _totalKgTortaHoy;

    [ObservableProperty]
    private decimal _totalKgMermaHoy;

    [ObservableProperty]
    private string _mensajeEstado = "Cargando...";

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel(CargaService cargaService)
    {
        _cargaService = cargaService;
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            IsLoading = true;
            MensajeEstado = "Cargando datos del día...";

            var (total, torta, merma) = await _cargaService.ObtenerResumenDiaAsync(DateTime.Today);

            TotalCargasHoy = total;
            TotalKgTortaHoy = torta;
            TotalKgMermaHoy = merma;

            MensajeEstado = total == 0 
                ? "No hay cargas registradas hoy" 
                : $"{total} carga(s) registrada(s) hoy";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al cargar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refrescar()
    {
        await CargarDatosAsync();
    }
}
