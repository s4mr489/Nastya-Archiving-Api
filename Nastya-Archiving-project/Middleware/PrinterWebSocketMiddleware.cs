using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Drawing.Printing;
using System.Management;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;

namespace Nastya_Archiving_project.Middleware
{
    public class PrinterWebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, WebSocket> _connectedPrinters = new();
        private static readonly ConcurrentDictionary<string, string> _printerNames = new();
        private static readonly ConcurrentDictionary<string, ScannerInfo> _detectedScanners = new();
        private static Timer? _scannerDiscoveryTimer;
        private ManagementEventWatcher? _deviceWatcher;
        private static readonly object _scannerLock = new object();

        public PrinterWebSocketMiddleware(RequestDelegate next)
        {
            _next = next;

            // Initialize scanner discovery timer for periodic scanning
            InitializeScannerDiscovery();
            
            // Initialize USB device change monitoring for immediate detection
            InitializeDeviceChangeMonitoring();
        }

        private void InitializeScannerDiscovery()
        {
            // Discover scanners immediately and then every 60 seconds
            _scannerDiscoveryTimer = new Timer(_ => DiscoverScanners(), null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
        }

        private void InitializeDeviceChangeMonitoring()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
            
            try
            {
                // Monitor for device interface changes
                var query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE " +
                                             "TargetInstance ISA 'Win32_PnPEntity' AND " +
                                             "(TargetInstance.PNPClass = 'Image' OR " +
                                             "TargetInstance.PNPClass = 'Camera')");
                
                _deviceWatcher = new ManagementEventWatcher(query);
                _deviceWatcher.EventArrived += DeviceChangeEvent;
                _deviceWatcher.Start();
                
                Console.WriteLine("USB device monitoring started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize device monitoring: {ex.Message}");
            }
        }

        private void DeviceChangeEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var targetInstance = e.NewEvent["TargetInstance"] as ManagementBaseObject;
                string eventType = e.NewEvent.ClassPath.ClassName;
                
                if (targetInstance != null)
                {
                    string deviceId = targetInstance["DeviceID"]?.ToString() ?? "";
                    string name = targetInstance["Caption"]?.ToString() ?? "Unknown Device";
                    string description = targetInstance["Description"]?.ToString() ?? "";
                    
                    // Log the device change
                    Console.WriteLine($"Device change detected: {eventType} - {name} ({deviceId})");
                    
                    // Check if it's a scanner device
                    if (name.Contains("Scanner") || name.Contains("scan") || 
                        description.Contains("Scanner") || description.Contains("scan"))
                    {
                        Console.WriteLine($"Scanner device change detected: {name}");
                        
                        // We need to perform a scanner discovery to update our list
                        Task.Run(() => DiscoverScanners());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling device change event: {ex.Message}");
            }
        }

        // Improve the DiscoverScanners method for USB devices
        private void DiscoverScanners()
        {
            lock (_scannerLock) // Prevent multiple concurrent discoveries
            {
                try
                {
                    Console.WriteLine("Starting scanner discovery...");

                    var scanners = new List<ScannerInfo>();

                    // First, check for USB-connected scanners specifically
                    DiscoverUSBScanners(scanners);

                    // Then try the other methods
                    DiscoverWIAScanners(scanners);
                    DiscoverWMIScanners(scanners);

                    // Update the detected scanners dictionary
                    UpdateDetectedScanners(scanners);

                    // Notify connected clients about available scanners
                    NotifyClientsAboutDetectedScanners();

                    Console.WriteLine($"Scanner discovery completed. Found {_detectedScanners.Count} scanners.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Scanner discovery error: {ex.Message}");
                }
            }
        }

        private void DiscoverUSBScanners(List<ScannerInfo> scanners)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                // USB scanners will typically have a USB\VID_ prefix in their device ID
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_USBControllerDevice"))
                {
                    foreach (var device in searcher.Get())
                    {
                        try
                        {
                            string deviceDependentId = device["Dependent"]?.ToString() ?? "";
                            
                            // Extract the DeviceID from the Dependent string
                            string deviceId = "";
                            var match = System.Text.RegularExpressions.Regex.Match(
                                deviceDependentId, "DeviceID=\"([^\"]*)\"");
                            if (match.Success)
                            {
                                deviceId = match.Groups[1].Value;
                            }
                            
                            if (string.IsNullOrEmpty(deviceId))
                                continue;
                            
                            // Get more details about this USB device
                            using (var deviceSearcher = new ManagementObjectSearcher(
                                $"SELECT * FROM Win32_PnPEntity WHERE DeviceID='{deviceId.Replace("\\", "\\\\")}'"))
                            {
                                foreach (var deviceInfo in deviceSearcher.Get())
                                {
                                    string name = deviceInfo["Caption"]?.ToString() ?? "Unknown Device";
                                    string description = deviceInfo["Description"]?.ToString() ?? "";
                                    string pnpClass = deviceInfo["PNPClass"]?.ToString() ?? "";
                                    
                                    // Check if it's a scanner
                                    if ((pnpClass == "Image" || pnpClass == "Camera") &&
                                        (name.Contains("Scanner") || name.Contains("scan") || 
                                         description.Contains("Scanner") || description.Contains("scan")))
                                    {
                                        Console.WriteLine($"Found USB scanner: {name} ({deviceId})");
                                        
                                        scanners.Add(new ScannerInfo
                                        {
                                            Id = $"usb:{deviceId}",
                                            Name = name,
                                            Type = "USB",
                                            Status = "Available",
                                            Driver = description,
                                            ConnectionType = "USB"
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing USB device: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering USB scanners: {ex.Message}");
            }
        }

        private void DiscoverWIAScanners(List<ScannerInfo> scanners)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                // Use PowerShell to enumerate WIA devices since .NET doesn't have built-in WIA support
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = "-Command \"try { " +
                                    "$wia = New-Object -ComObject WIA.DeviceManager; " +
                                    "$devices = $wia.DeviceInfos | Where-Object { $_.Type -eq 1 }; " + // Type 1 = Scanner
                                    "$devices | ForEach-Object { $_.DeviceID + '|' + $_.Properties('Name').Value }; " +
                                    "} catch { 'Error: ' + $_.Exception.Message }\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"WIA scanner discovery error: {line}");
                        continue;
                    }

                    string[] parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        string deviceId = parts[0];
                        string name = parts[1];

                        scanners.Add(new ScannerInfo
                        {
                            Id = $"wia:{deviceId}",
                            Name = name,
                            Type = "WIA",
                            Status = "Available",
                            Driver = "Windows Image Acquisition"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering WIA scanners: {ex.Message}");
            }
        }

        private void DiscoverWMIScanners(List<ScannerInfo> scanners)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                // Use WMI to find scanner devices
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
                {
                    foreach (var device in searcher.Get())
                    {
                        try
                        {
                            string deviceId = device["DeviceID"]?.ToString() ?? "";
                            string name = device["Caption"]?.ToString() ?? "Unknown Scanner";
                            string description = device["Description"]?.ToString() ?? "";

                            if (name.Contains("Scanner") || description.Contains("Scanner") ||
                                name.Contains("scan") || description.Contains("scan"))
                            {
                                scanners.Add(new ScannerInfo
                                {
                                    Id = $"wmi:{deviceId}",
                                    Name = name,
                                    Type = "WMI",
                                    Status = "Available",
                                    Driver = description
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing WMI scanner: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering WMI scanners: {ex.Message}");
            }
        }

        private void UpdateDetectedScanners(List<ScannerInfo> newScanners)
        {
            // Create a dictionary of new scanners by ID for quick lookup
            var newScannersDict = newScanners.ToDictionary(s => s.Id);

            // Remove scanners that are no longer available
            var scannersToRemove = _detectedScanners.Keys
                .Where(id => !newScannersDict.ContainsKey(id))
                .ToList();

            foreach (var id in scannersToRemove)
            {
                _detectedScanners.TryRemove(id, out _);
                Console.WriteLine($"Scanner no longer available: {id}");
            }

            // Add or update scanners
            foreach (var scanner in newScanners)
            {
                _detectedScanners.AddOrUpdate(scanner.Id, scanner, (_, _) => scanner);
            }
        }

        private void NotifyClientsAboutDetectedScanners()
        {
            var scannerList = _detectedScanners.Values.ToList();

            foreach (var client in _connectedPrinters)
            {
                if (client.Value.State == WebSocketState.Open)
                {
                    try
                    {
                        SendToWebSocket(client.Value, new WebSocketMessage
                        {
                            Type = "detected_scanners",
                            Data = scannerList
                        }).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error notifying client about scanners: {ex.Message}");
                    }
                }
            }
        }

        public static List<ScannerInfo> GetDetectedScanners()
        {
            return _detectedScanners.Values.ToList();
        }

        public static async Task<bool> InitiateScan(string scannerId, ScanSettings settings)
        {
            try
            {
                if (!_detectedScanners.TryGetValue(scannerId, out var scanner))
                {
                    Console.WriteLine($"Scanner not found: {scannerId}");
                    return false;
                }

                Console.WriteLine($"Initiating scan on scanner: {scanner.Name}");

                // For WIA scanners, we can use PowerShell to initiate scanning
                if (scanner.Id.StartsWith("wia:"))
                {
                    string resolution = settings.Resolution?.ToString() ?? "300";
                    string outputFormat = settings.Format?.ToLower() ?? "jpg";
                    string outputPath = settings.OutputPath ?? Path.Combine(Path.GetTempPath(), $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.{outputFormat}");

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "powershell",
                            Arguments = $"-Command \"try {{ " +
                                       $"$deviceId = '{scanner.Id.Substring(4)}'; " +
                                       $"$wia = New-Object -ComObject WIA.DeviceManager; " +
                                       $"$device = $wia.DeviceInfos | Where-Object {{ $_.DeviceID -eq $deviceId }} | ForEach-Object {{ $_.Connect() }}; " +
                                       $"$item = $device.Items[1]; " + // Get the first scanner item
                                       $"$image = $item.Transfer(); " +
                                       $"$image.SaveFile('{outputPath}'); " +
                                       $"Write-Output 'Scan completed: {outputPath}'; " +
                                       $"}} catch {{ Write-Output ('Error: ' + $_.Exception.Message) }}\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (output.Contains("Error:"))
                    {
                        Console.WriteLine($"Error initiating scan: {output}");
                        return false;
                    }

                    Console.WriteLine(output);

                    // If scan was successful, we can notify clients
                    foreach (var client in _connectedPrinters)
                    {
                        if (client.Value.State == WebSocketState.Open)
                        {
                            await SendToWebSocket(client.Value, new WebSocketMessage
                            {
                                Type = "scan_completed",
                                Data = new
                                {
                                    ScannerId = scannerId,
                                    OutputPath = outputPath,
                                    Timestamp = DateTime.Now
                                }
                            });
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initiating scan: {ex.Message}");
                return false;
            }
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

                // Send the list of detected scanners to the new client
                await SendToWebSocket(webSocket, new WebSocketMessage
                {
                    Type = "detected_scanners",
                    Data = _detectedScanners.Values.ToList()
                });

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

                    case "request_scanners":
                        // Client is explicitly requesting the list of scanners
                        await SendToWebSocket(webSocket, new WebSocketMessage
                        {
                            Type = "detected_scanners",
                            Data = _detectedScanners.Values.ToList()
                        });
                        break;

                    case "scan":
                        var scanData = JsonSerializer.Deserialize<ScanRequest>(messageObj.Data.ToString());
                        bool success = await InitiateScan(scanData?.ScannerId ?? "", scanData?.Settings ?? new ScanSettings());

                        await SendToWebSocket(webSocket, new WebSocketMessage
                        {
                            Type = "scan_response",
                            Data = new { Success = success }
                        });
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

        // Clean up resources
        public static void Dispose()
        {
            try
            {
                _scannerDiscoveryTimer?.Dispose();
                
                // Get the instance through reflection to access _deviceWatcher
                var field = typeof(PrinterWebSocketMiddleware).GetField("_deviceWatcher", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                var instances = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(IApplicationBuilder).IsAssignableFrom(p))
                    .SelectMany(t => AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(s => s.GetTypes())
                        .Where(p => t.IsAssignableFrom(p) && !p.IsAbstract))
                    .ToList();
                
                foreach (var instance in instances)
                {
                    var deviceWatcher = field?.GetValue(instance) as ManagementEventWatcher;
                    deviceWatcher?.Stop();
                    deviceWatcher?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing resources: {ex.Message}");
            }
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

    public class ScannerInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = ""; // "WIA", "TWAIN", etc.
        public string Status { get; set; } = "";
        public string? Driver { get; set; }
        public string ConnectionType { get; set; } = "Unknown"; // "USB", "Network", etc.
    }

    public class ScanRequest
    {
        public string? ScannerId { get; set; }
        public ScanSettings? Settings { get; set; }
    }

    public class ScanSettings
    {
        public int? Resolution { get; set; } = 300;
        public string? Format { get; set; } = "JPG";
        public string? OutputPath { get; set; }
        public bool? Color { get; set; } = true;
    }   
}