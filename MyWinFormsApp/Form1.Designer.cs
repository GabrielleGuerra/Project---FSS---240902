namespace MyWinFormsApp
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1865, 750);
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

            // Etiqueta ruta archivo
            System.Windows.Forms.Label lblArchivo = new System.Windows.Forms.Label();
            lblArchivo.Text = "Archivo:";
            lblArchivo.AutoSize = true;
            lblArchivo.Location = new System.Drawing.Point(6, 22);
            lblArchivo.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);

            // TextBox ruta (solo visual, no tiene lógica propia)
            System.Windows.Forms.TextBox txtRutaArchivo = new System.Windows.Forms.TextBox();
            txtRutaArchivo.Name = "txtRutaArchivo";
            txtRutaArchivo.Location = new System.Drawing.Point(75, 20);
            txtRutaArchivo.Width = 500;
            txtRutaArchivo.Height = 28;
            txtRutaArchivo.ReadOnly = true;
            txtRutaArchivo.Font = new System.Drawing.Font("Courier New", 9);

            // Cargar Archivo
            this.cargarArchivo = new System.Windows.Forms.Button();
            this.cargarArchivo.Name = "cargarArchivo";
            this.cargarArchivo.Text = "Cargar Archivo";
            this.cargarArchivo.Location = new System.Drawing.Point(585, 15);
            this.cargarArchivo.Size = new System.Drawing.Size(120, 40);
            this.cargarArchivo.Font = new System.Drawing.Font("Arial", 9);
            this.cargarArchivo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.cargarArchivo.Click += new System.EventHandler(this.cargarArchivo_Click);

            // Checar Sintaxis
            this.checarSintax = new System.Windows.Forms.Button();
            this.checarSintax.Name = "checarSintax";
            this.checarSintax.Text = "Checar Sintaxis";
            this.checarSintax.Location = new System.Drawing.Point(715, 15);
            this.checarSintax.Size = new System.Drawing.Size(120, 40);
            this.checarSintax.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
            this.checarSintax.BackColor = System.Drawing.Color.LimeGreen;
            this.checarSintax.ForeColor = System.Drawing.Color.White;
            this.checarSintax.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checarSintax.Click += new System.EventHandler(this.checarSintax_Click);

            // tabla → Generar TABSIM (lógica: tabla_Click)
            this.tabla = new System.Windows.Forms.Button();
            this.tabla.Name = "tabla";
            this.tabla.Text = "Generar TABSIM";
            this.tabla.Location = new System.Drawing.Point(845, 15);
            this.tabla.Size = new System.Drawing.Size(140, 40);
            this.tabla.Font = new System.Drawing.Font("Arial", 9);
            this.tabla.BackColor = System.Drawing.Color.SteelBlue;
            this.tabla.ForeColor = System.Drawing.Color.White;
            this.tabla.Cursor = System.Windows.Forms.Cursors.Hand;
            this.tabla.Click += new System.EventHandler(this.tabla_Click);

            // Generar Codigo Objeto
            this.CodigoObj = new System.Windows.Forms.Button();
            this.CodigoObj.Name = "CodigoObj";
            this.CodigoObj.Text = "Generar Codigo Objeto";
            this.CodigoObj.Location = new System.Drawing.Point(995, 15);
            this.CodigoObj.Size = new System.Drawing.Size(140, 40);
            this.CodigoObj.Font = new System.Drawing.Font("Arial", 9);
            this.CodigoObj.BackColor = System.Drawing.Color.DarkOrange;
            this.CodigoObj.ForeColor = System.Drawing.Color.White;
            this.CodigoObj.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CodigoObj.Click += new System.EventHandler(this.CodigoObj_Click);

            //FileGenerattor → Generar FILE (lógica: FileGenerattor_Click)
            this.FileGenerattor = new System.Windows.Forms.Button();
            this.FileGenerattor.Name = "FileGenerattor";
            this.FileGenerattor.Text = "Generar Programa Objeto";
            this.FileGenerattor.Location = new System.Drawing.Point(1145, 15);
            this.FileGenerattor.Size = new System.Drawing.Size(120, 40);
            this.FileGenerattor.Font = new System.Drawing.Font("Arial", 9);
            this.FileGenerattor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.FileGenerattor.Click += new System.EventHandler(this.FileGenerattor_Click);

            // Botón Limpiar Limpia UI
            System.Windows.Forms.Button btnLimpiar = new System.Windows.Forms.Button();
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Text = "Limpiar";
            btnLimpiar.Location = new System.Drawing.Point(1275, 15);
            btnLimpiar.Size = new System.Drawing.Size(100, 40);
            btnLimpiar.Font = new System.Drawing.Font("Arial", 9);
            btnLimpiar.BackColor = System.Drawing.Color.Orange;
            btnLimpiar.ForeColor = System.Drawing.Color.White;
            btnLimpiar.Cursor = System.Windows.Forms.Cursors.Hand;
            btnLimpiar.Click += new System.EventHandler((s, ev) =>
            {
                rtbCode.Clear();
                rtbErrors.Clear();
                rtbObjArchivo.Clear();
                TablaSimbolos_Panel.Rows.Clear();
                panelResultados.Rows.Clear();
                txtRutaArchivo.Clear();
                numTamProg.Text = "0H";
            });

            // label Tamaño del programa
            this.tamProg = new System.Windows.Forms.Label();
            this.tamProg.AutoSize = true;
            this.tamProg.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.tamProg.Location = new System.Drawing.Point(1530, 22);
            this.tamProg.Text = "Tamaño del programa:";

            this.numTamProg = new System.Windows.Forms.Label();
            this.numTamProg.AutoSize = true;
            this.numTamProg.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.numTamProg.Location = new System.Drawing.Point(1750, 22);
            this.numTamProg.Text = "0H";

            panelSuperior.Controls.Add(lblArchivo);
            panelSuperior.Controls.Add(txtRutaArchivo);
            panelSuperior.Controls.Add(this.cargarArchivo);
            panelSuperior.Controls.Add(this.checarSintax);
            panelSuperior.Controls.Add(this.tabla);
            panelSuperior.Controls.Add(this.CodigoObj);
            panelSuperior.Controls.Add(this.FileGenerattor);
            panelSuperior.Controls.Add(btnLimpiar);
            panelSuperior.Controls.Add(this.tamProg);
            panelSuperior.Controls.Add(this.numTamProg);

            // Botón Ver Tabla de Bloques
            this.btnVerBloques = new System.Windows.Forms.Button();
            this.btnVerBloques.Name = "btnVerBloques";
            this.btnVerBloques.Text = "Ver Tabla Bloques";
            this.btnVerBloques.Location = new System.Drawing.Point(1385, 15);
            this.btnVerBloques.Size = new System.Drawing.Size(130, 40);
            this.btnVerBloques.Font = new System.Drawing.Font("Arial", 9);
            this.btnVerBloques.BackColor = System.Drawing.Color.MediumPurple;
            this.btnVerBloques.ForeColor = System.Drawing.Color.White;
            this.btnVerBloques.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVerBloques.Click += new System.EventHandler(this.btnVerBloques_Click);
            panelSuperior.Controls.Add(this.btnVerBloques);

            // ============================================================================
            // SPLITTER PRINCIPAL (izquierda | derecha)
            // ============================================================================

            System.Windows.Forms.SplitContainer splitPrincipal = new System.Windows.Forms.SplitContainer();
            splitPrincipal.Dock = System.Windows.Forms.DockStyle.Fill;
            splitPrincipal.SplitterDistance = 8;
            splitPrincipal.Orientation = System.Windows.Forms.Orientation.Vertical;

            // ============================================================================
            // PANEL IZQUIERDO: código fuente (rtbCode) + errores (rtbErrors)
            // ============================================================================

            System.Windows.Forms.SplitContainer splitIzquierdo = new System.Windows.Forms.SplitContainer();
            splitIzquierdo.Dock = System.Windows.Forms.DockStyle.Fill;
            splitIzquierdo.Orientation = System.Windows.Forms.Orientation.Horizontal;
            splitIzquierdo.SplitterDistance = 200;

            // Panel código fuente
            System.Windows.Forms.Panel panelCodigo = new System.Windows.Forms.Panel();
            panelCodigo.Dock = System.Windows.Forms.DockStyle.Fill;
            panelCodigo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblCodigo = new System.Windows.Forms.Label();
            lblCodigo.Text = "CÓDIGO FUENTE";
            lblCodigo.Dock = System.Windows.Forms.DockStyle.Top;
            lblCodigo.Height = 28;
            lblCodigo.BackColor = System.Drawing.Color.LightBlue;
            lblCodigo.ForeColor = System.Drawing.Color.DarkBlue;
            lblCodigo.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            lblCodigo.Padding = new System.Windows.Forms.Padding(5);
            lblCodigo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.rtbCode = new System.Windows.Forms.RichTextBox();
            this.rtbCode.Name = "rtbCode";
            this.rtbCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbCode.Font = new System.Drawing.Font("Courier New", 10);
            this.rtbCode.WordWrap = false;
            this.rtbCode.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.rtbCode.Text = "";

            panelCodigo.Controls.Add(this.rtbCode);
            panelCodigo.Controls.Add(lblCodigo);

            // Panel errores
            System.Windows.Forms.Panel panelErrores = new System.Windows.Forms.Panel();
            panelErrores.Dock = System.Windows.Forms.DockStyle.Fill;
            panelErrores.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblErrores = new System.Windows.Forms.Label();
            lblErrores.Text = "ERRORES ENCONTRADOS";
            lblErrores.Dock = System.Windows.Forms.DockStyle.Top;
            lblErrores.Height = 28;
            lblErrores.BackColor = System.Drawing.Color.LightCoral;
            lblErrores.ForeColor = System.Drawing.Color.DarkRed;
            lblErrores.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            lblErrores.Padding = new System.Windows.Forms.Padding(5);
            lblErrores.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.rtbErrors = new System.Windows.Forms.RichTextBox();
            this.rtbErrors.Name = "rtbErrors";
            this.rtbErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbErrors.ReadOnly = true;
            this.rtbErrors.Font = new System.Drawing.Font("Courier New", 9);
            this.rtbErrors.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.rtbErrors.Text = "";

            panelErrores.Controls.Add(this.rtbErrors);
            panelErrores.Controls.Add(lblErrores);

            splitIzquierdo.Panel1.Controls.Add(panelCodigo);
            splitIzquierdo.Panel2.Controls.Add(panelErrores);

            // ============================================================================
            // PANEL DERECHO: TABSIM | tabla de archivo Intermedio
            // ============================================================================

            System.Windows.Forms.SplitContainer splitDerecho = new System.Windows.Forms.SplitContainer();
            splitDerecho.Dock = System.Windows.Forms.DockStyle.Fill;
            splitDerecho.Orientation = System.Windows.Forms.Orientation.Vertical;
            splitDerecho.SplitterDistance = 30;

            // ---- TABSIM + Archivo Objeto (split horizontal dentro de Panel1) ----
            System.Windows.Forms.SplitContainer splitSim = new System.Windows.Forms.SplitContainer();
            splitSim.Dock = System.Windows.Forms.DockStyle.Fill;
            splitSim.Orientation = System.Windows.Forms.Orientation.Horizontal;
            splitSim.SplitterDistance = 55; // 55% TABSIM, 45% OBJ

            //TABSIM
            System.Windows.Forms.Panel panelSim = new System.Windows.Forms.Panel();
            panelSim.Dock = System.Windows.Forms.DockStyle.Fill;
            panelSim.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblSim = new System.Windows.Forms.Label();
            lblSim.Text = "TABLA DE SÍMBOLOS (TABSIM)";
            lblSim.Dock = System.Windows.Forms.DockStyle.Top;
            lblSim.Height = 28;
            lblSim.BackColor = System.Drawing.Color.LightGreen;
            lblSim.ForeColor = System.Drawing.Color.DarkGreen;
            lblSim.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            lblSim.Padding = new System.Windows.Forms.Padding(5);
            lblSim.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.TablaSimbolos_Panel = new System.Windows.Forms.DataGridView();
            this.Simbolo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Direccion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TipoTermino = new System.Windows.Forms.DataGridViewTextBoxColumn();

            ((System.ComponentModel.ISupportInitialize)(this.TablaSimbolos_Panel)).BeginInit();

            this.TablaSimbolos_Panel.Name = "TablaSimbolos_Panel";
            this.TablaSimbolos_Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TablaSimbolos_Panel.AllowUserToAddRows = false;
            this.TablaSimbolos_Panel.AllowUserToDeleteRows = false;
            this.TablaSimbolos_Panel.ReadOnly = true;
            this.TablaSimbolos_Panel.RowHeadersWidth = 62;
            this.TablaSimbolos_Panel.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.TablaSimbolos_Panel.Font = new System.Drawing.Font("Courier New", 9);
            this.TablaSimbolos_Panel.BackgroundColor = System.Drawing.Color.White;
            this.TablaSimbolos_Panel.GridColor = System.Drawing.Color.LightGray;

            this.Simbolo.HeaderText = "Símbolo";
            this.Simbolo.Name = "Simbolo";
            this.Simbolo.ReadOnly = true;
            this.Simbolo.MinimumWidth = 8;
            this.Simbolo.Width = 150;

            this.Direccion.HeaderText = "Dirección";
            this.Direccion.Name = "Direccion";
            this.Direccion.ReadOnly = true;
            this.Direccion.MinimumWidth = 8;
            this.Direccion.Width = 150;

            this.TipoTermino.HeaderText = "Tipo";
            this.TipoTermino.Name = "TipoTermino";
            this.TipoTermino.ReadOnly = true;
            this.TipoTermino.MinimumWidth = 8;
            this.TipoTermino.Width = 120;

            this.TablaSimbolos_Panel.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.Simbolo,
                this.Direccion,
                this.TipoTermino });

            ((System.ComponentModel.ISupportInitialize)(this.TablaSimbolos_Panel)).EndInit();

            panelSim.Controls.Add(this.TablaSimbolos_Panel);
            panelSim.Controls.Add(lblSim);

            // Panel código objeto (archivo .obj)
            System.Windows.Forms.Panel panelObjArch = new System.Windows.Forms.Panel();
            panelObjArch.Dock = System.Windows.Forms.DockStyle.Fill;
            panelObjArch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblObjArch = new System.Windows.Forms.Label();
            lblObjArch.Text = "CÓDIGO OBJETO (.OBJ)";
            lblObjArch.Dock = System.Windows.Forms.DockStyle.Top;
            lblObjArch.Height = 28;
            lblObjArch.BackColor = System.Drawing.Color.Goldenrod;
            lblObjArch.ForeColor = System.Drawing.Color.White;
            lblObjArch.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            lblObjArch.Padding = new System.Windows.Forms.Padding(5);
            lblObjArch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.rtbObjArchivo = new System.Windows.Forms.RichTextBox();
            this.rtbObjArchivo.Name = "rtbObjArchivo";
            this.rtbObjArchivo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbObjArchivo.ReadOnly = true;
            this.rtbObjArchivo.Font = new System.Drawing.Font("Courier New", 9);
            this.rtbObjArchivo.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.rtbObjArchivo.BackColor = System.Drawing.Color.Black;
            this.rtbObjArchivo.ForeColor = System.Drawing.Color.LimeGreen;
            this.rtbObjArchivo.Text = "";

            panelObjArch.Controls.Add(this.rtbObjArchivo);
            panelObjArch.Controls.Add(lblObjArch);

            splitSim.Panel1.Controls.Add(panelSim);
            splitSim.Panel2.Controls.Add(panelObjArch);

            // tabla intermedia / resultados
            System.Windows.Forms.Panel panelInt = new System.Windows.Forms.Panel();
            panelInt.Dock = System.Windows.Forms.DockStyle.Fill;
            panelInt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            System.Windows.Forms.Label lblInt = new System.Windows.Forms.Label();
            lblInt.Text = "RESULTADOS DEL ANÁLISIS";
            lblInt.Dock = System.Windows.Forms.DockStyle.Top;
            lblInt.Height = 28;
            lblInt.BackColor = System.Drawing.Color.LightSteelBlue;
            lblInt.ForeColor = System.Drawing.Color.DarkBlue;
            lblInt.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            lblInt.Padding = new System.Windows.Forms.Padding(5);
            lblInt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.panelResultados = new System.Windows.Forms.DataGridView();
            this.Num = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Formato = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NoBloque = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ETQ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.INS = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OPER = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MODO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Obj = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Err = new System.Windows.Forms.DataGridViewTextBoxColumn();

            ((System.ComponentModel.ISupportInitialize)(this.panelResultados)).BeginInit();

            this.panelResultados.Name = "panelResultados";
            this.panelResultados.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelResultados.AllowUserToAddRows = false;
            this.panelResultados.AllowUserToDeleteRows = false;
            this.panelResultados.AllowUserToOrderColumns = true;
            this.panelResultados.ReadOnly = true;
            this.panelResultados.RowHeadersWidth = 62;
            this.panelResultados.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.panelResultados.Font = new System.Drawing.Font("Courier New", 9);
            this.panelResultados.BackgroundColor = System.Drawing.Color.White;
            this.panelResultados.GridColor = System.Drawing.Color.LightGray;

            this.Num.HeaderText = "Num";
            this.Num.Name = "Num";
            this.Num.ReadOnly = true;
            this.Num.MinimumWidth = 8;
            this.Num.Width = 50;

            this.Formato.HeaderText = "Formato";
            this.Formato.Name = "Formato";
            this.Formato.ReadOnly = true;
            this.Formato.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Formato.MinimumWidth = 8;
            this.Formato.Width = 80;

            this.NoBloque.HeaderText = "No.Bloque";
            this.NoBloque.Name = "NoBloque";
            this.NoBloque.ReadOnly = true;
            this.NoBloque.MinimumWidth = 8;
            this.NoBloque.Width = 80;

            this.CP.HeaderText = "CP";
            this.CP.Name = "CP";
            this.CP.ReadOnly = true;
            this.CP.MinimumWidth = 8;
            this.CP.Width = 70;

            this.ETQ.HeaderText = "ETQ";
            this.ETQ.Name = "ETQ";
            this.ETQ.ReadOnly = true;
            this.ETQ.MinimumWidth = 8;
            this.ETQ.Width = 90;

            this.INS.HeaderText = "INS";
            this.INS.Name = "INS";
            this.INS.ReadOnly = true;
            this.INS.MinimumWidth = 8;
            this.INS.Width = 80;

            this.OPER.HeaderText = "OPER";
            this.OPER.Name = "OPER";
            this.OPER.ReadOnly = true;
            this.OPER.MinimumWidth = 8;
            this.OPER.Width = 150;

            this.MODO.HeaderText = "MODO";
            this.MODO.Name = "MODO";
            this.MODO.ReadOnly = true;
            this.MODO.MinimumWidth = 8;
            this.MODO.Width = 218;

            this.Obj.HeaderText = "Objeto";
            this.Obj.Name = "Obj";
            this.Obj.ReadOnly = true;
            this.Obj.MinimumWidth = 8;
            this.Obj.Width = 150;

            this.Err.HeaderText = "ERR";
            this.Err.Name = "Err";
            this.Err.ReadOnly = true;
            this.Err.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Err.MinimumWidth = 8;
            this.Err.Width = 80;

            this.panelResultados.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.Num,
                this.Formato,
                this.NoBloque,
                this.CP,
                this.ETQ,
                this.INS,
                this.OPER,
                this.MODO,
                this.Obj,
                this.Err });

            ((System.ComponentModel.ISupportInitialize)(this.panelResultados)).EndInit();

            panelInt.Controls.Add(this.panelResultados);
            panelInt.Controls.Add(lblInt);

            splitDerecho.Panel1.Controls.Add(splitSim);
            splitDerecho.Panel2.Controls.Add(panelInt);

            // ============================================================================
            // ENSAMBLAR TODO
            // ============================================================================

            splitPrincipal.Panel1.Controls.Add(splitIzquierdo);
            splitPrincipal.Panel2.Controls.Add(splitDerecho);

            this.Controls.Add(splitPrincipal);
            this.Controls.Add(panelSuperior);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbCode;
        private System.Windows.Forms.Button cargarArchivo;
        private System.Windows.Forms.RichTextBox rtbErrors;
        private System.Windows.Forms.Button checarSintax;
        private System.Windows.Forms.DataGridView TablaSimbolos_Panel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Simbolo;
        private System.Windows.Forms.DataGridViewTextBoxColumn Direccion;
        private System.Windows.Forms.DataGridViewTextBoxColumn TipoTermino;
        private System.Windows.Forms.DataGridView panelResultados;
        private System.Windows.Forms.DataGridViewTextBoxColumn Num;
        private System.Windows.Forms.DataGridViewTextBoxColumn Formato;
        private System.Windows.Forms.DataGridViewTextBoxColumn NoBloque;
        private System.Windows.Forms.DataGridViewTextBoxColumn CP;
        private System.Windows.Forms.DataGridViewTextBoxColumn ETQ;
        private System.Windows.Forms.DataGridViewTextBoxColumn INS;
        private System.Windows.Forms.DataGridViewTextBoxColumn OPER;
        private System.Windows.Forms.DataGridViewTextBoxColumn MODO;
        private System.Windows.Forms.DataGridViewTextBoxColumn Obj;
        private System.Windows.Forms.DataGridViewTextBoxColumn Err;
        private System.Windows.Forms.Label numTamProg;
        private System.Windows.Forms.Label tamProg;
        private System.Windows.Forms.Button tabla;
        private System.Windows.Forms.Button CodigoObj;
        private System.Windows.Forms.Button FileGenerattor;
        private System.Windows.Forms.Button btnVerBloques;
        private System.Windows.Forms.RichTextBox rtbObjArchivo;
    }
}