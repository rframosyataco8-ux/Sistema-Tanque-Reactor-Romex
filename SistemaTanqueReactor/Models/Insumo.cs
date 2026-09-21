namespace SistemaTanqueReactor.Models;

public class Insumo
{
    public int IdInsumo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = "Kg";
    public bool EsPrincipal { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Lote> Lotes { get; set; } = new List<Lote>();
    public ICollection<CargaInsumo> CargaInsumos { get; set; } = new List<CargaInsumo>();
}
