using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IT3B_Chat.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientWebSocket _clientWebSocket;

        public MainWindow()
        {
            InitializeComponent();
            ConnectButton.Click += ConnectButton_Click;
            DisconnectButton.Click += DisconnectButton_Click;
            SendButton.Click += SendButton_Click;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            _clientWebSocket = new ClientWebSocket();
            try
            {
                await _clientWebSocket.ConnectAsync(new Uri(ServerAddress.Text), CancellationToken.None);
                ServerMessages.Items.Add("Connected to server");

                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                ServerMessages.Items.Add("Error: " + ex.Message);
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientWebSocket != null)
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                ServerMessages.Items.Add("Disconnected from server");
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
            {
                var message = MessageTextBox.Text;
                var buffer = Encoding.UTF8.GetBytes(message);
                await _clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                ServerMessages.Items.Add("Sent: " + message);
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024];
            while (_clientWebSocket.State == WebSocketState.Open)
            {
                var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Dispatcher.Invoke(() => ServerMessages.Items.Add("Received: " + message));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    Dispatcher.Invoke(() => ServerMessages.Items.Add("Server closed connection"));
                }
            }
        }
    }
}