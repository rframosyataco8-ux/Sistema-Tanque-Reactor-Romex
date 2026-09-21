namespace SistemaTanqueReactor.Models;

public class RegistroProduccion
{
    public int IdRegistro { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaProduccion { get; set; }
    public string Turno { get; set; } = "I"; // I o II
    public int CantidadBolsas { get; set; }
    public string? ExpresionCantidad { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public string? UsuarioRegistro { get; set; }
    public string? Observaciones { get; set; }

    public int CantidadKg => CantidadBolsas * 25;
}

public class LoteTorta
{
    public int IdLoteTorta { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public int CantidadBolsasInicial { get; set; } = 400;
    public int CantidadBolsasDisponible { get; set; } = 400;
    public int StockKg => CantidadBolsasDisponible * 25;
    public int DespachoBolsas { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
