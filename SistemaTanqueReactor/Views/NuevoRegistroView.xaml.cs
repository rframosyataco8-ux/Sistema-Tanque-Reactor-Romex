using System.Windows.Controls;
using System.Windows.Input;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class NuevoRegistroView : UserControl
{
    public NuevoRegistroView()
    {
        InitializeComponent();
    }

    private void TxtCantidad_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true;
            if (DataContext is NuevoRegistroViewModel vm)
            {
                if (vm.CalcularCantidadCommand.CanExecute(null))
                    vm.CalcularCantidadCommand.Execute(null);
            }
        }
    }
}
