using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class HistorialMensualView : UserControl
{
    private static readonly SolidColorBrush HeaderFijoBg = BrushFrom("#E2E8F0");
    private static readonly SolidColorBrush HeaderDiaBg = BrushFrom("#F1F5F9");
    private static readonly SolidColorBrush HeaderIBg = BrushFrom("#CCFBF1");   // teal claro
    private static readonly SolidColorBrush HeaderIIBg = BrushFrom("#E0E7FF");  // índigo claro
    private static readonly SolidColorBrush BorderBrushColor = BrushFrom("#CBD5E1");
    private static readonly SolidColorBrush TextDark = BrushFrom("#1E293B");
    private static readonly SolidColorBrush TextTeal = BrushFrom("#0F766E");
    private static readonly SolidColorBrush TextIndigo = BrushFrom("#4338CA");

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

    /// <summary>
    /// Construye columnas con encabezado de 2 líneas: fecha arriba, I/II abajo.
    /// Igual espíritu que la planilla Excel.
    /// </summary>
    private void ReconstruirColumnas(HistorialMensualViewModel vm)
    {
        DgHistorial.Columns.Clear();

        // —— Columnas fijas ——
        DgHistorial.Columns.Add(CrearColumnaFija(HistorialMensualViewModel.ColLote, "Nº de lote", 118));
        DgHistorial.Columns.Add(CrearColumnaFija(HistorialMensualViewModel.ColBolsas, "cantidad_bolsas", 108));
        DgHistorial.Columns.Add(CrearColumnaFija(HistorialMensualViewModel.ColStok, "stok", 64));

        // —— Un par I / II por cada día del mes ——
        int dias = vm.DiasEnMes > 0 ? vm.DiasEnMes : DateTime.DaysInMonth(vm.Anio, vm.Mes);
        for (int d = 1; d <= dias; d++)
        {
            string etiquetaFecha = $"{d:D2}/{vm.Mes:D2}";

            DgHistorial.Columns.Add(CrearColumnaDia(
                HistorialMensualViewModel.ColDia(d, "I"),
                etiquetaFecha, "I", esTurnoI: true, dia: d));

            DgHistorial.Columns.Add(CrearColumnaDia(
                HistorialMensualViewModel.ColDia(d, "II"),
                etiquetaFecha, "II", esTurnoI: false, dia: d));
        }
    }

    private DataGridTextColumn CrearColumnaFija(string bindingPath, string titulo, double width)
    {
        var col = new DataGridTextColumn
        {
            Header = CrearHeaderFijo(titulo),
            Binding = new Binding(bindingPath),
            Width = new DataGridLength(width),
            MinWidth = width - 10,
            CanUserSort = false,
            ElementStyle = EstiloCeldaCentrada()
        };
        return col;
    }

    private DataGridTextColumn CrearColumnaDia(string bindingPath, string fecha, string turno, bool esTurnoI, int dia)
    {
        var col = new DataGridTextColumn
        {
            Header = CrearHeaderDia(fecha, turno, esTurnoI),
            Binding = new Binding(bindingPath),
            Width = new DataGridLength(52),
            MinWidth = 46,
            CanUserSort = false,
            ElementStyle = EstiloCeldaCentrada()
        };
        // Guardamos día y turno en Tag para el doble clic
        col.HeaderStyle = CrearHeaderStyle(esTurnoI);
        return col;
    }

    private static FrameworkElement CrearHeaderFijo(string titulo)
    {
        return new TextBlock
        {
            Text = titulo,
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            Foreground = TextDark,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 2, 4, 2)
        };
    }

    /// <summary>Encabezado de 2 líneas: fecha + turno (como planilla Excel).</summary>
    private static FrameworkElement CrearHeaderDia(string fecha, string turno, bool esTurnoI)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Children.Add(new TextBlock
        {
            Text = fecha,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 1)
        });

        stack.Children.Add(new TextBlock
        {
            Text = turno,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = esTurnoI ? TextTeal : TextIndigo,
            TextAlignment = TextAlignment.Center
        });

        return stack;
    }

    private static Style CrearHeaderStyle(bool esTurnoI)
    {
        var style = new Style(typeof(DataGridColumnHeader));
        style.Setters.Add(new Setter(Control.BackgroundProperty, esTurnoI ? HeaderIBg : HeaderIIBg));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, BorderBrushColor));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2, 4, 2, 4)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        return style;
    }

    private static Style EstiloCeldaCentrada()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
        return style;
    }

    private static SolidColorBrush BrushFrom(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        brush.Freeze();
        return brush;
    }

    private async void DgHistorial_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not HistorialMensualViewModel vm) return;
        if (DgHistorial.CurrentCell.Item is not System.Data.DataRowView row) return;

        var col = DgHistorial.CurrentCell.Column as DataGridTextColumn;
        if (col?.Binding is not Binding binding) return;

        string path = binding.Path?.Path ?? "";
        // Formato interno: D20_I  /  D20_II
        if (!path.StartsWith("D") || !path.Contains('_')) return;

        var parts = path.Split('_');
        if (parts.Length != 2) return;

        if (!int.TryParse(parts[0].TrimStart('D'), out int dia)) return;
        string turno = parts[1];
        if (turno != "I" && turno != "II") return;

        string numeroLote = row[HistorialMensualViewModel.ColLote]?.ToString() ?? "";
        if (string.IsNullOrEmpty(numeroLote)) return;

        await vm.MostrarDetalleAsync(numeroLote, dia, turno);
    }
}
