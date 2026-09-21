namespace SistemaTanqueReactor.Models;

public class ChecklistResultado
{
    public int IdResultado { get; set; }
    public int IdCarga { get; set; }
    public int IdItem { get; set; }
    public string Estado { get; set; } = "Conforme"; // Conforme, No Conforme, N/A
    public string? Observacion { get; set; }

    public CargaReactor Carga { get; set; } = null!;
    public ChecklistItem Item { get; set; } = null!;
}
