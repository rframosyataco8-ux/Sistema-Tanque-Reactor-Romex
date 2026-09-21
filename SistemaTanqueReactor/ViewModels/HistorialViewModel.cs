using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;
using System.Windows;

namespace SistemaTanqueReactor.ViewModels;

public partial class HistorialViewModel : ObservableObject
{
    private readonly ProduccionService _service;

    [ObservableProperty] private ObservableCollection<RegistroProduccion> _registros = new();
    [ObservableProperty] private DateTime? _filtroDesde = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _filtroHasta = DateTime.Today;
    [ObservableProperty] private string _filtroLote = "";
    [ObservableProperty] private string _filtroTurno = "Todos";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _mensaje = "";

    public ObservableCollection<string> TurnosFiltro { get; } = new() { "Todos", "I", "II" };

    public HistorialViewModel(ProduccionService service)
    {
        _service = service;
    }

    public async Task CargarDatosAsync() => await BuscarAsync();

    [RelayCommand]
    private async Task Buscar() => await BuscarAsync();

    private async Task BuscarAsync()
    {
        try
        {
            IsLoading = true;
            Mensaje = "Cargando...";

            var lista = await _service.ObtenerTodosRegistrosAsync(
                FiltroDesde, FiltroHasta,
                string.IsNullOrWhiteSpace(FiltroLote) ? null : FiltroLote.Trim(),
                FiltroTurno == "Todos" ? null : FiltroTurno);

            Registros = new ObservableCollection<RegistroProduccion>(lista);
            Mensaje = $"{lista.Count} registro(s)";
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
    private async Task LimpiarFiltros()
    {
        FiltroDesde = DateTime.Today.AddDays(-30);
        FiltroHasta = DateTime.Today;
        FiltroLote = "";
        FiltroTurno = "Todos";
        await BuscarAsync();
    }

    [RelayCommand]
    private async Task Eliminar(RegistroProduccion? reg)
    {
        if (reg == null) return;

        var ok = MessageBox.Show(
            $"¿Eliminar registro?\n\nLote: {reg.NumeroLote}\nFecha: {reg.FechaProduccion:dd/MM/yyyy}\nTurno: {reg.Turno}\nCantidad: {reg.CantidadBolsas} bolsas\n\nSe devolverá el stock al lote.",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (ok != MessageBoxResult.Yes) return;

        try
        {
            await _service.EliminarRegistroAsync(reg.IdRegistro);
            MessageBox.Show("Registro eliminado y stock restaurado.", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
            await BuscarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
