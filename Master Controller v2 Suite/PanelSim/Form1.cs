using System;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing;

namespace PanelSim
{
    public partial class Form1 : Form
    {
        private TcpClient _tcpClient;
        private NetworkStream _netStream;

        // Designer controls:
        // txtIp: IP address input
        // txtPort: Port input
        // btnConnect: Connect button
        // btnDisconnect: Disconnect button
        // button1: Panel 1 LED
        // button2: Panel 2 LED
        // txtStatus: Status display
        // txtResponse: Response display

        private Timer[] failureTimers = new Timer[2];
        private const int TimeoutMs = 2000;

        public Form1()
        {
            InitializeComponent();

            btnConnect.Click += async (s, e) => await ConnectAsync();
            btnDisconnect.Click += (s, e) => Disconnect();

            failureTimers[0] = new Timer();
            failureTimers[1] = new Timer();
            failureTimers[0].Tick += (s, e) => { AnimatePanelLed(1, "general_system_failure"); failureTimers[0].Stop(); };
            failureTimers[1].Tick += (s, e) => { AnimatePanelLed(2, "general_system_failure"); failureTimers[1].Stop(); };

            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isConnected = _netStream != null;

            txtIp.Enabled = !isConnected;
            txtPort.Enabled = !isConnected;
            btnConnect.Enabled = !isConnected;
            btnDisconnect.Enabled = isConnected;

            button1.Enabled = true;
            button2.Enabled = true;

            txtIp.BackColor = txtIp.Enabled ? SystemColors.Window : SystemColors.Control;
            txtPort.BackColor = txtPort.Enabled ? SystemColors.Window : SystemColors.Control;

            txtStatus.Text = isConnected
                ? $"Connected to {txtIp.Text}:{txtPort.Text}"
                : "Not connected";
        }

        private async Task ConnectAsync()
        {
            try
            {
                txtStatus.Text = "Connecting...";

                if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    txtStatus.Text = "Invalid port number";
                    return;
                }

                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(txtIp.Text.Trim(), port);
                _netStream = _tcpClient.GetStream();
                txtStatus.Text = $"Connected to {txtIp.Text}:{port}";

                // Cancel any pending failure timers
                failureTimers[0].Stop();
                failureTimers[1].Stop();
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Connection failed: " + ex.Message;
                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                }
                _netStream = null;
            }
            finally
            {
                UpdateUI();
            }
        }

        private void Disconnect()
        {
            if (_netStream != null)
            {
                _netStream.Close();
                _netStream = null;
            }
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }
            UpdateUI();
        }

        // Present card to a specific panel (1 or 2)
        private async void PresentCard(int panelIndex)
        {
            if (_netStream == null)
            {
                // Not connected: start timeout for general system failure animation
                failureTimers[panelIndex - 1].Interval = TimeoutMs;
                failureTimers[panelIndex - 1].Start();
                txtStatus.Text = "Waiting for controller...";
                return;
            }

            ulong uid = 12345678 + (ulong)panelIndex;
            byte[] cmd = new byte[9];
            cmd[0] = (byte)'L';
            Array.Copy(BitConverter.GetBytes(uid), 0, cmd, 1, 8);
            await SendCommandAsync(cmd, panelIndex);
        }

        // Send command and animate LED for the correct panel (when connected)
        private async Task SendCommandAsync(byte[] cmd, int panelIndex)
        {
            try
            {
                await _netStream.WriteAsync(cmd, 0, cmd.Length);

                byte[] ack = new byte[3];
                int ackRead = await _netStream.ReadAsync(ack, 0, 3);
                if (ackRead != 3 || ack[0] != 0x01 || ack[1] != 0x01 || ack[2] != 0x9A)
                {
                    txtStatus.Text = "Invalid ACK received";
                    return;
                }

                int respLen = 2;
                byte[] resp = new byte[respLen];
                int respRead = await _netStream.ReadAsync(resp, 0, respLen);

                byte[] crc = new byte[1];
                int crcRead = await _netStream.ReadAsync(crc, 0, 1);

                if (crcRead == 1 && CRC8(resp, respLen) == crc[0])
                {
                    txtResponse.Text = GetResponseDescription(resp);
                    txtStatus.Text = "";
                    AnimatePanelLed(panelIndex, GetResultType(resp));
                }
                else
                {
                    txtStatus.Text = "CRC validation failed";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Communication error: " + ex.Message;
                Disconnect();
            }
        }

        private string GetResultType(byte[] resp)
        {
            if (resp.Length >= 2)
            {
                switch (resp[1])
                {
                    case 68: return "card_declined";
                    case 69: return "card_approved";
                    case 70: return "alt_card_approved";
                    default: return "general_system_failure";
                }
            }
            return "general_system_failure";
        }

        private void AnimatePanelLed(int panelIndex, string animationType)
        {
            Button targetButton = panelIndex == 1 ? button1 : button2;
            Color[] sequence;
            int[] durations;

            switch (animationType)
            {
                case "card_approved":
                    sequence = new[] { Color.Black, Color.Green, Color.Black };
                    durations = new[] { 200, 1000, 200 };
                    break;
                case "alt_card_approved":
                    sequence = new[] { Color.Black, Color.Blue, Color.Black };
                    durations = new[] { 200, 1000, 200 };
                    break;
                case "card_declined":
                    sequence = new[] { Color.Black, Color.Red, Color.Black };
                    durations = new[] { 200, 1000, 200 };
                    break;
                case "general_system_failure":
                    sequence = new[] { Color.OrangeRed, Color.Black, Color.OrangeRed, Color.Black };
                    durations = new[] { 250, 250, 250, 250 };
                    break;
                case "power_on":
                    sequence = new[] { Color.Orange, Color.Yellow, Color.LightGreen, Color.LightBlue, Color.Black };
                    durations = new[] { 250, 250, 250, 250, 250 };
                    break;
                default:
                    sequence = new[] { Color.Black };
                    durations = new[] { 200 };
                    break;
            }

            int step = 0;
            var timer = new Timer();
            timer.Interval = durations[step];
            timer.Tick += (s, e) =>
            {
                targetButton.BackColor = sequence[step];
                step++;
                if (step >= sequence.Length)
                {
                    timer.Stop();
                    timer.Dispose();
                }
                else
                {
                    timer.Interval = durations[step];
                }
            };
            timer.Start();
        }

        public static byte CRC8(byte[] data, int len)
        {
            byte crc = 0x00;
            int data_index_counter = 0;
            while (len > 0)
            {
                len--;
                byte extract = data[data_index_counter];
                data_index_counter++;

                for (byte tempI = 8; tempI > 0; tempI--)
                {
                    byte sum = Convert.ToByte((crc ^ extract) & 1);
                    crc >>= 1;
                    if (sum > 0)
                    {
                        crc ^= 0x8C;
                    }
                    extract >>= 1;
                }
            }
            return crc;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            UpdateUI();
            AnimatePanelLed(1, "power_on");
            AnimatePanelLed(2, "power_on");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PresentCard(1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PresentCard(2);
        }

        private void txtResponse_TextChanged(object sender, EventArgs e)
        {

        }

        private string GetResponseDescription(byte[] resp)
        {
            if (resp.Length < 2)
                return "Invalid response";

            switch (resp[1])
            {
                case 68: // 'D'
                    return "Card Declined";
                case 69: // 'E'
                    return "Card Approved";
                case 70: // 'F'
                    return "Alternate Card Approved";
                default:
                    return $"Unknown response code ({resp[1]})";
            }
        }
    }
}
