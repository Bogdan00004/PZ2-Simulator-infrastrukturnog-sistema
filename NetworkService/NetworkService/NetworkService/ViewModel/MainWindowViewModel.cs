using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : BindableBase
    {
        // Shared entities — accessible by all ViewModels
        public static ObservableCollection<PressureGauge> Entities { get; private set; } = new ObservableCollection<PressureGauge>();

        // Navigation

        private BindableBase _currentViewModel;
        public BindableBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        private readonly NetworkEntitiesViewModel _networkEntitiesViewModel;
        private readonly MeasurementGraphViewModel _measurementGraphViewModel;

        public NetworkDisplayViewModel NetworkDisplayViewModel { get; private set; }
        public MyICommand<string> NavigateCommand { get; private set; }

        // Status bar

        private string _lastUpdateTime = "—";
        private string _connectionStatus = "Waiting for simulator...";

        public string LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public int EntityCount => Entities.Count;

        // Constructor

        public MainWindowViewModel()
        {
            KillStaleSimulatorProcesses();

            InitializeSampleEntities();

            _networkEntitiesViewModel = new NetworkEntitiesViewModel();
            _measurementGraphViewModel = new MeasurementGraphViewModel();
            NetworkDisplayViewModel = new NetworkDisplayViewModel();

            NavigateCommand = new MyICommand<string>(OnNavigate);
            CurrentViewModel = _networkEntitiesViewModel;

            Entities.CollectionChanged += (s, e) => OnPropertyChanged(nameof(EntityCount));

            InitializeTcpListener();
            StartSimulator();
        }

        // Ensure a clean start — kill leftover simulator
        // instances from a previous session
     
        private static void KillStaleSimulatorProcesses()
        {
            try
            {
                foreach (var process in
                    System.Diagnostics.Process.GetProcessesByName("MeteringSimulator"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Startup Cleanup Error] {ex.Message}");
            }
        }

        // Navigation
        private void OnNavigate(string destination)
        {
            switch (destination)
            {
                case "entities":
                    CurrentViewModel = _networkEntitiesViewModel;
                    break;
                case "graph":
                    CurrentViewModel = _measurementGraphViewModel;
                    break;
            }
        }

        // Pre-created sample entities 
        private void InitializeSampleEntities()
        {
            var cableSensor = PressureGaugeType.PredefinedTypes[0];
            var digitalManometer = PressureGaugeType.PredefinedTypes[1];

            Entities.Add(new PressureGauge
            {
                Id = 1,
                Name = "PG-VALVE-001",
                Type = cableSensor,
                CurrentValue = 8.5
            });
            Entities.Add(new PressureGauge
            {
                Id = 2,
                Name = "PG-VALVE-002",
                Type = digitalManometer,
                CurrentValue = 12.3
            });
            Entities.Add(new PressureGauge
            {
                Id = 3,
                Name = "PG-VALVE-003",
                Type = cableSensor,
                CurrentValue = 6.7
            });
        }

        // TCP Listener
        private void InitializeTcpListener()
        {
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            var listeningThread = new Thread(() =>
            {
                while (true)
                {
                    var tcpClient = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            NetworkStream stream = tcpClient.GetStream();
                            byte[] bytes = new byte[1024];
                            int bytesRead = stream.Read(bytes, 0, bytes.Length);
                            string incoming = System.Text.Encoding.ASCII.GetString(bytes, 0, bytesRead);

                            if (incoming.Equals("Need object count"))
                            {
                                byte[] response = System.Text.Encoding.ASCII.GetBytes(Entities.Count.ToString());
                                stream.Write(response, 0, response.Length);
                            }
                            else
                            {
                                ProcessMeasurement(incoming);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TCP Error] {ex.Message}");
                        }
                    }, null);
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();
        }

        // Process incoming measurement
        // Format: "Entitet_N:value"
        private void ProcessMeasurement(string message)
        {
            try
            {
                var parts = message.Split(':');
                if (parts.Length != 2) return;

                int entityIndex = int.Parse(parts[0].Replace("Entitet_", ""));
                double value = double.Parse(parts[1], CultureInfo.InvariantCulture);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (entityIndex < 0 || entityIndex >= Entities.Count) return;
                    var timestamp = DateTime.Now;
                    Entities[entityIndex].RecordMeasurement(value, timestamp);
                    LastUpdateTime = timestamp.ToString("HH:mm:ss");
                    ConnectionStatus = "Connected";

                    WriteToLog(Entities[entityIndex], value);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Measurement Error] {ex.Message}");
            }
        }

        // Launch the simulator (used on first startup —
        // no existing process to kill at this point)
        
        private static void StartSimulator()
        {
            try
            {
                string simulatorPath = FindSimulatorExecutable();

                if (simulatorPath != null)
                    System.Diagnostics.Process.Start(simulatorPath);
                else
                    Console.WriteLine("[Simulator] MeteringSimulator.exe not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Simulator Start Error] {ex.Message}");
            }
        }

        // Simulator restart — called after add/delete
        public static void RestartSimulator()
        {
            try
            {
                foreach (var process in
                    System.Diagnostics.Process.GetProcessesByName("MeteringSimulator"))
                {
                    process.Kill();
                    process.WaitForExit();
                }

                string simulatorPath = FindSimulatorExecutable();

                if (simulatorPath != null)
                    System.Diagnostics.Process.Start(simulatorPath);
                else
                    Console.WriteLine("[Simulator] MeteringSimulator.exe not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Simulator Restart Error] {ex.Message}");
            }
        }

        private static string FindSimulatorExecutable()
        {
            DirectoryInfo currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            for (int i = 0; i < 8; i++)
            {
                if (currentDir == null) break;

                string[] candidatePaths =
                {
                    Path.Combine(currentDir.FullName, "MeteringSimulator", "MeteringSimulator",
                                 "bin", "Debug", "MeteringSimulator.exe"),
                    Path.Combine(currentDir.FullName, "MeteringSimulator",
                                 "bin", "Debug", "MeteringSimulator.exe"),
                    Path.Combine(currentDir.FullName, "MeteringSimulator.exe")
                };

                foreach (string candidate in candidatePaths)
                {
                    if (File.Exists(candidate))
                        return candidate;
                }

                currentDir = currentDir.Parent;
            }

            return null;
        }

        // Log file writer
        private static void WriteToLog(PressureGauge entity, double value)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.txt");
                string status = entity.IsValueValid ? "VALID" : "OUT OF RANGE";
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                                  $"Entity: {entity.Name} (ID: {entity.Id}) | " +
                                  $"Type: {entity.TypeName} | " +
                                  $"Value: {value:F2} MPa | " +
                                  $"Status: {status}{Environment.NewLine}";

                File.AppendAllText(logPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Log Error] {ex.Message}");
            }
        }
    }
}