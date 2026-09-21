namespace SistemaTanqueReactor.Models;

public class Turno
{
    public int IdTurno { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public bool EsDomingo { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<CargaReactor> Cargas { get; set; } = new List<CargaReactor>();
}
