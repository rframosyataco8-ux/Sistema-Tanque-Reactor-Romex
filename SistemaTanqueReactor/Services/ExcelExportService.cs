using System.Data;
using ClosedXML.Excel;
using Microsoft.Win32;
using SistemaTanqueReactor.Models;

namespace SistemaTanqueReactor.Services;

public static class ExcelExportService
{
    /// <summary>
    /// Exporta la planilla mensual (Registro) con estructura:
    /// mes | fechas | Nº LOTE | CANTIDAD_BOLSAS | STOOCK | I | II por día
    /// </summary>
    public static bool ExportarPlanillaMensual(DataTable tabla, string tituloMes, int anio, int mes, int diasEnMes)
    {
        if (tabla == null || tabla.Rows.Count == 0)
            return false;

        var dlg = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Registro_{tituloMes}_{anio}.xlsx",
            Title = "Exportar planilla mensual"
        };

        if (dlg.ShowDialog() != true)
            return false;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(tituloMes);

        // Fila 1: mes centrado sobre columnas de días
        int colFijas = 3;
        int colInicioDias = 4;
        int colFin = colFijas + diasEnMes * 2;

        ws.Range(1, colInicioDias, 1, colFin).Merge();
        ws.Cell(1, colInicioDias).Value = tituloMes;
        ws.Cell(1, colInicioDias).Style.Font.Bold = true;
        ws.Cell(1, colInicioDias).Style.Font.FontSize = 14;
        ws.Cell(1, colInicioDias).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Fila 2: fechas (cada una abarca 2 columnas I/II)
        for (int d = 1; d <= diasEnMes; d++)
        {
            int c = colFijas + (d - 1) * 2 + 1;
            ws.Range(2, c, 2, c + 1).Merge();
            ws.Cell(2, c).Value = $"{d:D2}/{mes:D2}/{anio}";
            ws.Cell(2, c).Style.Font.Bold = true;
            ws.Cell(2, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, c).Style.Font.FontSize = 10;
        }

        // Fila 3: headers fijos + I | II
        ws.Cell(3, 1).Value = "Nº DE LOTE";
        ws.Cell(3, 2).Value = "CANTIDAD_BOLSAS";
        ws.Cell(3, 3).Value = "STOOCK";
        for (int d = 1; d <= diasEnMes; d++)
        {
            int c = colFijas + (d - 1) * 2 + 1;
            ws.Cell(3, c).Value = "I";
            ws.Cell(3, c + 1).Value = "II";
        }

        var headerRange = ws.Range(3, 1, 3, colFin);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

        // Datos
        int row = 4;
        foreach (DataRow dr in tabla.Rows)
        {
            ws.Cell(row, 1).Value = dr["Nº DE LOTE"]?.ToString() ?? "";
            ws.Cell(row, 2).Value = ToInt(dr["CANTIDAD_BOLSAS"]);
            ws.Cell(row, 3).Value = ToInt(dr["STOOCK"]);

            for (int d = 1; d <= diasEnMes; d++)
            {
                int c = colFijas + (d - 1) * 2 + 1;
                string colI = $"D{d:D2}_I";
                string colII = $"D{d:D2}_II";
                var vI = dr.Table.Columns.Contains(colI) ? dr[colI]?.ToString() : "";
                var vII = dr.Table.Columns.Contains(colII) ? dr[colII]?.ToString() : "";
                if (!string.IsNullOrEmpty(vI) && int.TryParse(vI, out int nI))
                    ws.Cell(row, c).Value = nI;
                if (!string.IsNullOrEmpty(vII) && int.TryParse(vII, out int nII))
                    ws.Cell(row, c + 1).Value = nII;
            }
            row++;
        }

        // Bordes y anchos
        var used = ws.Range(1, 1, row - 1, colFin);
        used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        used.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(1).Width = 16;
        ws.Column(2).Width = 16;
        ws.Column(3).Width = 10;
        for (int c = 4; c <= colFin; c++)
            ws.Column(c).Width = 6;

        ws.SheetView.FreezeRows(3);
        ws.SheetView.FreezeColumns(3);

        wb.SaveAs(dlg.FileName);
        return true;
    }

    /// <summary>Exporta lista de registros de producción (Historial).</summary>
    public static bool ExportarHistorial(IEnumerable<RegistroProduccion> registros)
    {
        var list = registros?.ToList() ?? new List<RegistroProduccion>();
        if (list.Count == 0)
            return false;

        var dlg = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Historial_{DateTime.Today:yyyyMMdd}.xlsx",
            Title = "Exportar historial"
        };

        if (dlg.ShowDialog() != true)
            return false;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Historial");

        string[] headers = { "Fecha", "Turno", "Nº Lote", "Bolsas", "Kg", "Expresión", "Usuario", "Registrado" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0D9488");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int r = 2;
        foreach (var reg in list)
        {
            ws.Cell(r, 1).Value = reg.FechaProduccion;
            ws.Cell(r, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(r, 2).Value = reg.Turno;
            ws.Cell(r, 3).Value = reg.NumeroLote;
            ws.Cell(r, 4).Value = reg.CantidadBolsas;
            ws.Cell(r, 5).Value = reg.CantidadKg;
            ws.Cell(r, 6).Value = reg.ExpresionCantidad ?? "";
            ws.Cell(r, 7).Value = reg.UsuarioRegistro ?? "";
            ws.Cell(r, 8).Value = reg.FechaRegistro;
            ws.Cell(r, 8).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            r++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
        wb.SaveAs(dlg.FileName);
        return true;
    }

    private static int ToInt(object? v)
    {
        if (v == null || v == DBNull.Value) return 0;
        return int.TryParse(v.ToString(), out int n) ? n : 0;
    }
}
