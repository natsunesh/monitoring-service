using System.Windows;
using GUI_zakupki.Views;

namespace GUI_zakupki;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new MainWindow().Show();
    }
}