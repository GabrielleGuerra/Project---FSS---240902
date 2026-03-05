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
            ConfigurarEventos();
        }

        private void ConfigurarEventos()
        {
            var btnAbrir   = this.Controls.Find("btnAbrir",   true).FirstOrDefault() as Button;
            var btnAnalizar= this.Controls.Find("btnAnalizar",true).FirstOrDefault() as Button;
            var btnGuardar = this.Controls.Find("btnGuardar", true).FirstOrDefault() as Button;
            var btnLimpiar = this.Controls.Find("btnLimpiar", true).FirstOrDefault() as Button;

            if (btnAbrir    != null) btnAbrir.Click    += (s, e) => AbrirArchivo();
            if (btnAnalizar != null) btnAnalizar.Click += (s, e) => Analizar();
            if (btnGuardar  != null) btnGuardar.Click  += (s, e) => GuardarResultados();
            if (btnLimpiar  != null) btnLimpiar.Click  += (s, e) => Limpiar();
        }

        // ============================================================
        // Abrir archivo
        // ============================================================
        private void AbrirArchivo()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                Title  = "Seleccionar archivo de ensamblador SIC/XE"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            rutaArchivoActual = dlg.FileName;
            var txtRuta = this.Controls.Find("txtRutaArchivo", true).FirstOrDefault() as TextBox;
            if (txtRuta != null) txtRuta.Text = rutaArchivoActual;

            try
            {
                string contenido = File.ReadAllText(rutaArchivoActual, Encoding.UTF8);
                var txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
                if (txtEntrada != null) txtEntrada.Text = contenido;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar archivo: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // Analizar
        // ============================================================
        private void Analizar()
        {
            var txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
            if (txtEntrada == null || string.IsNullOrWhiteSpace(txtEntrada.Text))
            {
                MessageBox.Show("Por favor ingrese código o cargue un archivo", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var dgv       = this.Controls.Find("dgvResultados", true).FirstOrDefault() as DataGridView;
                var lstErrores= this.Controls.Find("lstErrores",    true).FirstOrDefault() as ListBox;

                if (dgv       != null) dgv.Rows.Clear();
                if (lstErrores!= null) lstErrores.Items.Clear();

                analizador.Analizar(txtEntrada.Text);

                MostrarResultadosEnTabla(dgv);
                MostrarErrores(lstErrores);

                int inicio = analizador.ObtenerContadorInicio();
                int final  = analizador.ObtenerContadorFinal();
                int tam    = analizador.ObtenerTamanioProgramaTotal();

                MessageBox.Show(
                    $"Contador Inicial (START): 0x{inicio:X4}\n" +
                    $"Contador Final:           0x{final:X4}\n" +
                    $"Tamaño Total del Programa: {tam} bytes (0x{tam:X4})",
                    "Información del Programa");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el análisis: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // Mostrar resultados en DataGridView
        // ============================================================
        private void MostrarResultadosEnTabla(DataGridView? dgv)
        {
            if (dgv == null) return;

            var lineas = analizador.ObtenerLineasProcesadas();

            foreach (var linea in lineas)
            {
                if (string.IsNullOrEmpty(linea.Operacion)) continue;

                // Columna "Estado/Errores": muestra el mensaje de error del paso 2
                // o los errores léxicos/sintácticos, o vacío si todo OK
                string estadoTexto = "";
                if (linea.Errores.Count > 0)
                    estadoTexto = string.Join("; ", linea.Errores);
                else if (!string.IsNullOrEmpty(linea.ErrorMessage))
                    estadoTexto = linea.ErrorMessage;

                // Formato mostrado: "---" para directivas (la línea ya trae "---")
                string fmtDisplay = linea.Formato == "---" ? "---" : linea.Formato;

                int rowIndex = dgv.Rows.Add(
                    linea.NumeroLinea,           // 0: Nº
                    fmtDisplay,                  // 1: Formato
                    linea.Contador,              // 2: CP
                    linea.Etiqueta,              // 3: Etiqueta
                    linea.Operacion,             // 4: Instrucción
                    linea.Operandos,             // 5: Operandos
                    DeterminarModo(linea),       // 6: Modo
                    linea.CodigoObjeto,          // 7: Cod. Objeto
                    estadoTexto                  // 8: Estado/Errores
                );

                DataGridViewRow row = dgv.Rows[rowIndex];

                bool tieneError = linea.Errores.Count > 0
                    || (!string.IsNullOrEmpty(linea.ErrorMessage)
                        && linea.ErrorMessage.Contains("Error"));

                if (tieneError)
                {
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }
        }

        // ============================================================
        // Determinar modo de direccionamiento
        // ============================================================
        private string DeterminarModo(LineaProcesada linea)
        {
            string fmt = linea.Formato;
            if (fmt == "---" || fmt == "1" || fmt == "2") return "---";
            if (string.IsNullOrEmpty(linea.Operandos))     return "---";

            string op = linea.Operandos;
            if (op.StartsWith("@"))                               return "Indirecto";
            if (op.StartsWith("#"))                               return "Inmediato";
            if (op.EndsWith(",X", StringComparison.OrdinalIgnoreCase)) return "Indexado";
            return "Simple";
        }

        // ============================================================
        // Mostrar errores en ListBox
        // ============================================================
        private void MostrarErrores(ListBox? lst)
        {
            if (lst == null) return;
            lst.Items.Clear();

            var lineas    = analizador.ObtenerLineasProcesadas();
            bool huboErr  = false;

            foreach (var linea in lineas)
            {
                if (linea.Errores.Count > 0)
                {
                    foreach (var err in linea.Errores)
                        lst.Items.Add($"Línea {linea.NumeroLinea} [{linea.Operacion}]: {err}");
                    huboErr = true;
                }
                if (!string.IsNullOrEmpty(linea.ErrorMessage)
                    && linea.ErrorMessage.Contains("Error"))
                {
                    lst.Items.Add($"Línea {linea.NumeroLinea} [{linea.Operacion}]: {linea.ErrorMessage}");
                    huboErr = true;
                }
            }

            if (!huboErr)
                lst.Items.Add("✓ Análisis finalizado: No se encontraron errores.");
        }

        // ============================================================
        // Guardar resultados
        // ============================================================
        private void GuardarResultados()
        {
            if (analizador.ObtenerLineasProcesadas().Count == 0)
            {
                MessageBox.Show("Primero debes analizar un archivo", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter   = "Archivos de texto (*.txt)|*.txt",
                Title    = "Guardar resultados del análisis",
                FileName = !string.IsNullOrEmpty(rutaArchivoActual)
                    ? Path.GetFileNameWithoutExtension(rutaArchivoActual) + "_analisis.txt"
                    : "analisis.txt"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb           = new StringBuilder();
                var lineas       = analizador.ObtenerLineasProcesadas();
                var tablaSimbolos= analizador.ObtenerTablaSimbolos();
                int inicio       = analizador.ObtenerContadorInicio();
                int final        = analizador.ObtenerContadorFinal();
                int tam          = analizador.ObtenerTamanioProgramaTotal();

                sb.AppendLine("═════════════════════════════════════════════════════════════");
                sb.AppendLine("ANÁLISIS DE ENSAMBLADOR SIC/XE");
                sb.AppendLine("═════════════════════════════════════════════════════════════\n");
                sb.AppendLine($"START: 0x{inicio:X4}   Final: 0x{final:X4}   Tamaño: {tam} bytes\n");

                sb.AppendLine("Nº | FMT | CP   | ETIQ     | INSTR       | OPERANDOS   | MODO      | COD.OBJ        | ERRORES");
                sb.AppendLine("───┼─────┼──────┼──────────┼─────────────┼─────────────┼───────────┼────────────────┼────────");

                foreach (var l in lineas)
                {
                    if (string.IsNullOrEmpty(l.Operacion)) continue;
                    string err = l.Errores.Count > 0
                        ? string.Join("; ", l.Errores)
                        : l.ErrorMessage ?? "";
                    string fmt = l.Formato == "---" ? "---" : l.Formato;
                    sb.AppendLine(
                        $"{l.NumeroLinea,-3}| {fmt,-4}| {l.Contador,-5}| {l.Etiqueta,-8} | " +
                        $"{l.Operacion,-11} | {l.Operandos,-11} | {DeterminarModo(l),-9} | " +
                        $"{l.CodigoObjeto,-14} | {err}");
                }

                if (tablaSimbolos.Count > 0)
                {
                    sb.AppendLine("\n═════════════════════════════════════════════");
                    sb.AppendLine("TABLA DE SÍMBOLOS");
                    sb.AppendLine("─────────────────┬──────────");
                    sb.AppendLine("SÍMBOLO          | DIRECCIÓN");
                    sb.AppendLine("─────────────────┼──────────");
                    foreach (var kv in tablaSimbolos.OrderBy(x => x.Value))
                        sb.AppendLine($"{kv.Key,-16} | {kv.Value}");
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"✓ Guardado en:\n{dlg.FileName}", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // Limpiar
        // ============================================================
        private void Limpiar()
        {
            var txtEntrada = this.Controls.Find("txtEntrada",    true).FirstOrDefault() as TextBox;
            var dgv        = this.Controls.Find("dgvResultados", true).FirstOrDefault() as DataGridView;
            var lstErrores = this.Controls.Find("lstErrores",    true).FirstOrDefault() as ListBox;
            var txtRuta    = this.Controls.Find("txtRutaArchivo",true).FirstOrDefault() as TextBox;

            txtEntrada?.Clear();
            dgv?.Rows.Clear();
            lstErrores?.Items.Clear();
            txtRuta?.Clear();

            analizador        = new AnalizadorSicXe();
            rutaArchivoActual = "";

            MessageBox.Show("✓ Interfaz limpiada", "Información",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}