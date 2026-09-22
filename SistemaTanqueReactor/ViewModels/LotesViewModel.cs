using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;
using System.Windows;

namespace SistemaTanqueReactor.ViewModels;

public partial class LotesViewModel : ObservableObject
{
    private readonly string _cs;

    [ObservableProperty] private ObservableCollection<LoteTorta> _lotes = new();
    [ObservableProperty] private string _numeroLote = "";
    [ObservableProperty] private int _cantidadBolsas = 400;
    [ObservableProperty] private DateTime? _fechaIngreso = DateTime.Today;
    [ObservableProperty] private string _observaciones = "";
    [ObservableProperty] private string _mensaje = "";
    [ObservableProperty] private string _errorLote = "";
    [ObservableProperty] private string _errorBolsas = "";
    [ObservableProperty] private string _errorFecha = "";
    [ObservableProperty] private bool _isSaving;

    public LotesViewModel(IConfiguration config)
    {
        _cs = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task CargarAsync()
    {
        try
        {
            var lista = new ObservableCollection<LoteTorta>();
            using var conn = new SqlConnection(_cs);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(
                @"SELECT IdLoteTorta, NumeroLote, CantidadBolsasInicial, CantidadBolsasDisponible,
                         ISNULL(ProduccionBolsas, 0), ISNULL(Despachado, 0),
                         FechaIngreso, Activo, FechaRegistro
                  FROM LotesTorta WHERE Activo = 1 ORDER BY FechaRegistro DESC", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new LoteTorta
                {
                    IdLoteTorta = r.GetInt32(0),
                    NumeroLote = r.GetString(1),
                    CantidadBolsasInicial = r.GetInt32(2),
                    CantidadBolsasDisponible = r.GetInt32(3),
                    ProduccionBolsas = r.GetInt32(4),
                    Despachado = r.GetBoolean(5),
                    FechaIngreso = r.IsDBNull(6) ? null : r.GetDateTime(6),
                    Activo = r.GetBoolean(7),
                    FechaRegistro = r.GetDateTime(8)
                });
            }

            Lotes = lista;
            Mensaje = $"{lista.Count} lote(s)";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
    }

    private InputValidator.Result Validar()
    {
        var r = new InputValidator.Result();
        InputValidator.ValidateLoteNumero(NumeroLote, r, "Lote");
        InputValidator.ValidateBolsas(CantidadBolsas, r, "Bolsas", 10000);
        InputValidator.ValidateFecha(FechaIngreso, r, "Fecha", allowNull: true, maxYearsBack: 5, allowFuture: false);

        ErrorLote = r["Lote"] ?? "";
        ErrorBolsas = r["Bolsas"] ?? "";
        ErrorFecha = r["Fecha"] ?? "";
        return r;
    }

    [RelayCommand]
    private async Task Guardar()
    {
        var result = Validar();
        if (!result.IsValid)
        {
            MessageBox.Show(result.Summary, Loc.T("ui.validation"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsSaving = true;
            using var conn = new SqlConnection(_cs);
            await conn.OpenAsync();

            using var check = new SqlCommand("SELECT COUNT(1) FROM LotesTorta WHERE NumeroLote = @l", conn);
            check.Parameters.AddWithValue("@l", NumeroLote.Trim());
            if ((int)(await check.ExecuteScalarAsync() ?? 0) > 0)
            {
                ErrorLote = Loc.T("val.lote.exists");
                MessageBox.Show(ErrorLote, Loc.T("ui.validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var cmd = new SqlCommand(
                @"INSERT INTO LotesTorta (NumeroLote, CantidadBolsasInicial, CantidadBolsasDisponible, ProduccionBolsas, Despachado, FechaIngreso, Observaciones)
                  VALUES (@l, @bolsas, @bolsas, 0, 0, @fecha, @obs)", conn);
            cmd.Parameters.AddWithValue("@l", NumeroLote.Trim());
            cmd.Parameters.AddWithValue("@bolsas", CantidadBolsas);
            cmd.Parameters.AddWithValue("@fecha", (object?)FechaIngreso ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(Observaciones) ? DBNull.Value : Observaciones);
            await cmd.ExecuteNonQueryAsync();

            MessageBox.Show($"Lote {NumeroLote} creado ({CantidadBolsas} bolsas = {CantidadBolsas * 25:N0} kg).",
                Loc.T("ui.success"), MessageBoxButton.OK, MessageBoxImage.Information);
            NumeroLote = "";
            CantidadBolsas = 400;
            Observaciones = "";
            ErrorLote = ErrorBolsas = ErrorFecha = "";
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.T("ui.error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task ToggleDespachado(LoteTorta? lote)
    {
        if (lote == null) return;
        try
        {
            using var conn = new SqlConnection(_cs);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "UPDATE LotesTorta SET Despachado = @d WHERE NumeroLote = @l", conn);
            cmd.Parameters.AddWithValue("@d", lote.Despachado);
            cmd.Parameters.AddWithValue("@l", lote.NumeroLote);
            await cmd.ExecuteNonQueryAsync();
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.T("ui.error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Refrescar() => await CargarAsync();
}
