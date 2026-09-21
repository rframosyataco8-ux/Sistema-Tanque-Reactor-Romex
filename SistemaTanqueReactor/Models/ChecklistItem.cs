namespace SistemaTanqueReactor.Models;

public class ChecklistItem
{
    public int IdItem { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<ChecklistResultado> Resultados { get; set; } = new List<ChecklistResultado>();
}
