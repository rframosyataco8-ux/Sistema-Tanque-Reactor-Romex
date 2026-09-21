using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class HistorialMensualView : UserControl
{
    // Anchos fijos (como planilla Excel de la imagen)
    private const double W_LOTE = 120;
    private const double W_BOLSAS = 120;
    private const double W_STOK = 70;
    private const double W_TURNO = 48;          // I o II
    private const double W_FECHA = W_TURNO * 2; // fecha = I + II
    private const double W_FIJAS = W_LOTE + W_BOLSAS + W_STOK;
    private const double H = 28;

    private static readonly Thickness B1 = new(1, 1, 1, 1);
    private static readonly Thickness B_TopRightBottom = new(0, 1, 1, 1);
    private static readonly Thickness B_RightBottom = new(0, 0, 1, 1);
    private static readonly Thickness B_LeftTopRight = new(1, 1, 1, 0);
    private static readonly Thickness B_TopRight = new(0, 1, 1, 0);

    public HistorialMensualView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is HistorialMensualViewModel o)
                o.PropertyChanged -= OnVmChanged;
            if (e.NewValue is HistorialMensualViewModel n)
            {
                n.PropertyChanged += OnVmChanged;
                if (n.TablaHistorial != null) Dibujar(n);
            }
        };
    }

    private void OnVmChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistorialMensualViewModel.TablaHistorial)
            && s is HistorialMensualViewModel vm)
            Dibujar(vm);
    }

    private void Dibujar(HistorialMensualViewModel vm)
    {
        PlanillaRoot.Children.Clear();
        if (vm.TablaHistorial == null || vm.DiasEnMes <= 0) return;

        int dias = vm.DiasEnMes;
        double wDias = dias * W_FECHA;

        // ═══════════════════════════════════════════
        // FILA 0 — vacío sobre fijas + mes fusionado
        // ═══════════════════════════════════════════
        var filaMes = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        filaMes.Children.Add(Caja("", W_FIJAS, H, B_LeftTopRight, FontWeights.Normal, 12));
        filaMes.Children.Add(Caja(vm.TituloMes, wDias, H, B_TopRight, FontWeights.Bold, 13));
        PlanillaRoot.Children.Add(filaMes);

        // ═══════════════════════════════════════════
        // FILA 1 — vacío sobre fijas + fechas
        // ═══════════════════════════════════════════
        var filaFechas = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        filaFechas.Children.Add(Caja("", W_FIJAS, H, new Thickness(1, 0, 0, 0), FontWeights.Normal, 11));

        for (int d = 1; d <= dias; d++)
        {
            string txt = d - 1 < vm.FechasEncabezado.Count
                ? vm.FechasEncabezado[d - 1]
                : $"{d:D2}/{vm.Mes:D2}/{vm.Anio}";

            filaFechas.Children.Add(Caja(txt, W_FECHA, H, B_TopRight, FontWeights.SemiBold, 11));
        }
        PlanillaRoot.Children.Add(filaFechas);

        // ═══════════════════════════════════════════
        // FILA 2 — Nº DE LOTE | CANTIDAD_BOLSAS | STOOCK | I | II | I | II ...
        // ═══════════════════════════════════════════
        var filaHead = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        filaHead.Children.Add(Caja("Nº DE LOTE", W_LOTE, H, B1, FontWeights.SemiBold, 11));
        filaHead.Children.Add(Caja("CANTIDAD_BOLSAS", W_BOLSAS, H, B_TopRightBottom, FontWeights.SemiBold, 10));
        filaHead.Children.Add(Caja("STOOCK", W_STOK, H, B_TopRightBottom, FontWeights.SemiBold, 11));

        for (int d = 1; d <= dias; d++)
        {
            filaHead.Children.Add(Caja("I", W_TURNO, H, B_TopRightBottom, FontWeights.Bold, 12));
            filaHead.Children.Add(Caja("II", W_TURNO, H, B_TopRightBottom, FontWeights.Bold, 12));
        }
        PlanillaRoot.Children.Add(filaHead);

        // ═══════════════════════════════════════════
        // FILAS DE DATOS
        // ═══════════════════════════════════════════
        int i = 0;
        foreach (DataRow row in vm.TablaHistorial.Rows)
        {
            var bg = (i % 2 == 0)
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));

            var fila = new StackPanel { Orientation = Orientation.Horizontal, Height = H };

            string lote = row[HistorialMensualViewModel.ColLote]?.ToString() ?? "";
            string bolsas = row[HistorialMensualViewModel.ColBolsas]?.ToString() ?? "";
            string stok = row[HistorialMensualViewModel.ColStok]?.ToString() ?? "";

            fila.Children.Add(CajaDato(lote, W_LOTE, bg, new Thickness(1, 0, 1, 1)));
            fila.Children.Add(CajaDato(bolsas, W_BOLSAS, bg, B_RightBottom));
            fila.Children.Add(CajaDato(stok, W_STOK, bg, B_RightBottom));

            for (int d = 1; d <= dias; d++)
            {
                string vI = row[HistorialMensualViewModel.ColDia(d, "I")]?.ToString() ?? "";
                string vII = row[HistorialMensualViewModel.ColDia(d, "II")]?.ToString() ?? "";

                fila.Children.Add(CajaTurno(vI, bg, row, d, "I"));
                fila.Children.Add(CajaTurno(vII, bg, row, d, "II"));
            }

            PlanillaRoot.Children.Add(fila);
            i++;
        }
    }

    // ——— helpers visuales ———

    private static Border Caja(string texto, double w, double h, Thickness border, FontWeight weight, double size)
    {
        return new Border
        {
            Width = w,
            Height = h,
            BorderBrush = Brushes.Black,
            BorderThickness = border,
            Background = Brushes.White,
            Child = new TextBlock
            {
                Text = texto,
                FontSize = size,
                FontWeight = weight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static Border CajaDato(string texto, double w, Brush bg, Thickness border)
    {
        return new Border
        {
            Width = w,
            Height = H,
            BorderBrush = Brushes.Black,
            BorderThickness = border,
            Background = bg,
            Child = new TextBlock
            {
                Text = texto,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private Border CajaTurno(string valor, Brush bg, DataRow row, int dia, string turno)
    {
        var b = new Border
        {
            Width = W_TURNO,
            Height = H,
            BorderBrush = Brushes.Black,
            BorderThickness = B_RightBottom,
            Background = bg,
            Cursor = string.IsNullOrEmpty(valor) ? Cursors.Arrow : Cursors.Hand,
            Child = new TextBlock
            {
                Text = valor,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            Tag = (row, dia, turno)
        };

        b.MouseLeftButtonDown += async (_, e) =>
        {
            if (e.ClickCount < 2) return;
            if (DataContext is not HistorialMensualViewModel vm) return;
            if (b.Tag is not (DataRow r, int d, string t)) return;
            string lote = r[HistorialMensualViewModel.ColLote]?.ToString() ?? "";
            if (string.IsNullOrEmpty(lote)) return;
            await vm.MostrarDetalleAsync(lote, d, t);
        };

        return b;
    }
}
