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
    private const double W_LOTE = 110;
    private const double W_BOLSAS = 110;
    private const double W_STOK = 64;
    private const double W_TURNO = 44;
    private const double W_FECHA = W_TURNO * 2;
    private const double W_FIJAS = W_LOTE + W_BOLSAS + W_STOK;
    private const double H = 30;

    private static readonly Thickness B1 = new(1, 1, 1, 1);
    private static readonly Thickness B_TRB = new(0, 1, 1, 1);
    private static readonly Thickness B_RB = new(0, 0, 1, 1);
    private static readonly Thickness B_LTR = new(1, 1, 1, 0);
    private static readonly Thickness B_TR = new(0, 1, 1, 0);

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

        // Fila mes
        var filaMes = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        filaMes.Children.Add(Caja("", W_FIJAS, H, B_LTR, FontWeights.Normal, 11));
        filaMes.Children.Add(Caja(vm.TituloMes, wDias, H, B_TR, FontWeights.Bold, 13));
        PlanillaRoot.Children.Add(filaMes);

        // Fila fechas
        var filaFechas = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        filaFechas.Children.Add(Caja("", W_FIJAS, H, new Thickness(1, 0, 0, 0), FontWeights.Normal, 10));
        for (int d = 1; d <= dias; d++)
        {
            string txt = d - 1 < vm.FechasEncabezado.Count
                ? vm.FechasEncabezado[d - 1]
                : $"{d:D2}/{vm.Mes:D2}/{vm.Anio}";
            filaFechas.Children.Add(Caja(txt, W_FECHA, H, B_TR, FontWeights.SemiBold, 10));
        }
        PlanillaRoot.Children.Add(filaFechas);

        // Fila headers: fijas + I | II por día
        var filaHead = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        filaHead.Children.Add(Caja("Nº DE LOTE", W_LOTE, H, B1, FontWeights.SemiBold, 10));
        filaHead.Children.Add(Caja("CANTIDAD_BOLSAS", W_BOLSAS, H, B_TRB, FontWeights.SemiBold, 9));
        filaHead.Children.Add(Caja("STOOCK", W_STOK, H, B_TRB, FontWeights.SemiBold, 10));
        for (int d = 1; d <= dias; d++)
        {
            filaHead.Children.Add(Caja("I", W_TURNO, H, B_TRB, FontWeights.Bold, 12));
            filaHead.Children.Add(Caja("II", W_TURNO, H, B_TRB, FontWeights.Bold, 12));
        }
        PlanillaRoot.Children.Add(filaHead);

        // Datos
        int i = 0;
        foreach (DataRow row in vm.TablaHistorial.Rows)
        {
            var bg = (i % 2 == 0) ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            var fila = new StackPanel { Orientation = Orientation.Horizontal, Height = H };

            fila.Children.Add(CajaDato(row[HistorialMensualViewModel.ColLote]?.ToString() ?? "", W_LOTE, bg, new Thickness(1, 0, 1, 1)));
            fila.Children.Add(CajaDato(row[HistorialMensualViewModel.ColBolsas]?.ToString() ?? "", W_BOLSAS, bg, B_RB));
            fila.Children.Add(CajaDato(row[HistorialMensualViewModel.ColStok]?.ToString() ?? "", W_STOK, bg, B_RB));

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
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap
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
                Foreground = Brushes.Black,
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
            BorderThickness = B_RB,
            Background = bg,
            Cursor = string.IsNullOrEmpty(valor) ? Cursors.Arrow : Cursors.Hand,
            Child = new TextBlock
            {
                Text = valor,
                FontSize = 12,
                FontWeight = string.IsNullOrEmpty(valor) ? FontWeights.Normal : FontWeights.SemiBold,
                Foreground = Brushes.Black,
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
