using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Services;

namespace SistemaTanqueReactor.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ProduccionService _service;

    [ObservableProperty] private int _registrosHoy;
    [ObservableProperty] private int _bolsasHoy;
    [ObservableProperty] private int _kgHoy;
    [ObservableProperty] private int _lotesActivos;
    [ObservableProperty] private string _mensajeEstado = "Cargando...";
    [ObservableProperty] private bool _isLoading;

    public DashboardViewModel(ProduccionService service)
    {
        _service = service;
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            IsLoading = true;
            MensajeEstado = "Cargando indicadores...";

            var hoy = DateTime.Today;
            var regs = await _service.ObtenerTodosRegistrosAsync(hoy, hoy, null, null);
            var lotes = await _service.ObtenerLotesConStockAsync();

            RegistrosHoy = regs.Count;
            BolsasHoy = regs.Sum(r => r.CantidadBolsas);
            KgHoy = BolsasHoy * 25;
            LotesActivos = lotes.Count(l => !l.Despachado && l.CantidadBolsasDisponible > 0);

            MensajeEstado = RegistrosHoy == 0
                ? "Sin registros de producción hoy · Listo para operar"
                : $"{RegistrosHoy} registro(s) hoy · {BolsasHoy} bolsas · {KgHoy:N0} kg";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refrescar() => await CargarDatosAsync();
}
