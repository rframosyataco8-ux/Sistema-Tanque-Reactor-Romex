using System.Collections.ObjectModel;
using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaTanqueReactor.Models;
using SistemaTanqueReactor.Services;
using System.Windows;

namespace SistemaTanqueReactor.ViewModels;

public partial class HistorialMensualViewModel : ObservableObject
{
    private readonly ProduccionService _service;

    [ObservableProperty] private int _anio = DateTime.Today.Year;
    [ObservableProperty] private int _mes = DateTime.Today.Month;
    [ObservableProperty] private DataTable? _tablaHistorial;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _mensaje = "";
    [ObservableProperty] private string _tituloMes = "";

    public ObservableCollection<int> Anios { get; } = new();
    public ObservableCollection<MesItem> Meses { get; } = new();

    public HistorialMensualViewModel(ProduccionService service)
    {
        _service = service;

        for (int a = DateTime.Today.Year; a >= DateTime.Today.Year - 3; a--)
            Anios.Add(a);

        string[] nombres = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                             "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
        for (int i = 1; i <= 12; i++)
            Meses.Add(new MesItem { Numero = i, Nombre = nombres[i - 1] });
    }

    public async Task CargarAsync()
    {
        try
        {
            IsLoading = true;
            var nombreMes = Meses.First(m => m.Numero == Mes).Nombre.ToUpper();
            TituloMes = $"{nombreMes} {Anio}";
            Mensaje = "Cargando...";

            var registros = await _service.ObtenerRegistrosDelMesAsync(Anio, Mes);
            var lotes = await _service.ObtenerLotesConStockAsync();
            int diasEnMes = DateTime.DaysInMonth(Anio, Mes);

            var dt = new DataTable();

            // Columnas fijas (izquierda de la planilla)
            dt.Columns.Add("Nº DE LOTE", typeof(string));
            dt.Columns.Add("CANTIDAD_BOLSAS", typeof(int));
            dt.Columns.Add("DESPACHO", typeof(int));
            dt.Columns.Add("STOCK kg", typeof(int));

            // Por cada día: Turno I y Turno II
            for (int d = 1; d <= diasEnMes; d++)
            {
                dt.Columns.Add($"{d:D2} I", typeof(string));
                dt.Columns.Add($"{d:D2} II", typeof(string));
            }

            var agrupado = registros
                .GroupBy(r => r.NumeroLote)
                .ToDictionary(g => g.Key, g => g.ToList());

            var lotesUnicos = lotes.Select(l => l.NumeroLote)
                .Union(registros.Select(r => r.NumeroLote))
                .Distinct()
                .OrderBy(x => x);

            foreach (var numeroLote in lotesUnicos)
            {
                var loteInfo = lotes.FirstOrDefault(l => l.NumeroLote == numeroLote);
                var row = dt.NewRow();
                row["Nº DE LOTE"] = numeroLote;
                row["CANTIDAD_BOLSAS"] = loteInfo?.CantidadBolsasInicial ?? 400;
                row["DESPACHO"] = loteInfo?.DespachoBolsas ?? 0;
                row["STOCK kg"] = loteInfo?.StockKg ?? 10000;

                if (agrupado.TryGetValue(numeroLote, out var regsLote))
                {
                    for (int d = 1; d <= diasEnMes; d++)
                    {
                        var fecha = new DateTime(Anio, Mes, d);
                        var turnoI = regsLote.Where(r => r.FechaProduccion.Date == fecha && r.Turno == "I").Sum(r => r.CantidadBolsas);
                        var turnoII = regsLote.Where(r => r.FechaProduccion.Date == fecha && r.Turno == "II").Sum(r => r.CantidadBolsas);
                        row[$"{d:D2} I"] = turnoI > 0 ? turnoI.ToString() : "";
                        row[$"{d:D2} II"] = turnoII > 0 ? turnoII.ToString() : "";
                    }
                }

                dt.Rows.Add(row);
            }

            TablaHistorial = dt;
            Mensaje = $"{dt.Rows.Count} lote(s)  ·  {registros.Count} registro(s)";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            MessageBox.Show(ex.Message, "Error al cargar historial", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Buscar() => await CargarAsync();

    public async Task MostrarDetalleAsync(string numeroLote, int dia, string turno)
    {
        try
        {
            var fecha = new DateTime(Anio, Mes, dia);
            var detalle = await _service.ObtenerDetalleCeldaAsync(numeroLote, fecha, turno);

            if (detalle.Count == 0)
            {
                MessageBox.Show("No hay registros en esta celda.", "Detalle", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Lote: {numeroLote}");
            sb.AppendLine($"Fecha: {fecha:dd/MM/yyyy}  ·  Turno {turno}");
            sb.AppendLine(new string('-', 40));

            int total = 0;
            foreach (var r in detalle)
            {
                var expr = string.IsNullOrEmpty(r.ExpresionCantidad) ? r.CantidadBolsas.ToString() : r.ExpresionCantidad;
                sb.AppendLine($"  {expr}  =  {r.CantidadBolsas} bolsas  ({r.CantidadKg} kg)");
                total += r.CantidadBolsas;
            }

            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"TOTAL: {total} bolsas = {total * 25:N0} kg");

            MessageBox.Show(sb.ToString(), "Detalle de cantidades", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class MesItem
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = "";
}
