namespace MyWinFormsApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 900);
            this.Text = "Analizador de Ensamblador SIC/XE";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;

            // ============================================================================
            // PANEL SUPERIOR - CONTROLES
            // ============================================================================
            
            System.Windows.Forms.Panel panelSuperior = new System.Windows.Forms.Panel();
            panelSuperior.Dock = System.Windows.Forms.DockStyle.Top;
            panelSuperior.Height = 70;
            panelSuperior.BackColor = System.Drawing.Color.LightGray;
            panelSuperior.Padding = new System.Windows.Forms.Padding(10);

            // Label "Archivo:"
            System.Windows.Forms.Label lblArchivo = new System.Windows.Forms.Label();
            lblArchivo.Text = "Archivo:";
            lblArchivo.AutoSize = true;
            lblArchivo.Location = new System.Drawing.Point(10, 10);
            lblArchivo.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);

            // TextBox para ruta del archivo
            System.Windows.Forms.TextBox txtRutaArchivo = new System.Windows.Forms.TextBox();
            txtRutaArchivo.Name = "txtRutaArchivo";
            txtRutaArchivo.Location = new System.Drawing.Point(72, 10);
            txtRutaArchivo.Width = 700;
            txtRutaArchivo.Height = 25;
            txtRutaArchivo.ReadOnly = true;
            txtRutaArchivo.Font = new System.Drawing.Font("Courier New", 9);

            // Botón Abrir
            System.Windows.Forms.Button btnAbrir = new System.Windows.Forms.Button();
            btnAbrir.Name = "btnAbrir";
            btnAbrir.Text = "Abrir Documento";
            btnAbrir.Location = new System.Drawing.Point(780, 10);
            btnAbrir.Width = 80;
            btnAbrir.Height = 25;
            btnAbrir.Font = new System.Drawing.Font("Arial", 9);
            btnAbrir.Cursor = System.Windows.Forms.Cursors.Hand;

            // Botón Analizar
            System.Windows.Forms.Button btnAnalizar = new System.Windows.Forms.Button();
            btnAnalizar.Name = "btnAnalizar";
            btnAnalizar.Text = "Analizar";
            btnAnalizar.Location = new System.Drawing.Point(870, 10);
            btnAnalizar.Width = 100;
            btnAnalizar.Height = 25;
            btnAnalizar.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
            btnAnalizar.BackColor = System.Drawing.Color.LimeGreen;
            btnAnalizar.ForeColor = System.Drawing.Color.White;
            btnAnalizar.Cursor = System.Windows.Forms.Cursors.Hand;

            // Botón Guardar
            System.Windows.Forms.Button btnGuardar = new System.Windows.Forms.Button();
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Text = "Guardar Documento";
            btnGuardar.Location = new System.Drawing.Point(980, 10);
            btnGuardar.Width = 100;
            btnGuardar.Height = 25;
            btnGuardar.Font = new System.Drawing.Font("Arial", 9);
            btnGuardar.Cursor = System.Windows.Forms.Cursors.Hand;

            // Botón Limpiar
            System.Windows.Forms.Button btnLimpiar = new System.Windows.Forms.Button();
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Text = "Limpiar";
            btnLimpiar.Location = new System.Drawing.Point(1090, 10);
            btnLimpiar.Width = 100;
            btnLimpiar.Height = 25;
            btnLimpiar.Font = new System.Drawing.Font("Arial", 9);
            btnLimpiar.BackColor = System.Drawing.Color.Orange;
            btnLimpiar.Cursor = System.Windows.Forms.Cursors.Hand;

            // Agregar controles al panel superior
            panelSuperior.Controls.Add(lblArchivo);
            panelSuperior.Controls.Add(txtRutaArchivo);
            panelSuperior.Controls.Add(btnAbrir);
            panelSuperior.Controls.Add(btnAnalizar);
            panelSuperior.Controls.Add(btnGuardar);
            panelSuperior.Controls.Add(btnLimpiar);

            // ============================================================================
            // SPLITTER PRINCIPAL
            // ============================================================================
            
            System.Windows.Forms.SplitContainer splitPrincipal = new System.Windows.Forms.SplitContainer();
            splitPrincipal.Dock = System.Windows.Forms.DockStyle.Fill;
            splitPrincipal.SplitterDistance = 500;
            splitPrincipal.Orientation = System.Windows.Forms.Orientation.Vertical;

            // ============================================================================
            // PANEL IZQUIERDO - CÓDIGO FUENTE
            // ============================================================================
            
            System.Windows.Forms.Panel panelCodigo = new System.Windows.Forms.Panel();
            panelCodigo.Dock = System.Windows.Forms.DockStyle.Fill;
            panelCodigo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblCodigo = new System.Windows.Forms.Label();
            lblCodigo.Text = "CÓDIGO FUENTE";
            lblCodigo.Dock = System.Windows.Forms.DockStyle.Top;
            lblCodigo.Height = 30;
            lblCodigo.BackColor = System.Drawing.Color.LightBlue;
            lblCodigo.ForeColor = System.Drawing.Color.DarkBlue;
            lblCodigo.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            lblCodigo.Padding = new System.Windows.Forms.Padding(5);
            lblCodigo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            System.Windows.Forms.TextBox txtEntrada = new System.Windows.Forms.TextBox();
            txtEntrada.Name = "txtEntrada";
            txtEntrada.Dock = System.Windows.Forms.DockStyle.Fill;
            txtEntrada.Multiline = true;
            txtEntrada.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txtEntrada.Font = new System.Drawing.Font("Courier New", 10);
            txtEntrada.WordWrap = false;
            txtEntrada.AcceptsTab = true;
            txtEntrada.Text = "SUM\tSTART\t0\nFIRST\tLDX\t#0\n\tLDA\t#0";

            panelCodigo.Controls.Add(txtEntrada);
            panelCodigo.Controls.Add(lblCodigo);

            // ============================================================================
            // PANEL DERECHO - DIVIDIDO EN DOS PARTES
            // ============================================================================
            
            System.Windows.Forms.SplitContainer splitDerecho = new System.Windows.Forms.SplitContainer();
            splitDerecho.Dock = System.Windows.Forms.DockStyle.Fill;
            splitDerecho.SplitterDistance = 400;
            splitDerecho.Orientation = System.Windows.Forms.Orientation.Horizontal;

            // Panel superior derecho - Resultados
            System.Windows.Forms.Panel panelResultados = new System.Windows.Forms.Panel();
            panelResultados.Dock = System.Windows.Forms.DockStyle.Fill;
            panelResultados.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblResultados = new System.Windows.Forms.Label();
            lblResultados.Text = "RESULTADOS DEL ANÁLISIS";
            lblResultados.Dock = System.Windows.Forms.DockStyle.Top;
            lblResultados.Height = 30;
            lblResultados.BackColor = System.Drawing.Color.LightGreen;
            lblResultados.ForeColor = System.Drawing.Color.DarkGreen;
            lblResultados.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            lblResultados.Padding = new System.Windows.Forms.Padding(5);
            lblResultados.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            System.Windows.Forms.TreeView treeResultados = new System.Windows.Forms.TreeView();
            treeResultados.Name = "treeResultados";
            treeResultados.Dock = System.Windows.Forms.DockStyle.Fill;
            treeResultados.Font = new System.Drawing.Font("Arial", 9);
            treeResultados.LineColor = System.Drawing.Color.Black;
            treeResultados.ShowLines = true;
            treeResultados.ShowRootLines = true;

            panelResultados.Controls.Add(treeResultados);
            panelResultados.Controls.Add(lblResultados);

            // Panel inferior derecho - Errores
            System.Windows.Forms.Panel panelErrores = new System.Windows.Forms.Panel();
            panelErrores.Dock = System.Windows.Forms.DockStyle.Fill;
            panelErrores.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblErrores = new System.Windows.Forms.Label();
            lblErrores.Text = "ERRORES ENCONTRADOS";
            lblErrores.Dock = System.Windows.Forms.DockStyle.Top;
            lblErrores.Height = 30;
            lblErrores.BackColor = System.Drawing.Color.LightCoral;
            lblErrores.ForeColor = System.Drawing.Color.DarkRed;
            lblErrores.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            lblErrores.Padding = new System.Windows.Forms.Padding(5);
            lblErrores.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            System.Windows.Forms.ListBox lstErrores = new System.Windows.Forms.ListBox();
            lstErrores.Name = "lstErrores";
            lstErrores.Dock = System.Windows.Forms.DockStyle.Fill;
            lstErrores.Font = new System.Drawing.Font("Courier New", 9);
            lstErrores.ScrollAlwaysVisible = true;

            panelErrores.Controls.Add(lstErrores);
            panelErrores.Controls.Add(lblErrores);

            // Agregar paneles al splitDerecho
            splitDerecho.Panel1.Controls.Add(panelResultados);
            splitDerecho.Panel2.Controls.Add(panelErrores);

            // Agregar paneles al splitPrincipal
            splitPrincipal.Panel1.Controls.Add(panelCodigo);
            splitPrincipal.Panel2.Controls.Add(splitDerecho);

            // ============================================================================
            // AGREGAR TODO AL FORMULARIO
            // ============================================================================
            
            this.Controls.Add(splitPrincipal);
            this.Controls.Add(panelSuperior);
        }
    }
}