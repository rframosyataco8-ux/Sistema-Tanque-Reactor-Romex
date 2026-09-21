namespace SistemaTanqueReactor.Models;

public class Lote
{
    public int IdLote { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public int? IdInsumo { get; set; }
    public string? TipoProducto { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public decimal? CantidadInicialKg { get; set; }
    public decimal? CantidadDisponibleKg { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public Insumo? Insumo { get; set; }
    public ICollection<CargaInsumo> CargaInsumos { get; set; } = new List<CargaInsumo>();
}
