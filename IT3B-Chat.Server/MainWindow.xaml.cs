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

namespace IT3B_Chat.Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpListener _httpListener;
        private List<WebSocket> _clients = new List<WebSocket>();

        public MainWindow()
        {
            InitializeComponent();
            ConnectButton.Click += ConnectButton_Click;
            DisconnectButton.Click += DisconnectButton_Click;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(ServerAddress.Text);
            _httpListener.Start();

            ClientActions.Items.Add("Server started");

            while (true)
            {
                var listenerContext = await _httpListener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(listenerContext);
                }
                else
                {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(null);
                WebSocket webSocket = webSocketContext.WebSocket;

                _clients.Add(webSocket);
                ClientActions.Items.Add("Client connected");

                while (webSocket.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024];
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Dispatcher.Invoke(() => ClientMessages.Items.Add(message));
                        await BroadcastMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        _clients.Remove(webSocket);
                        ClientActions.Items.Add("Client disconnected");
                    }
                }
            }
            catch (Exception ex)
            {
                ClientActions.Items.Add("Error: " + ex.Message);
            }
        }

        private async Task BroadcastMessage(string message)
        {
            foreach (var client in _clients)
            {
                if (client.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_httpListener != null)
            {
                _httpListener.Stop();
                _httpListener.Close();
                _httpListener = null;
                ClientActions.Items.Add("Server stopped");
            }
        }
    }
}