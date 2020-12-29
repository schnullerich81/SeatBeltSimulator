using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SeatBeltSimulator
{
    /// <summary>
    /// Interaction logic for ComDialog.xaml
    /// </summary>
    public partial class ComDialog : Window
    {
        SeatBeltSimulatorPlugin Plugin;
        SettingsControlDemo Ui;

        public ComDialog()
        {
            InitializeComponent();
        }

        public void Populate(SettingsControlDemo ui, SeatBeltSimulatorPlugin plugin, string[] ports)
        {
            this.Plugin = plugin;
            this.Ui = ui;
            List<MyButton> buttons = new List<MyButton>();
            foreach (var port in ports) {
                buttons.Add(new MyButton { ButtonContent = port, ButtonID = port });
            }

            buttons.Add(new MyButton { ButtonContent = "Cancel", ButtonID = "cancel" });

            
            ic.ItemsSource = buttons;
            // ic.AddHandler(Button_Click, );
        }

        private void On_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            
            if (!button.Content.Equals("Cancel"))
            {
                Plugin.SetSerialPort(button.Content.ToString());
                Ui.UpdateUi();
            }
            Close();
        }
    }

    class MyButton
    {
        public string ButtonContent { get; set; }
        public string ButtonID { get; set; }
    }
}
