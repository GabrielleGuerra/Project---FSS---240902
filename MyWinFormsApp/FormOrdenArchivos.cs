using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MyWinFormsApp
{
    /// <summary>
    /// Formulario de configuración del Cargador-Ligador.
    /// Permite abrir archivos OBJ, reordenarlos, asignar dirección de carga y confirmar.
    /// </summary>
    public class FormOrdenArchivos : Form
    {
        // --- Controles -------------------------------------------------------------
        private ListBox lstArchivos;
        private Button btnAbrirArchivos;
        private Button btnSubir;
        private Button btnBajar;
        private Button btnEliminar;
        private Button btnConfirm;
        private Button btnCancelar;
        private Label lblDirCarga;
        private TextBox txtDirCarga;

        // Lista paralela de rutas completas (lstArchivos solo muestra el nombre)
        private List<string> rutasCompletas;

        // --- Constructor -------------------------------------------------------------
        public FormOrdenArchivos(List<string> rutas)
        {
            rutasCompletas = new List<string>(rutas ?? new List<string>());
            InicializarComponentes();
            RefrescarLista();
        }

        // --- Cargar nombres en el ListBox -------------------------------------------------
        private void RefrescarLista()
        {
            lstArchivos.Items.Clear();
            foreach (string r in rutasCompletas)
                lstArchivos.Items.Add(Path.GetFileName(r));
        }

        // --- Abrir archivos -------------------------------------------------------------
        private void btnAbrirArchivos_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Selecciona archivos OBJ";
                ofd.Filter = "Archivos OBJ (*.obj)|*.obj|Todos los archivos (*.*)|*.*";
                ofd.Multiselect = true;

                string dir = Path.Combine(Application.StartupPath, "Archivos");
                ofd.InitialDirectory = Directory.Exists(dir) ? dir : Application.StartupPath;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (string ruta in ofd.FileNames)
                    {
                        if (!rutasCompletas.Contains(ruta))
                        {
                            rutasCompletas.Add(ruta);
                            lstArchivos.Items.Add(Path.GetFileName(ruta));
                        }
                    }
                }
            }
        }

        // --- Mover arriba -------------------------------------------------------------
        private void btnSubir_Click(object sender, EventArgs e)
        {
            int idx = lstArchivos.SelectedIndex;
            if (idx <= 0) return;

            Intercambiar(idx, idx - 1);
            lstArchivos.SelectedIndex = idx - 1;
        }

        // --- Mover abajo -------------------------------------------------------------
        private void btnBajar_Click(object sender, EventArgs e)
        {
            int idx = lstArchivos.SelectedIndex;
            if (idx < 0 || idx >= lstArchivos.Items.Count - 1) return;

            Intercambiar(idx, idx + 1);
            lstArchivos.SelectedIndex = idx + 1;
        }

        private void Intercambiar(int a, int b)
        {
            // Rutas
            string tmp = rutasCompletas[a];
            rutasCompletas[a] = rutasCompletas[b];
            rutasCompletas[b] = tmp;
            // Listbox
            object item = lstArchivos.Items[a];
            lstArchivos.Items[a] = lstArchivos.Items[b];
            lstArchivos.Items[b] = item;
        }

        // --- Eliminar seleccionado -------------------------------------------------
        private void btnEliminar_Click(object sender, EventArgs e)
        {
            int idx = lstArchivos.SelectedIndex;
            if (idx < 0) return;

            rutasCompletas.RemoveAt(idx);
            lstArchivos.Items.RemoveAt(idx);

            if (lstArchivos.Items.Count > 0)
                lstArchivos.SelectedIndex = Math.Min(idx, lstArchivos.Items.Count - 1);
        }

        // --- Confirmar y abrir FormCL -------------------------------------------------
        private void btnConfirm_Click(object sender, EventArgs e)
        {
            if (rutasCompletas.Count == 0)
            {
                MessageBox.Show("Agrega al menos un archivo OBJ antes de continuar.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar dirección hex
            string dir = txtDirCarga.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(dir)) dir = "1000";
            try { Convert.ToInt32(dir, 16); }
            catch
            {
                MessageBox.Show("Dirección de carga inválida.\nIngresa un valor hexadecimal (ej: 1000).",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var formCL = new FormCL
            {
                RutasArchivos = rutasCompletas.ToList(),
                DIRPROG = dir
            };

            formCL.Show();
            this.Close();
        }

        // --- Cancelar -------------------------------------------------------------
        private void btnCancelar_Click(object sender, EventArgs e) => this.Close();

        // --- Diseño del formulario (sin archivo .Designer.cs separado) -------------
        private void InicializarComponentes()
        {
            this.Text = "Cargador-Ligador";
            this.Size = new Size(540, 490);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 245, 250);

            // --- Header -------------------------------------------------------------
            var lblHeader = new Label
            {
                Text = "CARGADOR LIGADOR",
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(25, 50, 100),
                ForeColor = Color.White,
                Font = new Font("Arial", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // --- Título lista -------------------------------------------------------------
            var lblTitulo = new Label
            {
                Text = "Archivos OBJ (en orden de carga):",
                AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Location = new Point(14, 52),
                ForeColor = Color.FromArgb(30, 30, 80)
            };

            // --- ListBox -------------------------------------------------------------
            lstArchivos = new ListBox
            {
                Location = new Point(14, 74),
                Size = new Size(368, 270),
                Font = new Font("Courier New", 9),
                HorizontalScrollbar = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // --- Botones laterales -------------------------------------------------
            btnAbrirArchivos = CrearBoton("＋ Abrir archivos", 392, 74, Color.SteelBlue);
            btnAbrirArchivos.Click += btnAbrirArchivos_Click;

            btnSubir = CrearBoton("▲  Subir", 392, 122, Color.FromArgb(80, 80, 80));
            btnSubir.Click += btnSubir_Click;

            btnBajar = CrearBoton("▼  Bajar", 392, 168, Color.FromArgb(80, 80, 80));
            btnBajar.Click += btnBajar_Click;

            btnEliminar = CrearBoton("X Eliminar", 392, 214, Color.Crimson);
            btnEliminar.Click += btnEliminar_Click;

            // --- Dirección de carga -------------------------------------------------
            lblDirCarga = new Label
            {
                Text = "Dirección de carga (hex):",
                AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Location = new Point(14, 360),
                ForeColor = Color.FromArgb(30, 30, 80)
            };

            txtDirCarga = new TextBox
            {
                Text = "0000",
                Location = new Point(212, 357),
                Size = new Size(90, 26),
                Font = new Font("Courier New", 10),
                CharacterCasing = CharacterCasing.Upper,
                MaxLength = 6,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblHex = new Label
            {
                Text = "(valor hexadecimal)",
                AutoSize = true,
                Font = new Font("Arial", 8),
                Location = new Point(310, 362),
                ForeColor = Color.Gray
            };

            // --- Panel inferior con botones principales -------------------------------------------------
            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(230, 230, 240)
            };

            btnConfirm = new Button
            {
                Text = "Confirmar y Cargar",
                Location = new Point(100, 12),
                Size = new Size(180, 36),
                BackColor = Color.ForestGreen,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += btnConfirm_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(295, 12),
                Size = new Size(110, 36),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Arial", 9),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += btnCancelar_Click;

            panelBottom.Controls.Add(btnConfirm);
            panelBottom.Controls.Add(btnCancelar);

            // --- Agregar al form -------------------------------------------------
            this.Controls.Add(lblHex);
            this.Controls.Add(txtDirCarga);
            this.Controls.Add(lblDirCarga);
            this.Controls.Add(btnEliminar);
            this.Controls.Add(btnBajar);
            this.Controls.Add(btnSubir);
            this.Controls.Add(btnAbrirArchivos);
            this.Controls.Add(lstArchivos);
            this.Controls.Add(lblTitulo);
            this.Controls.Add(panelBottom);
            this.Controls.Add(lblHeader);
        }

        // --- Helper para crear botones laterales -------------------------------------------------
        private Button CrearBoton(string texto, int x, int y, Color color)
        {
            var btn = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(126, 34),
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Arial", 9),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}