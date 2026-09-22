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
    private const double W_LOTE = 120;
    private const double W_BOLSAS = 120;
    private const double W_STOCK = 72;
    private const double W_TURNO = 48;
    private const double W_FECHA = W_TURNO * 2;
    private const double W_FIJAS = W_LOTE + W_BOLSAS + W_STOCK;
    private const double H = 32;

    private static readonly Thickness B1 = new(1, 1, 1, 1);
    private static readonly Thickness B_TRB = new(0, 1, 1, 1);
    private static readonly Thickness B_RB = new(0, 0, 1, 1);
    private static readonly Thickness B_LTR = new(1, 1, 1, 0);
    private static readonly Thickness B_TR = new(0, 1, 1, 0);

    private static readonly Brush BorderGx = new SolidColorBrush(Color.FromRgb(0x2A, 0x2E, 0x3A));
    private static readonly Brush BgHeader = new SolidColorBrush(Color.FromRgb(0x12, 0x14, 0x1C));
    private static readonly Brush BgCell = new SolidColorBrush(Color.FromRgb(0x1A, 0x1D, 0x27));
    private static readonly Brush BgAlt = new SolidColorBrush(Color.FromRgb(0x14, 0x16, 0x1E));
    private static readonly Brush BgHover = new SolidColorBrush(Color.FromRgb(0x12, 0x25, 0x2C));
    private static readonly Brush TextGx = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
    private static readonly Brush CyanGx = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
    private static readonly Brush MagentaGx = new SolidColorBrush(Color.FromRgb(0xFF, 0x2D, 0x95));
    private static readonly Brush MutedGx = new SolidColorBrush(Color.FromRgb(0x8B, 0x92, 0xA5));

    private bool _syncingScroll;

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

    private void ScrollDias_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_syncingScroll) return;
        _syncingScroll = true;
        ScrollFijas.ScrollToVerticalOffset(e.VerticalOffset);
        _syncingScroll = false;
    }

    private void Dibujar(HistorialMensualViewModel vm)
    {
        PlanillaFijas.Children.Clear();
        PlanillaDias.Children.Clear();
        if (vm.TablaHistorial == null || vm.DiasEnMes <= 0) return;

        int dias = vm.DiasEnMes;
        double wDias = dias * W_FECHA;

        // === Headers (3 filas) ===
        // Fila 1: vacío fijo | mes sobre días
        PlanillaFijas.Children.Add(Caja("", W_FIJAS, H, B_LTR, FontWeights.Normal, 11, BgHeader, TextGx));
        PlanillaDias.Children.Add(Caja(vm.TituloMes, wDias, H, B_TR, FontWeights.Bold, 13, BgHeader, CyanGx));

        // Fila 2: vacío fijo | fechas
        PlanillaFijas.Children.Add(Caja("", W_FIJAS, H, new Thickness(1, 0, 0, 0), FontWeights.Normal, 10, BgHeader, TextGx));
        var filaFechas = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        for (int d = 1; d <= dias; d++)
        {
            string txt = d - 1 < vm.FechasEncabezado.Count
                ? vm.FechasEncabezado[d - 1]
                : $"{d:D2}/{vm.Mes:D2}/{vm.Anio}";
            filaFechas.Children.Add(Caja(txt, W_FECHA, H, B_TR, FontWeights.SemiBold, 10, BgHeader, TextGx));
        }
        PlanillaDias.Children.Add(filaFechas);

        // Fila 3: headers fijos | I II
        var filaHeadFija = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        filaHeadFija.Children.Add(Caja("Nº DE LOTE", W_LOTE, H, B1, FontWeights.SemiBold, 10, BgHeader, CyanGx));
        filaHeadFija.Children.Add(Caja("CANTIDAD_BOLSAS", W_BOLSAS, H, B_TRB, FontWeights.SemiBold, 9, BgHeader, CyanGx));
        filaHeadFija.Children.Add(Caja("STOCK", W_STOCK, H, B_TRB, FontWeights.SemiBold, 10, BgHeader, CyanGx));
        PlanillaFijas.Children.Add(filaHeadFija);

        var filaHeadDias = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
        for (int d = 1; d <= dias; d++)
        {
            filaHeadDias.Children.Add(Caja("I", W_TURNO, H, B_TRB, FontWeights.Bold, 12, BgHeader, MagentaGx));
            filaHeadDias.Children.Add(Caja("II", W_TURNO, H, B_TRB, FontWeights.Bold, 12, BgHeader, CyanGx));
        }
        PlanillaDias.Children.Add(filaHeadDias);

        // === Datos ===
        int i = 0;
        foreach (DataRow row in vm.TablaHistorial.Rows)
        {
            var bg = (i % 2 == 0) ? BgCell : BgAlt;

            var filaFija = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
            filaFija.Children.Add(CajaDato(row[HistorialMensualViewModel.ColLote]?.ToString() ?? "", W_LOTE, bg, new Thickness(1, 0, 1, 1)));
            filaFija.Children.Add(CajaDato(row[HistorialMensualViewModel.ColBolsas]?.ToString() ?? "", W_BOLSAS, bg, B_RB));
            filaFija.Children.Add(CajaDato(row[HistorialMensualViewModel.ColStock]?.ToString() ?? "", W_STOCK, bg, B_RB));
            PlanillaFijas.Children.Add(filaFija);

            var filaDias = new StackPanel { Orientation = Orientation.Horizontal, Height = H };
            for (int d = 1; d <= dias; d++)
            {
                string vI = row[HistorialMensualViewModel.ColDia(d, "I")]?.ToString() ?? "";
                string vII = row[HistorialMensualViewModel.ColDia(d, "II")]?.ToString() ?? "";
                filaDias.Children.Add(CajaTurno(vI, bg, row, d, "I"));
                filaDias.Children.Add(CajaTurno(vII, bg, row, d, "II"));
            }
            PlanillaDias.Children.Add(filaDias);
            i++;
        }
    }

    private static Border Caja(string texto, double w, double h, Thickness border, FontWeight weight, double size, Brush bg, Brush fg)
    {
        return new Border
        {
            Width = w,
            Height = h,
            BorderBrush = BorderGx,
            BorderThickness = border,
            Background = bg,
            Child = new TextBlock
            {
                Text = texto,
                FontSize = size,
                FontWeight = weight,
                Foreground = fg,
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
            BorderBrush = BorderGx,
            BorderThickness = border,
            Background = bg,
            Child = new TextBlock
            {
                Text = texto,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextGx,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private Border CajaTurno(string valor, Brush bg, DataRow row, int dia, string turno)
    {
        bool has = !string.IsNullOrEmpty(valor);
        var b = new Border
        {
            Width = W_TURNO,
            Height = H,
            BorderBrush = BorderGx,
            BorderThickness = B_RB,
            Background = bg,
            Cursor = Cursors.Hand,
            ToolTip = has
                ? $"Turno {turno}: {valor} bolsas · Doble clic para ver/editar"
                : $"Turno {turno} vacío · Doble clic para cargar cantidad",
            Child = new TextBlock
            {
                Text = valor,
                FontSize = 12,
                FontWeight = has ? FontWeights.Bold : FontWeights.Normal,
                Foreground = has ? (turno == "I" ? MagentaGx : CyanGx) : MutedGx,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            Tag = (row, dia, turno)
        };

        b.MouseEnter += (_, _) => b.Background = BgHover;
        b.MouseLeave += (_, _) => b.Background = bg;

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
