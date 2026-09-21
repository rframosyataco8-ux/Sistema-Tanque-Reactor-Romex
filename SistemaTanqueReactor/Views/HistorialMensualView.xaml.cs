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

    private async void DgHistorial_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not HistorialMensualViewModel vm) return;
        if (DgHistorial.CurrentCell.Item is not System.Data.DataRowView row) return;

        var col = DgHistorial.CurrentCell.Column;
        if (col == null) return;

        string header = col.Header?.ToString() ?? "";
        // Headers de días: "01 I", "01 II", "18 I", etc.
        if (header.Length < 4) return;

        var parts = header.Split(' ');
        if (parts.Length != 2) return;
        if (!int.TryParse(parts[0], out int dia)) return;
        string turno = parts[1]; // I o II

        string numeroLote = row["Nº Lote"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(numeroLote)) return;

        await vm.MostrarDetalleAsync(numeroLote, dia, turno);
    }
}
