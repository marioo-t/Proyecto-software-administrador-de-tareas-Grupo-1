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
