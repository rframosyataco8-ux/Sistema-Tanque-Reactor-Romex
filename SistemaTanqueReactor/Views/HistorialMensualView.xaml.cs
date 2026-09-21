using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class HistorialMensualView : UserControl
{
    private static readonly SolidColorBrush BorderBlack = BrushFrom("#1E293B");
    private static readonly SolidColorBrush HeaderBg = BrushFrom("#FFFFFF");
    private static readonly SolidColorBrush TextDark = BrushFrom("#0F172A");

    private const double AnchoFijoTotal = 300; // LOTE + BOLSAS + STOOCK
    private const double AnchoLote = 120;
    private const double AnchoBolsas = 110;
    private const double AnchoStok = 70;
    private const double AnchoTurno = 52;     // I o II

    public HistorialMensualView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is HistorialMensualViewModel oldVm)
            oldVm.PropertyChanged -= Vm_PropertyChanged;

        if (e.NewValue is HistorialMensualViewModel newVm)
        {
            newVm.PropertyChanged += Vm_PropertyChanged;
            if (newVm.TablaHistorial != null)
                ReconstruirColumnas(newVm);
        }
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistorialMensualViewModel.TablaHistorial)
            && sender is HistorialMensualViewModel vm
            && vm.TablaHistorial != null)
        {
            ReconstruirColumnas(vm);
        }
    }

    private void ReconstruirColumnas(HistorialMensualViewModel vm)
    {
        DgHistorial.Columns.Clear();

        // Columnas fijas
        DgHistorial.Columns.Add(CrearColFija(HistorialMensualViewModel.ColLote, "Nº DE LOTE", AnchoLote));
        DgHistorial.Columns.Add(CrearColFija(HistorialMensualViewModel.ColBolsas, "CANTIDAD_BOLSAS", AnchoBolsas));
        DgHistorial.Columns.Add(CrearColFija(HistorialMensualViewModel.ColStok, "STOOCK", AnchoStok));

        // Solo I e II debajo de cada fecha (la fecha está en la fila de arriba)
        int dias = vm.DiasEnMes > 0 ? vm.DiasEnMes : DateTime.DaysInMonth(vm.Anio, vm.Mes);
        for (int d = 1; d <= dias; d++)
        {
            DgHistorial.Columns.Add(CrearColTurno(HistorialMensualViewModel.ColDia(d, "I"), "I"));
            DgHistorial.Columns.Add(CrearColTurno(HistorialMensualViewModel.ColDia(d, "II"), "II"));
        }
    }

    private DataGridTextColumn CrearColFija(string path, string titulo, double width)
    {
        return new DataGridTextColumn
        {
            Header = titulo,
            Binding = new Binding(path),
            Width = new DataGridLength(width),
            MinWidth = width,
            CanUserSort = false,
            ElementStyle = CeldaCentrada(),
            HeaderStyle = HeaderStyle()
        };
    }

    private DataGridTextColumn CrearColTurno(string path, string turno)
    {
        return new DataGridTextColumn
        {
            Header = turno,
            Binding = new Binding(path),
            Width = new DataGridLength(AnchoTurno),
            MinWidth = AnchoTurno,
            CanUserSort = false,
            ElementStyle = CeldaCentrada(),
            HeaderStyle = HeaderStyle()
        };
    }

    private static Style HeaderStyle()
    {
        var s = new Style(typeof(DataGridColumnHeader));
        s.Setters.Add(new Setter(Control.BackgroundProperty, HeaderBg));
        s.Setters.Add(new Setter(Control.ForegroundProperty, TextDark));
        s.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        s.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));
        s.Setters.Add(new Setter(Control.BorderBrushProperty, BorderBlack));
        s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
        s.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        s.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2, 2, 2, 2)));
        s.Setters.Add(new Setter(Control.HeightProperty, 28.0));
        return s;
    }

    private static Style CeldaCentrada()
    {
        var s = new Style(typeof(TextBlock));
        s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        s.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
        s.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
        return s;
    }

    private static SolidColorBrush BrushFrom(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        b.Freeze();
        return b;
    }

    /// <summary>Sincroniza el scroll horizontal del encabezado de fechas con el DataGrid.</summary>
    private void DgHistorial_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.HorizontalChange == 0) return;

        // El DataGrid congela 3 columnas (300px). El scroll de fechas solo mueve la parte de días.
        var sv = GetScrollViewer(DgHistorial);
        if (sv == null) return;

        ScrollFechas.ScrollToHorizontalOffset(sv.HorizontalOffset);
    }

    private static ScrollViewer? GetScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = GetScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private async void DgHistorial_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not HistorialMensualViewModel vm) return;
        if (DgHistorial.CurrentCell.Item is not System.Data.DataRowView row) return;

        var col = DgHistorial.CurrentCell.Column as DataGridTextColumn;
        if (col?.Binding is not Binding binding) return;

        string path = binding.Path?.Path ?? "";
        if (!path.StartsWith("D") || !path.Contains('_')) return;

        var parts = path.Split('_');
        if (parts.Length != 2) return;
        if (!int.TryParse(parts[0].TrimStart('D'), out int dia)) return;

        string turno = parts[1];
        if (turno is not ("I" or "II")) return;

        string numeroLote = row[HistorialMensualViewModel.ColLote]?.ToString() ?? "";
        if (string.IsNullOrEmpty(numeroLote)) return;

        await vm.MostrarDetalleAsync(numeroLote, dia, turno);
    }
}
