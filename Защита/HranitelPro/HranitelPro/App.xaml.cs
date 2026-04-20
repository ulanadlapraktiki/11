using System.Windows;

namespace HranitelPro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Ошибка: {args.Exception.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}