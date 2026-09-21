namespace SistemaTanqueReactor.Models;

public class CargaReactor
{
    public int IdCarga { get; set; }
    public DateTime Fecha { get; set; }
    public int IdTurno { get; set; }
    public int? IdOperario { get; set; }
    public int NumeroTanque { get; set; } = 1;
    public int NumeroCargaDelTurno { get; set; } = 1;

    // Tiempos
    public TimeSpan? HoraInicioCarga { get; set; }
    public TimeSpan? HoraFinCarga { get; set; }
    public TimeSpan? HoraInicioSolucion { get; set; }
    public TimeSpan? HoraFinSolucion { get; set; }
    public TimeSpan? HoraInicioVacio { get; set; }
    public TimeSpan? HoraFinVacio { get; set; }
    public string? TiempoSecadoAlVacio { get; set; }

    // Parámetros del producto
    public decimal? pH { get; set; }
    public decimal? Humedad { get; set; }
    public decimal? TempProductoFinal { get; set; }
    public decimal? CantidadElaboradaKg { get; set; }
    public string? LoteProductoFinal { get; set; }
    public decimal? StockKg { get; set; }

    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "En Proceso";

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public string? UsuarioRegistro { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }

    // Navegación
    public Turno Turno { get; set; } = null!;
    public Operario? Operario { get; set; }
    public ICollection<CargaInsumo> Insumos { get; set; } = new List<CargaInsumo>();
    public ICollection<ControlTemperatura> ControlesTemperatura { get; set; } = new List<ControlTemperatura>();
    public ICollection<ChecklistResultado> ChecklistResultados { get; set; } = new List<ChecklistResultado>();
}
