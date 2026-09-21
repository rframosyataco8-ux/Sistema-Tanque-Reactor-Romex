namespace SistemaTanqueReactor.Models;

public class ControlTemperatura
{
    public int IdControl { get; set; }
    public int IdCarga { get; set; }
    public TimeSpan Hora { get; set; }
    public decimal? TempChaqueta { get; set; }
    public decimal? TempProducto { get; set; }
    public decimal? Presion { get; set; }
    public string? Observacion { get; set; }

    public CargaReactor Carga { get; set; } = null!;
}
