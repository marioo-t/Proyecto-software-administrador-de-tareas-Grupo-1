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
            this.BackColor = PaletaColores.AzulPrincipal;

            pnlLogin.FillColor = PaletaColores.AzulSecundario;
            pnlLogin.FillColor2 = PaletaColores.AzulSecundario;

            txtUsuario.FillColor = PaletaColores.AzulSecundario;
            txtUsuario.ForeColor = PaletaColores.BlancoCrema;
            txtUsuario.PlaceholderForeColor = Color.Silver;
            txtUsuario.BorderColor = PaletaColores.AzulClaro;
            txtUsuario.FocusedState.BorderColor = PaletaColores.DoradoPrincipal;

            txtContrasena.FillColor = PaletaColores.AzulSecundario;
            txtContrasena.ForeColor = PaletaColores.BlancoCrema;
            txtContrasena.PlaceholderForeColor = Color.Silver;
            txtContrasena.BorderColor = PaletaColores.AzulClaro;
            txtContrasena.FocusedState.BorderColor = PaletaColores.DoradoPrincipal;

            btnIngresar.FillColor = PaletaColores.DoradoPrincipal;
            btnIngresar.ForeColor = PaletaColores.AzulPrincipal;
            btnIngresar.HoverState.FillColor = PaletaColores.DoradoClaro;
            btnIngresar.HoverState.ForeColor = PaletaColores.AzulPrincipal;

            btnCerrar.FillColor = Color.Transparent;
            btnCerrar.ForeColor = PaletaColores.BlancoCrema;
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
                MessageBox.Show("Debe ingresar tanto el usuario como la contrase a.",
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
                ProcesosSimulator procesosSimulator = new ProcesosSimulator(usuarioIngresado);

                procesosSimulator.FormClosed += (s, args) => this.Show();

                procesosSimulator.ShowDialog();
            }
            else
            {
                MessageBox.Show("El usuario o la contrase a no son correctos. Int ntelo nuevamente.",
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
            this.Close(); 
        }
    }
} 
