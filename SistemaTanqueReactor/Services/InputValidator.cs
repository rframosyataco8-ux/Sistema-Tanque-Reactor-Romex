using System.Text.RegularExpressions;

namespace SistemaTanqueReactor.Services;

/// <summary>
/// Validación centralizada de formularios (lógica única, reutilizable).
/// </summary>
public static class InputValidator
{
    public sealed class Result
    {
        public bool IsValid => Errors.Count == 0;
        public Dictionary<string, string> Errors { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string field, string message)
        {
            if (!Errors.ContainsKey(field))
                Errors[field] = message;
        }

        public string? this[string field] =>
            Errors.TryGetValue(field, out var m) ? m : null;

        public string Summary => string.Join(Environment.NewLine, Errors.Values);
    }

    // —— Lote ——
    public static void ValidateLoteNumero(string? value, Result r, string field = "Lote")
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v))
            r.Add(field, Loc.T("val.lote.required"));
        else if (v.Length < 3)
            r.Add(field, Loc.T("val.lote.min"));
        else if (v.Length > 50)
            r.Add(field, Loc.T("val.lote.max"));
        else if (!Regex.IsMatch(v, @"^[A-Za-z0-9._\-]+$"))
            r.Add(field, Loc.T("val.lote.format"));
    }

    public static void ValidateBolsas(int value, Result r, string field = "Bolsas", int max = 10000)
    {
        if (value <= 0)
            r.Add(field, Loc.T("val.bolsas.min"));
        else if (value > max)
            r.Add(field, string.Format(Loc.T("val.bolsas.max"), max));
    }

    public static void ValidateFecha(
        DateTime? fecha,
        Result r,
        string field = "Fecha",
        bool allowNull = true,
        int maxYearsBack = 5,
        bool allowFuture = false)
    {
        if (!fecha.HasValue)
        {
            if (!allowNull)
                r.Add(field, Loc.T("val.fecha.required"));
            return;
        }

        var d = fecha.Value.Date;
        if (!allowFuture && d > DateTime.Today)
            r.Add(field, Loc.T("val.fecha.future"));
        else if (d < DateTime.Today.AddYears(-maxYearsBack))
            r.Add(field, Loc.T("val.fecha.old"));
    }

    public static void ValidateTurno(string? turno, Result r, string field = "Turno")
    {
        var t = (turno ?? "").Trim().ToUpperInvariant();
        if (t != "I" && t != "II")
            r.Add(field, Loc.T("val.turno"));
    }

    public static void ValidateExpresionCantidad(string? texto, Result r, out int cantidad, string field = "Cantidad", int max = 400)
    {
        cantidad = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            r.Add(field, Loc.T("val.cantidad.required"));
            return;
        }

        try
        {
            cantidad = ProduccionService.EvaluarExpresion(texto);
            if (cantidad <= 0)
                r.Add(field, Loc.T("val.cantidad.min"));
            else if (cantidad > max)
                r.Add(field, string.Format(Loc.T("val.cantidad.max"), max));
        }
        catch
        {
            r.Add(field, Loc.T("val.cantidad.format"));
        }
    }

    public static void ValidateNombrePersona(string? value, Result r, string field = "Nombres", bool required = true)
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v))
        {
            if (required)
                r.Add(field, Loc.T("val.nombre.required"));
            return;
        }
        if (v.Length < 2)
            r.Add(field, Loc.T("val.nombre.min"));
        else if (v.Length > 100)
            r.Add(field, Loc.T("val.nombre.max"));
        else if (!Regex.IsMatch(v, @"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s.'\-]+$"))
            r.Add(field, Loc.T("val.nombre.format"));
    }

    public static void ValidateDni(string? value, Result r, string field = "Dni", bool required = false)
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrEmpty(v))
        {
            if (required)
                r.Add(field, Loc.T("val.dni.required"));
            return;
        }
        if (!Regex.IsMatch(v, @"^\d{8}$"))
            r.Add(field, Loc.T("val.dni.format"));
    }

    public static void ValidateObservaciones(string? value, Result r, string field = "Obs", int max = 300)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > max)
            r.Add(field, string.Format(Loc.T("val.obs.max"), max));
    }
}
