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

        // Método para conectar los botones con sus eventos
        private void ConfigurarEventos()
        {
            var btnAbrir = this.Controls.Find("btnAbrir", true).FirstOrDefault() as Button;
            var btnAnalizar = this.Controls.Find("btnAnalizar", true).FirstOrDefault() as Button;
            var btnGuardar = this.Controls.Find("btnGuardar", true).FirstOrDefault() as Button;
            var btnLimpiar = this.Controls.Find("btnLimpiar", true).FirstOrDefault() as Button;

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

            if (openFile.ShowDialog() == DialogResult.OK) //mostramos el dialog y esperamos respuesta del usuario
            {
                rutaArchivoActual = openFile.FileName;
                // buscamos el TextBox para mostrar la ruta del archivo y lo actualizamos
                var txtRuta = this.Controls.Find("txtRutaArchivo", true).FirstOrDefault() as TextBox;
                if (txtRuta != null)
                    txtRuta.Text = rutaArchivoActual;
                
                try
                {
                    //leemos el contenido del archivo y lo mostramos en el TextBox de entrada
                    string contenido = File.ReadAllText(rutaArchivoActual, Encoding.UTF8);
                    var txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
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
            //jalo el texto del TextBox de entrada para analizarlo, si no hay texto muestro un mensaje de advertencia
            var txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
            if (txtEntrada == null || string.IsNullOrWhiteSpace(txtEntrada.Text))
            {
                MessageBox.Show("Por favor ingrese código o cargue un archivo", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                //encontramos el DataGridView y el ListBox para mostrar resultados y errores, y los limpiamos antes de analizar
                var dgv = this.Controls.Find("dgvResultados", true).FirstOrDefault() as DataGridView;
                var lstErrores = this.Controls.Find("lstErrores", true).FirstOrDefault() as ListBox;
                
                if (dgv != null) dgv.Rows.Clear();
                if (lstErrores != null) lstErrores.Items.Clear();

                //llamo al analizador para procesar el texto del TextBox de entrada
                analizador.Analizar(txtEntrada.Text);

                MostrarResultadosEnTabla(dgv);
                MostrarErrores(lstErrores);

                //muestro un mensaje indicando si el análisis se completó con o sin errores
                if (!analizador.HayErrores())
                {
                    MessageBox.Show(" Análisis completado sin errores", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($" Análisis completado con {analizador.ObtenerErrores().Count} errores", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el análisis: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MostrarResultadosEnTabla(DataGridView? dgv)
        {
            if (dgv == null)//si no encuentro el DataGridView, salgo del método
                return;

            var lineas = analizador.ObtenerLineasProcesadas(); //obtengo las líneas procesadas del analizador

            foreach (var linea in lineas)  //recorro cada línea procesada para mostrarla en la tabla
            {
                if (string.IsNullOrEmpty(linea.Operacion))//en caso de ser una línea vacía o sin operación, la salto y no la muestro en la tabla
                    continue;

                //checo el formato para imprimirlo en tabla
                string formato = DeterminarFormato(linea.Operacion); 

                //agrego una nueva fila a la tabla con los datos de la línea procesada
                int rowIndex = dgv.Rows.Add(
                    linea.NumeroLinea,
                    linea.Contador,
                    linea.Etiqueta,
                    linea.Operacion,
                    linea.Operandos,
                    formato,
                    linea.Valida ? "Correcto" : "Error"//imprimo correct solo para no dejarlo vacio, pero lo puedo quitar
                );

                //cambio el color de fondo de la fila si es correcto o incorrecto
                DataGridViewRow row = dgv.Rows[rowIndex];
                if (linea.Valida)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    row.DefaultCellStyle.ForeColor = Color.DarkGreen;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                }

                //solo en caso de errores
                if (linea.Errores.Count > 0)
                {
                    string erroresTexto = string.Join("\n", linea.Errores);
                    row.Cells["Estado"].ToolTipText = erroresTexto;
                }
            }
        }

        //aqui checamos el formato de la operación para mostrarlo en la tabla
        private string DeterminarFormato(string operacion)
        {
            operacion = operacion.ToUpper();
            if (operacion.StartsWith("+"))
                operacion = operacion.Substring(1);

            if (new[] { "FIX", "FLOAT", "HIO", "NORM", "SIO", "TIO" }.Contains(operacion))
                return "1";

            if (new[] { "ADDR", "CLEAR", "COMPR", "DIVR", "MULR", "RMO", "SHIFTL", "SUBR", "SVC", "TIXR" }.Contains(operacion))
                return "2";

            if (new[] { "ADD", "ADDF", "AND", "COMP", "COMPF", "DIV", "DIVF", "J", "JEQ", "JGT", "JLT", "JSUB", 
                       "LDA", "LDB", "LDCH", "LDF", "LDL", "LDS", "LDT", "LDX", "LPS", "MUL", "MULF", "OR", 
                       "RD", "RSUB", "SSK", "STA", "STB", "STCH", "STF", "STI", "STL", "STS", "STSW", "STT", 
                       "STX", "SUB", "SUBF", "TD", "TIX", "WD" }.Contains(operacion))
                return "3/4";

            if (new[] { "START", "END", "BASE", "BYTE", "RESB", "RESW", "WORD" }.Contains(operacion))
                return "Directiva";

            return "?"; // quitaar esto :p
        }

        //mando un mensaje en un listbox 
        private void MostrarErrores(ListBox? lst)
        {
            if (lst == null)//si la lista de errores no existe, salgo del método
                return;

            //jalo los errores del analizador para checar si hay
            var errores = analizador.ObtenerErrores();

            if (errores.Count == 0)
            {
                lst.Items.Add("Sin errores");
                return;
            }

            //imprimo los errores
            foreach (var error in errores)
            {
                lst.Items.Add(error);
            }
        }

        private void GuardarResultados()
        {
            //esto es para evitar que el usuario intente guardar sin haber analizado un archivo
            if (analizador.ObtenerLineasProcesadas().Count == 0)
            {
                MessageBox.Show("Primero debes analizar un archivo", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //muestro un SaveFileDialog para que el usuario elija dónde guardar el archivo de resultados, con un nombre sugerido basado en el archivo actual
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
                    //necesito un string builder construir el texto
                    var sb = new StringBuilder();
                    //obtengo las lineas analizadas
                    var lineas = analizador.ObtenerLineasProcesadas();
                    var errores = analizador.ObtenerErrores();
                    var tablaSimbolos = analizador.ObtenerTablaSimbolos();

                    //armo una estructura de archivo bonitasss
                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════════════");
                    sb.AppendLine("ANÁLISIS DE ENSAMBLADOR SIC/XE");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════════════\n");

                    // imprimo tipo tabla tambien, pero si quiere solo texto lo puedo quitar
                    sb.AppendLine("Nº LÍNEA | CONTADOR | ETIQUETA | INSTRUCCIÓN | OPERANDOS | FORMATO | ESTADO");
                    sb.AppendLine("─────────┼──────────┼──────────┼─────────────┼───────────┼─────────┼─────────");

                    //recorremos por linea y lo agregamos
                    foreach (var linea in lineas)
                    {
                        if (string.IsNullOrEmpty(linea.Operacion))
                            continue;

                        string formato = DeterminarFormato(linea.Operacion);
                        string estado = linea.Valida ? "Correcto" : "Error";
                        
                        //agregamos la linea formateada, tengo que cambiarlo se ve feo.
                        sb.AppendLine($"{linea.NumeroLinea,-7} | {linea.Contador,-8} | {linea.Etiqueta,-8} | {linea.Operacion,-11} | {linea.Operandos,-9} | {formato,-7} | {estado,-7}");
                        
                        //un append extra para los errores que saque de internet, pero se ve feo, lo puedo mejorar
                        if (linea.Errores.Count > 0)
                        {
                            foreach (var error in linea.Errores)
                                sb.AppendLine($"  └─ {error}");
                        }
                    }

                    //imprimimos la tabla de simbolos
                    if (tablaSimbolos.Count > 0)
                    {
                        sb.AppendLine("\n═══════════════════════════════════════════════════════════════════════════════════════");
                        sb.AppendLine("TABLA DE SÍMBOLOS");
                        sb.AppendLine("─────────────────────────────────────────────────────────────────────────────────────");
                        sb.AppendLine("SÍMBOLO          | DIRECCIÓN");
                        sb.AppendLine("─────────────────┼──────────");

                        //agregamos cada simbolo ordenado por direccion
                        foreach (var simbolo in tablaSimbolos.OrderBy(x => x.Value))
                        {
                            sb.AppendLine($"{simbolo.Key,-16} | {simbolo.Value,-8}");
                        }
                    }

                    //esto fue agregado a peticion de uno de mis compañeros de salon para checar un overview general
                    sb.AppendLine("\n═══════════════════════════════════════════════════════════════════════════════════════");
                    sb.AppendLine($"RESUMEN: Total {lineas.Count} líneas, {errores.Count} errores");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════════════");

                    //ahora si lo guardamos en el archivo seleccionado
                    File.WriteAllText(saveFile.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Resultados guardados en:\n{saveFile.FileName}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Limpiar()
        {
            var txtEntrada = this.Controls.Find("txtEntrada", true).FirstOrDefault() as TextBox;
            var dgv = this.Controls.Find("dgvResultados", true).FirstOrDefault() as DataGridView;
            var lstErrores = this.Controls.Find("lstErrores", true).FirstOrDefault() as ListBox;
            var txtRuta = this.Controls.Find("txtRutaArchivo", true).FirstOrDefault() as TextBox;

            //limpiamos los controles
            if (txtEntrada != null) txtEntrada.Clear();
            if (dgv != null) dgv.Rows.Clear();
            if (lstErrores != null) lstErrores.Items.Clear();
            if (txtRuta != null) txtRuta.Clear();
            //reinicializamos el analizador y la ruta del archivo actual
            analizador = new AnalizadorSicXe();
            rutaArchivoActual = "";
        }
    }
}