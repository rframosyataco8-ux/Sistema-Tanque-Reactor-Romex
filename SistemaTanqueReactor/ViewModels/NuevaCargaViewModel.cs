using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;
using System.Windows;

namespace SistemaTanqueReactor.ViewModels;

public partial class NuevaCargaViewModel : ObservableObject
{
    private readonly CargaService _cargaService;

    [ObservableProperty]
    private DateTime _fecha = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<Turno> _turnos = new();

    [ObservableProperty]
    private Turno? _turnoSeleccionado;

    [ObservableProperty]
    private ObservableCollection<Operario> _operarios = new();

    [ObservableProperty]
    private Operario? _operarioSeleccionado;

    [ObservableProperty]
    private int _numeroTanque = 1;

    [ObservableProperty]
    private int _numeroCargaDelTurno = 1;

    // Tiempos
    [ObservableProperty]
    private string _horaInicioCarga = "";

    [ObservableProperty]
    private string _horaFinCarga = "";

    [ObservableProperty]
    private string _horaInicioSolucion = "";

    [ObservableProperty]
    private string _horaFinSolucion = "";

    [ObservableProperty]
    private string _horaInicioVacio = "";

    [ObservableProperty]
    private string _horaFinVacio = "";

    [ObservableProperty]
    private string _tiempoSecado = "";

    // Insumos
    [ObservableProperty]
    private ObservableCollection<InsumoCargaItem> _insumos = new();

    [ObservableProperty]
    private ObservableCollection<Insumo> _listaInsumos = new();

    // Parámetros finales
    [ObservableProperty]
    private string _ph = "";

    [ObservableProperty]
    private string _humedad = "";

    [ObservableProperty]
    private string _tempProductoFinal = "";

    [ObservableProperty]
    private string _cantidadElaborada = "";

    [ObservableProperty]
    private string _loteProductoFinal = "";

    [ObservableProperty]
    private string _stock = "";

    [ObservableProperty]
    private string _observaciones = "";

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _mensaje = "";

    public NuevaCargaViewModel(CargaService cargaService)
    {
        _cargaService = cargaService;
    }

    public async Task InicializarAsync()
    {
        try
        {
            var turnos = await _cargaService.ObtenerTurnosAsync();
            Turnos = new ObservableCollection<Turno>(turnos);

            // Seleccionar turno automáticamente según hora actual
            var hora = DateTime.Now.TimeOfDay;
            TurnoSeleccionado = turnos.FirstOrDefault(t => 
                (hora >= t.HoraInicio && hora < t.HoraFin) || 
                (t.Nombre == "Noche" && (hora >= t.HoraInicio || hora < t.HoraFin)));

            if (TurnoSeleccionado == null && turnos.Any())
                TurnoSeleccionado = turnos.First();

            var operarios = await _cargaService.ObtenerOperariosAsync();
            Operarios = new ObservableCollection<Operario>(operarios);

            var insumos = await _cargaService.ObtenerInsumosAsync();
            ListaInsumos = new ObservableCollection<Insumo>(insumos);

            // Agregar automáticamente la carga estándar 28 + 2
            AgregarCargaEstandar();

            if (TurnoSeleccionado != null)
            {
                NumeroCargaDelTurno = await _cargaService.ObtenerSiguienteNumeroCargaAsync(Fecha, TurnoSeleccionado.IdTurno);
            }
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al inicializar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AgregarCargaEstandar()
    {
        // 28 bolsas de Torta Trozada (700 kg) + 2 bolsas de Merma (50 kg)
        var torta = ListaInsumos.FirstOrDefault(i => i.Nombre.Contains("Torta Trozada"));
        var merma = ListaInsumos.FirstOrDefault(i => i.Nombre.Contains("Merma"));

        if (torta != null)
        {
            Insumos.Add(new InsumoCargaItem
            {
                IdInsumo = torta.IdInsumo,
                NombreInsumo = torta.Nombre,
                CantidadBolsas = 28,
                CantidadKg = 700,
                EsMerma = false
            });
        }

        if (merma != null)
        {
            Insumos.Add(new InsumoCargaItem
            {
                IdInsumo = merma.IdInsumo,
                NombreInsumo = merma.Nombre,
                CantidadBolsas = 2,
                CantidadKg = 50,
                EsMerma = true
            });
        }
    }

    [RelayCommand]
    private void AgregarInsumo()
    {
        Insumos.Add(new InsumoCargaItem
        {
            CantidadBolsas = 0,
            CantidadKg = 0
        });
    }

    [RelayCommand]
    private void EliminarInsumo(InsumoCargaItem item)
    {
        if (item != null)
            Insumos.Remove(item);
    }

    [RelayCommand]
    private async Task GuardarCarga()
    {
        if (TurnoSeleccionado == null)
        {
            MessageBox.Show("Selecciona un turno.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Insumos.Count == 0)
        {
            MessageBox.Show("Debes agregar al menos un insumo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsSaving = true;
            Mensaje = "Guardando carga...";

            var carga = new CargaReactor
            {
                Fecha = Fecha,
                IdTurno = TurnoSeleccionado.IdTurno,
                IdOperario = OperarioSeleccionado?.IdOperario,
                NumeroTanque = NumeroTanque,
                NumeroCargaDelTurno = NumeroCargaDelTurno,
                HoraInicioCarga = ParseTime(HoraInicioCarga),
                HoraFinCarga = ParseTime(HoraFinCarga),
                HoraInicioSolucion = ParseTime(HoraInicioSolucion),
                HoraFinSolucion = ParseTime(HoraFinSolucion),
                HoraInicioVacio = ParseTime(HoraInicioVacio),
                HoraFinVacio = ParseTime(HoraFinVacio),
                TiempoSecadoAlVacio = TiempoSecado,
                pH = ParseDecimal(Ph),
                Humedad = ParseDecimal(Humedad),
                TempProductoFinal = ParseDecimal(TempProductoFinal),
                CantidadElaboradaKg = ParseDecimal(CantidadElaborada),
                LoteProductoFinal = LoteProductoFinal,
                StockKg = ParseDecimal(Stock),
                Observaciones = Observaciones,
                Estado = "En Proceso",
                UsuarioRegistro = Environment.UserName
            };

            foreach (var item in Insumos)
            {
                carga.Insumos.Add(new CargaInsumo
                {
                    IdInsumo = item.IdInsumo,
                    IdLote = item.IdLote,
                    CantidadBolsas = item.CantidadBolsas,
                    CantidadKg = item.CantidadKg,
                    EsMerma = item.EsMerma
                });
            }

            await _cargaService.GuardarCargaAsync(carga);

            MessageBox.Show(
                $"Carga #{carga.NumeroCargaDelTurno} guardada correctamente.\n\nFecha: {carga.Fecha:dd/MM/yyyy}\nTurno: {TurnoSeleccionado.Nombre}",
                "Éxito",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Limpiar para nueva carga
            LimpiarFormulario();
            await InicializarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Mensaje = $"Error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void LimpiarFormulario()
    {
        Insumos.Clear();
        HoraInicioCarga = HoraFinCarga = HoraInicioSolucion = HoraFinSolucion = "";
        HoraInicioVacio = HoraFinVacio = TiempoSecado = "";
        Ph = Humedad = TempProductoFinal = CantidadElaborada = LoteProductoFinal = Stock = "";
        Observaciones = "";
        Mensaje = "";
    }

    private static TimeSpan? ParseTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (TimeSpan.TryParse(value, out var result)) return result;
        return null;
    }

    private static decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, out var result)) return result;
        return null;
    }
}

public partial class InsumoCargaItem : ObservableObject
{
    [ObservableProperty]
    private int _idInsumo;

    [ObservableProperty]
    private string _nombreInsumo = "";

    [ObservableProperty]
    private int? _idLote;

    [ObservableProperty]
    private string _numeroLote = "";

    [ObservableProperty]
    private int? _cantidadBolsas;

    [ObservableProperty]
    private decimal _cantidadKg;

    [ObservableProperty]
    private bool _esMerma;
}
