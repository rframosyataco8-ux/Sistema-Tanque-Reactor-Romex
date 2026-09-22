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

    [ObservableProperty] private ObservableCollection<string> _lotes = new();
    [ObservableProperty] private string _numeroLote = "";
    [ObservableProperty] private DateTime _fechaProduccion = DateTime.Today.AddDays(-1);
    [ObservableProperty] private string _cantidadTexto = "";
    [ObservableProperty] private int _cantidadCalculada;
    [ObservableProperty] private int _cantidadKg;
    [ObservableProperty] private bool _mostrarResultado;
    [ObservableProperty] private string _turno = "I";
    [ObservableProperty] private string _observaciones = "";
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _mensaje = "";
    [ObservableProperty] private string _errorLote = "";
    [ObservableProperty] private string _errorFecha = "";
    [ObservableProperty] private string _errorCantidad = "";
    [ObservableProperty] private string _stockInfo = "";

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
            StockInfo = "";
            LimpiarErrores();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
    }

    partial void OnNumeroLoteChanged(string value)
    {
        _ = ActualizarStockInfoAsync();
    }

    private async Task ActualizarStockInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(NumeroLote))
        {
            StockInfo = "";
            return;
        }
        try
        {
            var stock = await _service.ObtenerStockDisponibleAsync(NumeroLote.Trim());
            StockInfo = stock.HasValue
                ? $"Stock disponible: {stock.Value} bolsas ({stock.Value * 25:N0} kg)"
                : "Lote nuevo → se creará con 400 bolsas";
        }
        catch
        {
            StockInfo = "";
        }
    }

    private void LimpiarErrores()
    {
        ErrorLote = "";
        ErrorFecha = "";
        ErrorCantidad = "";
    }

    private InputValidator.Result ValidarCompleto(out int cantidad)
    {
        var r = new InputValidator.Result();
        InputValidator.ValidateLoteNumero(NumeroLote, r, "Lote");
        InputValidator.ValidateFecha(FechaProduccion, r, "Fecha", allowNull: false, maxYearsBack: 2, allowFuture: false);
        InputValidator.ValidateTurno(Turno, r, "Turno");
        InputValidator.ValidateExpresionCantidad(CantidadTexto, r, out cantidad, "Cantidad", max: 400);
        InputValidator.ValidateObservaciones(Observaciones, r, "Obs", 300);

        ErrorLote = r["Lote"] ?? "";
        ErrorFecha = r["Fecha"] ?? r["Turno"] ?? "";
        ErrorCantidad = r["Cantidad"] ?? r["Obs"] ?? "";
        return r;
    }

    [RelayCommand]
    private void CalcularCantidad()
    {
        ErrorCantidad = "";
        var r = new InputValidator.Result();
        InputValidator.ValidateExpresionCantidad(CantidadTexto, r, out int total, "Cantidad", 400);
        if (!r.IsValid)
        {
            ErrorCantidad = r["Cantidad"] ?? r.Summary;
            CantidadCalculada = 0;
            CantidadKg = 0;
            MostrarResultado = false;
            return;
        }

        CantidadCalculada = total;
        CantidadKg = total * 25;
        MostrarResultado = true;
        Mensaje = $"Total: {total} bolsas = {CantidadKg:N0} kg";
    }

    [RelayCommand]
    private async Task Guardar()
    {
        var result = ValidarCompleto(out int cantidad);
        if (!result.IsValid)
        {
            MessageBox.Show(result.Summary, Loc.T("ui.validation"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validar stock antes de guardar
        try
        {
            var stock = await _service.ObtenerStockDisponibleAsync(NumeroLote.Trim());
            int disponible = stock ?? 400; // lote nuevo
            if (cantidad > disponible)
            {
                ErrorCantidad = $"Stock insuficiente ({disponible} bolsas disponibles)";
                MessageBox.Show(
                    $"No se puede guardar.\n\nLote: {NumeroLote.Trim()}\nStock disponible: {disponible} bolsas\nCantidad solicitada: {cantidad} bolsas",
                    "Stock insuficiente",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al verificar stock: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
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
                Turno = Turno.Trim().ToUpperInvariant(),
                CantidadBolsas = cantidad,
                ExpresionCantidad = CantidadTexto.Trim(),
                UsuarioRegistro = Environment.UserName,
                Observaciones = string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones.Trim()
            };

            await _service.GuardarRegistroAsync(registro);

            MessageBox.Show(
                $"Registro guardado\n\nLote: {registro.NumeroLote}\nFecha: {registro.FechaProduccion:dd/MM/yyyy}\nTurno: {registro.Turno}\nCantidad: {registro.CantidadBolsas} bolsas ({registro.CantidadKg:N0} kg)",
                Loc.T("ui.success"), MessageBoxButton.OK, MessageBoxImage.Information);

            CantidadTexto = "";
            CantidadCalculada = 0;
            CantidadKg = 0;
            MostrarResultado = false;
            Observaciones = "";
            LimpiarErrores();
            Mensaje = $"Último: {registro.CantidadBolsas} bolsas · Lote {registro.NumeroLote} · Turno {registro.Turno}";

            var lotes = await _service.ObtenerLotesAsync();
            Lotes = new ObservableCollection<string>(lotes);
            await ActualizarStockInfoAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Loc.T("ui.error")}: {ex.Message}", Loc.T("ui.error"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }
}
