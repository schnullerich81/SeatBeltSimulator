using System.IO.Ports;
using System.Windows.Controls;

namespace SeatBeltSimulator
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl
    {
        public SeatBeltSimulatorPlugin Plugin { get; }

        public SettingsControlDemo()
        {
            InitializeComponent();
        }

        public SettingsControlDemo(SeatBeltSimulatorPlugin plugin) : this()
        {
            this.Plugin = plugin;
            UpdateUi();
        }

        public void UpdateUi()
        {
            this.SelectPort.Content = "Anschluss wählen [" + Plugin.Settings.ComPort + "]";
            this.ButtonIn.Content = "Einfahren";
        }

        private void SHButtonPrimary_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.GoIn();
        }

        private void SHButtonSecondary_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.GoOut();
        }

        private void SHButtonPrimary_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {

            string[] ports = SerialPort.GetPortNames();

            ComDialog dlg = new ComDialog();
            dlg.Populate(this, Plugin, ports);

            // Configure the dialog box
            // dlg.Owner = this.w;
            // dlg.Owner = this.Parent;
            //dlg.DocumentMargin = this.documentTextBox.Margin;

            // Open the dialog box modally
            dlg.ShowDialog();

        }
    }
}
