using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SistemaTanqueReactor.Models;

namespace SistemaTanqueReactor.Services;

public class ProduccionService
{
    private readonly string _connectionString;

    public ProduccionService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string no configurada");
    }

    public async Task<List<string>> ObtenerLotesAsync()
    {
        var lotes = new List<string>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "SELECT NumeroLote FROM LotesTorta WHERE Activo = 1 ORDER BY FechaRegistro DESC", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            lotes.Add(reader.GetString(0).Trim());

        return lotes;
    }

    public async Task AsegurarLoteAsync(string numeroLote)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var check = new SqlCommand(
            "SELECT COUNT(1) FROM LotesTorta WHERE NumeroLote = @lote", conn);
        check.Parameters.AddWithValue("@lote", numeroLote);
        var exists = (int)(await check.ExecuteScalarAsync() ?? 0) > 0;

        if (!exists)
        {
            try
            {
                using var insert = new SqlCommand(
                    @"INSERT INTO LotesTorta (NumeroLote, CantidadBolsasInicial, CantidadBolsasDisponible, ProduccionBolsas, Despachado, FechaIngreso)
                      VALUES (@lote, 400, 400, 0, 0, @fecha)", conn);
                insert.Parameters.AddWithValue("@lote", numeroLote);
                insert.Parameters.AddWithValue("@fecha", DateTime.Today);
                await insert.ExecuteNonQueryAsync();
            }
            catch
            {
                using var insert = new SqlCommand(
                    @"INSERT INTO LotesTorta (NumeroLote, CantidadBolsasInicial, CantidadBolsasDisponible, DespachoBolsas, FechaIngreso)
                      VALUES (@lote, 400, 400, 0, @fecha)", conn);
                insert.Parameters.AddWithValue("@lote", numeroLote);
                insert.Parameters.AddWithValue("@fecha", DateTime.Today);
                await insert.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task GuardarRegistroAsync(RegistroProduccion registro)
    {
        await AsegurarLoteAsync(registro.NumeroLote);

        // Normalizar turno: solo "I" o "II"
        var turno = (registro.Turno ?? "I").Trim().ToUpperInvariant();
        if (turno != "I" && turno != "II")
            turno = "I";
        registro.Turno = turno;

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var tran = conn.BeginTransaction();
        try
        {
            using var cmd = new SqlCommand(
                @"INSERT INTO RegistrosProduccion 
                  (NumeroLote, FechaProduccion, Turno, CantidadBolsas, ExpresionCantidad, UsuarioRegistro, Observaciones)
                  VALUES (@lote, @fecha, @turno, @cantidad, @expresion, @usuario, @obs)", conn, tran);

            cmd.Parameters.AddWithValue("@lote", registro.NumeroLote.Trim());
            cmd.Parameters.AddWithValue("@fecha", registro.FechaProduccion.Date);
            cmd.Parameters.AddWithValue("@turno", turno); // NVARCHAR-safe, sin padding raro
            cmd.Parameters.AddWithValue("@cantidad", registro.CantidadBolsas);
            cmd.Parameters.AddWithValue("@expresion", (object?)registro.ExpresionCantidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@usuario", (object?)registro.UsuarioRegistro ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@obs", (object?)registro.Observaciones ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();

            // Producción: resta disponible + suma produccion (NO despacho)
            try
            {
                using var upd = new SqlCommand(
                    @"UPDATE LotesTorta 
                      SET CantidadBolsasDisponible = CantidadBolsasDisponible - @cantidad,
                          ProduccionBolsas = ProduccionBolsas + @cantidad
                      WHERE NumeroLote = @lote", conn, tran);
                upd.Parameters.AddWithValue("@cantidad", registro.CantidadBolsas);
                upd.Parameters.AddWithValue("@lote", registro.NumeroLote.Trim());
                await upd.ExecuteNonQueryAsync();
            }
            catch
            {
                using var upd = new SqlCommand(
                    @"UPDATE LotesTorta 
                      SET CantidadBolsasDisponible = CantidadBolsasDisponible - @cantidad,
                          DespachoBolsas = DespachoBolsas + @cantidad
                      WHERE NumeroLote = @lote", conn, tran);
                upd.Parameters.AddWithValue("@cantidad", registro.CantidadBolsas);
                upd.Parameters.AddWithValue("@lote", registro.NumeroLote.Trim());
                await upd.ExecuteNonQueryAsync();
            }

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    public async Task<List<RegistroProduccion>> ObtenerRegistrosDelMesAsync(int anio, int mes)
    {
        var lista = new List<RegistroProduccion>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            @"SELECT IdRegistro, NumeroLote, FechaProduccion, Turno, CantidadBolsas, 
                     ExpresionCantidad, FechaRegistro, UsuarioRegistro, Observaciones
              FROM RegistrosProduccion
              WHERE YEAR(FechaProduccion) = @anio AND MONTH(FechaProduccion) = @mes
              ORDER BY NumeroLote, FechaProduccion, Turno", conn);
        cmd.Parameters.AddWithValue("@anio", anio);
        cmd.Parameters.AddWithValue("@mes", mes);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new RegistroProduccion
            {
                IdRegistro = reader.GetInt32(0),
                NumeroLote = reader.GetString(1).Trim(),
                FechaProduccion = reader.GetDateTime(2),
                // CRÍTICO: CHAR(2) rellena con espacios → "I " debe ser "I"
                Turno = reader.GetString(3).Trim(),
                CantidadBolsas = reader.GetInt32(4),
                ExpresionCantidad = reader.IsDBNull(5) ? null : reader.GetString(5),
                FechaRegistro = reader.GetDateTime(6),
                UsuarioRegistro = reader.IsDBNull(7) ? null : reader.GetString(7),
                Observaciones = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }
        return lista;
    }

    public async Task<List<LoteTorta>> ObtenerLotesConStockAsync()
    {
        var lista = new List<LoteTorta>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        try
        {
            using var cmd = new SqlCommand(
                @"SELECT IdLoteTorta, NumeroLote, CantidadBolsasInicial, CantidadBolsasDisponible,
                         ISNULL(ProduccionBolsas, 0), ISNULL(Despachado, 0),
                         FechaIngreso, Activo, FechaRegistro
                  FROM LotesTorta WHERE Activo = 1 ORDER BY NumeroLote", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new LoteTorta
                {
                    IdLoteTorta = reader.GetInt32(0),
                    NumeroLote = reader.GetString(1).Trim(),
                    CantidadBolsasInicial = reader.GetInt32(2),
                    CantidadBolsasDisponible = reader.GetInt32(3),
                    ProduccionBolsas = reader.GetInt32(4),
                    Despachado = reader.GetBoolean(5),
                    FechaIngreso = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    Activo = reader.GetBoolean(7),
                    FechaRegistro = reader.GetDateTime(8)
                });
            }
        }
        catch
        {
            using var cmd = new SqlCommand(
                @"SELECT IdLoteTorta, NumeroLote, CantidadBolsasInicial, CantidadBolsasDisponible,
                         DespachoBolsas, FechaIngreso, Activo, FechaRegistro
                  FROM LotesTorta WHERE Activo = 1 ORDER BY NumeroLote", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new LoteTorta
                {
                    IdLoteTorta = reader.GetInt32(0),
                    NumeroLote = reader.GetString(1).Trim(),
                    CantidadBolsasInicial = reader.GetInt32(2),
                    CantidadBolsasDisponible = reader.GetInt32(3),
                    ProduccionBolsas = reader.GetInt32(4),
                    Despachado = false,
                    FechaIngreso = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    Activo = reader.GetBoolean(6),
                    FechaRegistro = reader.GetDateTime(7)
                });
            }
        }
        return lista;
    }

    public async Task MarcarDespachadoAsync(string numeroLote, bool despachado)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "UPDATE LotesTorta SET Despachado = @d WHERE NumeroLote = @l", conn);
        cmd.Parameters.AddWithValue("@d", despachado);
        cmd.Parameters.AddWithValue("@l", numeroLote);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<RegistroProduccion>> ObtenerDetalleCeldaAsync(string numeroLote, DateTime fecha, string turno)
    {
        var lista = new List<RegistroProduccion>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        turno = (turno ?? "I").Trim();

        using var cmd = new SqlCommand(
            @"SELECT IdRegistro, NumeroLote, FechaProduccion, Turno, CantidadBolsas, 
                     ExpresionCantidad, FechaRegistro, UsuarioRegistro, Observaciones
              FROM RegistrosProduccion
              WHERE NumeroLote = @lote AND FechaProduccion = @fecha AND LTRIM(RTRIM(Turno)) = @turno
              ORDER BY FechaRegistro", conn);
        cmd.Parameters.AddWithValue("@lote", numeroLote);
        cmd.Parameters.AddWithValue("@fecha", fecha.Date);
        cmd.Parameters.AddWithValue("@turno", turno);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new RegistroProduccion
            {
                IdRegistro = reader.GetInt32(0),
                NumeroLote = reader.GetString(1).Trim(),
                FechaProduccion = reader.GetDateTime(2),
                Turno = reader.GetString(3).Trim(),
                CantidadBolsas = reader.GetInt32(4),
                ExpresionCantidad = reader.IsDBNull(5) ? null : reader.GetString(5),
                FechaRegistro = reader.GetDateTime(6),
                UsuarioRegistro = reader.IsDBNull(7) ? null : reader.GetString(7),
                Observaciones = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }
        return lista;
    }

    public static int EvaluarExpresion(string expresion)
    {
        if (string.IsNullOrWhiteSpace(expresion))
            return 0;

        expresion = expresion.Replace(" ", "");
        if (!System.Text.RegularExpressions.Regex.IsMatch(expresion, @"^[0-9+]+$"))
            throw new ArgumentException("Solo se permiten números y el signo +");

        var partes = expresion.Split('+', StringSplitOptions.RemoveEmptyEntries);
        int total = 0;
        foreach (var p in partes)
        {
            if (int.TryParse(p, out int n))
                total += n;
            else
                throw new ArgumentException($"Valor inválido: {p}");
        }
        return total;
    }
}
