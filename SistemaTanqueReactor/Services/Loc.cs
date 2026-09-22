namespace SistemaTanqueReactor.Services;

/// <summary>
/// Internacionalización simple ES / EN.
/// Uso: Loc.T("key")  ·  Loc.SetLanguage("en")
/// </summary>
public static class Loc
{
    public static string Language { get; private set; } = "es";

    public static event Action? LanguageChanged;

    public static void SetLanguage(string code)
    {
        code = code.ToLowerInvariant();
        if (code is not ("es" or "en")) return;
        if (Language == code) return;
        Language = code;
        LanguageChanged?.Invoke();
    }

    public static void Toggle()
        => SetLanguage(Language == "es" ? "en" : "es");

    public static string T(string key)
    {
        if (Language == "en" && En.TryGetValue(key, out var en))
            return en;
        if (Es.TryGetValue(key, out var es))
            return es;
        return key;
    }

    private static readonly Dictionary<string, string> Es = new()
    {
        // Validación
        ["val.lote.required"] = "El Nº de lote es obligatorio.",
        ["val.lote.min"] = "Mínimo 3 caracteres.",
        ["val.lote.max"] = "Máximo 50 caracteres.",
        ["val.lote.format"] = "Solo letras, números, punto, guion o guion bajo.",
        ["val.lote.exists"] = "Ese lote ya existe.",
        ["val.bolsas.min"] = "La cantidad debe ser mayor a 0.",
        ["val.bolsas.max"] = "Máximo {0} bolsas.",
        ["val.fecha.required"] = "La fecha es obligatoria.",
        ["val.fecha.future"] = "La fecha no puede ser futura.",
        ["val.fecha.old"] = "Fecha demasiado antigua.",
        ["val.turno"] = "Turno inválido. Use I o II.",
        ["val.cantidad.required"] = "Ingrese la cantidad (ej: 30+30+30 o 120).",
        ["val.cantidad.min"] = "La cantidad debe ser mayor a 0.",
        ["val.cantidad.max"] = "No puede superar {0} bolsas por registro.",
        ["val.cantidad.format"] = "Cantidad inválida. Use números y + (ej: 30+28+2).",
        ["val.nombre.required"] = "El nombre es obligatorio.",
        ["val.nombre.min"] = "Mínimo 2 caracteres.",
        ["val.nombre.max"] = "Máximo 100 caracteres.",
        ["val.nombre.format"] = "Solo letras y espacios.",
        ["val.dni.required"] = "El DNI es obligatorio.",
        ["val.dni.format"] = "DNI debe tener 8 dígitos.",
        ["val.dni.exists"] = "Ya existe un operario con ese DNI.",
        ["val.obs.max"] = "Observaciones: máximo {0} caracteres.",

        // UI
        ["ui.dashboard"] = "Dashboard",
        ["ui.nuevo"] = "Nuevo Registro",
        ["ui.registro"] = "Registro mensual",
        ["ui.historial"] = "Historial de registros",
        ["ui.otros"] = "Otros · Lotes y Operarios",
        ["ui.lang"] = "ES",
        ["ui.validation"] = "Validación",
        ["ui.error"] = "Error",
        ["ui.success"] = "Éxito",
    };

    private static readonly Dictionary<string, string> En = new()
    {
        ["val.lote.required"] = "Lot number is required.",
        ["val.lote.min"] = "Minimum 3 characters.",
        ["val.lote.max"] = "Maximum 50 characters.",
        ["val.lote.format"] = "Only letters, numbers, dot, hyphen or underscore.",
        ["val.lote.exists"] = "That lot already exists.",
        ["val.bolsas.min"] = "Quantity must be greater than 0.",
        ["val.bolsas.max"] = "Maximum {0} bags.",
        ["val.fecha.required"] = "Date is required.",
        ["val.fecha.future"] = "Date cannot be in the future.",
        ["val.fecha.old"] = "Date is too old.",
        ["val.turno"] = "Invalid shift. Use I or II.",
        ["val.cantidad.required"] = "Enter quantity (e.g. 30+30+30 or 120).",
        ["val.cantidad.min"] = "Quantity must be greater than 0.",
        ["val.cantidad.max"] = "Cannot exceed {0} bags per entry.",
        ["val.cantidad.format"] = "Invalid quantity. Use numbers and + (e.g. 30+28+2).",
        ["val.nombre.required"] = "Name is required.",
        ["val.nombre.min"] = "Minimum 2 characters.",
        ["val.nombre.max"] = "Maximum 100 characters.",
        ["val.nombre.format"] = "Only letters and spaces.",
        ["val.dni.required"] = "ID number is required.",
        ["val.dni.format"] = "ID must be 8 digits.",
        ["val.dni.exists"] = "An operator with that ID already exists.",
        ["val.obs.max"] = "Notes: maximum {0} characters.",

        ["ui.dashboard"] = "Dashboard",
        ["ui.nuevo"] = "New Entry",
        ["ui.registro"] = "Monthly log",
        ["ui.historial"] = "Entry history",
        ["ui.otros"] = "Other · Lots & Operators",
        ["ui.lang"] = "EN",
        ["ui.validation"] = "Validation",
        ["ui.error"] = "Error",
        ["ui.success"] = "Success",
    };
}
