using Microsoft.EntityFrameworkCore;
using SistemaTanqueReactor.Data;
using SistemaTanqueReactor.Models;

namespace SistemaTanqueReactor.Services;

public class CargaService
{
    private readonly TanqueReactorContext _context;

    public CargaService(TanqueReactorContext context)
    {
        _context = context;
    }

    public async Task<List<CargaReactor>> ObtenerCargasAsync(DateTime? fecha = null, int? idTurno = null)
    {
        var query = _context.CargasReactor
            .Include(c => c.Turno)
            .Include(c => c.Operario)
            .Include(c => c.Insumos).ThenInclude(i => i.Insumo)
            .Include(c => c.Insumos).ThenInclude(i => i.Lote)
            .AsQueryable();

        if (fecha.HasValue)
            query = query.Where(c => c.Fecha.Date == fecha.Value.Date);

        if (idTurno.HasValue)
            query = query.Where(c => c.IdTurno == idTurno.Value);

        return await query
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.NumeroCargaDelTurno)
            .ToListAsync();
    }

    public async Task<CargaReactor?> ObtenerCargaPorIdAsync(int idCarga)
    {
        return await _context.CargasReactor
            .Include(c => c.Turno)
            .Include(c => c.Operario)
            .Include(c => c.Insumos).ThenInclude(i => i.Insumo)
            .Include(c => c.Insumos).ThenInclude(i => i.Lote)
            .Include(c => c.ControlesTemperatura)
            .Include(c => c.ChecklistResultados).ThenInclude(r => r.Item)
            .FirstOrDefaultAsync(c => c.IdCarga == idCarga);
    }

    public async Task<int> ObtenerSiguienteNumeroCargaAsync(DateTime fecha, int idTurno)
    {
        var max = await _context.CargasReactor
            .Where(c => c.Fecha.Date == fecha.Date && c.IdTurno == idTurno)
            .MaxAsync(c => (int?)c.NumeroCargaDelTurno) ?? 0;

        return max + 1;
    }

    public async Task<CargaReactor> GuardarCargaAsync(CargaReactor carga)
    {
        if (carga.IdCarga == 0)
        {
            carga.FechaRegistro = DateTime.Now;
            _context.CargasReactor.Add(carga);
        }
        else
        {
            carga.FechaModificacion = DateTime.Now;
            _context.CargasReactor.Update(carga);
        }

        await _context.SaveChangesAsync();
        return carga;
    }

    public async Task FinalizarCargaAsync(int idCarga)
    {
        var carga = await _context.CargasReactor.FindAsync(idCarga);
        if (carga != null)
        {
            carga.Estado = "Finalizado";
            carga.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Turno>> ObtenerTurnosAsync()
    {
        return await _context.Turnos.Where(t => t.Activo).OrderBy(t => t.IdTurno).ToListAsync();
    }

    public async Task<List<Operario>> ObtenerOperariosAsync()
    {
        return await _context.Operarios.Where(o => o.Activo).OrderBy(o => o.Nombres).ToListAsync();
    }

    public async Task<List<Insumo>> ObtenerInsumosAsync()
    {
        return await _context.Insumos.Where(i => i.Activo).OrderBy(i => i.Nombre).ToListAsync();
    }

    public async Task<List<Lote>> BuscarLotesAsync(string? texto = null)
    {
        var query = _context.Lotes.Where(l => l.Activo).AsQueryable();

        if (!string.IsNullOrWhiteSpace(texto))
            query = query.Where(l => l.NumeroLote.Contains(texto));

        return await query.OrderByDescending(l => l.FechaRegistro).Take(50).ToListAsync();
    }

    public async Task<List<ChecklistItem>> ObtenerChecklistItemsAsync()
    {
        return await _context.ChecklistItems
            .Where(i => i.Activo)
            .OrderBy(i => i.Orden)
            .ToListAsync();
    }

    public async Task<(int TotalCargas, decimal TotalKgTorta, decimal TotalKgMerma)> ObtenerResumenDiaAsync(DateTime fecha)
    {
        var cargas = await _context.CargasReactor
            .Include(c => c.Insumos)
            .Where(c => c.Fecha.Date == fecha.Date && c.Estado != "Anulado")
            .ToListAsync();

        var totalCargas = cargas.Count;
        var totalTorta = cargas.SelectMany(c => c.Insumos).Where(i => !i.EsMerma).Sum(i => i.CantidadKg);
        var totalMerma = cargas.SelectMany(c => c.Insumos).Where(i => i.EsMerma).Sum(i => i.CantidadKg);

        return (totalCargas, totalTorta, totalMerma);
    }
}
