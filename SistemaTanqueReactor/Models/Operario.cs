namespace SistemaTanqueReactor.Models;

public class Operario
{
    public int IdOperario { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string? Dni { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string NombreCompleto => string.IsNullOrWhiteSpace(Apellidos) 
        ? Nombres 
        : $"{Nombres} {Apellidos}";

    public ICollection<CargaReactor> Cargas { get; set; } = new List<CargaReactor>();
}
