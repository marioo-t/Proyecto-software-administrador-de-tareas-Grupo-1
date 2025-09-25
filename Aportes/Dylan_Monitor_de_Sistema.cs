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
        // INICIALIZACIÓN DEL MONITOR DEL SISTEMA - DYLAN HERNANDEZ
        // =============================================
        private void InitializeSystemMonitor()
        {
            actualizacionMetricasTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
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
