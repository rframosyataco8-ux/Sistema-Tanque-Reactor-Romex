using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class HistorialMensualView : UserControl
{
    public HistorialMensualView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Ajusta anchos y formato de columnas al generarse (planilla ordenada).
    /// </summary>
    private void DgHistorial_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        string header = e.Column.Header?.ToString() ?? "";

        // Columnas fijas (izquierda)
        if (header == "Nº de lote")
        {
            e.Column.Width = new DataGridLength(120);
            e.Column.MinWidth = 100;
        }
        else if (header == "cantidad_bolsas")
        {
            e.Column.Width = new DataGridLength(110);
            e.Column.MinWidth = 90;
        }
        else if (header == "stok")
        {
            e.Column.Width = new DataGridLength(70);
            e.Column.MinWidth = 55;
        }
        else
        {
            // Columnas de día: "20/09 I" / "20/09 II" → angostas y centradas
            e.Column.Width = new DataGridLength(58);
            e.Column.MinWidth = 50;
        }

        // Texto centrado en celdas
        if (e.Column is DataGridTextColumn textCol)
        {
            textCol.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center),
                    new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center)
                }
            };
        }
    }

    private async void DgHistorial_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not HistorialMensualViewModel vm) return;
        if (DgHistorial.CurrentCell.Item is not System.Data.DataRowView row) return;

        var col = DgHistorial.CurrentCell.Column;
        if (col == null) return;

        string header = col.Header?.ToString() ?? "";

        // Headers: "20/09 I", "20/09 II", "01/09 I", etc.
        // Formato: "dd/MM I" o "dd/MM II"
        var parts = header.Split(' ');
        if (parts.Length != 2) return;

        string turno = parts[1]; // "I" o "II"
        if (turno != "I" && turno != "II") return;

        // Extraer día de "20/09"
        var fechaParts = parts[0].Split('/');
        if (fechaParts.Length < 1) return;
        if (!int.TryParse(fechaParts[0], out int dia)) return;

        string numeroLote = row["Nº de lote"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(numeroLote)) return;

        await vm.MostrarDetalleAsync(numeroLote, dia, turno);
    }
}
