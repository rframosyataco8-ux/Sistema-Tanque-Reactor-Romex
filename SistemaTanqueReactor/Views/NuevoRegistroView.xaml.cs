using System.Windows.Controls;
using System.Windows.Input;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class NuevoRegistroView : UserControl
{
    public NuevoRegistroView()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void TxtCantidad_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true;
            if (DataContext is NuevoRegistroViewModel vm && vm.CalcularCantidadCommand.CanExecute(null))
                vm.CalcularCantidadCommand.Execute(null);
        }
    }

    /// <summary>Ctrl+S guarda el registro; Enter en cantidad calcula.</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            e.Handled = true;
            if (DataContext is NuevoRegistroViewModel vm && vm.GuardarCommand.CanExecute(null))
                vm.GuardarCommand.Execute(null);
        }
    }
}
