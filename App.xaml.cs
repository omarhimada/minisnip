using System.Windows;

namespace minisnip {
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			ShutdownMode = ShutdownMode.OnMainWindowClose;
			MainWindow = new MinisnipWindow();
			MainWindow.Show();
		}
	}
}