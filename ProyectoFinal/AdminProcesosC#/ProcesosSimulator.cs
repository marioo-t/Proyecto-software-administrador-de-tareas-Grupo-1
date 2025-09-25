using Guna.UI2.WinForms;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms.DataVisualization.Charting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using System.ComponentModel;
using System.Threading;

namespace AdminProcesosC_
{
    public partial class ProcesosSimulator : Form
    {
        // =============================================
        // DECLARACIÓN DE COMPONENTES DE INTERFAZ - GABRIEL GUILLEN
        // =============================================
        private Guna2Panel pnlSidebar;
        private Guna2Panel pnlMain;
        private Guna2Button btnActualizar;
        private Guna2Button btnFinalizarProceso;
        private Guna2Button btnNuevaTarea;
        private Guna2Button btnCerrar;
        private Guna2DataGridView dgvProcesos;
        private Guna2HtmlLabel lblTitulo;
        private Guna2HtmlLabel lblUsuario;
        private Guna2HtmlLabel lblSelectedInfo;
        private Guna2TabControl tabControl;
        private TabPage tabProcesos;
        private TabPage tabRendimiento;

        // =============================================
        // GRÁFICAS Y DATOS EN TIEMPO REAL - EDUARDO RODRIGUEZ
        // =============================================
        private Dictionary<string, Chart> miniCharts = new Dictionary<string, Chart>();
        private Dictionary<string, Series> chartSeries = new Dictionary<string, Series>();
        private Dictionary<string, List<float>> chartData = new Dictionary<string, List<float>>();
        private Dictionary<string, Label> cardValueLabels = new Dictionary<string, Label>();

        // =============================================
        // MONITOREO DEL SISTEMA - OPTIMIZADO - EDUARDO RODRIGUEZ
        // =============================================
        private System.Windows.Forms.Timer actualizacionMetricasTimer;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private PerformanceCounter diskCounter;
        private PerformanceCounter networkCounter;
        private PerformanceCounter diskReadCounter;
        private PerformanceCounter diskWriteCounter;
        private PerformanceCounter networkSentCounter;
        private PerformanceCounter networkReceivedCounter;

        // Contadores por proceso - optimizados
        private Dictionary<int, PerformanceCounter> cpuCountersPorProceso = new Dictionary<int, PerformanceCounter>();
        private Dictionary<int, DataGridViewRow> pidRowMap = new Dictionary<int, DataGridViewRow>();

        // Fallback optimizado para CPU
        private Dictionary<int, TimeSpan> lastTotalProcCpu = new Dictionary<int, TimeSpan>();
        private DateTime lastSampleTime = DateTime.UtcNow;
        private Dictionary<int, DateTime> processStartTimes = new Dictionary<int, DateTime>();


        // =============================================
        // DATOS DEL USUARIO Y MÉTRICAS ACTUALES - DYLAN HERNANDEZ
        // =============================================
        private readonly string usuarioActual;
        private float currentCpuUsage = 0;
        private float currentRamUsage = 0;
        private float currentDiskUsage = 0;
        private float currentNetworkUsage = 0;
        private bool isUpdating;

        // =============================================
        // CONSTRUCTOR PRINCIPAL
        // =============================================
        public ProcesosSimulator(string nombreUsuario)
        {
            usuarioActual = nombreUsuario;
            InitializeComponent();
            ConfigurarInterfazPersonalizada();

            // Inicializar contadores de proceso específicos
            Task.Run(async () => await InitializeProcessCounters());
        }

        // =============================================
        // INICIALIZACIÓN MEJORADA DE CONTADORES DE PROCESO - DYLAN HERNANDEZ
        // =============================================
        private async Task InitializeProcessCounters()
        {
            await Task.Delay(2000); // Esperar para que el sistema esté listo

            try
            {
                // Reinicializar contadores con método mejorado
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                // Primera lectura para inicializar
                cpuCounter.NextValue();

                await Task.Delay(1000); // Espera para estabilizar
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inicializando contadores: {ex.Message}");
            }
        }

        // =============================================
        // MÉTODO MEJORADO PARA OBTENER USO DE CPU POR PROCESO - EDUARDO RODRIGUEZ
        // =============================================
        private double GetImprovedProcessCpuUsage(Process process)
        {
            try
            {
                // Método 1: Usar PerformanceCounter si está disponible
                if (cpuCountersPorProceso.TryGetValue(process.Id, out var counter))
                {
                    try
                    {
                        float rawValue = counter.NextValue();
                        // El contador de procesos da el porcentaje total, no necesita dividir por cores
                        return Math.Round(rawValue, 1);
                    }
                    catch
                    {
                        // Fallback al método 2 si el contador falla
                    }
                }

                // Método 2: Cálculo manual más preciso (similar al Administrador de Tareas)
                var currentTime = DateTime.UtcNow;
                var currentCpuTime = process.TotalProcessorTime;

                if (lastTotalProcCpu.ContainsKey(process.Id) && processStartTimes.ContainsKey(process.Id))
                {
                    var previousCpuTime = lastTotalProcCpu[process.Id];
                    var previousTime = processStartTimes[process.Id];

                    var cpuUsedMs = (currentCpuTime - previousCpuTime).TotalMilliseconds;
                    var totalMsPassed = (currentTime - previousTime).TotalMilliseconds;

                    if (totalMsPassed > 0)
                    {
                        // Cálculo más preciso similar al Administrador de Tareas
                        double cpuUsagePercent = (cpuUsedMs / totalMsPassed) * 100.0;
                        return Math.Round(Math.Max(0, Math.Min(100, cpuUsagePercent)), 1);
                    }
                }

                // Actualizar para la próxima lectura
                lastTotalProcCpu[process.Id] = currentCpuTime;
                processStartTimes[process.Id] = currentTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en GetImprovedProcessCpuUsage para PID {process.Id}: {ex.Message}");
            }

            return 0.0;
        }

        // =============================================
        // DISEÑO DE INTERFAZ - GABRIEL GUILLEN
        // =============================================
        private void ConfigurarInterfazPersonalizada()
        {
            this.SuspendLayout();
            this.Text = $"Administrador de Procesos - Sesión: {usuarioActual}";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;

            InitializeMainPanel();
            InitializeTabControl();
            InitializeProcesosTab();
            InitializeRendimientoTab();
            InitializeSidebar();
            InitializeSystemMonitor();

            this.ResumeLayout(false);

            this.Shown += (s, e) =>
            {
                actualizacionMetricasTimer.Start();
                Task.Run(() => UpdateProcessList());
            };
        }

        // =============================================
        // INICIALIZACIÓN DEL PANEL PRINCIPAL - GABRIEL GUILLEN
        // =============================================
        private void InitializeMainPanel()
        {
            pnlMain = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.FromArgb(32, 32, 32)
            };
            this.Controls.Add(pnlMain);
        }

        // =============================================
        // INICIALIZACIÓN DEL CONTROL DE PESTAÑAS - GABRIEL GUILLEN
        // =============================================
        private void InitializeTabControl()
        {
            tabControl = new Guna2TabControl
            {
                Dock = DockStyle.Fill,
                ItemSize = new Size(120, 40),
                SelectedIndex = 0
            };

            pnlMain.Controls.Add(tabControl);

            tabProcesos = new TabPage("Procesos");
            tabRendimiento = new TabPage("Rendimiento");

            tabControl.TabPages.Add(tabProcesos);
            tabControl.TabPages.Add(tabRendimiento);
        }

        // =============================================
        // INICIALIZACIÓN DE LA PESTAÑA DE PROCESOS - GABRIEL GUILLEN
        // =============================================
        private void InitializeProcesosTab()
        {
            dgvProcesos = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                GridColor = Color.FromArgb(55, 55, 55),
                ColumnHeadersHeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(32, 32, 32),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 120, 215),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5f),
                    Padding = new Padding(3)
                }
            };

            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, dgvProcesos, new object[] { true });

            ConfigureDataGridViewColumns();

            dgvProcesos.SelectionChanged += dgvProcesos_SelectionChanged;
            dgvProcesos.ColumnHeaderMouseClick += dgvProcesos_ColumnHeaderMouseClick;

            tabProcesos.Controls.Add(dgvProcesos);
        }

        // =============================================
        // CONFIGURACIÓN DE COLUMNAS DEL DATAGRIDVIEW - GABRIEL GUILLEN
        // =============================================
        private void ConfigureDataGridViewColumns()
        {
            dgvProcesos.Columns.Clear();

            var columns = new[]
            {
                new { Name = "Nombre", Header = "NOMBRE", Width = 250, Alignment = DataGridViewContentAlignment.MiddleLeft },
                new { Name = "CPU", Header = "CPU", Width = 70, Alignment = DataGridViewContentAlignment.MiddleRight },
                new { Name = "Memoria", Header = "MEMORIA", Width = 90, Alignment = DataGridViewContentAlignment.MiddleRight },
                new { Name = "Disco", Header = "DISCO", Width = 70, Alignment = DataGridViewContentAlignment.MiddleRight },
                new { Name = "Red", Header = "RED", Width = 70, Alignment = DataGridViewContentAlignment.MiddleRight },
                new { Name = "GPU", Header = "GPU", Width = 70, Alignment = DataGridViewContentAlignment.MiddleRight }
            };

            foreach (var col in columns)
            {
                dgvProcesos.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = col.Name,
                    HeaderText = col.Header,
                    Width = col.Width,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = col.Alignment,
                        Padding = new Padding(5, 2, 5, 2)
                    }
                });
            }

            dgvProcesos.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        // =============================================
        // INICIALIZACIÓN DE LA PESTAÑA DE RENDIMIENTO - EDUARDO RODRIGUEZ
        // =============================================
        private void InitializeRendimientoTab()
        {
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 32, 32),
                AutoScroll = true,
                Padding = new Padding(20)
            };

            var lblTitulo = new Label
            {
                Text = "Rendimiento del Sistema",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(lblTitulo);

            var cardsMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };
            mainPanel.Controls.Add(cardsMainContainer);

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoScroll = true,
                ColumnCount = 2,
                RowCount = 0,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(10)
            };

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var metricCards = new[]
            {
                new { Type = "CPU", Title = "CPU", Color = Color.FromArgb(0, 164, 239), Description = "Uso del procesador. Muestra el porcentaje de capacidad utilizada." },
                new { Type = "RAM", Title = "Memoria", Color = Color.FromArgb(255, 185, 0), Description = "Memoria en uso. Porcentaje de memoria física utilizada." },
                new { Type = "Disco", Title = "Disco 0 (C:)", Color = Color.FromArgb(123, 201, 111), Description = "Actividad del disco. Tiempo que el disco está ocupado." },
                new { Type = "Red", Title = "Ethernet", Color = Color.FromArgb(216, 0, 115), Description = "Actividad de red. Velocidad de transferencia de datos." },
                new { Type = "GPU", Title = "GPU 0", Color = Color.FromArgb(230, 115, 0), Description = "Uso de gráficos. Porcentaje de utilización de la GPU." }
            };

            for (int i = 0; i < metricCards.Length; i++)
            {
                var metric = metricCards[i];
                var card = CreateModernMetricCard(
                    metric.Title,
                    "Métricas en tiempo real",
                    "Actualizado cada segundo",
                    metric.Description,
                    metric.Color,
                    metric.Type
                );

                tableLayout.RowCount++;
                tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tableLayout.Controls.Add(card, i % 2, i / 2);
            }

            cardsMainContainer.Controls.Add(tableLayout);
            tabRendimiento.Controls.Add(mainPanel);
        }

        // =============================================
        // CREACIÓN DE TARJETAS DE MÉTRICAS MODERNAS - GABRIEL GUILLEN
        // =============================================
        private Panel CreateModernMetricCard(string titulo, string subtitulo, string descripcion, string detalles, Color color, string chartType)
        {
            var card = new Panel
            {
                Width = 380,
                Height = 280,
                Margin = new Padding(15, 10, 15, 10),
                BackColor = Color.FromArgb(45, 45, 45),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20)
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };
            card.Controls.Add(lblTitulo);

            var lblValor = new Label
            {
                Name = $"lblValor{chartType}",
                Text = "0%",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(20, 45),
                AutoSize = true
            };
            card.Controls.Add(lblValor);
            cardValueLabels[chartType] = lblValor;

            var lblSubtitulo = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(20, 85),
                AutoSize = true
            };
            card.Controls.Add(lblSubtitulo);

            var miniChart = CreateHorizontalMiniChart(chartType, color);
            miniChart.Location = new Point(20, 110);
            miniChart.Size = new Size(340, 80);
            card.Controls.Add(miniChart);
            miniCharts[chartType] = miniChart;

            var lblDetalles = new Label
            {
                Text = detalles,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.LightGray,
                Location = new Point(20, 200),
                Size = new Size(340, 60),
                Padding = new Padding(2)
            };
            card.Controls.Add(lblDetalles);

            return card;
        }

        // =============================================
        // CREACIÓN DE GRÁFICAS HORIZONTALES EN TIEMPO REAL - DYLAN HERNANDEZ
        // =============================================
        private Chart CreateHorizontalMiniChart(string chartType, Color color)
        {
            var chart = new Chart
            {
                Width = 340,
                Height = 80,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            var chartArea = new ChartArea
            {
                Name = $"ChartArea_{chartType}",
                BackColor = Color.FromArgb(40, 40, 40),
                BorderColor = Color.FromArgb(60, 60, 60)
            };

            chartArea.AxisX.Enabled = AxisEnabled.False;
            chartArea.AxisY.Enabled = AxisEnabled.False;
            chartArea.AxisY.Maximum = 100;
            chartArea.AxisY.Minimum = 0;
            chartArea.Position = new ElementPosition(0, 0, 100, 100);
            chartArea.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);

            chart.ChartAreas.Add(chartArea);

            var series = new Series
            {
                Name = $"Series_{chartType}",
                ChartType = SeriesChartType.FastLine,
                Color = color,
                BorderWidth = 3,
                ChartArea = chartArea.Name,
                IsValueShownAsLabel = false
            };

            chart.Series.Add(series);
            chartSeries[chartType] = series;

            chartData[chartType] = new List<float>();
            for (int i = 0; i < 60; i++)
            {
                chartData[chartType].Add(0);
                series.Points.AddY(0);
            }

            return chart;
        }

        // =============================================
        // INICIALIZACIÓN DEL PANEL LATERAL (SIDEBAR) - EDUARDO RODRIGUEZ
        // =============================================
        private void InitializeSidebar()
        {
            pnlSidebar = new Guna2Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                FillColor = Color.FromArgb(45, 45, 45),
                BorderColor = Color.FromArgb(60, 60, 60),
                BorderThickness = 1
            };
            this.Controls.Add(pnlSidebar);

            lblTitulo = new Guna2HtmlLabel
            {
                Text = "<b>ADMINISTRADOR DE PROCESOS</b>",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Padding = new Padding(15, 20, 0, 10),
                Height = 60,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            pnlSidebar.Controls.Add(lblTitulo);

            lblUsuario = new Guna2HtmlLabel
            {
                Text = $"Usuario: {usuarioActual}",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Height = 30,
                Padding = new Padding(15, 5, 0, 0),
                BackColor = Color.Transparent
            };
            pnlSidebar.Controls.Add(lblUsuario);

            lblSelectedInfo = new Guna2HtmlLabel
            {
                Text = "Selecciona un proceso para ver detalles...",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.LightGray,
                Height = 70,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.Transparent
            };
            pnlSidebar.Controls.Add(lblSelectedInfo);

            btnActualizar = new Guna2Button
            {
                Text = "🔄 ACTUALIZAR AHORA",
                Dock = DockStyle.Top,
                Height = 40,
                FillColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(15, 10, 15, 5),
                BorderRadius = 3,
                Cursor = Cursors.Hand
            };
            btnActualizar.Click += (s, e) => Task.Run(() => UpdateProcessList());
            pnlSidebar.Controls.Add(btnActualizar);

            btnNuevaTarea = new Guna2Button
            {
                Text = "➕ NUEVA TAREA",
                Dock = DockStyle.Top,
                Height = 40,
                FillColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(15, 5, 15, 5),
                BorderRadius = 3,
                Cursor = Cursors.Hand
            };
            btnNuevaTarea.Click += btnNuevaTarea_Click;
            pnlSidebar.Controls.Add(btnNuevaTarea);

            btnFinalizarProceso = new Guna2Button
            {
                Text = "⏹️ FINALIZAR TAREA",
                Dock = DockStyle.Top,
                Height = 40,
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(15, 5, 15, 5),
                BorderRadius = 3,
                Cursor = Cursors.Hand
            };
            btnFinalizarProceso.Click += btnFinalizarProceso_Click;
            pnlSidebar.Controls.Add(btnFinalizarProceso);

            var spacer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlSidebar.Controls.Add(spacer);

            btnCerrar = new Guna2Button
            {
                Text = "CERRAR SESIÓN",
                Dock = DockStyle.Bottom,
                Height = 40,
                FillColor = Color.FromArgb(90, 90, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(15, 5, 15, 15),
                BorderRadius = 3,
                Cursor = Cursors.Hand
            };
            btnCerrar.Click += (s, e) => this.Close();
            pnlSidebar.Controls.Add(btnCerrar);
        }

        // =============================================
        // INICIALIZACIÓN DEL MONITOR DEL SISTEMA - DYLAN HERNANDEZ
        // =============================================
        private void InitializeSystemMonitor()
        {
            actualizacionMetricasTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // El tiempo de 1s para cpu
            };
            actualizacionMetricasTimer.Tick += ActualizacionMetricasTimer_Tick;

            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

                var nicNames = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
                string primaryNic = nicNames.FirstOrDefault(n =>
                    !n.ToLower().Contains("loopback") && !n.ToLower().Contains("virtual")) ?? nicNames.FirstOrDefault();

                if (!string.IsNullOrEmpty(primaryNic))
                {
                    networkCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", primaryNic);
                    networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", primaryNic);
                    networkReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", primaryNic);
                }

                // Primera lectura
                if (cpuCounter != null) cpuCounter.NextValue();
                if (ramCounter != null) ramCounter.NextValue();
                if (diskCounter != null) diskCounter.NextValue();
                if (networkCounter != null) networkCounter.NextValue();
                if (diskReadCounter != null) diskReadCounter.NextValue();
                if (diskWriteCounter != null) diskWriteCounter.NextValue();
                if (networkSentCounter != null) networkSentCounter.NextValue();
                if (networkReceivedCounter != null) networkReceivedCounter.NextValue();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inicializando contadores: {ex.Message}");
            }
        }

        // =============================================
        // TIMER PRINCIPAL - ACTUALIZACIÓN DE MÉTRICAS - EDUARDO RODRIGUEZ
        // =============================================

        private void ActualizacionMetricasTimer_Tick(object sender, EventArgs e)
        {
            // Verificar si el timer todavía existe y el formulario está activo
            if (actualizacionMetricasTimer == null || this.IsDisposed || !this.Visible)
                return;

            // Prevenir superposición de ejecuciones
            if (isUpdating)
                return;

            isUpdating = true;

            try
            {
                // Verificar nuevamente antes de ejecutar
                if (actualizacionMetricasTimer == null || this.IsDisposed)
                    return;

                // Ejecutar las actualizaciones
                UpdateSystemMetrics();
                UpdateChartsRealTime();

                // Actualizar procesos en segundo plano
                Task.Run(async () => await UpdateProcessCpuUsageSafeAsync());
            }
            catch (ObjectDisposedException)
            {
                // Ignorar si el formulario se está cerrando
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en timer: {ex.Message}");
            }
            finally
            {
                isUpdating = false;
            }
        }
        private async Task UpdateProcessCpuUsageSafeAsync()
        {
            // Verificar si el formulario está siendo cerrado
            if (this.IsDisposed || !this.Visible)
                return;

            try
            {
                await Task.Delay(100);

                List<int> pids;
                lock (pidRowMap)
                {
                    pids = new List<int>(pidRowMap.Keys);
                }

                if (pids.Count == 0) return;

                var results = await Task.Run(() =>
                {
                    var dict = new Dictionary<int, double>();
                    foreach (var pid in pids)
                    {
                        // Verificar periódicamente si debemos cancelar
                        if (this.IsDisposed)
                            break;

                        double cpuPercent = 0;
                        try
                        {
                            using (var proc = Process.GetProcessById(pid))
                            {
                                cpuPercent = GetImprovedProcessCpuUsage(proc);
                            }
                        }
                        catch
                        {
                            // Proceso terminado, remover del mapa
                            lock (pidRowMap)
                            {
                                pidRowMap.Remove(pid);
                            }
                        }
                        dict[pid] = cpuPercent;
                    }
                    return dict;
                });

                // Verificar nuevamente antes de actualizar UI
                if (this.IsDisposed || !this.Visible)
                    return;

                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (!this.IsDisposed && this.Visible)
                            UpdateProcessGridSafe(results);
                    }));
                }
                else
                {
                    UpdateProcessGridSafe(results);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en UpdateProcessCpuUsageSafeAsync: {ex.Message}");
            }
        }
        private void UpdateProcessGridSafe(Dictionary<int, double> results)
        {
            try
            {
                // Usar lock para evitar acceso concurrente
                lock (pidRowMap)
                {
                    foreach (var kv in results)
                    {
                        if (pidRowMap.TryGetValue(kv.Key, out var row) && row != null && !row.IsNewRow)
                        {
                            double cpuValue = Math.Round(kv.Value, 1);

                            // Verificar si la fila todavía existe en el DataGridView
                            if (dgvProcesos.Rows.Contains(row))
                            {
                                row.Cells["CPU"].Value = $"{cpuValue:F1}%";
                                row.Cells["CPU"].Tag = cpuValue;
                                ApplyHeatColorToRow(row, kv.Value);
                            }
                            else
                            {
                                // La fila fue eliminada, remover del mapa
                                pidRowMap.Remove(kv.Key);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando grilla segura: {ex.Message}");
            }
        }

        // =============================================
        // DETENER TIMER Y LIMPIAR RECURSOS DE FORMA SEGURA - EDUARDO RODRIGUEZ
        // =============================================
        public void DetenerMonitoreo()
        {
            try
            {
                // Detener el timer primero
                if (actualizacionMetricasTimer != null)
                {
                    actualizacionMetricasTimer.Stop();
                    actualizacionMetricasTimer.Dispose();
                    actualizacionMetricasTimer = null;
                }

                // Limpiar contadores de performance
                cpuCounter?.Dispose();
                ramCounter?.Dispose();
                diskCounter?.Dispose();
                networkCounter?.Dispose();

                // Limpiar contadores de procesos
                foreach (var counter in cpuCountersPorProceso.Values)
                {
                    counter?.Dispose();
                }
                cpuCountersPorProceso.Clear();

                // Limpiar diccionarios
                pidRowMap.Clear();
                lastTotalProcCpu.Clear();
                processStartTimes.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deteniendo monitoreo: {ex.Message}");
            }
        }

        // =============================================
        // ACTUALIZACIÓN DE MÉTRICAS DEL SISTEMA - EDUARDO RODRIGUEZ
        // =============================================
        private void UpdateSystemMetrics()
        {
            try
            {
                // CPU
                float cpuRaw = 0;
                try { cpuRaw = cpuCounter?.NextValue() ?? 0; } catch { }
                currentCpuUsage = Math.Max(0, Math.Min(100, cpuRaw));

                // RAM
                try { currentRamUsage = ramCounter?.NextValue() ?? 0; } catch { }

                // DISCO
                float diskRaw = 0;
                try { diskRaw = diskCounter?.NextValue() ?? 0; } catch { }
                currentDiskUsage = Math.Max(0, Math.Min(100, diskRaw));

                // RED
                float netRaw = 0;
                try { netRaw = networkCounter?.NextValue() ?? 0; } catch { }
                currentNetworkUsage = netRaw / 1024f / 1024f; // Convertir a MBps

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(UpdateMetricsUI));
                }
                else
                {
                    UpdateMetricsUI();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en métricas del sistema: {ex.Message}");
            }
        }

        // =============================================
        // OBTENCIÓN DE USO REAL DE CPU POR PROCESO - EDUARDO RODRIGUEZ
        // =============================================
        private double GetRealProcessCpuUsage(Process process)
        {
            try
            {
                if (cpuCountersPorProceso.TryGetValue(process.Id, out var counter))
                {
                    return Math.Round(counter.NextValue() / Environment.ProcessorCount, 1);
                }

                var currentTime = process.TotalProcessorTime;
                if (lastTotalProcCpu.ContainsKey(process.Id))
                {
                    var timeDiff = currentTime - lastTotalProcCpu[process.Id];
                    var totalMs = timeDiff.TotalMilliseconds;
                    var intervalMs = (DateTime.UtcNow - lastSampleTime).TotalMilliseconds;

                    if (intervalMs > 0)
                    {
                        return Math.Round((totalMs / intervalMs) * 100.0 / Environment.ProcessorCount, 1);
                    }
                }
                lastTotalProcCpu[process.Id] = currentTime;
            }
            catch { }

            return 0;
        }

        // =============================================
        // ACTUALIZACIÓN DE LA INTERFAZ DE MÉTRICAS - EDUARDO RODRIGUEZ
        // =============================================
        private void UpdateMetricsUI()
        {
            try
            {
                UpdateCardValues();

                if (dgvProcesos.SelectedRows.Count == 0)
                {
                    var computerInfo = new ComputerInfo();
                    double usedMemoryGB = Math.Round((computerInfo.TotalPhysicalMemory - computerInfo.AvailablePhysicalMemory) / (1024.0 * 1024.0 * 1024.0), 1);
                    double totalMemoryGB = Math.Round(computerInfo.TotalPhysicalMemory / (1024.0 * 1024.0 * 1024.0), 1);

                    lblSelectedInfo.Text = $"CPU: {currentCpuUsage:F1}% | RAM: {currentRamUsage:F1}%\n" +
                                          $"Memoria: {usedMemoryGB:F1}/{totalMemoryGB:F1} GB\n" +
                                          $"Disco: {currentDiskUsage:F1}% | Red: {currentNetworkUsage:F2} MBps";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando UI de métricas: {ex.Message}");
            }
        }

        // =============================================
        // ACTUALIZACIÓN DE GRÁFICAS EN TIEMPO REAL - EDUARDO RODRIGUEZ
        // =============================================
        private void UpdateChartsRealTime()
        {
            try
            {
                UpdateChartData("CPU", currentCpuUsage);
                UpdateChartData("RAM", currentRamUsage);
                UpdateChartData("Disco", currentDiskUsage);
                UpdateChartData("Red", Math.Min(currentNetworkUsage * 10f, 100f));

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(UpdateCardValues));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando gráficas: {ex.Message}");
            }
        }

        // =============================================
        // ACTUALIZACIÓN DE DATOS DE GRÁFICAS - EDUARDO RODRIGUEZ
        // =============================================
        private void UpdateChartData(string chartType, float value)
        {
            if (chartData.ContainsKey(chartType) && chartSeries.ContainsKey(chartType))
            {
                chartData[chartType].Add(value);

                if (chartData[chartType].Count > 50)
                {
                    chartData[chartType].RemoveAt(0);
                }

                var series = chartSeries[chartType];
                series.Points.Clear();

                foreach (var dataPoint in chartData[chartType])
                {
                    series.Points.AddY(dataPoint);
                }

                if (miniCharts.ContainsKey(chartType) && this.InvokeRequired)
                {
                    this.Invoke(new Action(() => miniCharts[chartType].Refresh()));
                }
            }
        }

        // =============================================
        // ACTUALIZACIÓN DE VALORES EN TARJETAS - EDUARDO RODRIGUEZ
        // =============================================
        private void UpdateCardValues()
        {
            try
            {
                if (cardValueLabels.ContainsKey("CPU"))
                    cardValueLabels["CPU"].Text = $"{currentCpuUsage:F1}%";

                if (cardValueLabels.ContainsKey("RAM"))
                    cardValueLabels["RAM"].Text = $"{currentRamUsage:F1}%";

                if (cardValueLabels.ContainsKey("Disco"))
                    cardValueLabels["Disco"].Text = $"{currentDiskUsage:F1}%";

                if (cardValueLabels.ContainsKey("Red"))
                    cardValueLabels["Red"].Text = $"{currentNetworkUsage:F2} MBps";

                if (cardValueLabels.ContainsKey("GPU"))
                    cardValueLabels["GPU"].Text = "0.0%";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando valores de tarjetas: {ex.Message}");
            }
        }

        // =============================================
        // ACTUALIZACIÓN MEJORADA DE CPU EN TIEMPO REAL - EDUARDO RODRIGUEZ
        // =============================================
        private async Task UpdateProcessCpuUsageAsync()
        {
            try
            {
                await Task.Delay(500); // Pequeña espera para estabilización

                var pids = pidRowMap.Keys.ToList();
                if (pids.Count == 0) return;

                var results = await Task.Run(() =>
                {
                    var dict = new Dictionary<int, double>();
                    foreach (var pid in pids)
                    {
                        double cpuPercent = 0;
                        try
                        {
                            using (var proc = Process.GetProcessById(pid))
                            {
                                cpuPercent = GetImprovedProcessCpuUsage(proc);
                            }
                        }
                        catch
                        {
                            // Proceso puede haber terminado
                            cpuPercent = 0;
                        }
                        dict[pid] = cpuPercent;
                    }
                    return dict;
                });

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateProcessGrid(results)));
                }
                else
                {
                    UpdateProcessGrid(results);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en UpdateProcessCpuUsageAsync: {ex.Message}");
            }
        }

        // =============================================
        // ACTUALIZACIÓN DE LA GRILLA DE PROCESOS - EDUARDO RODRIGUEZ
        // =============================================
        private void UpdateProcessGrid(Dictionary<int, double> results)
        {
            try
            {
                foreach (var kv in results)
                {
                    if (pidRowMap.TryGetValue(kv.Key, out var row) && row != null && !row.IsNewRow)
                    {
                        double cpuValue = Math.Round(kv.Value, 1);
                        row.Cells["CPU"].Value = $"{cpuValue:F1}%";
                        row.Cells["CPU"].Tag = cpuValue;

                        ApplyHeatColorToRow(row, kv.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando grilla: {ex.Message}");
            }
        }

        // =============================================
        // APLICACIÓN DE COLORES DE CALOR A FILAS
        // =============================================
        private void ApplyHeatColorToRow(DataGridViewRow row, double cpuPercent)
        {
            try
            {
                if (cpuPercent < 1)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
                    row.DefaultCellStyle.ForeColor = Color.LightGray;
                }
                else if (cpuPercent < 10)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(40, 60, 40);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (cpuPercent < 25)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(60, 80, 40);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (cpuPercent < 50)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(100, 80, 20);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (cpuPercent < 75)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(140, 60, 10);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(180, 40, 0);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
            }
            catch
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
                row.DefaultCellStyle.ForeColor = Color.White;
            }
        }

        // =============================================
        // ACTUALIZACIÓN DE LA LISTA DE PROCESOS - MARIO TARACENA
        // =============================================
        private void UpdateProcessList()
        {
            try
            {
                // Limpiar la grilla de forma segura
                if (dgvProcesos.InvokeRequired)
                {
                    dgvProcesos.Invoke(new Action(() =>
                    {
                        dgvProcesos.SuspendLayout();
                        dgvProcesos.Rows.Clear();
                    }));
                }
                else
                {
                    dgvProcesos.SuspendLayout();
                    dgvProcesos.Rows.Clear();
                }

                // Obtener procesos y limpiar diccionarios de forma segura
                lock (pidRowMap)
                {
                    pidRowMap.Clear();
                    cpuCountersPorProceso.Clear();
                    lastTotalProcCpu.Clear();
                    processStartTimes.Clear();
                }

                var procesos = Process.GetProcesses();
                var procesosValidos = new List<Process>();

                // Filtrar procesos válidos de forma segura
                foreach (var proceso in procesos)
                {
                    try
                    {
                        if (!proceso.HasExited && !string.IsNullOrEmpty(proceso.ProcessName))
                        {
                            procesosValidos.Add(proceso);
                        }
                    }
                    catch
                    {
                        // Ignorar procesos inaccesibles
                    }
                }

                // Ordenar por uso de memoria
                var procesosOrdenados = procesosValidos
                    .OrderByDescending(p =>
                    {
                        try { return p.WorkingSet64; }
                        catch { return 0; }
                    })
                    .ThenBy(p => p.ProcessName)
                    .ToList();

                // Agregar procesos a la grilla de forma segura
                foreach (var proceso in procesosOrdenados)
                {
                    try
                    {
                        if (proceso.HasExited) continue;

                        double memoriaMB = Math.Round(proceso.WorkingSet64 / 1024.0 / 1024.0, 1);
                        double cpuReal = GetImprovedProcessCpuUsage(proceso);

                        if (dgvProcesos.InvokeRequired)
                        {
                            dgvProcesos.Invoke(new Action(() =>
                            {
                                AddRealProcessRowSafe(proceso, cpuReal, memoriaMB);
                            }));
                        }
                        else
                        {
                            AddRealProcessRowSafe(proceso, cpuReal, memoriaMB);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error procesando {proceso.ProcessName}: {ex.Message}");
                    }
                }

                // Reanudar layout
                if (dgvProcesos.InvokeRequired)
                {
                    dgvProcesos.Invoke(new Action(() =>
                    {
                        dgvProcesos.ResumeLayout();
                    }));
                }
                else
                {
                    dgvProcesos.ResumeLayout();
                }

                // Actualizar uso de CPU después de poblar la lista
                _ = UpdateProcessCpuUsageSafeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando lista de procesos: {ex.Message}");

                if (dgvProcesos.InvokeRequired)
                {
                    dgvProcesos.Invoke(new Action(() =>
                    {
                        dgvProcesos.ResumeLayout();
                    }));
                }
            }
        }

        // =============================================
        // AGREGAR FILA DE PROCESO - MARIO TARACENA
        // =============================================
        private void AddRealProcessRowSafe(Process proceso, double cpuReal, double memoriaMB)
        {
            try
            {
                int rowIndex = dgvProcesos.Rows.Add(
                    proceso.ProcessName,
                    $"{cpuReal:F1}%",
                    $"{memoriaMB:F1} MB",
                    "0.0%",
                    "0.0 Mbps",
                    "0.0%"
                );

                DataGridViewRow row = dgvProcesos.Rows[rowIndex];
                row.Tag = proceso.Id;

                // Guardar valores numéricos para ordenamiento
                row.Cells["CPU"].Tag = cpuReal;
                row.Cells["Memoria"].Tag = memoriaMB;

                // Usar lock para agregar al diccionario de forma segura
                lock (pidRowMap)
                {
                    pidRowMap[proceso.Id] = row;
                }

                // Inicializar contador para este proceso si no existe
                if (!cpuCountersPorProceso.ContainsKey(proceso.Id))
                {
                    try
                    {
                        var counter = new PerformanceCounter("Process", "% Processor Time", proceso.ProcessName);
                        counter.NextValue(); // Lectura inicial
                        cpuCountersPorProceso[proceso.Id] = counter;
                    }
                    catch
                    {
                        // Si falla, usar método alternativo
                    }
                }

                ApplyHeatColorToRow(row, cpuReal);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error agregando fila segura para {proceso.ProcessName}: {ex.Message}");
            }
        }

        // =============================================
        // APLICACIÓN DE COLORES BASADOS EN MEMORIA - EDUARDO RODRIGUEZ
        // =============================================
        private void ApplyMemoryHeatColor(DataGridViewRow row, double memoriaMB)
        {
            try
            {
                if (memoriaMB < 10)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
                    row.DefaultCellStyle.ForeColor = Color.LightGray;
                }
                else if (memoriaMB < 50)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(40, 60, 40);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (memoriaMB < 100)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(60, 80, 40);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (memoriaMB < 200)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(100, 80, 20);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (memoriaMB < 500)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(140, 60, 10);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(180, 40, 0);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
            }
            catch
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
                row.DefaultCellStyle.ForeColor = Color.White;
            }
        }

        // =============================================
        // EVENTO DE CAMBIO DE SELECCIÓN EN LA GRILLA - EDUARDO RODRIGUEZ
        // =============================================
        private void dgvProcesos_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dgvProcesos.SelectedRows.Count > 0)
                {
                    DataGridViewRow selectedRow = dgvProcesos.SelectedRows[0];
                    lblSelectedInfo.Text =
                        $"Proceso: {selectedRow.Cells["Nombre"].Value}\n" +
                        $"CPU: {selectedRow.Cells["CPU"].Value} | Memoria: {selectedRow.Cells["Memoria"].Value}\n" +
                        $"Disco: {selectedRow.Cells["Disco"].Value} | Red: {selectedRow.Cells["Red"].Value}";
                }
                else
                {
                    var computerInfo = new ComputerInfo();
                    double usedMemoryGB = Math.Round((computerInfo.TotalPhysicalMemory - computerInfo.AvailablePhysicalMemory) / (1024.0 * 1024.0 * 1024.0), 1);
                    double totalMemoryGB = Math.Round(computerInfo.TotalPhysicalMemory / (1024.0 * 1024.0 * 1024.0), 1);

                    lblSelectedInfo.Text = $"CPU: {currentCpuUsage:F1}% | RAM: {currentRamUsage:F1}%\n" +
                                          $"Memoria: {usedMemoryGB:F1}/{totalMemoryGB:F1} GB\n" +
                                          $"Disco: {currentDiskUsage:F1}% | Red: {currentNetworkUsage:F2} MBps";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en selección: {ex.Message}");
            }
        }

        // =============================================
        // EVENTO DE CLICK EN FINALIZAR PROCESO - EDUARDO RODRIGUEZ
        // =============================================
        private void btnFinalizarProceso_Click(object sender, EventArgs e)
        {
            if (dgvProcesos.SelectedRows.Count > 0)
            {
                try
                {
                    DataGridViewRow selectedRow = dgvProcesos.SelectedRows[0];
                    string procesoNombre = selectedRow.Cells["Nombre"].Value.ToString();

                    Process proceso = Process.GetProcesses()
                        .FirstOrDefault(p => p.ProcessName == procesoNombre);

                    if (proceso != null)
                    {
                        DialogResult result = MessageBox.Show(
                            $"¿Finalizar '{proceso.ProcessName}' (PID: {proceso.Id})?",
                            "Confirmar",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.Yes)
                        {
                            proceso.Kill();
                            System.Threading.Thread.Sleep(100);
                            Task.Run(() => UpdateProcessList());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error finalizando proceso: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Selecciona un proceso para finalizar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =============================================
        // EVENTO DE CLICK EN NUEVA TAREA - MARIO TARACENA
        // =============================================
        private void btnNuevaTarea_Click(object sender, EventArgs e)
        {
            try
            {
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "Ejecutar programa:",
                    "Nueva Tarea",
                    "notepad.exe"
                );

                if (!string.IsNullOrWhiteSpace(input))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = input,
                        UseShellExecute = true
                    });

                    System.Threading.Thread.Sleep(500);
                    Task.Run(() => UpdateProcessList());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error ejecutando programa: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =============================================
        // EVENTO DE CLICK EN CABECERA DE COLUMNA - MARIO TARACENA
        // =============================================
        private void dgvProcesos_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                string columnName = dgvProcesos.Columns[e.ColumnIndex].Name;
                SortDataGridView(columnName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error ordenando columna: {ex.Message}");
            }
        }

        // =============================================
        // ORDENAMIENTO DE LA GRILLA - MARIO TARACENA
        // =============================================
        private void SortDataGridView(string columnName)
        {
            try
            {
                if (dgvProcesos.Rows.Count == 0) return;

                bool ascending = true;
                if (dgvProcesos.Tag != null && dgvProcesos.Tag.ToString() == columnName + "_ASC")
                {
                    ascending = false;
                }

                // Crear copia de las filas para evitar modificación durante enumeración
                List<DataGridViewRow> rows;
                lock (pidRowMap)
                {
                    rows = dgvProcesos.Rows.Cast<DataGridViewRow>()
                        .Where(r => !r.IsNewRow)
                        .ToList();
                }

                IOrderedEnumerable<DataGridViewRow> sortedRows;

                if (ascending)
                {
                    sortedRows = rows.OrderBy(r =>
                    {
                        var cell = r.Cells[columnName];
                        return cell.Tag ?? cell.Value;
                    });
                    dgvProcesos.Tag = columnName + "_ASC";
                }
                else
                {
                    sortedRows = rows.OrderByDescending(r =>
                    {
                        var cell = r.Cells[columnName];
                        return cell.Tag ?? cell.Value;
                    });
                    dgvProcesos.Tag = columnName + "_DESC";
                }

                // Limpiar y agregar filas ordenadas
                dgvProcesos.Rows.Clear();
                foreach (var row in sortedRows)
                {
                    dgvProcesos.Rows.Add(row);
                }

                // Reconstruir el pidRowMap después de reordenar
                lock (pidRowMap)
                {
                    pidRowMap.Clear();
                    foreach (DataGridViewRow row in dgvProcesos.Rows)
                    {
                        if (!row.IsNewRow && row.Tag is int pid)
                        {
                            pidRowMap[pid] = row;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en ordenamiento seguro: {ex.Message}");
            }
        }

        // =============================================
        // CIERRE SEGURO DEL FORMULARIO - EDUARDO RODRIGUEZ
        // =============================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DetenerMonitoreo();
            base.OnFormClosing(e);
        }
    }
}