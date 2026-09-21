using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;
using System.Windows;

namespace SistemaTanqueReactor.ViewModels;

public partial class NuevoRegistroViewModel : ObservableObject
{
    private readonly ProduccionService _service;

    [ObservableProperty]
    private ObservableCollection<string> _lotes = new();

    [ObservableProperty]
    private string _numeroLote = "";

    [ObservableProperty]
    private DateTime _fechaProduccion = DateTime.Today.AddDays(-1);

    [ObservableProperty]
    private string _cantidadTexto = "";

    [ObservableProperty]
    private int _cantidadCalculada;

    [ObservableProperty]
    private int _cantidadKg;

    [ObservableProperty]
    private bool _mostrarResultado;

    [ObservableProperty]
    private string _turno = "I";

    [ObservableProperty]
    private string _observaciones = "";

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _mensaje = "";

    public ObservableCollection<string> Turnos { get; } = new() { "I", "II" };

    public NuevoRegistroViewModel(ProduccionService service)
    {
        _service = service;
    }

    public async Task InicializarAsync()
    {
        try
        {
            var lotes = await _service.ObtenerLotesAsync();
            Lotes = new ObservableCollection<string>(lotes);
            FechaProduccion = DateTime.Today.AddDays(-1);
            Mensaje = $"Registrando producción del día {FechaProduccion:dd/MM/yyyy}";
            MostrarResultado = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CalcularCantidad()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CantidadTexto))
            {
                CantidadCalculada = 0;
                CantidadKg = 0;
                MostrarResultado = false;
                Mensaje = "Escribe una cantidad (ej: 30+30+30+30)";
                return;
            }

            var total = ProduccionService.EvaluarExpresion(CantidadTexto);
            CantidadCalculada = total;
            CantidadKg = total * 25;
            MostrarResultado = total > 0;

            // Reemplazar el texto por el total para que quede claro
            CantidadTexto = total.ToString();

            Mensaje = $"Total calculado: {total} bolsas = {CantidadKg:N0} kg";
        }
        catch (Exception ex)
        {
            CantidadCalculada = 0;
            CantidadKg = 0;
            MostrarResultado = false;
            Mensaje = ex.Message;
            MessageBox.Show(ex.Message, "Error en cantidad", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(NumeroLote))
        {
            MessageBox.Show("Ingresa o selecciona un Nº de Lote.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Si hay expresión, calcular primero
        int cantidad;
        try
        {
            cantidad = ProduccionService.EvaluarExpresion(CantidadTexto);
        }
        catch
        {
            MessageBox.Show("Cantidad inválida. Usa números y + (ej: 30+28+2)", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cantidad <= 0)
        {
            MessageBox.Show("La cantidad debe ser mayor a 0.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CantidadCalculada = cantidad;
        CantidadKg = cantidad * 25;
        MostrarResultado = true;

        try
        {
            IsSaving = true;

            var registro = new RegistroProduccion
            {
                NumeroLote = NumeroLote.Trim(),
                FechaProduccion = FechaProduccion.Date,
                Turno = Turno,
                CantidadBolsas = cantidad,
                ExpresionCantidad = CantidadTexto.Trim(),
                UsuarioRegistro = Environment.UserName,
                Observaciones = Observaciones
            };

            await _service.GuardarRegistroAsync(registro);

            MessageBox.Show(
                $"Registro guardado\n\nLote: {registro.NumeroLote}\nFecha: {registro.FechaProduccion:dd/MM/yyyy}\nTurno: {registro.Turno}\nCantidad: {registro.CantidadBolsas} bolsas ({registro.CantidadKg:N0} kg)",
                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            CantidadTexto = "";
            CantidadCalculada = 0;
            CantidadKg = 0;
            MostrarResultado = false;
            Observaciones = "";
            Mensaje = $"Último registro: {registro.CantidadBolsas} bolsas · Lote {registro.NumeroLote}";

            var lotes = await _service.ObtenerLotesAsync();
            Lotes = new ObservableCollection<string>(lotes);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }
}
