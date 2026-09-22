using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using SistemaTanqueReactor.ViewModels;

namespace SistemaTanqueReactor.Views;

public partial class MainWindow : Window
{
    private bool _sidebarOpen = true;
    private const double SidebarOpenW = 250;
    private const double SidebarClosedW = 68;

    // Win32 para redimensionar con SendMessage
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetService(typeof(MainViewModel));
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await System.Threading.Tasks.Task.Delay(700);
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        anim.Completed += (_, _) => SplashOverlay.Visibility = Visibility.Collapsed;
        SplashOverlay.BeginAnimation(OpacityProperty, anim);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            BtnMax_Click(sender, e);
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); }
            catch { /* estado de ventana no permite drag */ }
        }
    }

    /// <summary>Redimensionamiento manual desde bordes y esquinas.</summary>
    private void Resize_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (WindowState == WindowState.Maximized) return;
        if (sender is not FrameworkElement fe || fe.Tag is not string dir) return;

        int hit = dir switch
        {
            "N" => HTTOP,
            "S" => HTBOTTOM,
            "W" => HTLEFT,
            "E" => HTRIGHT,
            "NW" => HTTOPLEFT,
            "NE" => HTTOPRIGHT,
            "SW" => HTBOTTOMLEFT,
            "SE" => HTBOTTOMRIGHT,
            _ => 0
        };

        if (hit == 0) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        ReleaseCapture();
        SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)hit, IntPtr.Zero);
    }

    /// <summary>Atajos de teclado globales.</summary>
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var vm = DataContext as MainViewModel;

        // F11 = maximizar / restaurar
        if (e.Key == Key.F11)
        {
            BtnMax_Click(sender, e);
            e.Handled = true;
            return;
        }

        // Escape = no maximizado → minimizar no; solo desmaximizar si maximizado
        if (e.Key == Key.Escape && WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            e.Handled = true;
            return;
        }

        // Ctrl + M = menú lateral
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.M)
        {
            BtnMenu_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (vm == null) return;

        // Ctrl + 1..5 = navegación
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            string? page = e.Key switch
            {
                Key.D1 or Key.NumPad1 => "Dashboard",
                Key.D2 or Key.NumPad2 => "NuevaCarga",
                Key.D3 or Key.NumPad3 => "Registro",
                Key.D4 or Key.NumPad4 => "Historial",
                Key.D5 or Key.NumPad5 => "Otros",
                _ => null
            };

            if (page != null && vm.NavigateCommand.CanExecute(page))
            {
                vm.NavigateCommand.Execute(page);
                e.Handled = true;
            }
        }
    }

    private void BtnMenu_Click(object sender, RoutedEventArgs e)
    {
        _sidebarOpen = !_sidebarOpen;
        var target = _sidebarOpen ? SidebarOpenW : SidebarClosedW;

        var anim = new GridLengthAnimation
        {
            From = ColSidebar.Width,
            To = new GridLength(target),
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        ColSidebar.BeginAnimation(ColumnDefinition.WidthProperty, anim);

        var vis = _sidebarOpen ? Visibility.Visible : Visibility.Collapsed;
        TxtLogoTitle.Visibility = vis;
        TxtLogoSub.Visibility = vis;
        LblMenu.Visibility = vis;
        LblOtros.Visibility = vis;
        SepConfig.Visibility = vis;
        FooterBox.Visibility = vis;

        TxtDash.Visibility = vis;
        TxtNuevo.Visibility = vis;
        TxtReg.Visibility = vis;
        TxtHist.Visibility = vis;
        TxtOtros.Visibility = vis;
    }

    private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnMax_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}

public class GridLengthAnimation : AnimationTimeline
{
    public override Type TargetPropertyType => typeof(GridLength);

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }
    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

    public IEasingFunction? EasingFunction { get; set; }

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        double fromVal = From.Value;
        double toVal = To.Value;
        double progress = animationClock.CurrentProgress ?? 0;
        if (EasingFunction != null)
            progress = EasingFunction.Ease(progress);
        return new GridLength(fromVal + (toVal - fromVal) * progress);
    }

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
}
