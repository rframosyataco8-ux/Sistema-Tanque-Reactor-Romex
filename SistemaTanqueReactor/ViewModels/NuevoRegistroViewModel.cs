using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;
using System.Windows;
using System.Windows.Input;

namespace SistemaTanqueReactor.ViewModels;

public partial class NuevoRegistroViewModel : ObservableObject
{
    private readonly ProduccionService _service;

    [ObservableProperty]
    private ObservableCollection<string> _lotes = new();

    [ObservableProperty]
    private string _numeroLote = "";

    [ObservableProperty]
    private DateTime _fechaProduccion = DateTime.Today.AddDays(-1); // Siempre día anterior

    [ObservableProperty]
    private string _cantidadTexto = "";

    [ObservableProperty]
    private int _cantidadCalculada;

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
            CantidadCalculada = ProduccionService.EvaluarExpresion(CantidadTexto);
            Mensaje = $"Total: {CantidadCalculada} bolsas = {CantidadCalculada * 25:N0} kg";
        }
        catch (Exception ex)
        {
            CantidadCalculada = 0;
            Mensaje = ex.Message;
        }
    }

    partial void OnCantidadTextoChanged(string value)
    {
        // Auto-calcular si solo hay números y +
        try
        {
            if (!string.IsNullOrWhiteSpace(value) && value.All(c => char.IsDigit(c) || c == '+' || c == ' '))
            {
                CantidadCalculada = ProduccionService.EvaluarExpresion(value);
            }
        }
        catch { /* ignorar mientras escribe */ }
    }

    [RelayCommand]
    private async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(NumeroLote))
        {
            MessageBox.Show("Ingresa o selecciona un Nº de Lote.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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

            // Limpiar
            CantidadTexto = "";
            CantidadCalculada = 0;
            Observaciones = "";
            Mensaje = $"Último registro: {registro.CantidadBolsas} bolsas · Lote {registro.NumeroLote}";

            // Refrescar lotes por si se creó uno nuevo
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
