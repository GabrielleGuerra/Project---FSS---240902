namespace Project.WinForms;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Crea y configura los controles que componen la interfaz.
    /// Todos los controles se crean de forma programática para simplificar el ejemplo.
    /// </summary>
    private void InitializeComponent()
    {
        // Panel principal: contenedor con padding y color de fondo
        System.Windows.Forms.Panel panelPrincipal = new System.Windows.Forms.Panel();
        panelPrincipal.Dock = DockStyle.Fill;
        panelPrincipal.Padding = new System.Windows.Forms.Padding(20);
        panelPrincipal.BackColor = System.Drawing.Color.WhiteSmoke;

        // Título en la parte superior
        System.Windows.Forms.Label lblTitulo = new System.Windows.Forms.Label();
        lblTitulo.Text = "Calculadora de Expresiones Matemáticas";
        lblTitulo.Font = new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold);
        lblTitulo.Dock = DockStyle.Top;
        lblTitulo.ForeColor = System.Drawing.Color.HotPink;
        lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        lblTitulo.Height = 40;

        // Etiqueta que indica al usuario qué ingresar
        System.Windows.Forms.Label lblExpresion = new System.Windows.Forms.Label();
        lblExpresion.Text = "Ingresa una expresión matemática:";
        lblExpresion.Font = new System.Drawing.Font("Arial", 11);
        lblExpresion.Dock = DockStyle.Top;
        lblExpresion.Height = 30;
        lblExpresion.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);

        // TextBox para ingresar la expresión. Maneja la tecla Enter para calcular.
        System.Windows.Forms.TextBox txtExpresion = new System.Windows.Forms.TextBox();
        txtExpresion.Name = "txtExpresion";
        txtExpresion.Dock = DockStyle.Top;
        txtExpresion.Height = 40;
        txtExpresion.Font = new System.Drawing.Font("Arial", 12);
        txtExpresion.Padding = new System.Windows.Forms.Padding(5);
        txtExpresion.Multiline = false;
        txtExpresion.KeyPress += (sender, e) =>
        {
            // Al presionar Enter, ejecutar cálculo y evitar el sonido de ding
            if (e.KeyChar == (char)Keys.Return)
            {
                CalcularExpresion(txtExpresion);
                e.Handled = true;
            }
        };

        // Panel que contiene los botones (Calcular / Limpiar)
        System.Windows.Forms.Panel panelBotones = new System.Windows.Forms.Panel();
        panelBotones.Dock = DockStyle.Top;
        panelBotones.Height = 50;
        panelBotones.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);

        // Botón para ejecutar el cálculo
        System.Windows.Forms.Button btnCalcular = new System.Windows.Forms.Button();
        btnCalcular.Text = "Calcular";
        btnCalcular.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
        btnCalcular.Size = new System.Drawing.Size(120, 40);
        btnCalcular.Location = new System.Drawing.Point(10, 5);
        btnCalcular.BackColor = System.Drawing.Color.Pink;
        btnCalcular.ForeColor = System.Drawing.Color.White;
        btnCalcular.Click += (sender, e) => CalcularExpresion(txtExpresion);

        // Botón para limpiar entrada y resultados (restablece interfaz)
        System.Windows.Forms.Button btnLimpiar = new System.Windows.Forms.Button();
        btnLimpiar.Text = "Limpiar";
        btnLimpiar.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
        btnLimpiar.Size = new System.Drawing.Size(120, 40);
        btnLimpiar.Location = new System.Drawing.Point(140, 5);
        btnLimpiar.BackColor = System.Drawing.Color.HotPink;
        btnLimpiar.ForeColor = System.Drawing.Color.White;
        btnLimpiar.Click += (sender, e) => 
        { 
            // Limpiar caja de texto y etiquetas de resultado e historial
            txtExpresion.Clear();
            var lblResultado = this.Controls[0].Controls["lblResultado"] as System.Windows.Forms.Label;
            var lblHistorial = this.Controls[0].Controls["lblHistorial"] as System.Windows.Forms.Label;
            var treeArbol = this.Controls[0].Controls["treeArbolSintactico"] as System.Windows.Forms.TreeView;
            if (lblResultado != null) lblResultado.Text = "";
            if (lblHistorial != null) lblHistorial.Text = "";
            if (treeArbol != null) treeArbol.Nodes.Clear();
            txtExpresion.Focus();
        };

        panelBotones.Controls.Add(btnCalcular);
        panelBotones.Controls.Add(btnLimpiar);

        // Etiqueta pequeña que indica la sección de resultado
        System.Windows.Forms.Label lblEtiquetaResultado = new System.Windows.Forms.Label();
        lblEtiquetaResultado.Text = "Resultado:";
        lblEtiquetaResultado.Font = new System.Drawing.Font("Arial", 11);
        lblEtiquetaResultado.Dock = DockStyle.Top;
        lblEtiquetaResultado.Height = 30;
        lblEtiquetaResultado.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);

        // Label donde se muestra el resultado calculado
        System.Windows.Forms.Label lblResultado = new System.Windows.Forms.Label();
        lblResultado.Name = "lblResultado";
        lblResultado.Text = "";
        lblResultado.Font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold);
        lblResultado.ForeColor = System.Drawing.Color.HotPink;
        lblResultado.Dock = DockStyle.Top;
        lblResultado.Height = 40;
        lblResultado.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
        lblResultado.AutoSize = false;

        // ==================== NUEVO: TreeView para Árbol Sintáctico ====================
        System.Windows.Forms.Label lblEtiquetaArbol = new System.Windows.Forms.Label();
        lblEtiquetaArbol.Text = "Árbol Sintáctico:";
        lblEtiquetaArbol.Font = new System.Drawing.Font("Arial", 10);
        lblEtiquetaArbol.Dock = DockStyle.Top;
        lblEtiquetaArbol.Height = 25;
        lblEtiquetaArbol.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);

        System.Windows.Forms.TreeView treeArbolSintactico = new System.Windows.Forms.TreeView();
        treeArbolSintactico.Name = "treeArbolSintactico";
        treeArbolSintactico.Dock = DockStyle.Top;
        treeArbolSintactico.Height = 150;
        treeArbolSintactico.Font = new System.Drawing.Font("Consolas", 9);
        treeArbolSintactico.ShowLines = true;
        treeArbolSintactico.ShowPlusMinus = true;
        treeArbolSintactico.BorderStyle = BorderStyle.FixedSingle;

        // Historial de cálculos
        System.Windows.Forms.Label lblEtiquetaHistorial = new System.Windows.Forms.Label();
        lblEtiquetaHistorial.Text = "Historial:";
        lblEtiquetaHistorial.Font = new System.Drawing.Font("Arial", 10);
        lblEtiquetaHistorial.Dock = DockStyle.Top;
        lblEtiquetaHistorial.Height = 20;

        System.Windows.Forms.Label lblHistorial = new System.Windows.Forms.Label();
        lblHistorial.Name = "lblHistorial";
        lblHistorial.Text = "";
        lblHistorial.Font = new System.Drawing.Font("Courier New", 9);
        lblHistorial.Dock = DockStyle.Fill;
        lblHistorial.AutoSize = false;

        // Agregar controles al formulario en orden
        this.Controls.Add(panelPrincipal);

        panelPrincipal.Controls.Add(lblHistorial);
        panelPrincipal.Controls.Add(lblEtiquetaHistorial);
        panelPrincipal.Controls.Add(treeArbolSintactico);  // NUEVO
        panelPrincipal.Controls.Add(lblEtiquetaArbol);      // NUEVO
        panelPrincipal.Controls.Add(lblResultado);
        panelPrincipal.Controls.Add(lblEtiquetaResultado);
        panelPrincipal.Controls.Add(panelBotones);
        panelPrincipal.Controls.Add(txtExpresion);
        panelPrincipal.Controls.Add(lblExpresion);
        panelPrincipal.Controls.Add(lblTitulo);
    }
    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    
    #endregion
}