using System.Windows.Controls;
using System.Windows.Media;
using SistemaTanqueReactor.Models;

namespace SistemaTanqueReactor.Views;

public partial class LotesView : UserControl
{
    private static readonly SolidColorBrush DespachadoBg = new(Color.FromRgb(0xD1, 0xFA, 0xE5)); // verde claro
    private static readonly SolidColorBrush DespachadoAlt = new(Color.FromRgb(0xA7, 0xF3, 0xD0));
    private static readonly SolidColorBrush NormalBg = Brushes.White;
    private static readonly SolidColorBrush NormalAlt = new(Color.FromRgb(0xF8, 0xFA, 0xFC));

    public LotesView()
    {
        InitializeComponent();
    }

    private void DgLotes_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is LoteTorta lote)
        {
            if (lote.Despachado)
            {
                e.Row.Background = DespachadoBg;
                e.Row.Foreground = new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46));
            }
            else
            {
                e.Row.Background = e.Row.GetIndex() % 2 == 0 ? NormalBg : NormalAlt;
                e.Row.Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
            }
        }
    }
}
