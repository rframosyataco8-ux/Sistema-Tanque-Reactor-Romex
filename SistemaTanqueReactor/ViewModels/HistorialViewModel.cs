using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;

namespace SistemaTanqueReactor.ViewModels;

public partial class HistorialViewModel : ObservableObject
{
    private readonly CargaService _cargaService;

    [ObservableProperty]
    private ObservableCollection<CargaReactor> _cargas = new();

    [ObservableProperty]
    private DateTime? _filtroFecha = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<Turno> _turnos = new();

    [ObservableProperty]
    private Turno? _filtroTurno;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _mensaje = "";

    public HistorialViewModel(CargaService cargaService)
    {
        _cargaService = cargaService;
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            IsLoading = true;
            Mensaje = "Cargando historial...";

            var turnos = await _cargaService.ObtenerTurnosAsync();
            Turnos = new ObservableCollection<Turno>(turnos);

            await BuscarAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Buscar()
    {
        await BuscarAsync();
    }

    private async Task BuscarAsync()
    {
        try
        {
            IsLoading = true;
            var lista = await _cargaService.ObtenerCargasAsync(FiltroFecha, FiltroTurno?.IdTurno);
            Cargas = new ObservableCollection<CargaReactor>(lista);
            Mensaje = $"{lista.Count} carga(s) encontrada(s)";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al buscar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LimpiarFiltros()
    {
        FiltroFecha = DateTime.Today;
        FiltroTurno = null;
        await BuscarAsync();
    }
}
