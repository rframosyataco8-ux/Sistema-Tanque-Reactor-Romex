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
    private const double AnchoTurno = 48;
    private const double AnchoFecha = AnchoTurno * 2; // cada fecha cubre I + II
    private const double AltoFila = 28;

    private static readonly SolidColorBrush Borde = Brushes.Black;
    private static readonly SolidColorBrush Fondo = Brushes.White;
    private static readonly SolidColorBrush FondoAlt = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));

    public HistorialMensualView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is HistorialMensualViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is HistorialMensualViewModel newVm)
            {
                newVm.PropertyChanged += Vm_PropertyChanged;
                if (newVm.TablaHistorial != null)
                    ConstruirPlanilla(newVm);
            }
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistorialMensualViewModel.TablaHistorial)
            && sender is HistorialMensualViewModel vm)
            ConstruirPlanilla(vm);
    }

    private void ConstruirPlanilla(HistorialMensualViewModel vm)
    {
        PanelFechas.Children.Clear();
        PanelDatos.Children.Clear();

        if (vm.TablaHistorial == null) return;

        int dias = vm.DiasEnMes;
        if (dias <= 0) return;

        // —— Fila de fechas (cada una = ancho de I+II) ——
        for (int d = 1; d <= dias; d++)
        {
            string texto = d - 1 < vm.FechasEncabezado.Count
                ? vm.FechasEncabezado[d - 1]
                : $"{d:D2}/{vm.Mes:D2}/{vm.Anio}";

            var celda = new Border
            {
                Width = AnchoFecha,
                Height = AltoFila,
                BorderBrush = Borde,
                BorderThickness = new Thickness(0, 1, 1, 0),
                Background = Fondo,
                Child = new TextBlock
                {
                    Text = texto,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            PanelFechas.Children.Add(celda);
        }

        // Ancho total de la zona de días → el mes se estira igual
        CeldaMes.Width = dias * AnchoFecha;

        // —— Fila de headers I | II ——
        var filaHeaders = new StackPanel { Orientation = Orientation.Horizontal, Height = AltoFila };
        for (int d = 1; d <= dias; d++)
        {
            filaHeaders.Children.Add(CeldaHeaderTurno("I"));
            filaHeaders.Children.Add(CeldaHeaderTurno("II"));
        }
        PanelDatos.Children.Add(filaHeaders);

        // —— Filas de datos ——
        int idx = 0;
        foreach (DataRow row in vm.TablaHistorial.Rows)
        {
            var fila = new StackPanel { Orientation = Orientation.Horizontal, Height = AltoFila };
            var bg = (idx % 2 == 0) ? Fondo : FondoAlt;

            // Las 3 columnas fijas van en el Grid principal, no aquí.
            // Aquí solo van las celdas de turnos. Las fijas se dibujan aparte.

            for (int d = 1; d <= dias; d++)
            {
                string vI = row[HistorialMensualViewModel.ColDia(d, "I")]?.ToString() ?? "";
                string vII = row[HistorialMensualViewModel.ColDia(d, "II")]?.ToString() ?? "";

                fila.Children.Add(CeldaDato(vI, bg, row, d, "I"));
                fila.Children.Add(CeldaDato(vII, bg, row, d, "II"));
            }

            PanelDatos.Children.Add(fila);
            idx++;
        }

        // Dibujar también las columnas fijas de datos al lado izquierdo
        // (reemplazamos el approach: usamos una grilla completa fila a fila)
        RedibujarConFijas(vm);
    }

    /// <summary>
    /// Redibuja toda la planilla fila por fila para mantener bordes alineados
    /// exactamente como en la imagen.
    /// </summary>
    private void RedibujarConFijas(HistorialMensualViewModel vm)
    {
        PanelFechas.Children.Clear();
        PanelDatos.Children.Clear();

        // Quitar headers fijos estáticos del XAML y dibujar todo dinámico en PanelDatos
        // Usamos un enfoque unificado: cada fila de datos incluye las 3 fijas + turnos.

        if (vm.TablaHistorial == null) return;
        int dias = vm.DiasEnMes;

        // Fechas
        for (int d = 1; d <= dias; d++)
        {
            string texto = d - 1 < vm.FechasEncabezado.Count
                ? vm.FechasEncabezado[d - 1]
                : $"{d:D2}/{vm.Mes:D2}/{vm.Anio}";

            PanelFechas.Children.Add(new Border
            {
                Width = AnchoFecha,
                Height = AltoFila,
                BorderBrush = Borde,
                BorderThickness = new Thickness(d == 1 ? 1 : 0, 1, 1, 0),
                Background = Fondo,
                Child = new TextBlock
                {
                    Text = texto,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }
        CeldaMes.Width = Math.Max(dias * AnchoFecha, 100);

        // Header I/II (solo zona días; las fijas ya están en el Grid XAML)
        var headerTurnos = new StackPanel { Orientation = Orientation.Horizontal, Height = AltoFila };
        for (int d = 1; d <= dias; d++)
        {
            headerTurnos.Children.Add(CeldaHeaderTurno("I", d == 1));
            headerTurnos.Children.Add(CeldaHeaderTurno("II", false));
        }
        PanelDatos.Children.Add(headerTurnos);

        // Datos: necesitamos también pintar fijas. Cambiamos layout:
        // PanelDatos ocupará todo el ancho incluyendo fijas.
        // Para eso movemos las fijas aquí también.

        // Ocultar headers fijos del XAML (los redibujamos integrados)
        // Mejor: construir filas completas con fijas + turnos.

        PanelDatos.Children.Clear();

        // Header completo: Nº DE LOTE | CANTIDAD_BOLSAS | STOOCK | I | II | I | II ...
        var headerCompleto = new StackPanel { Orientation = Orientation.Horizontal, Height = AltoFila };
        headerCompleto.Children.Add(CeldaHeaderFijo("Nº DE LOTE", 120, true));
        headerCompleto.Children.Add(CeldaHeaderFijo("CANTIDAD_BOLSAS", 120, false));
        headerCompleto.Children.Add(CeldaHeaderFijo("STOOCK", 70, false));
        for (int d = 1; d <= dias; d++)
        {
            headerCompleto.Children.Add(CeldaHeaderTurno("I", false));
            headerCompleto.Children.Add(CeldaHeaderTurno("II", false));
        }
        PanelDatos.Children.Add(headerCompleto);

        // Filas de datos completas
        int idx = 0;
        foreach (DataRow row in vm.TablaHistorial.Rows)
        {
            var bg = (idx % 2 == 0) ? Fondo : FondoAlt;
            var fila = new StackPanel { Orientation = Orientation.Horizontal, Height = AltoFila };

            string lote = row[HistorialMensualViewModel.ColLote]?.ToString() ?? "";
            string bolsas = row[HistorialMensualViewModel.ColBolsas]?.ToString() ?? "";
            string stok = row[HistorialMensualViewModel.ColStok]?.ToString() ?? "";

            fila.Children.Add(CeldaDatoFijo(lote, 120, bg, true));
            fila.Children.Add(CeldaDatoFijo(bolsas, 120, bg, false));
            fila.Children.Add(CeldaDatoFijo(stok, 70, bg, false));

            for (int d = 1; d <= dias; d++)
            {
                string vI = row[HistorialMensualViewModel.ColDia(d, "I")]?.ToString() ?? "";
                string vII = row[HistorialMensualViewModel.ColDia(d, "II")]?.ToString() ?? "";
                fila.Children.Add(CeldaDato(vI, bg, row, d, "I"));
                fila.Children.Add(CeldaDato(vII, bg, row, d, "II"));
            }

            PanelDatos.Children.Add(fila);
            idx++;
        }

        // Ajustar Grid: las columnas 0-2 del XAML ya no se usan para datos;
        // PanelDatos (col 3) debe empezar desde la izquierda.
        // Simplificamos: ponemos PanelDatos en ColumnSpan completo.
        Grid.SetColumn(PanelDatos, 0);
        Grid.SetColumnSpan(PanelDatos, 4);

        // Ocultar los 3 headers fijos estáticos del XAML (ya van en headerCompleto)
        // Se hace poniendo Visibility en el primer load — los buscamos por posición.
        OcultarHeadersEstaticos();
    }

    private void OcultarHeadersEstaticos()
    {
        // Los 3 Border de headers fijos están en Grid.Row=2, Col 0/1/2
        if (Planilla == null) return;
        foreach (UIElement child in Planilla.Children)
        {
            if (child is Border b && Grid.GetRow(b) == 2 && Grid.GetColumn(b) < 3)
                b.Visibility = Visibility.Collapsed;
        }
        // También el vacío de fila 1 col 0-2 y fila 0 col 0-2 se mantienen
        // para empujar el mes y las fechas a la derecha.
        // Pero ahora PanelDatos cubre todo — las fechas deben alinearse con I/II.

        // Re-alineación: fechas deben empezar después de las 3 fijas (310px)
        // Insertar spacer en PanelFechas al inicio
        if (PanelFechas.Children.Count > 0 &&
            PanelFechas.Children[0] is Border first &&
            first.Tag as string != "spacer")
        {
            var spacer = new Border
            {
                Width = 120 + 120 + 70,
                Height = AltoFila,
                BorderBrush = Borde,
                BorderThickness = new Thickness(1, 1, 0, 0),
                Background = Fondo,
                Tag = "spacer"
            };
            PanelFechas.Children.Insert(0, spacer);

            // Mes también necesita empezar después de fijas
            // Ajustamos el Grid: mes y fechas en column 0 span 4, con padding izquierdo
        }
    }

    private Border CeldaHeaderFijo(string texto, double ancho, bool bordeIzq)
    {
        return new Border
        {
            Width = ancho,
            Height = AltoFila,
            BorderBrush = Borde,
            BorderThickness = new Thickness(bordeIzq ? 1 : 0, 1, 1, 1),
            Background = Fondo,
            Child = new TextBlock
            {
                Text = texto,
                FontSize = texto.Length > 12 ? 9.5 : 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private Border CeldaHeaderTurno(string turno, bool bordeIzq = false)
    {
        return new Border
        {
            Width = AnchoTurno,
            Height = AltoFila,
            BorderBrush = Borde,
            BorderThickness = new Thickness(bordeIzq ? 1 : 0, 1, 1, 1),
            Background = Fondo,
            Child = new TextBlock
            {
                Text = turno,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private Border CeldaDatoFijo(string valor, double ancho, Brush bg, bool bordeIzq)
    {
        return new Border
        {
            Width = ancho,
            Height = AltoFila,
            BorderBrush = Borde,
            BorderThickness = new Thickness(bordeIzq ? 1 : 0, 0, 1, 1),
            Background = bg,
            Child = new TextBlock
            {
                Text = valor,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private Border CeldaDato(string valor, Brush bg, DataRow row, int dia, string turno)
    {
        var border = new Border
        {
            Width = AnchoTurno,
            Height = AltoFila,
            BorderBrush = Borde,
            BorderThickness = new Thickness(0, 0, 1, 1),
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

        border.MouseLeftButtonDown += async (_, e) =>
        {
            if (e.ClickCount < 2) return;
            if (DataContext is not HistorialMensualViewModel vm) return;
            if (border.Tag is not (DataRow r, int d, string t)) return;

            string lote = r[HistorialMensualViewModel.ColLote]?.ToString() ?? "";
            if (string.IsNullOrEmpty(lote)) return;
            await vm.MostrarDetalleAsync(lote, d, t);
        };

        return border;
    }
}
