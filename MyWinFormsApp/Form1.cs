using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyWinFormsApp
{
    public partial class Form1 : Form
    {
        private AnalizadorSicXe analizador;
        private string rutaArchivoActual = "";

        public Form1()
        {
            InitializeComponent();
            analizador = new AnalizadorSicXe();
            ConfigurarControles();
        }

        private void ConfigurarControles()
        {
            Button btnAbrir = this.Controls.Find("btnAbrir", true).FirstOrDefault() as Button;
            Button btnAnalizar = this.Controls.Find("btnAnalizar", true).FirstOrDefault() as Button;
            Button btnGuardar = this.Controls.Find("btnGuardar", true).FirstOrDefault() as Button;
            Button btnLimpiar = this.Controls.Find("btnLimpiar", true).FirstOrDefault() as Button;

            if (btnAbrir != null) btnAbrir.Click += (s, e) => AbrirArchivo();
            if (btnAnalizar != null) btnAnalizar.Click += (s, e) => Analizar();
            if (btnGuardar != null) btnGuardar.Click += (s, e) => GuardarResultados();
            if (btnLimpiar != null) btnLimpiar.Click += (s, e) => Limpiar();
        }

        private void AbrirArchivo()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            openFile.Title = "Seleccionar archivo de ensamblador SIC/XE";

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                rutaArchivoActual = openFile.FileName;
                
                TextBox txtRutaArchivo = this.Controls.Find("txtRutaArchivo", true).FirstOrDefault() as TextBox;
                if (txtRutaArchivo != null)
                    txtRutaArchivo.Text = rutaArchivoActual;
                
                try
                {
                    string contenido = File.ReadAllText(rutaArchivoActual, Encoding.UTF8);
                    TextBox txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
                    if (txtEntrada != null)
                        txtEntrada.Text = contenido;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Analizar()
        {
            TextBox txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
            if (txtEntrada == null || string.IsNullOrWhiteSpace(txtEntrada.Text))
            {
                MessageBox.Show("Por favor ingrese código o cargue un archivo", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                TreeView treeResultados = this.Controls.Find("treeResultados", true).FirstOrDefault() as TreeView;
                ListBox lstErrores = this.Controls.Find("lstErrores", true).FirstOrDefault() as ListBox;
                
                treeResultados?.Nodes.Clear();
                lstErrores?.Items.Clear();

                analizador.Analizar(txtEntrada.Text);

                MostrarResultadosEnArbol(treeResultados);
                MostrarErrores(lstErrores);

                if (!analizador.HayErrores())
                {
                    MessageBox.Show("Análisis completado sin errores", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Análisis completado con {analizador.ObtenerErrores().Count} errores", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el análisis: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MostrarResultadosEnArbol(TreeView tree)
        {
            if (tree == null)
                return;

            var lineas = analizador.ObtenerLineasProcesadas();

            TreeNode raiz = new TreeNode($"ANÁLISIS DE ENSAMBLADOR SIC/XE ({lineas.Count} líneas)");
            raiz.BackColor = Color.LightBlue;

            foreach (var linea in lineas)
            {
                if (string.IsNullOrEmpty(linea.Operacion))
                    continue;

                string icono = linea.Valida ? "✓" : "✗";
                // Mostrar operación con + si la tiene
                string nodo = $"{icono} {linea.Contador}\t{linea.Etiqueta}\t{linea.Operacion}\t{linea.Operandos}";
                TreeNode nodoLinea = new TreeNode(nodo);
                nodoLinea.BackColor = linea.Valida ? Color.LightGreen : Color.LightCoral;
                nodoLinea.ForeColor = linea.Valida ? Color.DarkGreen : Color.DarkRed;

                if (linea.Errores.Count > 0)
                {
                    TreeNode nodoErrores = new TreeNode("Errores:");
                    nodoErrores.ForeColor = Color.Red;
                    foreach (var error in linea.Errores)
                    {
                        nodoErrores.Nodes.Add(error);
                    }
                    nodoLinea.Nodes.Add(nodoErrores);
                }

                raiz.Nodes.Add(nodoLinea);
            }

            // Agregar tabla de símbolos
            var tablaSimbolos = analizador.ObtenerTablaSimbolos();
            if (tablaSimbolos.Count > 0)
            {
                TreeNode nodoSimbolos = new TreeNode("TABLA DE SÍMBOLOS");
                nodoSimbolos.BackColor = Color.LightYellow;

                foreach (var simbolo in tablaSimbolos.OrderBy(x => x.Value))
                {
                    TreeNode nodoSimbolo = new TreeNode($"{simbolo.Key}\t| {simbolo.Value}");
                    nodoSimbolos.Nodes.Add(nodoSimbolo);
                }

                raiz.Nodes.Add(nodoSimbolos);
            }

            tree.Nodes.Add(raiz);
            tree.ExpandAll();
        }

        private void MostrarErrores(ListBox lst)
        {
            if (lst == null)
                return;

            var errores = analizador.ObtenerErrores();

            if (errores.Count == 0)
            {
                lst.Items.Add("Sin errores");
                return;
            }

            foreach (var error in errores)
            {
                lst.Items.Add(error);
            }
        }

        private void GuardarResultados()
        {
            if (analizador.ObtenerLineasProcesadas().Count == 0)
            {
                MessageBox.Show("Primero debes analizar un archivo", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Archivos de texto (*.txt)|*.txt";
            saveFile.Title = "Guardar resultados del análisis";
            saveFile.FileName = !string.IsNullOrEmpty(rutaArchivoActual) 
                ? Path.GetFileNameWithoutExtension(rutaArchivoActual) + "_analisis.txt"
                : "analisis.txt";

            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sb = new StringBuilder();
                    var lineas = analizador.ObtenerLineasProcesadas();
                    var errores = analizador.ObtenerErrores();
                    var tablaSimbolos = analizador.ObtenerTablaSimbolos();

                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
                    sb.AppendLine("ANÁLISIS DE ENSAMBLADOR SIC/XE");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════\n");

                    sb.AppendLine("CONTADOR\tETIQUETA\tOPERACIÓN\tOPERANDOS\tESTADO");
                    sb.AppendLine("───────────────────────────────────────────────────────────────────────");

                    foreach (var linea in lineas)
                    {
                        if (string.IsNullOrEmpty(linea.Operacion))
                            continue;

                        string estado = linea.Valida ? "Correcto" : "Error";
                        sb.AppendLine($"{linea.Contador}\t{linea.Etiqueta}\t{linea.Operacion}\t{linea.Operandos}\t{estado}");
                        
                        if (linea.Errores.Count > 0)
                        {
                            foreach (var error in linea.Errores)
                                sb.AppendLine($"\t\t\t\t\t{error}");
                        }
                    }

                    // Tabla de símbolos
                    if (tablaSimbolos.Count > 0)
                    {
                        sb.AppendLine("\n═══════════════════════════════════════════════════════════════════════");
                        sb.AppendLine("TABLA DE SÍMBOLOS");
                        sb.AppendLine("───────────────────────────────────────────────────────────────────────");
                        sb.AppendLine("Símbolo\t| Dirección");
                        sb.AppendLine("───────────────────────────────────────────────────────────────────────");

                        foreach (var simbolo in tablaSimbolos.OrderBy(x => x.Value))
                        {
                            sb.AppendLine($"{simbolo.Key}\t| {simbolo.Value}");
                        }
                    }

                    sb.AppendLine("\n═══════════════════════════════════════════════════════════════════════");
                    sb.AppendLine($"RESUMEN: Total {lineas.Count} líneas, {errores.Count} errores");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════");

                    File.WriteAllText(saveFile.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"✓ Resultados guardados en:\n{saveFile.FileName}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Limpiar()
        {
            TextBox txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
            TreeView treeResultados = this.Controls.Find("treeResultados", true).FirstOrDefault() as TreeView;
            ListBox lstErrores = this.Controls.Find("lstErrores", true).FirstOrDefault() as ListBox;
            TextBox txtRuta = this.Controls.Find("txtRutaArchivo", true).FirstOrDefault() as TextBox;

            if (txtEntrada != null) txtEntrada.Clear();
            if (treeResultados != null) treeResultados.Nodes.Clear();
            if (lstErrores != null) lstErrores.Items.Clear();
            if (txtRuta != null) txtRuta.Clear();

            analizador = new AnalizadorSicXe();
            rutaArchivoActual = "";
        }
    }
}