namespace Project.WinForms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using Svg;

/// Formulario principal de la aplicación.
/// Contiene la interfaz de usuario (UI) y la lógica de interacción para
/// evaluar expresiones matemáticas usando una instancia de la clase Evaluador.
public partial class Form1 : Form
{
    /*
     para correr el forms en vs code
      dotnet run --project src/Project.WinForms/Project.WinForms.csproj
    */
    /// Instancia del evaluador que procesa expresiones.
    private Evaluador evaluador;

    /// Constructor: configura la ventana principal y crea el evaluador.
    public Form1()
    {
        InitializeComponent();
        // Título y tamaño de la ventana principal
        this.Text = "Calculadora - Evaluador de Expresiones";
        // Intentar renderizar el SVG (assets\icon.svg) y usarlo como icono.
        try
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory ?? AppContext.BaseDirectory;
            string svgPath = Path.Combine(exeDir, "assets", "icon.svg");

            if (File.Exists(svgPath))
            {
                var doc = SvgDocument.Open<SvgDocument>(svgPath);
                // Dibujar a 256x256 para alta densidad; Windows escalará según necesite
                using (var bmp = doc.Draw(256, 256))
                {
                    IntPtr hIcon = bmp.GetHicon();
                    try
                    {
                        using (Icon tmp = Icon.FromHandle(hIcon))
                        {
                            // Clonar para que el handle pueda liberarse sin perder el Icon en el Form
                            this.Icon = (Icon)tmp.Clone();
                        }
                    }
                    finally
                    {
                        // Liberar el handle nativo
                        DestroyIcon(hIcon);
                    }
                }
            }
        }
        catch
        {
            // Si falla por cualquier motivo, aqui tengo que agregar otro ico por si falla
        }

        if (this.Icon == null)
            this.Icon = System.Drawing.SystemIcons.Application;
        this.Size = new System.Drawing.Size(700, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        // Crear el evaluador que usaremos al calcular expresiones
        evaluador = new Evaluador();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    


    /// Valida la expresión ingresada, la evalúa y actualiza la interfaz con
    /// el resultado y el historial.
    private void CalcularExpresion(System.Windows.Forms.TextBox txtExpresion)
    {
        // Obtener y limpiar el texto ingresado
        string expresion = txtExpresion.Text.Trim();

        // Validación básica: no permitir expresiones vacías
        if (string.IsNullOrEmpty(expresion))
        {
            MessageBox.Show("Por favor ingresa una expresión.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            // Evaluar la expresión usando la clase Evaluador
            double resultado = evaluador.Evaluar(expresion);

            // Obtener referencias a los labels donde mostraremos datos
            var lblResultado = this.Controls[0].Controls["lblResultado"] as System.Windows.Forms.Label;
            var lblHistorial = this.Controls[0].Controls["lblHistorial"] as System.Windows.Forms.Label;

            if (lblResultado != null && lblHistorial != null)
            {
                // Formatear resultado: si es entero mostrar sin decimales,
                // si tiene parte decimal limitar a 6 y recortar ceros innecesarios.
                string resultadoFormato = resultado == Math.Floor(resultado) ? 
                    resultado.ToString("F0") : 
                    resultado.ToString("F6").TrimEnd('0').TrimEnd('.');

                // Mostrar solo el número (sin texto adicional) en el label de resultado
                lblResultado.Text = resultadoFormato;

                // Agregar el cálculo al historial (expresión = resultado) en la parte superior
                string historialActual = lblHistorial.Text;
                lblHistorial.Text = $"{expresion} = {resultadoFormato}\n{historialActual}";
                
                // Preparar la UI para la siguiente entrada
                txtExpresion.Clear();
                txtExpresion.Focus();
            }
        }
        catch (Exception ex)
        {
            // Mensaje amigable con el detalle del error para depuración
            MessageBox.Show($"Error en la expresión:\n{ex.Message}", "Error de Cálculo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

/// Evaluador de expresiones matemáticas usando análisis recursivo descendente.
/// Soporta: +, -, *, /, paréntesis, números enteros y flotantes, y unarios negativos.
public class Evaluador
{
    private string expresion;
    private int posicion;

    public Evaluador()
    {
        expresion = "";
        posicion = 0;
    }

    /// Evalúa la expresión dada y devuelve el resultado como double.
    /// "Lanza ArgumentException" si la expresión está vacía y
    /// "FormatException" si hay caracteres inesperados.
    public double Evaluar(string expr)
    {
        // Eliminar espacios para simplificar el parsing
        expresion = expr.Replace(" ", "");
        posicion = 0;

        if (string.IsNullOrWhiteSpace(expresion))
            throw new ArgumentException("Expresión vacía");

        double resultado = ParseExpresion();

        // Si no consumimos toda la cadena hay un error de formato
        if (posicion < expresion.Length)
            throw new FormatException($"Caracteres inesperados después de la expresión: '{expresion[posicion]}'");

        return resultado;
    }

    // E → T (('+' | '-') T)*
    /// Nivel de menor precedencia: suma y resta.
    private double ParseExpresion()
    {
        double resultado = ParseTermino();

        while (posicion < expresion.Length && (expresion[posicion] == '+' || expresion[posicion] == '-'))
        {
            char op = expresion[posicion++];
            double derecha = ParseTermino();
            // Aplicar la operación correspondiente
            resultado = op == '+' ? resultado + derecha : resultado - derecha;
        }

        return resultado;
    }

    // T → U (('*' | '/') U)*
    /// Nivel de multiplicación y división.
    private double ParseTermino()
    {
        double resultado = ParseUnaria();

        while (posicion < expresion.Length && (expresion[posicion] == '*' || expresion[posicion] == '/'))
        {
            char op = expresion[posicion++];
            double derecha = ParseUnaria();
            
            if (op == '*')
                resultado *= derecha;
            else
            {
                // Comprobar división por cero y lanzar excepción descriptiva
                if (derecha == 0)
                    throw new DivideByZeroException("División por cero");
                resultado /= derecha;
            }
        }

        return resultado;
    }

    // U → '-' U | F
    /// Maneja el operador unario negativo.
    private double ParseUnaria()
    {
        if (posicion < expresion.Length && expresion[posicion] == '-')
        {
            posicion++;
            // Aplicar unario negativo de forma recursiva (soporta varios '-').
            return -ParseUnaria();
        }

        return ParseFactor();
    }

    // F → '(' E ')' | NUMBER
    /// Factor: paréntesis o número literal.
    private double ParseFactor()
    {
        if (posicion < expresion.Length && expresion[posicion] == '(')
        {
            posicion++; // consume '('
            double resultado = ParseExpresion();

            if (posicion >= expresion.Length || expresion[posicion] != ')')
                throw new FormatException("Paréntesis no cerrado");

            posicion++; // consume ')'
            return resultado;
        }

        return ParseNumero();
    }

    /// Lee un número (entero o flotante) desde la posición actual.
    private double ParseNumero()
    {
        int inicio = posicion;

        // Leer dígitos antes del punto decimal (si existen)
        while (posicion < expresion.Length && char.IsDigit(expresion[posicion]))
            posicion++;

        // Si hay punto, leer la parte fraccionaria
        if (posicion < expresion.Length && expresion[posicion] == '.')
        {
            posicion++;
            while (posicion < expresion.Length && char.IsDigit(expresion[posicion]))
                posicion++;
        }

        // Validar que se haya leído al menos un dígito válido
        if (posicion == inicio || (posicion == inicio + 1 && expresion[inicio] == '.'))
            throw new FormatException($"Número inválido en posición {posicion}");

        string numeroStr = expresion.Substring(inicio, posicion - inicio);
        
        if (!double.TryParse(numeroStr, out double numero))
            throw new FormatException($"No se puede parsear '{numeroStr}' como número");

        return numero;
    }
}