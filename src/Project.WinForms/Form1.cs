namespace Project.WinForms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
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
            // Si falla por cualquier motivo, aquí tengo que agregar otro ico por si falla
        }

        if (this.Icon == null)
            this.Icon = System.Drawing.SystemIcons.Application;
        this.Size = new System.Drawing.Size(700, 650); // Aumentado para el TreeView
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
            // Evaluar la expresión usando la clase Evaluador (ahora devuelve ResultadoEvaluacion)
            ResultadoEvaluacion resultado = evaluador.Evaluar(expresion);

            // Obtener referencias a los controles
            var lblResultado = this.Controls[0].Controls["lblResultado"] as System.Windows.Forms.Label;
            var lblHistorial = this.Controls[0].Controls["lblHistorial"] as System.Windows.Forms.Label;
            var treeArbol = this.Controls[0].Controls["treeArbolSintactico"] as System.Windows.Forms.TreeView;

            if (lblResultado != null && lblHistorial != null && treeArbol != null)
            {
                // Formatear resultado
                string resultadoFormato = resultado.Valor == Math.Floor(resultado.Valor) ? 
                    resultado.Valor.ToString("F0") : 
                    resultado.Valor.ToString("F6").TrimEnd('0').TrimEnd('.');

                // Mostrar resultado
                lblResultado.Text = resultadoFormato;

                // Actualizar árbol sintáctico
                treeArbol.Nodes.Clear();
                if (resultado.ArbolSintactico != null)
                {
                    TreeNode nodoRaiz = ConstruirNodoArbol(resultado.ArbolSintactico);
                    treeArbol.Nodes.Add(nodoRaiz);
                    treeArbol.ExpandAll();
                }

                // Agregar al historial con info de variables asignadas
                string historialActual = lblHistorial.Text;
                string infoVariables = "";
                if (resultado.VariablesAsignadas.Count > 0)
                {
                    var asignaciones = resultado.VariablesAsignadas.Select(v => $"{v.Key}={v.Value:F6}".TrimEnd('0').TrimEnd('.'));
                    infoVariables = $" ({string.Join(", ", asignaciones)})";
                }
                lblHistorial.Text = $"{expresion} = {resultadoFormato}{infoVariables}\n{historialActual}";
                
                // Preparar para siguiente entrada
                txtExpresion.Clear();
                txtExpresion.Focus();
            }
        }
        catch (ErrorSintactico ex)
        {
            // Error con posición exacta
            string mensajeError = "ERROR DE SINTAXIS:\n\n";
            mensajeError += ex.Message + "\n\n";
            mensajeError += expresion + "\n";
            mensajeError += new string(' ', ex.Posicion) + "^\n";
            mensajeError += new string(' ', ex.Posicion) + "└── Aquí está el error";
            
            MessageBox.Show(mensajeError, "Error de Sintaxis", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            // Posicionar cursor en el error
            if (ex.Posicion < txtExpresion.Text.Length)
            {
                txtExpresion.Select(ex.Posicion, 1);
                txtExpresion.Focus();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error en la expresión:\n{ex.Message}", "Error de Cálculo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// Construye un TreeNode a partir de un nodo del AST
    private TreeNode ConstruirNodoArbol(NodoAST nodo)
    {
        TreeNode treeNode = new TreeNode();

        if (nodo is NodoNumero numeroNodo)
        {
            treeNode.Text = $"Número: {numeroNodo.Valor}";
            treeNode.ForeColor = Color.DarkBlue;
        }
        else if (nodo is NodoVariable variableNodo)
        {
            treeNode.Text = $"Variable: {variableNodo.Nombre}";
            treeNode.ForeColor = Color.DarkGreen;
        }
        else if (nodo is NodoOperacionBinaria binarioNodo)
        {
            treeNode.Text = $"Operación: {binarioNodo.Operador}";
            treeNode.ForeColor = Color.DarkRed;
            treeNode.Nodes.Add(ConstruirNodoArbol(binarioNodo.Izquierdo));
            treeNode.Nodes.Add(ConstruirNodoArbol(binarioNodo.Derecho));
        }
        else if (nodo is NodoOperacionUnaria unarioNodo)
        {
            treeNode.Text = $"Unario: {unarioNodo.Operador}";
            treeNode.ForeColor = Color.DarkOrange;
            treeNode.Nodes.Add(ConstruirNodoArbol(unarioNodo.Operando));
        }
        else if (nodo is NodoAsignacion asignacionNodo)
        {
            treeNode.Text = $"Asignación: {asignacionNodo.NombreVariable}";
            treeNode.ForeColor = Color.DarkMagenta;
            treeNode.Nodes.Add(ConstruirNodoArbol(asignacionNodo.Expresion));
        }

        return treeNode;
    }
}

// error sintáctico con posición exacta
public class ErrorSintactico : Exception
{
    public int Posicion { get; set; }
    
    public ErrorSintactico(string mensaje, int posicion) : base(mensaje)
    {
        Posicion = posicion;
    }
}

// ==================== RESULTADO DE EVALUACIÓN ====================
public class ResultadoEvaluacion
{
    public double Valor { get; set; }
    public NodoAST ArbolSintactico { get; set; }
    public Dictionary<string, double> VariablesAsignadas { get; set; }
    
    public ResultadoEvaluacion()
    {
        VariablesAsignadas = new Dictionary<string, double>();
    }
}

//nodos del AST para representar números, variables, operaciones binarias, unarias y asignaciones
public abstract class NodoAST
{
    public abstract double Evaluar(Dictionary<string, double> variables, Dictionary<string, double> asignadas);
}

// Nodo para representar un número literal
public class NodoNumero : NodoAST
{
    public double Valor { get; set; }
    
    public NodoNumero(double valor)
    {
        Valor = valor;
    }
    
    public override double Evaluar(Dictionary<string, double> variables, Dictionary<string, double> asignadas)
    {
        return Valor;
    }
}

//nodo para representar una variable (su valor se obtiene del diccionario de variables)
public class NodoVariable : NodoAST
{
    public string Nombre { get; set; }
    
    public NodoVariable(string nombre)
    {
        Nombre = nombre;
    }
    
    public override double Evaluar(Dictionary<string, double> variables, Dictionary<string, double> asignadas)
    {
        if (!variables.ContainsKey(Nombre))
        {
            throw new Exception($"Variable '{Nombre}' no está definida");
        }
        return variables[Nombre];
    }
}

// Nodo para representar una operación binaria (suma, resta, multiplicación, división)
public class NodoOperacionBinaria : NodoAST
{
    public NodoAST Izquierdo { get; set; }
    public NodoAST Derecho { get; set; }
    public char Operador { get; set; }
    
    public NodoOperacionBinaria(NodoAST izq, char op, NodoAST der)
    {
        Izquierdo = izq;
        Operador = op;
        Derecho = der;
    }
    
    public override double Evaluar(Dictionary<string, double> variables, Dictionary<string, double> asignadas)
    {
        double izq = Izquierdo.Evaluar(variables, asignadas);
        double der = Derecho.Evaluar(variables, asignadas);
        
        return Operador switch
        {
            '+' => izq + der,
            '-' => izq - der,
            '*' => izq * der,
            '/' => der == 0 ? throw new DivideByZeroException("División por cero") : izq / der,
            _ => throw new Exception($"Operador desconocido: {Operador}")
        };
    }
}

//nodo para representar una operación unaria (negación)
public class NodoOperacionUnaria : NodoAST
{
    public NodoAST Operando { get; set; }
    public char Operador { get; set; }
    
    public NodoOperacionUnaria(char op, NodoAST operando)
    {
        Operador = op;
        Operando = operando;
    }
    
    public override double Evaluar(Dictionary<string, double> variables, Dictionary<string, double> asignadas)
    {
        double valor = Operando.Evaluar(variables, asignadas);
        return Operador == '-' ? -valor : valor;
    }
}

//nodo para representar una asignación de variable (ej: x = 5 + 3)
public class NodoAsignacion : NodoAST
{
    public string NombreVariable { get; set; }
    public NodoAST Expresion { get; set; }
    
    public NodoAsignacion(string nombre, NodoAST expresion)
    {
        NombreVariable = nombre;
        Expresion = expresion;
    }
    
    public override double Evaluar(Dictionary<string, double> variables, Dictionary<string, double> asignadas)
    {
        double valor = Expresion.Evaluar(variables, asignadas);
        variables[NombreVariable] = valor;
        asignadas[NombreVariable] = valor; // Registrar asignación
        return valor;
    }
}

/// Evaluador de expresiones matemáticas con análisis léxico y sintáctico.
/// Soporta: +, -, *, /, paréntesis, números (enteros y decimales), variables y asignaciones.
/// Las asignaciones pueden aparecer en cualquier lugar de la expresión.
public class Evaluador
{
    private string expresion;
    private int posicion;
    private Dictionary<string, double> variables;

    public Evaluador()
    {
        expresion = "";
        posicion = 0;
        variables = new Dictionary<string, double>();
    }

    /// Evalúa la expresión y devuelve el resultado con el árbol sintáctico
    public ResultadoEvaluacion Evaluar(string expr)
    {
        // Eliminar espacios
        expresion = expr.Replace(" ", "");
        posicion = 0;

        if (string.IsNullOrWhiteSpace(expresion))
            throw new ArgumentException("Expresión vacía");

        // Rastrear variables asignadas en esta evaluación
        var asignadas = new Dictionary<string, double>();

        // Construir árbol sintáctico
        NodoAST arbol = ParseExpresion();

        // Verificar que se consumió toda la expresión
        if (posicion < expresion.Length)
        {
            throw new ErrorSintactico(
                $"Caracteres inesperados: '{expresion[posicion]}'",
                posicion
            );
        }

        // Evaluar
        double resultado = arbol.Evaluar(variables, asignadas);

        return new ResultadoEvaluacion
        {
            Valor = resultado,
            ArbolSintactico = arbol,
            VariablesAsignadas = asignadas
        };
    }

    // E → T (('+' | '-') T)*
    private NodoAST ParseExpresion()
    {
        NodoAST izquierdo = ParseTermino();

        while (posicion < expresion.Length && (expresion[posicion] == '+' || expresion[posicion] == '-'))
        {
            char op = expresion[posicion++];
            NodoAST derecho = ParseTermino();
            izquierdo = new NodoOperacionBinaria(izquierdo, op, derecho);
        }

        return izquierdo;
    }

    // T → U (('*' | '/') U)*
    private NodoAST ParseTermino()
    {
        NodoAST izquierdo = ParseUnaria();

        while (posicion < expresion.Length && (expresion[posicion] == '*' || expresion[posicion] == '/'))
        {
            char op = expresion[posicion++];
            NodoAST derecho = ParseUnaria();
            izquierdo = new NodoOperacionBinaria(izquierdo, op, derecho);
        }

        return izquierdo;
    }

    // U → '-' U | '+' U | F
    private NodoAST ParseUnaria()
    {
        if (posicion < expresion.Length && (expresion[posicion] == '-' || expresion[posicion] == '+'))
        {
            char op = expresion[posicion++];
            return new NodoOperacionUnaria(op, ParseUnaria());
        }

        return ParsePrimario();
    }

    // F → '(' E ')' | '(' ASIGNACIÓN ')' | ASIGNACIÓN | NÚMERO | IDENTIFICADOR
    private NodoAST ParsePrimario()
    {
        // Paréntesis - puede contener expresión o asignación
        if (posicion < expresion.Length && expresion[posicion] == '(')
        {
            posicion++; // consumir '('
            
            // Verificar si es una asignación dentro de paréntesis
            if (posicion < expresion.Length && char.IsLetter(expresion[posicion]))
            {
                int posTemp = posicion;
                string id = LeerIdentificador();
                
                if (posicion < expresion.Length && expresion[posicion] == '=')
                {
                    // Es una asignación: (x = expr)
                    posicion++; // consumir '='
                    NodoAST valorExpresion = ParseExpresion();
                    
                    if (posicion >= expresion.Length || expresion[posicion] != ')')
                    {
                        throw new ErrorSintactico("Paréntesis no cerrado", posicion);
                    }
                    posicion++; // consumir ')'
                    
                    return new NodoAsignacion(id, valorExpresion);
                }
                else
                {
                    // No es asignación, retroceder y parsear como expresión normal
                    posicion = posTemp;
                }
            }
            
            // Parsear como expresión normal
            NodoAST nodo = ParseExpresion();

            if (posicion >= expresion.Length || expresion[posicion] != ')')
            {
                throw new ErrorSintactico("Paréntesis no cerrado", posicion);
            }

            posicion++; // consumir ')'
            return nodo;
        }

        // Identificador - puede ser variable o asignación
        if (posicion < expresion.Length && char.IsLetter(expresion[posicion]))
        {
            int posInicial = posicion;
            string identificador = LeerIdentificador();
            
            // Verificar si es una asignación (sin paréntesis)
            if (posicion < expresion.Length && expresion[posicion] == '=')
            {
                posicion++; // consumir '='
                NodoAST valorExpresion = ParseExpresion();
                return new NodoAsignacion(identificador, valorExpresion);
            }
            
            // Es solo una variable
            return new NodoVariable(identificador);
        }

        // Número
        if (posicion < expresion.Length && (char.IsDigit(expresion[posicion]) || expresion[posicion] == '.'))
        {
            return new NodoNumero(ParseNumero());
        }

        throw new ErrorSintactico(
            $"Se esperaba un número, variable o '(', pero se encontró '{(posicion < expresion.Length ? expresion[posicion].ToString() : "fin de expresión")}'",
            posicion
        );
    }

    private string LeerIdentificador()
    {
        int inicio = posicion;
        
        while (posicion < expresion.Length && (char.IsLetterOrDigit(expresion[posicion]) || expresion[posicion] == '_'))
        {
            posicion++;
        }

        if (posicion == inicio)
        {
            throw new ErrorSintactico("Identificador esperado", posicion);
        }

        return expresion.Substring(inicio, posicion - inicio);
    }

    private double ParseNumero()
    {
        int inicio = posicion;

        // Leer dígitos antes del punto
        while (posicion < expresion.Length && char.IsDigit(expresion[posicion]))
            posicion++;

        // Si hay punto decimal
        if (posicion < expresion.Length && expresion[posicion] == '.')
        {
            posicion++;
            
            // Debe haber al menos un dígito después del punto
            if (posicion >= expresion.Length || !char.IsDigit(expresion[posicion]))
            {
                throw new ErrorSintactico("Número decimal inválido (falta dígito después del punto)", posicion);
            }
            
            while (posicion < expresion.Length && char.IsDigit(expresion[posicion]))
                posicion++;
        }

        // Validar que se leyó algo
        if (posicion == inicio || (posicion == inicio + 1 && expresion[inicio] == '.'))
        {
            throw new ErrorSintactico($"Número inválido", posicion);
        }

        string numeroStr = expresion.Substring(inicio, posicion - inicio);
        
        if (!double.TryParse(numeroStr, out double numero))
        {
            throw new ErrorSintactico($"No se puede convertir '{numeroStr}' a número", inicio);
        }

        return numero;
    }
}