// =======================================================
// Aporte individual – Proyecto Administrador de Procesos
// Autor: Gabriel Guillén
// Módulo: Diseño de Interfaz
// Descripción: Implementación del diseño gráfico de la ventana
// principal usando Guna.UI2 (paneles, botones, datagrid, etc.).
// Este aporte se centra únicamente en la vista, sin incluir
// lógica de procesos ni eventos funcionales.
// =======================================================

using Guna.UI2.WinForms;
using System.Drawing;
using System.Windows.Forms;

namespace AdminProcesosC_
{
    public partial class ProcesosSimulator : Form
    {
        // Controles de la interfaz
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

        private string usuarioActual;

        public ProcesosSimulator(string nombreUsuario)
        {
            this.usuarioActual = nombreUsuario;

            InitializeComponent();
            InitializeGunaUI(); // Cargar solo el diseño
        }

        private void InitializeGunaUI()
        {
            // Ventana
            this.Text = $"Administrador de Procesos - Sesión: {usuarioActual}";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = PaletaColores.AzulPrincipal;
            this.ForeColor = PaletaColores.BlancoCrema;

            // Panel lateral
            pnlSidebar = new Guna2Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                FillColor = PaletaColores.AzulSecundario
            };
            this.Controls.Add(pnlSidebar);

            // Panel principal
            pnlMain = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = PaletaColores.AzulPrincipal
            };
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

            // Usuario
            lblUsuario = new Guna2HtmlLabel
            {
                Text = $"Usuario: {usuarioActual}",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.White,
                Padding = new Padding(10, 0, 0, 10),
                Height = 25,
                BackColor = Color.Transparent
            };
            pnlSidebar.Controls.Add(lblUsuario);

            // Botón Actualizar
            btnActualizar = new Guna2Button
            {
                Text = "Actualizar",
                Dock = DockStyle.Top,
                FillColor = PaletaColores.AzulGris,
                ForeColor = PaletaColores.BlancoCrema,
                Height = 45,
                Margin = new Padding(10)
            };
            pnlSidebar.Controls.Add(btnActualizar);

            // Botón Nueva Tarea
            btnNuevaTarea = new Guna2Button
            {
                Text = "Nueva Tarea",
                Dock = DockStyle.Top,
                FillColor = PaletaColores.DoradoClaro,
                ForeColor = Color.White,
                Height = 45,
                Margin = new Padding(10)
            };
            pnlSidebar.Controls.Add(btnNuevaTarea);

            // Botón Finalizar
            btnFinalizarProceso = new Guna2Button
            {
                Text = "Finalizar",
                Dock = DockStyle.Top,
                FillColor = PaletaColores.DoradoPrincipal,
                ForeColor = Color.White,
                Height = 45,
                Margin = new Padding(10)
            };
            pnlSidebar.Controls.Add(btnFinalizarProceso);

            // Botón Cerrar Sesión
            btnCerrar = new Guna2Button
            {
                Text = "Cerrar Sesión",
                Dock = DockStyle.Bottom,
                FillColor = PaletaColores.DoradoPrincipal,
                ForeColor = Color.White,
                Height = 45,
                Margin = new Padding(10)
            };
            pnlSidebar.Controls.Add(btnCerrar);

            // DataGridView
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

            dgvProcesos.ThemeStyle.AlternatingRowsStyle.BackColor = PaletaColores.AzulSecundario;
            dgvProcesos.ThemeStyle.HeaderStyle.BackColor = PaletaColores.AzulIntermedio;
            dgvProcesos.ThemeStyle.HeaderStyle.ForeColor = PaletaColores.BlancoCrema;
            dgvProcesos.ThemeStyle.RowsStyle.BackColor = PaletaColores.AzulPrincipal;
            dgvProcesos.ThemeStyle.RowsStyle.ForeColor = PaletaColores.BlancoCrema;
            dgvProcesos.ThemeStyle.GridColor = PaletaColores.AzulGris;
            dgvProcesos.ThemeStyle.ReadOnly = true;
            dgvProcesos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvProcesos.ColumnCount = 6;
            dgvProcesos.Columns[0].Name = "Nombre";
            dgvProcesos.Columns[1].Name = "ID";
            dgvProcesos.Columns[2].Name = "Memoria (MB)";
            dgvProcesos.Columns[3].Name = "CPU (%)";
            dgvProcesos.Columns[4].Name = "Prioridad";
            dgvProcesos.Columns[5].Name = "Tipo";

            // Barra CPU
            pbCpuUsage = new Guna2ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 10,
                ProgressColor = PaletaColores.DoradoPrincipal,
                ProgressColor2 = PaletaColores.DoradoClaro
            };
            pnlMain.Controls.Add(pbCpuUsage);

            // Info Proceso
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
                Margin = new Padding(5),
                PlaceholderText = "Selecciona un proceso para ver detalles..."
            };
            pnlMain.Controls.Add(txtProcessInfo);

            // Orden visual
            pnlMain.BringToFront();
            pbCpuUsage.BringToFront();
            txtProcessInfo.BringToFront();
        }
    }
}