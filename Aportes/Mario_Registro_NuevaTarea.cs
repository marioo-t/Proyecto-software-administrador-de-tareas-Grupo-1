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
        // AGREGAR VALORES NUMERICO  - MARIO TARACENA
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
        // AGREGAR FILA DE PROCESO - MARIO TARACENA
        // =============================================
        private void AddRealProcessRow(Process proceso, double cpuReal, double memoriaMB)
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
                row.Tag = proceso.Id; // Guardar PID en el Tag de la fila

                // Guardar valores numéricos para ordenamiento
                row.Cells["CPU"].Tag = cpuReal;
                row.Cells["Memoria"].Tag = memoriaMB;

                pidRowMap[proceso.Id] = row;

                // Aplicar coloración basada en CPU inicial
                ApplyHeatColorToRow(row, cpuReal);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error agregando fila para {proceso.ProcessName}: {ex.Message}");
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
