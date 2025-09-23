// =======================================================
// Proyecto Final – Administrador de Procesos
// -------------------------------------------------------
// Aportes individuales:
//   - Gabriel Guillén: Diseño de Interfaz con Guna.UI2
//   - Mario Taracena: Funcionalidad "Nueva Tarea"
//   - Eduardo Rodríguez: Lógica de monitoreo y control de procesos
//
// Algoritmo de Planificación Simulado: Round Robin
// Descripción: Cada proceso se atiende en intervalos regulares
// (5 segundos), simulando un cuanto de tiempo fijo compartido.
// =======================================================

using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AdminProcesosC_
{
    public partial class ProcesosSimulator : Form
    {
        // =======================================================
        // Controles de la interfaz (aporte: Gabriel Guillén)
        // =======================================================
        private Guna2Panel pnlSidebar;
        private Guna2Panel pnlMain;
        private Guna2Button btnActualizar;
        private Guna2Button btnFinalizarProceso;
        private Guna2Button btnNuevaTarea;
        private Guna2Button btnCerrar;
        private Guna2DataGridView dgvProcesos;
        private Guna2ProgressBar pbCpuUsage;
        private Guna2TextBox txtProcessInfo;
        private Guna2HtmlLabel lblTitulo;
        private Guna2HtmlLabel lblUsuario;

        // =======================================================
        // Variables de monitoreo del sistema (aporte: Eduardo)
        // =======================================================
        private System.Windows.Forms.Timer actualizacionMetricasTimer;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;

        // Diccionario para llevar uso de CPU por proceso
        private Dictionary<int, PerformanceCounter> cpuCountersPorProceso = new();

        // Usuario actual (logueado desde Login)
        private readonly string usuarioActual;

        public ProcesosSimulator(string nombreUsuario)
        {
            InitializeComponent();
            usuarioActual = nombreUsuario;
            InitializeGunaUI();          // Construcción interfaz
            InitializeSystemMonitor();   // Configuración métricas
        }

        // =======================================================
        // Inicializar Interfaz Gráfica – aporte Gabriel Guillén
        // =======================================================
        private void InitializeGunaUI()
        {
            this.Text = $"Administrador de Procesos - Sesión: {usuarioActual}";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = PaletaColores.AzulPrincipal;
            this.ForeColor = PaletaColores.BlancoCrema;

            // Panel lateral (barra izquierda)
            pnlSidebar = new Guna2Panel { Dock = DockStyle.Left, Width = 200, FillColor = PaletaColores.AzulSecundario };
            this.Controls.Add(pnlSidebar);

            // Panel principal (contenido central)
            pnlMain = new Guna2Panel { Dock = DockStyle.Fill, FillColor = PaletaColores.AzulPrincipal };
            this.Controls.Add(pnlMain);

            // Título
            lblTitulo = new Guna2HtmlLabel
            {
                Text = "Admin de Procesos",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = PaletaColores.DoradoClaro,
                Padding = new Padding(10, 20, 0, 5),
                Height = 60,
                BackColor = PaletaColores.AzulSecundario
            };
            pnlSidebar.Controls.Add(lblTitulo);

            // Usuario actual
            lblUsuario = new Guna2HtmlLabel
            {
                Text = $"Usuario: {usuarioActual}",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.White,
                Height = 25,
                BackColor = Color.Transparent
            };
            pnlSidebar.Controls.Add(lblUsuario);

            // Botón Actualizar lista procesos
            btnActualizar = new Guna2Button
            {
                Text = "Actualizar",
                Dock = DockStyle.Top,
                FillColor = PaletaColores.AzulGris,
                ForeColor = PaletaColores.BlancoCrema,
                Height = 45
            };
            btnActualizar.Click += (s, e) => UpdateProcessList();
            pnlSidebar.Controls.Add(btnActualizar);

            // Botón Nueva Tarea – aporte Mario Taracena
            btnNuevaTarea = new Guna2Button
            {
                Text = "Nueva Tarea",
                Dock = DockStyle.Top,
                FillColor = PaletaColores.DoradoClaro,
                ForeColor = Color.White,
                Height = 45
            };
            btnNuevaTarea.Click += btnNuevaTarea_Click;
            pnlSidebar.Controls.Add(btnNuevaTarea);

            // Botón Finalizar proceso
            btnFinalizarProceso = new Guna2Button
            {
                Text = "Finalizar",
                Dock = DockStyle.Top,
                FillColor = PaletaColores.DoradoPrincipal,
                ForeColor = Color.White,
                Height = 45
            };
            btnFinalizarProceso.Click += btnFinalizarProceso_Click;
            pnlSidebar.Controls.Add(btnFinalizarProceso);

            // Botón Cerrar Sesión
            btnCerrar = new Guna2Button
            {
                Text = "Cerrar Sesión",
                Dock = DockStyle.Bottom,
                FillColor = PaletaColores.DoradoPrincipal,
                ForeColor = Color.White,
                Height = 45
            };
            btnCerrar.Click += (s, e) => this.Close();
            pnlSidebar.Controls.Add(btnCerrar);

            // Tabla de procesos
            dgvProcesos = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = PaletaColores.AzulPrincipal,
                ForeColor = PaletaColores.BlancoCrema,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BorderStyle = BorderStyle.None
            };
            pnlMain.Controls.Add(dgvProcesos);

            // Estilos de la tabla
            dgvProcesos.ThemeStyle.AlternatingRowsStyle.BackColor = PaletaColores.AzulSecundario;
            dgvProcesos.ThemeStyle.HeaderStyle.BackColor = PaletaColores.AzulIntermedio;
            dgvProcesos.ThemeStyle.HeaderStyle.ForeColor = PaletaColores.BlancoCrema;
            dgvProcesos.ThemeStyle.RowsStyle.BackColor = PaletaColores.AzulPrincipal;
            dgvProcesos.ThemeStyle.RowsStyle.ForeColor = PaletaColores.BlancoCrema;
            dgvProcesos.ThemeStyle.GridColor = PaletaColores.AzulGris;
            dgvProcesos.ThemeStyle.ReadOnly = true;
            dgvProcesos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProcesos.SelectionChanged += dgvProcesos_SelectionChanged;

            // Columnas 
            dgvProcesos.ColumnCount = 5;
            dgvProcesos.Columns[0].Name = "Nombre";
            dgvProcesos.Columns[1].Name = "ID";
            dgvProcesos.Columns[2].Name = "Memoria Física (MB)";
            dgvProcesos.Columns[3].Name = "Memoria Virtual (MB)";
            dgvProcesos.Columns[4].Name = "CPU (%)";
            dgvProcesos.Columns.Add("Tipo", "Tipo");

            // Barra de uso CPU
            pbCpuUsage = new Guna2ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 10,
                ProgressColor = PaletaColores.DoradoPrincipal,
                ProgressColor2 = PaletaColores.DoradoClaro
            };
            pnlMain.Controls.Add(pbCpuUsage);

            // Info de procesos
            txtProcessInfo = new Guna2TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                FillColor = PaletaColores.AzulSecundario,
                ForeColor = PaletaColores.BlancoCrema,
                BorderThickness = 0,
                PlaceholderText = "Selecciona un proceso para ver detalles..."
            };
            pnlMain.Controls.Add(txtProcessInfo);

            // Asegurar orden visual
            pnlMain.BringToFront();
            pbCpuUsage.BringToFront();
            txtProcessInfo.BringToFront();
        }

        // =======================================================
        // Inicializar monitoreo del sistema – aporte Eduardo
        // =======================================================
        private void InitializeSystemMonitor()
        {
            // Timer de actualización (Round Robin simulado cada 5s)
            actualizacionMetricasTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // cuanto de tiempo (5s)
            };
            actualizacionMetricasTimer.Tick += actualizacionMetricasTimer_Tick;

            // Contadores globales
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Al cargar ventana -> iniciar monitoreo
            this.Load += (s, e) =>
            {
                actualizacionMetricasTimer.Start();
                UpdateProcessList();
            };
        }

        // =======================================================
        // EVENTOS Y MÉTODOS DE MONITOREO
        // =======================================================
        private void actualizacionMetricasTimer_Tick(object sender, EventArgs e)
        {
            // Simulación Round Robin: atender procesos cada "tick"
            UpdateSystemMetrics();
            UpdateProcessCpuUsage();
        }

        private void UpdateProcessList()
        {
            Process[] allProcesses = Process.GetProcesses().OrderBy(p => p.ProcessName).ToArray();
            var procesosActuales = allProcesses.Select(p => p.Id).ToHashSet();

            // Quitar procesos que ya no existen
            var keysToRemove = cpuCountersPorProceso.Keys.Where(id => !procesosActuales.Contains(id)).ToList();
            foreach (var key in keysToRemove) cpuCountersPorProceso.Remove(key);

            dgvProcesos.Rows.Clear();

            foreach (Process proc in allProcesses)
            {
                try
                {
                    string processType = proc.Responding ? "Aplicación" : "Fondo";

                    // Crear contador CPU por proceso si no existe
                    if (!cpuCountersPorProceso.ContainsKey(proc.Id))
                    {
                        var cpuCounterProceso = new PerformanceCounter("Process", "% Processor Time", proc.ProcessName, true);
                        cpuCounterProceso.NextValue(); // inicializar
                        cpuCountersPorProceso[proc.Id] = cpuCounterProceso;
                    }

                    // Memoria física y virtual
                    double memoriaFisica = proc.WorkingSet64 / 1024.0 / 1024.0;
                    double memoriaVirtual = proc.VirtualMemorySize64 / 1024.0 / 1024.0;

                    // Agregar fila
                    dgvProcesos.Rows.Add(
                        proc.ProcessName,
                        proc.Id,
                        memoriaFisica.ToString("F2"),
                        memoriaVirtual.ToString("F2"),
                        "0.00", // CPU se actualizará luego
                        processType
                    );
                }
                catch { /* procesos protegidos */ }
            }
        }

        private void UpdateSystemMetrics()
        {
            try
            {
                float cpuUsage = cpuCounter.NextValue();
                pbCpuUsage.Value = (int)Math.Min(cpuUsage, 100);
                float ramAvailable = ramCounter.NextValue();

                if (dgvProcesos.SelectedRows.Count == 0)
                {
                    txtProcessInfo.Text =
                        $"Uso de CPU: {cpuUsage:F2}%\r\n" +
                        $"RAM disponible: {ramAvailable:F2} MB\r\n";
                }
            }
            catch { }
        }

        private void UpdateProcessCpuUsage()
        {
            foreach (DataGridViewRow row in dgvProcesos.Rows)
            {
                try
                {
                    int pid = (int)row.Cells["ID"].Value;
                    if (cpuCountersPorProceso.ContainsKey(pid))
                    {
                        float cpu = cpuCountersPorProceso[pid].NextValue() / Environment.ProcessorCount;
                        row.Cells["CPU (%)"].Value = cpu.ToString("F2");
                    }
                }
                catch
                {
                    // Puede fallar si el proceso ya terminó
                }
            }
        }

        private void dgvProcesos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProcesos.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dgvProcesos.SelectedRows[0];
                txtProcessInfo.Text =
                    $"Uso de CPU: {cpuCounter.NextValue():F2}%\r\n" +
                    $"RAM disponible: {ramCounter.NextValue():F2} MB\r\n\r\n" +
                    $"Proceso Seleccionado:\r\n" +
                    $"Nombre: {selectedRow.Cells["Nombre"].Value}\r\n" +
                    $"ID: {selectedRow.Cells["ID"].Value}\r\n" +
                    $"Memoria Física: {selectedRow.Cells["Memoria Física (MB)"].Value} MB\r\n" +
                    $"Memoria Virtual: {selectedRow.Cells["Memoria Virtual (MB)"].Value} MB\r\n" +
                    $"CPU: {selectedRow.Cells["CPU (%)"].Value}%\r\n" +
                    $"Tipo: {selectedRow.Cells["Tipo"].Value}\r\n";
            }
            else
            {
                UpdateSystemMetrics();
            }
        }

        // =======================================================
        // BOTONES
        // =======================================================

        // Finalizar proceso (Kill)
        private void btnFinalizarProceso_Click(object sender, EventArgs e)
        {
            if (dgvProcesos.SelectedRows.Count > 0)
            {
                try
                {
                    int procesoId = (int)dgvProcesos.SelectedRows[0].Cells["ID"].Value;
                    Process procesoAFinalizar = Process.GetProcessById(procesoId);
                    procesoAFinalizar.Kill();

                    txtProcessInfo.AppendText($"\r\nFinalizado: {procesoAFinalizar.ProcessName} (ID: {procesoId})\r\n");
                    UpdateProcessList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo finalizar el proceso: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Selecciona un proceso para finalizar.", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Crear nueva tarea – aporte Mario Taracena
        private void btnNuevaTarea_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Ingresa el nombre del programa o ruta completa:",
                "Nueva Tarea",
                "notepad.exe"
            );

            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = input,
                        UseShellExecute = true
                    });
                    txtProcessInfo.AppendText($"\r\nEjecutado: {input}\r\n");
                    UpdateProcessList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo ejecutar la tarea: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}