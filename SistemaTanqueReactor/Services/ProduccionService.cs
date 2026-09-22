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

    /// <summary>Bolsas disponibles del lote. Si no existe, retorna null.</summary>
    public async Task<int?> ObtenerStockDisponibleAsync(string numeroLote)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT CantidadBolsasDisponible FROM LotesTorta WHERE NumeroLote = @l AND Activo = 1", conn);
        cmd.Parameters.AddWithValue("@l", numeroLote.Trim());
        var o = await cmd.ExecuteScalarAsync();
        if (o == null || o == DBNull.Value) return null;
        return Convert.ToInt32(o);
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

        var turno = (registro.Turno ?? "I").Trim().ToUpperInvariant();
        if (turno != "I" && turno != "II")
            turno = "I";
        registro.Turno = turno;

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var tran = conn.BeginTransaction();
        try
        {
            // Validar stock dentro de la transacción (evita race conditions)
            int disponible;
            using (var stockCmd = new SqlCommand(
                "SELECT CantidadBolsasDisponible FROM LotesTorta WITH (UPDLOCK) WHERE NumeroLote = @lote", conn, tran))
            {
                stockCmd.Parameters.AddWithValue("@lote", registro.NumeroLote.Trim());
                var o = await stockCmd.ExecuteScalarAsync();
                disponible = o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }

            if (registro.CantidadBolsas > disponible)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente en lote {registro.NumeroLote}.\n" +
                    $"Disponible: {disponible} bolsas · Intentas usar: {registro.CantidadBolsas} bolsas.");
            }

            using var cmd = new SqlCommand(
                @"INSERT INTO RegistrosProduccion 
                  (NumeroLote, FechaProduccion, Turno, CantidadBolsas, ExpresionCantidad, UsuarioRegistro, Observaciones)
                  VALUES (@lote, @fecha, @turno, @cantidad, @expresion, @usuario, @obs)", conn, tran);

            cmd.Parameters.AddWithValue("@lote", registro.NumeroLote.Trim());
            cmd.Parameters.AddWithValue("@fecha", registro.FechaProduccion.Date);
            cmd.Parameters.AddWithValue("@turno", turno);
            cmd.Parameters.AddWithValue("@cantidad", registro.CantidadBolsas);
            cmd.Parameters.AddWithValue("@expresion", (object?)registro.ExpresionCantidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@usuario", (object?)registro.UsuarioRegistro ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@obs", (object?)registro.Observaciones ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();

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

    /// <summary>
    /// Edición inline: establece el total de bolsas de un lote/fecha/turno.
    /// Ajusta stock por la diferencia (nueva - anterior).
    /// </summary>
    public async Task EstablecerCantidadCeldaAsync(string numeroLote, DateTime fecha, string turno, int nuevaCantidad)
    {
        if (nuevaCantidad < 0)
            throw new ArgumentException("La cantidad no puede ser negativa.");

        turno = (turno ?? "I").Trim().ToUpperInvariant();
        if (turno != "I" && turno != "II") turno = "I";

        await AsegurarLoteAsync(numeroLote);

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            // Total actual en esa celda
            int actual = 0;
            using (var sumCmd = new SqlCommand(
                @"SELECT ISNULL(SUM(CantidadBolsas),0) FROM RegistrosProduccion
                  WHERE NumeroLote = @l AND FechaProduccion = @f AND LTRIM(RTRIM(Turno)) = @t", conn, tran))
            {
                sumCmd.Parameters.AddWithValue("@l", numeroLote.Trim());
                sumCmd.Parameters.AddWithValue("@f", fecha.Date);
                sumCmd.Parameters.AddWithValue("@t", turno);
                actual = Convert.ToInt32(await sumCmd.ExecuteScalarAsync() ?? 0);
            }

            int delta = nuevaCantidad - actual; // positivo = consumir más stock

            if (delta > 0)
            {
                int disponible;
                using (var stockCmd = new SqlCommand(
                    "SELECT CantidadBolsasDisponible FROM LotesTorta WITH (UPDLOCK) WHERE NumeroLote = @l", conn, tran))
                {
                    stockCmd.Parameters.AddWithValue("@l", numeroLote.Trim());
                    disponible = Convert.ToInt32(await stockCmd.ExecuteScalarAsync() ?? 0);
                }
                if (delta > disponible)
                    throw new InvalidOperationException(
                        $"Stock insuficiente. Disponible: {disponible} · Necesitas {delta} bolsas más.");
            }

            // Borrar registros previos de esa celda
            using (var del = new SqlCommand(
                @"DELETE FROM RegistrosProduccion
                  WHERE NumeroLote = @l AND FechaProduccion = @f AND LTRIM(RTRIM(Turno)) = @t", conn, tran))
            {
                del.Parameters.AddWithValue("@l", numeroLote.Trim());
                del.Parameters.AddWithValue("@f", fecha.Date);
                del.Parameters.AddWithValue("@t", turno);
                await del.ExecuteNonQueryAsync();
            }

            if (nuevaCantidad > 0)
            {
                using var ins = new SqlCommand(
                    @"INSERT INTO RegistrosProduccion
                      (NumeroLote, FechaProduccion, Turno, CantidadBolsas, ExpresionCantidad, UsuarioRegistro)
                      VALUES (@l, @f, @t, @c, @e, @u)", conn, tran);
                ins.Parameters.AddWithValue("@l", numeroLote.Trim());
                ins.Parameters.AddWithValue("@f", fecha.Date);
                ins.Parameters.AddWithValue("@t", turno);
                ins.Parameters.AddWithValue("@c", nuevaCantidad);
                ins.Parameters.AddWithValue("@e", nuevaCantidad.ToString());
                ins.Parameters.AddWithValue("@u", Environment.UserName);
                await ins.ExecuteNonQueryAsync();
            }

            // Ajustar stock por delta
            if (delta != 0)
            {
                try
                {
                    using var upd = new SqlCommand(
                        @"UPDATE LotesTorta
                          SET CantidadBolsasDisponible = CantidadBolsasDisponible - @d,
                              ProduccionBolsas = ProduccionBolsas + @d
                          WHERE NumeroLote = @l", conn, tran);
                    upd.Parameters.AddWithValue("@d", delta);
                    upd.Parameters.AddWithValue("@l", numeroLote.Trim());
                    await upd.ExecuteNonQueryAsync();
                }
                catch
                {
                    using var upd = new SqlCommand(
                        @"UPDATE LotesTorta
                          SET CantidadBolsasDisponible = CantidadBolsasDisponible - @d,
                              DespachoBolsas = DespachoBolsas + @d
                          WHERE NumeroLote = @l", conn, tran);
                    upd.Parameters.AddWithValue("@d", delta);
                    upd.Parameters.AddWithValue("@l", numeroLote.Trim());
                    await upd.ExecuteNonQueryAsync();
                }
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
            lista.Add(LeerRegistro(reader));
        return lista;
    }

    public async Task<List<RegistroProduccion>> ObtenerTodosRegistrosAsync(
        DateTime? desde, DateTime? hasta, string? lote, string? turno)
    {
        var lista = new List<RegistroProduccion>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT IdRegistro, NumeroLote, FechaProduccion, Turno, CantidadBolsas,
                           ExpresionCantidad, FechaRegistro, UsuarioRegistro, Observaciones
                    FROM RegistrosProduccion WHERE 1=1";

        if (desde.HasValue) sql += " AND FechaProduccion >= @desde";
        if (hasta.HasValue) sql += " AND FechaProduccion <= @hasta";
        if (!string.IsNullOrWhiteSpace(lote)) sql += " AND NumeroLote LIKE @lote";
        if (!string.IsNullOrWhiteSpace(turno)) sql += " AND LTRIM(RTRIM(Turno)) = @turno";
        sql += " ORDER BY FechaProduccion DESC, NumeroLote, Turno";

        using var cmd = new SqlCommand(sql, conn);
        if (desde.HasValue) cmd.Parameters.AddWithValue("@desde", desde.Value.Date);
        if (hasta.HasValue) cmd.Parameters.AddWithValue("@hasta", hasta.Value.Date);
        if (!string.IsNullOrWhiteSpace(lote)) cmd.Parameters.AddWithValue("@lote", "%" + lote.Trim() + "%");
        if (!string.IsNullOrWhiteSpace(turno)) cmd.Parameters.AddWithValue("@turno", turno.Trim());

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            lista.Add(LeerRegistro(reader));

        return lista;
    }

    public async Task EliminarRegistroAsync(int idRegistro)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();
        try
        {
            string? numeroLote = null;
            int cantidad = 0;
            using (var sel = new SqlCommand(
                "SELECT NumeroLote, CantidadBolsas FROM RegistrosProduccion WHERE IdRegistro = @id", conn, tran))
            {
                sel.Parameters.AddWithValue("@id", idRegistro);
                using var r = await sel.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    numeroLote = r.GetString(0).Trim();
                    cantidad = r.GetInt32(1);
                }
            }

            if (numeroLote == null)
                throw new InvalidOperationException("Registro no encontrado.");

            using (var del = new SqlCommand(
                "DELETE FROM RegistrosProduccion WHERE IdRegistro = @id", conn, tran))
            {
                del.Parameters.AddWithValue("@id", idRegistro);
                await del.ExecuteNonQueryAsync();
            }

            try
            {
                using var upd = new SqlCommand(
                    @"UPDATE LotesTorta
                      SET CantidadBolsasDisponible = CantidadBolsasDisponible + @c,
                          ProduccionBolsas = CASE WHEN ProduccionBolsas >= @c THEN ProduccionBolsas - @c ELSE 0 END
                      WHERE NumeroLote = @l", conn, tran);
                upd.Parameters.AddWithValue("@c", cantidad);
                upd.Parameters.AddWithValue("@l", numeroLote);
                await upd.ExecuteNonQueryAsync();
            }
            catch
            {
                using var upd = new SqlCommand(
                    @"UPDATE LotesTorta
                      SET CantidadBolsasDisponible = CantidadBolsasDisponible + @c,
                          DespachoBolsas = CASE WHEN DespachoBolsas >= @c THEN DespachoBolsas - @c ELSE 0 END
                      WHERE NumeroLote = @l", conn, tran);
                upd.Parameters.AddWithValue("@c", cantidad);
                upd.Parameters.AddWithValue("@l", numeroLote);
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
            lista.Add(LeerRegistro(reader));

        return lista;
    }

    private static RegistroProduccion LeerRegistro(SqlDataReader reader)
    {
        return new RegistroProduccion
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
        };
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
