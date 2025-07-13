using System;
using System.Windows;
using SimulationCore;

namespace SimulationUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OfflineControllerSim _sim = new OfflineControllerSim();

        public MainWindow()
        {
            InitializeComponent();
            _sim.StateChanged += msg => Dispatcher.Invoke(() => txtStatus.Text = msg);
            _sim.DataReceived += data => Dispatcher.Invoke(() => txtResponse.Text = BitConverter.ToString(data));
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            _sim.Start();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            _sim.Stop();
        }

        private void btnPresentCard_Click(object sender, RoutedEventArgs e)
        {
            ulong uid = 12345678;
            byte[] cmd = new byte[9];
            cmd[0] = (byte)'L';
            Array.Copy(BitConverter.GetBytes(uid), 0, cmd, 1, 8);
            _sim.SendCommand(cmd);
        }

        private void btnGetOutputs_Click(object sender, RoutedEventArgs e)
        {
            _sim.SendCommand(new byte[] { (byte)'Z' });
        }

        private void btnSetOutputs_Click(object sender, RoutedEventArgs e)
        {
            byte value = chkOutput.IsChecked == true ? (byte)1 : (byte)0;
            _sim.SendCommand(new byte[] { (byte)'A', value });
        }
    }
}
