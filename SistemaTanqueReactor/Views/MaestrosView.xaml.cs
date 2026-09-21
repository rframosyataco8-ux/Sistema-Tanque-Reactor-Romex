using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class MaestrosView : UserControl
{
    public MaestrosView()
    {
        InitializeComponent();

        var lotesVm = App.Services.GetRequiredService<LotesViewModel>();
        LotesContent.Content = new LotesView { DataContext = lotesVm };
        _ = lotesVm.CargarAsync();

        var opVm = App.Services.GetRequiredService<OperariosViewModel>();
        OperariosContent.Content = new OperariosView { DataContext = opVm };
        _ = opVm.CargarAsync();
    }
}
