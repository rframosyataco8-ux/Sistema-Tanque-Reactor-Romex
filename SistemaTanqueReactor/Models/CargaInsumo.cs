namespace SistemaTanqueReactor.Models;

public class CargaInsumo
{
    public int IdCargaInsumo { get; set; }
    public int IdCarga { get; set; }
    public int IdInsumo { get; set; }
    public int? IdLote { get; set; }
    public int? CantidadBolsas { get; set; }
    public decimal CantidadKg { get; set; }
    public bool EsMerma { get; set; }
    public string? Observacion { get; set; }

    public CargaReactor Carga { get; set; } = null!;
    public Insumo Insumo { get; set; } = null!;
    public Lote? Lote { get; set; }
}
