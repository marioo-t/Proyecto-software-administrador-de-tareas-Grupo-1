// =======================================================
// Proyecto Final – Administrador de Procesos
// -------------------------------------------------------
// Aporte de Eduardo Rodríguez:
//   - Implementación de la lógica de monitoreo y control de procesos
//   - Uso de PerformanceCounter para métricas de CPU y RAM
//   - Simulación de planificación Round Robin (5s)
// =======================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace AdminProcesosC_
{
    public partial class ProcesosSimulator : Form
    {
        // =======================================================
        // Variables de monitoreo del sistema
        // =======================================================
        private System.Windows.Forms.Timer actualizacionMetricasTimer;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;

        // Diccionario: Contadores de CPU por proceso
        private Dictionary<int, PerformanceCounter> cpuCountersPorProceso = new();

        // =======================================================
        // Inicializar monitoreo del sistema
        // =======================================================
        private void InitializeSystemMonitor()
        {
            actualizacionMetricasTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // cuanto de tiempo (5 segundos)
            };
            actualizacionMetricasTimer.Tick += actualizacionMetricasTimer_Tick;

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

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
            UpdateSystemMetrics();
            UpdateProcessCpuUsage();
        }

        private void UpdateProcessList()
        {
            Process[] allProcesses = Process.GetProcesses().OrderBy(p => p.ProcessName).ToArray();
            var procesosActuales = allProcesses.Select(p => p.Id).ToHashSet();

            // Eliminar procesos finalizados
            var keysToRemove = cpuCountersPorProceso.Keys.Where(id => !procesosActuales.Contains(id)).ToList();
            foreach (var key in keysToRemove) cpuCountersPorProceso.Remove(key);

            dgvProcesos.Rows.Clear();

            foreach (Process proc in allProcesses)
            {
                try
                {
                    string processType = proc.Responding ? "Aplicación" : "Fondo";

                    if (!cpuCountersPorProceso.ContainsKey(proc.Id))
                    {
                        var cpuCounterProceso = new PerformanceCounter("Process", "% Processor Time", proc.ProcessName, true);
                        cpuCounterProceso.NextValue();
                        cpuCountersPorProceso[proc.Id] = cpuCounterProceso;
                    }

                    double memoriaFisica = proc.WorkingSet64 / 1024.0 / 1024.0;
                    double memoriaVirtual = proc.VirtualMemorySize64 / 1024.0 / 1024.0;

                    dgvProcesos.Rows.Add(
                        proc.ProcessName,
                        proc.Id,
                        memoriaFisica.ToString("F2"),
                        memoriaVirtual.ToString("F2"),
                        "0.00",
                        processType
                    );
                }
                catch
                {
                    // Algunos procesos del sistema están protegidos
                }
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
    }
}
