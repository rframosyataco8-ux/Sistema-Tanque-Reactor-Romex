using System.Windows;
using System.Windows.Input;

namespace SistemaTanqueReactor.Views;

public partial class InputDialog : Window
{
    public string InputText => TxtInput.Text;

    public InputDialog(string prompt, string defaultValue = "")
    {
        InitializeComponent();
        TxtPrompt.Text = prompt;
        TxtInput.Text = defaultValue;
        TxtInput.SelectAll();
        Loaded += (_, _) => TxtInput.Focus();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TxtInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DialogResult = true;
            Close();
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    public static string? Show(Window? owner, string prompt, string defaultValue = "")
    {
        var dlg = new InputDialog(prompt, defaultValue);
        if (owner != null) dlg.Owner = owner;
        return dlg.ShowDialog() == true ? dlg.InputText : null;
    }
}
