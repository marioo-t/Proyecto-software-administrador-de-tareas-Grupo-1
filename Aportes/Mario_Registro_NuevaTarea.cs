// Mario Taracena: Funcionalidad de Nueva Tarea

private void btnNuevaTarea_Click(object sender, EventArgs e)
{
    // Aporte Mario Taracena: crear nueva tarea / ejecutar programa
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
