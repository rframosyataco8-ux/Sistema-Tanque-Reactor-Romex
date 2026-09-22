using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;
using System.Windows;

namespace SistemaTanqueReactor.ViewModels;

public partial class OperariosViewModel : ObservableObject
{
    private readonly string _cs;

    [ObservableProperty] private ObservableCollection<Operario> _operarios = new();
    [ObservableProperty] private string _nombres = "";
    [ObservableProperty] private string _apellidos = "";
    [ObservableProperty] private string _dni = "";
    [ObservableProperty] private string _mensaje = "";
    [ObservableProperty] private string _errorNombres = "";
    [ObservableProperty] private string _errorDni = "";
    [ObservableProperty] private bool _isSaving;

    public OperariosViewModel(IConfiguration config)
    {
        _cs = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task CargarAsync()
    {
        try
        {
            var lista = new ObservableCollection<Operario>();
            using var conn = new SqlConnection(_cs);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT IdOperario, Nombres, Apellidos, Dni, Activo, FechaRegistro FROM Operarios WHERE Activo = 1 ORDER BY Nombres", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new Operario
                {
                    IdOperario = r.GetInt32(0),
                    Nombres = r.GetString(1),
                    Apellidos = r.IsDBNull(2) ? null : r.GetString(2),
                    Dni = r.IsDBNull(3) ? null : r.GetString(3),
                    Activo = r.GetBoolean(4),
                    FechaRegistro = r.GetDateTime(5)
                });
            }
            Operarios = lista;
            Mensaje = $"{lista.Count} operario(s)";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
        }
    }

    private InputValidator.Result Validar()
    {
        var r = new InputValidator.Result();
        InputValidator.ValidateNombrePersona(Nombres, r, "Nombres", required: true);
        InputValidator.ValidateNombrePersona(Apellidos, r, "Apellidos", required: false);
        InputValidator.ValidateDni(Dni, r, "Dni", required: false);

        ErrorNombres = r["Nombres"] ?? r["Apellidos"] ?? "";
        ErrorDni = r["Dni"] ?? "";
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

            if (!string.IsNullOrWhiteSpace(Dni))
            {
                using var check = new SqlCommand(
                    "SELECT COUNT(1) FROM Operarios WHERE Dni = @d AND Activo = 1", conn);
                check.Parameters.AddWithValue("@d", Dni.Trim());
                if ((int)(await check.ExecuteScalarAsync() ?? 0) > 0)
                {
                    ErrorDni = Loc.T("val.dni.exists");
                    MessageBox.Show(ErrorDni, Loc.T("ui.validation"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            using var cmd = new SqlCommand(
                "INSERT INTO Operarios (Nombres, Apellidos, Dni) VALUES (@n, @a, @d)", conn);
            cmd.Parameters.AddWithValue("@n", Nombres.Trim());
            cmd.Parameters.AddWithValue("@a", string.IsNullOrWhiteSpace(Apellidos) ? DBNull.Value : Apellidos.Trim());
            cmd.Parameters.AddWithValue("@d", string.IsNullOrWhiteSpace(Dni) ? DBNull.Value : Dni.Trim());
            await cmd.ExecuteNonQueryAsync();

            MessageBox.Show($"Operario {Nombres} registrado.", Loc.T("ui.success"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            Nombres = "";
            Apellidos = "";
            Dni = "";
            ErrorNombres = ErrorDni = "";
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.T("ui.error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task Refrescar() => await CargarAsync();
}
