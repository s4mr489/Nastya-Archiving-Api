using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Nastya_Archiving_project.Middleware
{
    public class PrinterWebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, WebSocket> _connectedPrinters = new();
        private static readonly ConcurrentDictionary<string, string> _printerNames = new();

        public PrinterWebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest || !context.Request.Path.StartsWithSegments("/printer-ws"))
            {
                await _next.Invoke(context);
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = Guid.NewGuid().ToString();

            try
            {
                // Register the new WebSocket connection
                _connectedPrinters.TryAdd(connectionId, webSocket);

                // Handle the WebSocket connection
                await HandleWebSocketConnection(connectionId, webSocket);
            }
            finally
            {
                // Remove the WebSocket connection when it's closed
                _connectedPrinters.TryRemove(connectionId, out _);
                _printerNames.TryRemove(connectionId, out _);
            }
        }

        private async Task HandleWebSocketConnection(string connectionId, WebSocket webSocket)
        {
            var buffer = new byte[4096];
            WebSocketReceiveResult result;

            do
            {
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (!result.CloseStatus.HasValue)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessMessage(connectionId, webSocket, message);
                    }
                }
                catch (WebSocketException)
                {
                    break; // Connection closed unexpectedly
                }
            }
            while (webSocket.State == WebSocketState.Open);

            // Close the WebSocket connection properly
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed by the server",
                    CancellationToken.None);
            }
        }

        private async Task ProcessMessage(string connectionId, WebSocket webSocket, string message)
        {
            try
            {
                var messageObj = JsonSerializer.Deserialize<WebSocketMessage>(message);

                switch (messageObj?.Type)
                {
                    case "register":
                        var printerData = JsonSerializer.Deserialize<PrinterRegistration>(messageObj.Data.ToString());
                        _printerNames.TryAdd(connectionId, printerData?.PrinterName ?? "Unknown Printer");
                        await SendToWebSocket(webSocket, new WebSocketMessage
                        {
                            Type = "registered",
                            Data = new { Id = connectionId, Success = true }
                        });
                        break;

                    case "status":
                        var statusData = JsonSerializer.Deserialize<PrinterStatus>(messageObj.Data.ToString());
                        // Here you could log or notify about printer status
                        Console.WriteLine($"Printer {connectionId} status: {statusData?.Status}");
                        break;

                    default:
                        await SendToWebSocket(webSocket, new WebSocketMessage
                        {
                            Type = "error",
                            Data = "Unknown message type"
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                await SendToWebSocket(webSocket, new WebSocketMessage
                {
                    Type = "error",
                    Data = $"Error processing message: {ex.Message}"
                });
            }
        }

        public static async Task SendPrintJob(string printerId, byte[] documentData, Dictionary<string, string> settings)
        {
            if (_connectedPrinters.TryGetValue(printerId, out var webSocket) &&
                webSocket.State == WebSocketState.Open)
            {
                await SendToWebSocket(webSocket, new WebSocketMessage
                {
                    Type = "print",
                    Data = new PrintJob
                    {
                        DocumentData = Convert.ToBase64String(documentData),
                        Settings = settings
                    }
                });
            }
            else
            {
                throw new InvalidOperationException($"Printer {printerId} is not connected");
            }
        }

        private static async Task SendToWebSocket(WebSocket webSocket, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        public static List<PrinterInfo> GetConnectedPrinters()
        {
            return _connectedPrinters.Select(p => new PrinterInfo
            {
                Id = p.Key,
                Name = _printerNames.TryGetValue(p.Key, out var name) ? name : "Unknown Printer",
                Status = p.Value.State.ToString()
            }).ToList();
        }
    }

    public class WebSocketMessage
    {
        public string Type { get; set; } = "";
        public object? Data { get; set; }
    }

    public class PrinterRegistration
    {
        public string PrinterId { get; set; } = "";
        public string PrinterName { get; set; } = "";
    }

    public class PrinterStatus
    {
        public string Status { get; set; } = "";
        public string? Details { get; set; }
    }

    public class PrintJob
    {
        public string DocumentData { get; set; } = "";
        public Dictionary<string, string> Settings { get; set; } = new();
    }

    public class PrinterInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
