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
    [ObservableProperty] private int _diasEnMes;

    public ObservableCollection<int> Anios { get; } = new();
    public ObservableCollection<MesItem> Meses { get; } = new();
    public ObservableCollection<string> FechasEncabezado { get; } = new();

    public const string ColLote = "Nº DE LOTE";
    public const string ColBolsas = "CANTIDAD_BOLSAS";
    public const string ColStok = "STOOCK";

    public HistorialMensualViewModel(ProduccionService service)
    {
        _service = service;

        for (int a = DateTime.Today.Year; a >= DateTime.Today.Year - 3; a--)
            Anios.Add(a);

        string[] nombres =
        {
            "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
            "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"
        };
        for (int i = 1; i <= 12; i++)
            Meses.Add(new MesItem { Numero = i, Nombre = nombres[i - 1] });
    }

    public static string ColDia(int dia, string turno) => $"D{dia:D2}_{turno}";

    public async Task CargarAsync()
    {
        try
        {
            IsLoading = true;
            var nombreMes = Meses.First(m => m.Numero == Mes).Nombre;
            TituloMes = nombreMes;
            Mensaje = "Cargando...";

            var registros = await _service.ObtenerRegistrosDelMesAsync(Anio, Mes);
            var lotes = await _service.ObtenerLotesConStockAsync();
            DiasEnMes = DateTime.DaysInMonth(Anio, Mes);

            FechasEncabezado.Clear();
            for (int d = 1; d <= DiasEnMes; d++)
                FechasEncabezado.Add($"{d:D2}/{Mes:D2}/{Anio}");

            var dt = new DataTable();
            dt.Columns.Add(ColLote, typeof(string));
            dt.Columns.Add(ColBolsas, typeof(int));
            dt.Columns.Add(ColStok, typeof(int));

            for (int d = 1; d <= DiasEnMes; d++)
            {
                dt.Columns.Add(ColDia(d, "I"), typeof(string));
                dt.Columns.Add(ColDia(d, "II"), typeof(string));
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

                row[ColLote] = numeroLote;
                row[ColBolsas] = loteInfo?.CantidadBolsasInicial ?? 400;
                row[ColStok] = loteInfo?.CantidadBolsasDisponible ?? 400;

                if (agrupado.TryGetValue(numeroLote, out var regsLote))
                {
                    for (int d = 1; d <= DiasEnMes; d++)
                    {
                        var fecha = new DateTime(Anio, Mes, d);
                        int tI = regsLote
                            .Where(r => r.FechaProduccion.Date == fecha && r.Turno == "I")
                            .Sum(r => r.CantidadBolsas);
                        int tII = regsLote
                            .Where(r => r.FechaProduccion.Date == fecha && r.Turno == "II")
                            .Sum(r => r.CantidadBolsas);

                        row[ColDia(d, "I")] = tI > 0 ? tI.ToString() : "";
                        row[ColDia(d, "II")] = tII > 0 ? tII.ToString() : "";
                    }
                }

                dt.Rows.Add(row);
            }

            TablaHistorial = dt;
            Mensaje = dt.Rows.Count == 0
                ? "Sin lotes para este mes"
                : $"{dt.Rows.Count} lote(s)  ·  {registros.Count} registro(s)";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            MessageBox.Show(ex.Message, "Error al cargar registro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Buscar() => await CargarAsync();

    [RelayCommand]
    private void ExportarExcel()
    {
        if (TablaHistorial == null || TablaHistorial.Rows.Count == 0)
        {
            MessageBox.Show("No hay datos para exportar. Pulsa MOSTRAR primero.", "Exportar",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            bool ok = ExcelExportService.ExportarPlanillaMensual(
                TablaHistorial, TituloMes, Anio, Mes, DiasEnMes);

            if (ok)
                MessageBox.Show("Planilla exportada correctamente.", "Excel",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al exportar: {ex.Message}", "Excel",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task MostrarDetalleAsync(string numeroLote, int dia, string turno)
    {
        try
        {
            var fecha = new DateTime(Anio, Mes, dia);
            var detalle = await _service.ObtenerDetalleCeldaAsync(numeroLote, fecha, turno);

            if (detalle.Count == 0)
            {
                MessageBox.Show("No hay registros en esta celda.", "Detalle",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
