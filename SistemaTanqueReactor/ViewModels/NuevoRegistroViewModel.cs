using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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
            LimpiarErrores();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
    }

    private void LimpiarErrores()
    {
        ErrorLote = "";
        ErrorFecha = "";
        ErrorCantidad = "";
    }

    /// <summary>Validación completa de entrada antes de guardar.</summary>
    private bool ValidarEntrada(out int cantidad)
    {
        LimpiarErrores();
        cantidad = 0;
        bool ok = true;

        // Lote
        var lote = (NumeroLote ?? "").Trim();
        if (string.IsNullOrWhiteSpace(lote))
        {
            ErrorLote = "El Nº de lote es obligatorio.";
            ok = false;
        }
        else if (lote.Length > 50)
        {
            ErrorLote = "Máximo 50 caracteres.";
            ok = false;
        }
        else if (!Regex.IsMatch(lote, @"^[A-Za-z0-9._\-]+$"))
        {
            ErrorLote = "Solo letras, números, punto, guion o guion bajo.";
            ok = false;
        }

        // Fecha
        if (FechaProduccion.Date > DateTime.Today)
        {
            ErrorFecha = "La fecha no puede ser futura.";
            ok = false;
        }
        else if (FechaProduccion.Date < DateTime.Today.AddYears(-2))
        {
            ErrorFecha = "Fecha demasiado antigua (máx. 2 años).";
            ok = false;
        }

        // Turno
        var t = (Turno ?? "").Trim().ToUpperInvariant();
        if (t != "I" && t != "II")
        {
            Mensaje = "Turno inválido. Use I o II.";
            ok = false;
        }

        // Cantidad
        if (string.IsNullOrWhiteSpace(CantidadTexto))
        {
            ErrorCantidad = "Ingrese la cantidad (ej: 30+30+30 o 120).";
            ok = false;
        }
        else
        {
            try
            {
                cantidad = ProduccionService.EvaluarExpresion(CantidadTexto);
                if (cantidad <= 0)
                {
                    ErrorCantidad = "La cantidad debe ser mayor a 0.";
                    ok = false;
                }
                else if (cantidad > 400)
                {
                    ErrorCantidad = "No puede superar 400 bolsas por registro.";
                    ok = false;
                }
            }
            catch (Exception ex)
            {
                ErrorCantidad = ex.Message;
                ok = false;
            }
        }

        // Observaciones longitud
        if (!string.IsNullOrEmpty(Observaciones) && Observaciones.Length > 300)
        {
            Mensaje = "Observaciones: máximo 300 caracteres.";
            ok = false;
        }

        return ok;
    }

    [RelayCommand]
    private void CalcularCantidad()
    {
        ErrorCantidad = "";
        try
        {
            if (string.IsNullOrWhiteSpace(CantidadTexto))
            {
                CantidadCalculada = 0;
                CantidadKg = 0;
                MostrarResultado = false;
                ErrorCantidad = "Escribe una cantidad (ej: 30+30+30+30)";
                return;
            }

            var total = ProduccionService.EvaluarExpresion(CantidadTexto);
            if (total <= 0)
            {
                ErrorCantidad = "El total debe ser mayor a 0.";
                MostrarResultado = false;
                return;
            }
            if (total > 400)
            {
                ErrorCantidad = "Máximo 400 bolsas por registro.";
                MostrarResultado = false;
                return;
            }

            CantidadCalculada = total;
            CantidadKg = total * 25;
            MostrarResultado = true;
            Mensaje = $"Total: {total} bolsas = {CantidadKg:N0} kg";
        }
        catch (Exception ex)
        {
            CantidadCalculada = 0;
            CantidadKg = 0;
            MostrarResultado = false;
            ErrorCantidad = ex.Message;
        }
    }

    [RelayCommand]
    private async Task Guardar()
    {
        if (!ValidarEntrada(out int cantidad))
        {
            var msgs = new List<string>();
            if (!string.IsNullOrEmpty(ErrorLote)) msgs.Add(ErrorLote);
            if (!string.IsNullOrEmpty(ErrorFecha)) msgs.Add(ErrorFecha);
            if (!string.IsNullOrEmpty(ErrorCantidad)) msgs.Add(ErrorCantidad);
            if (msgs.Count > 0)
                MessageBox.Show(string.Join("\n", msgs), "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            CantidadTexto = "";
            CantidadCalculada = 0;
            CantidadKg = 0;
            MostrarResultado = false;
            Observaciones = "";
            LimpiarErrores();
            Mensaje = $"Último: {registro.CantidadBolsas} bolsas · Lote {registro.NumeroLote} · Turno {registro.Turno}";

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
