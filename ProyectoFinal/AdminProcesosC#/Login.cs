using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AdminProcesosC_
{
    public partial class Login : Form
    {
        private readonly Dictionary<string, string> credencialesValidas = new Dictionary<string, string>();

        public Login()
        {
            InitializeComponent();
            CargarCredenciales();

            this.btnCerrar.Click += btnCerrar_Click;
            this.Load += Login_Load;
            this.Resize += Login_Resize;
            this.btnIngresar.Click += btnIngresar_Click;
        }

        private void CargarCredenciales()
        {
            credencialesValidas.Clear();
            credencialesValidas.Add("dylan hernandez", "519");
            credencialesValidas.Add("mario taracena", "9335");
            credencialesValidas.Add("eduardo rodriguez", "2459");
            credencialesValidas.Add("gabriel guillen", "1998");
        }

        private void Login_Load(object sender, EventArgs e)
        {
            ApplyColors();
            PositionCloseButton();
            CenterPanel();
        }

        private void Login_Resize(object sender, EventArgs e)
        {
            CenterPanel();
            PositionCloseButton();
        }

        private void ApplyColors()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);

            pnlLogin.FillColor = Color.FromArgb(45, 45, 45);
            pnlLogin.FillColor2 = Color.FromArgb(45, 45, 45);

            txtUsuario.FillColor = Color.FromArgb(45, 45, 45);
            txtUsuario.ForeColor = Color.White;
            txtUsuario.PlaceholderForeColor = Color.Silver;
            txtUsuario.BorderColor = Color.FromArgb(70, 70, 70);
            txtUsuario.FocusedState.BorderColor = Color.FromArgb(0, 120, 215);

            txtContrasena.FillColor = Color.FromArgb(45, 45, 45);
            txtContrasena.ForeColor = Color.White;
            txtContrasena.PlaceholderForeColor = Color.Silver;
            txtContrasena.BorderColor = Color.FromArgb(70, 70, 70);
            txtContrasena.FocusedState.BorderColor = Color.FromArgb(0, 120, 215);

            btnIngresar.FillColor = Color.FromArgb(0, 120, 215);
            btnIngresar.ForeColor = Color.White;
            btnIngresar.HoverState.FillColor = Color.FromArgb(0, 150, 255);
            btnIngresar.HoverState.ForeColor = Color.White;

            btnCerrar.FillColor = Color.Transparent;
            btnCerrar.ForeColor = Color.White;
            btnCerrar.HoverState.FillColor = Color.Red;
            btnCerrar.HoverState.ForeColor = Color.White;
        }

        private void CenterPanel()
        {
            int x = (this.Width - pnlLogin.Width) / 2;
            int y = (this.Height - pnlLogin.Height) / 2;
            pnlLogin.Location = new Point(x, y);
        }

        private void PositionCloseButton()
        {
            int margin = 10;
            int x = this.Width - btnCerrar.Width - margin;
            int y = margin;
            btnCerrar.Location = new Point(x, y);
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            string usuarioIngresado = txtUsuario.Text.Trim();
            string contrasenaIngresada = txtContrasena.Text.Trim();

            if (string.IsNullOrEmpty(usuarioIngresado) || string.IsNullOrEmpty(contrasenaIngresada))
            {
                MessageBox.Show("Debe ingresar tanto el usuario como la contraseña.",
                                "Campos incompletos",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            string usuarioKey = usuarioIngresado.ToLower();

            if (credencialesValidas.ContainsKey(usuarioKey) && credencialesValidas[usuarioKey] == contrasenaIngresada)
            {
                MessageBox.Show($"Bienvenido, {usuarioIngresado}.",
                                "Acceso concedido",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                txtUsuario.Clear();
                txtContrasena.Clear();

                this.Hide();

                // CREAR INSTANCIA SIN USING Y MANEJAR EL CIERRE MANUALMENTE
                ProcesosSimulator procesosSimulator = null;
                try
                {
                    procesosSimulator = new ProcesosSimulator(usuarioIngresado);
                    procesosSimulator.FormClosed += (s, args) =>
                    {
                        this.Show();
                        procesosSimulator?.Dispose();
                    };

                    procesosSimulator.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir el administrador: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Show();
                }
            }
            else
            {
                MessageBox.Show("El usuario o la contraseña no son correctos. Inténtelo nuevamente.",
                                "Acceso denegado",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                txtUsuario.Clear();
                txtContrasena.Clear();
                txtUsuario.Focus();
            }
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}