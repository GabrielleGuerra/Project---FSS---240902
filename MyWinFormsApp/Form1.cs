using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace MyWinFormsApp
{
    // ================================================================
    //  TOKEN
    //  Tipos:
    //   0  = desconocido
    //   1  = número decimal
    //   2  = número hexadecimal (ej. 1AH)
    //   3  = RSUB
    //   4  = literal X'...'
    //   5  = literal C'...'
    //   6  = +RSUB
    //   7  = directiva START
    //   8  = directiva END
    //   9  = directiva BASE
    //  10  = directiva RESW
    //  11  = directiva RESB
    //  12  = directiva WORD
    //  13  = directiva BYTE
    //  14  = registro (A X L B S T F PC SW)
    //  15  = instrucción formato 1
    //  16  = instrucción formato 2 variante A (2 regs)
    //  17  = instrucción formato 2 variante B (1 reg)
    //  18  = instrucción formato 2 variante C (SHIFTL/SHIFTR)
    //  19  = instrucción formato 2 variante D (SVC)
    //  20  = instrucción formato 3
    //  21  = instrucción formato 4 (+INS)
    //  22  = coma
    //  23  = símbolo ordinario
    //  24  = operador @ o #
    //  25  = etiqueta al inicio de línea
    //  26  = ,X indexado
    //  27  = símbolo con @
    //  28  = símbolo con #
    //  29  = símbolo operando simple
    //  30  = directiva ORG
    //  31  = directiva EQU
    //  32  = expresión/operando compuesto (EQU / WORD)
    //  33  = directiva USE
    //  34  = directiva CSECT
    //  35  = directiva EXTDEF
    //  36  = directiva EXTREF
    // ================================================================

    public class SicToken
    {
        public int Type { get; }
        public string Text { get; }
        public SicToken(int type, string text) { Type = type; Text = text; }
        public override string ToString() => $"[{Type}] {Text}";
    }

    // ================================================================
    //  LEXER
    // ================================================================
    public static class SicLexer
    {
        static readonly HashSet<string> Fmt1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "FIX","FLOAT","HIO","NORM","SIO","TIO" };

        static readonly HashSet<string> Fmt2A = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "ADDR","COMPR","DIVR","MULR","RMO","SUBR" };
        static readonly HashSet<string> Fmt2B = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "CLEAR","TIXR" };
        static readonly HashSet<string> Fmt2C = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "SHIFTL","SHIFTR" };
        static readonly HashSet<string> Fmt2D = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "SVC" };

        static readonly HashSet<string> Fmt3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ADD","ADDF","AND","COMP","COMPF","DIV","DIVF","J","JEQ","JGT",
            "JLT","JSUB","LDA","LDB","LDCH","LDF","LDL","LDS","LDT","LDX",
            "LPS","MUL","MULF","OR","RD","RSUB","SSK","STA","STB","STCH",
            "STF","STI","STL","STS","STSW","STT","STX","SUB","SUBF","TD",
            "TIX","WD"
        };

        static readonly HashSet<string> Directives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "START","END","BASE","RESW","RESB","WORD","BYTE", "ORG", "EQU", "USE",
              "CSECT","EXTDEF","EXTREF" };

        static readonly HashSet<string> Regs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "A","X","L","B","S","T","F","PC","SW" };

        public static List<SicToken> Tokenize(string line)
        {
            var result = new List<SicToken>();
            if (string.IsNullOrWhiteSpace(line)) return result;
            if (line.TrimStart().StartsWith(".")) return result;

            var parts = SplitLine(line);
            if (parts.Count == 0) return result;

            int idx = 0;

            // ---- Etiqueta al inicio (tipo 25) ----
            string first = parts[0];
            if (!IsInstOrDir(first) && !first.StartsWith("+"))
            {
                result.Add(new SicToken(25, first));
                idx = 1;
            }

            if (idx >= parts.Count) return result;

            // ---- Instrucción / directiva ----
            string ins = parts[idx];
            idx++;
            bool isPlus = ins.StartsWith("+");
            string insCore = isPlus ? ins.Substring(1) : ins;

            if (ins.Equals("RSUB", StringComparison.OrdinalIgnoreCase))
                result.Add(new SicToken(3, ins.ToUpper()));
            else if (ins.Equals("+RSUB", StringComparison.OrdinalIgnoreCase))
                result.Add(new SicToken(6, ins.ToUpper()));
            else if (isPlus && Fmt3.Contains(insCore))
                result.Add(new SicToken(21, ins.ToUpper()));
            else if (Fmt1.Contains(insCore))
                result.Add(new SicToken(15, ins.ToUpper()));
            else if (Fmt2A.Contains(insCore))
                result.Add(new SicToken(16, ins.ToUpper()));
            else if (Fmt2B.Contains(insCore))
                result.Add(new SicToken(17, ins.ToUpper()));
            else if (Fmt2C.Contains(insCore))
                result.Add(new SicToken(18, ins.ToUpper()));
            else if (Fmt2D.Contains(insCore))
                result.Add(new SicToken(19, ins.ToUpper()));
            else if (Fmt3.Contains(insCore))
                result.Add(new SicToken(20, ins.ToUpper()));
            else if (Directives.Contains(insCore))
                result.Add(new SicToken(DirectiveType(insCore), ins.ToUpper()));
            else
                result.Add(new SicToken(0, ins));

            // ---- Operandos ----
            if (idx < parts.Count)
            {
                string operandRaw = string.Join(" ", parts.Skip(idx));
                int dotIdx = FindCommentDot(operandRaw);
                if (dotIdx >= 0) operandRaw = operandRaw.Substring(0, dotIdx).Trim();
                if (!string.IsNullOrWhiteSpace(operandRaw))
                {
                    bool esEQU = result.Any(tk => tk.Type == 31);
                    bool esWORD = result.Any(tk => tk.Type == 12);
                    bool esUSE = result.Any(tk => tk.Type == 33);
                    bool esEXTDEF = result.Any(tk => tk.Type == 35);
                    bool esEXTREF = result.Any(tk => tk.Type == 36);
                    if (esEQU || esWORD || esUSE || esEXTDEF || esEXTREF)
                        result.Add(new SicToken(32, operandRaw.Replace(" ", "")));
                    else
                        TokenizeOperand(operandRaw, result);
                }
            }

            return result;
        }

        static List<string> SplitLine(string line)
        {
            var parts = new List<string>();
            int i = 0;
            while (i < line.Length)
            {
                while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
                if (i >= line.Length) break;
                if (line[i] == '.') break;

                // Detectar literal X'...' o C'...'
                if (i + 1 < line.Length &&
                    (line[i] == 'X' || line[i] == 'x' || line[i] == 'C' || line[i] == 'c') &&
                    line[i + 1] == '\'')
                {
                    int end = line.IndexOf('\'', i + 2);
                    if (end >= 0)
                    {
                        parts.Add(line.Substring(i, end - i + 1));
                        i = end + 1;
                        continue;
                    }
                }

                int start = i;
                while (i < line.Length && line[i] != ' ' && line[i] != '\t') i++;
                string tok = line.Substring(start, i - start);
                if (tok.StartsWith(".")) break;
                parts.Add(tok);
            }
            return parts;
        }

        static int FindCommentDot(string s)
        {
            bool inLiteral = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\'') inLiteral = !inLiteral;
                if (!inLiteral && s[i] == '.') return i;
            }
            return -1;
        }

        static bool IsInstOrDir(string s)
        {
            if (s.StartsWith("+")) s = s.Substring(1);
            return Fmt1.Contains(s) || Fmt2A.Contains(s) || Fmt2B.Contains(s) ||
                   Fmt2C.Contains(s) || Fmt2D.Contains(s) || Fmt3.Contains(s) ||
                   Directives.Contains(s) ||
                   s.Equals("RSUB", StringComparison.OrdinalIgnoreCase);
        }

        static int DirectiveType(string d)
        {
            switch (d.ToUpper())
            {
                case "START": return 7;
                case "END": return 8;
                case "BASE": return 9;
                case "RESW": return 10;
                case "RESB": return 11;
                case "WORD": return 12;
                case "BYTE": return 13;
                case "ORG": return 30; /// -------------------Nuevas Directivas-------------------
                case "EQU": return 31;
                case "USE": return 33;
                case "CSECT": return 34;
                case "EXTDEF": return 35;
                case "EXTREF": return 36;
                default: return 0;
            }
        }

        static void TokenizeOperand(string raw, List<SicToken> result)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;

            // Literal X'...' o C'...'
            if ((raw.StartsWith("X'", StringComparison.OrdinalIgnoreCase) ||
                 raw.StartsWith("C'", StringComparison.OrdinalIgnoreCase)) &&
                raw.EndsWith("'"))
            {
                int t = raw.ToUpper().StartsWith("X'") ? 4 : 5;
                result.Add(new SicToken(t, raw));
                return;
            }

            // Prefijo @ o #
            string prefix = "";
            string body = raw;
            if (raw.StartsWith("@") || raw.StartsWith("#"))
            {
                prefix = raw.Substring(0, 1);
                body = raw.Substring(1);
            }

            // Sufijo ,X
            bool indexado = body.EndsWith(",X", StringComparison.OrdinalIgnoreCase);
            if (indexado) body = body.Substring(0, body.Length - 2);

            // Número decimal
            if (int.TryParse(body, out _))
            {
                result.Add(new SicToken(1, prefix + body + (indexado ? ",X" : "")));
                return;
            }

            // Número hex (termina en H)
            if (body.Length > 1 &&
                body.EndsWith("H", StringComparison.OrdinalIgnoreCase) &&
                IsHex(body.Substring(0, body.Length - 1)))
            {
                result.Add(new SicToken(2, prefix + body + (indexado ? ",X" : "")));
                return;
            }

            // Par de registros ej: A,S
            if (raw.Contains(",") && !raw.EndsWith(",X", StringComparison.OrdinalIgnoreCase))
            {
                string[] regs = raw.Split(',');
                if (regs.All(r => Regs.Contains(r.Trim())))
                {
                    foreach (var r in regs)
                        result.Add(new SicToken(14, r.Trim().ToUpper()));
                    return;
                }
            }

            // Registro individual
            if (Regs.Contains(body))
            {
                result.Add(new SicToken(14, body.ToUpper()));
                if (indexado) result.Add(new SicToken(26, ",X"));
                return;
            }

            // Símbolo operando
            int symType = prefix == "@" ? 27 : prefix == "#" ? 28 : 29;
            result.Add(new SicToken(symType, prefix + body + (indexado ? ",X" : "")));
        }

        public static bool IsHex(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.All(c => "0123456789ABCDEFabcdef".Contains(c));
        }
    }

    //  ERROR INFO -----------------------------------------------------------------------------------------------------------------------
    public class SyntaxErrorInfo
    {
        public int Line { get; }
        public int Column { get; }
        public string Message { get; }
        public SyntaxErrorInfo(int line, int column, string message)
        { Line = line; Column = column; Message = message; }
        public override string ToString() => $"Línea {Line}, Columna {Column}: {Message}";
    }

    public class MyErrorListener
    {
        public List<SyntaxErrorInfo> ErrorList { get; } = new List<SyntaxErrorInfo>();
        public bool HasErrors => ErrorList.Count > 0;
        public string ErrorMessages => string.Join("\n", ErrorList.Select(e => $"• {e}"));

        public void AddError(int line, int col, string msg)
        {
            ErrorList.Add(new SyntaxErrorInfo(line, col, msg));
            Form1.ListaErrores.Add(line + ": " + msg);
        }
    }


    //  VALIDADOR -----------------------------------------------------------------------------------------------------------------------

    public static class SicValidator
    {
        static readonly HashSet<int> InsTypes = new HashSet<int>
            { 3,6,7,8,9,10,11,12,13,15,16,17,18,19,20,21,30,31,33,34,35,36 };

        public static bool Validate(List<SicToken> tokens, MyErrorListener errors,
                                    int lineNum, string lineContext,
                                    bool esInicio, bool esFin,
                                    int startCount = 0, int endCount = 0)
        {
            if (tokens.Count == 0) return true;

            var insToks = tokens.Where(t => InsTypes.Contains(t.Type)).ToList();

            if (insToks.Count == 0)
            {
                var unknown = tokens.FirstOrDefault(t => t.Type == 0);
                errors.AddError(lineNum, 0,
                    unknown != null
                        ? $"Instrucción desconocida: '{unknown.Text}'"
                        : "No se reconoce instrucción en la línea");
                return false;
            }
            if (insToks.Count > 1)
            {
                errors.AddError(lineNum, 0, "Instrucción duplicada o ambigua");
                return false;
            }

            var ins = insToks[0];
            var ops = OperandTokens(tokens);
            bool hasLabel = tokens.Any(t => t.Type == 25);

            switch (ins.Type)
            {
                // ---- START ----
                case 7:
                    if (startCount > 0)
                    { errors.AddError(lineNum, 0, "Error: START solo puede aparecer una vez al inicio"); return false; }
                    if (!esInicio)
                    { errors.AddError(lineNum, 0, "Error: START debe ser la primera línea del programa"); return false; }
                    if (!hasLabel)
                    { errors.AddError(lineNum, 0, "Error: START requiere un NombreProg (etiqueta)"); return false; }
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, "Error: START requiere una dirección inicial"); return false; }
                    if (!IsDecOrHex(ops[0].Text))
                    { errors.AddError(lineNum, 0, $"Error: dirección de START inválida '{ops[0].Text}'"); return false; }
                    break;

                // ---- END ----
                case 8:
                    if (endCount > 0)
                    { errors.AddError(lineNum, 0, "Error: END solo puede aparecer una vez al final"); return false; }
                    if (!esFin)
                    { errors.AddError(lineNum, 0, "Error: END debe ser la última línea del programa"); return false; }
                    // Existencia del símbolo se verifica en Paso2
                    break;

                // ---- BASE ----
                case 9:
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, "Error: BASE requiere un símbolo"); return false; }
                    break;

                // ---- RESW ----
                case 10:
                    if (ops.Count == 0 || !IsDecOrHex(ops[0].Text))
                    { errors.AddError(lineNum, 0, $"Error: operando de RESW inválido o faltante"); return false; }
                    break;

                // ---- RESB ----
                case 11:
                    if (ops.Count == 0 || !IsDecOrHex(ops[0].Text))
                    { errors.AddError(lineNum, 0, $"Error: operando de RESB inválido o faltante"); return false; }
                    break;

                // ---- WORD ----
                case 12:
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, $"Error: operando de WORD inválido o faltante"); return false; }
                    // Acepta constante, símbolo o expresión; validación en Paso 2.
                    break;

                // ---- BYTE ----
                case 13:
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, "Error: BYTE requiere C'...' o X'...'"); return false; }
                    {
                        string bval = ops[0].Text;
                        bool validByte =
                            (bval.StartsWith("C'", StringComparison.OrdinalIgnoreCase) && bval.EndsWith("'")) ||
                            (bval.StartsWith("X'", StringComparison.OrdinalIgnoreCase) && bval.EndsWith("'") &&
                             SicLexer.IsHex(bval.Substring(2, bval.Length - 3)));
                        if (!validByte)
                        { errors.AddError(lineNum, 0, $"Error: operando BYTE inválido '{bval}'. Use C'texto' o X'hex'"); return false; }
                    }
                    break;

                // ---- RSUB / +RSUB ----
                case 3:
                case 6:
                    if (ops.Count > 0)
                    { errors.AddError(lineNum, 0, "Error: RSUB no acepta operandos"); return false; }
                    break;

                // ---- Formato 1 ----
                case 15:
                    if (ops.Count > 0)
                    { errors.AddError(lineNum, 0, $"Error: instrucción formato 1 '{ins.Text}' no acepta operandos"); return false; }
                    break;
                // ---- ORG ----
                case 30:
                    if (ops.Count == 0 || !IsDecOrHex(ops[0].Text))
                    { errors.AddError(lineNum, 0, $"Error: operando de ORG inválido o faltante"); return false; }
                    break;
                // ---- EQU ----
                case 31:
                    if (!hasLabel)
                    { errors.AddError(lineNum, 0, "Error: EQU requiere una etiqueta (símbolo) al inicio"); return false; }
                    if (ops.Count == 0 || string.IsNullOrWhiteSpace(ops[0].Text))
                    { errors.AddError(lineNum, 0, "Error: EQU requiere un operando o expresion"); return false; }
                    // Validación profunda (símbolos, reglas relativo/absoluto) en Paso1
                    break;
                // ---- USE ----
                case 33:
                    // El operando (nombre de bloque) es opcional; sin operando = bloque por omisión.
                    // No acepta etiqueta al inicio de línea.
                    if (hasLabel)
                    { errors.AddError(lineNum, 0, "Error: USE no acepta etiqueta al inicio"); return false; }
                    break;
                // ---- CSECT ----
                case 34:
                    if (!hasLabel)
                    { errors.AddError(lineNum, 0, "Error: CSECT requiere un nombre de sección (etiqueta)"); return false; }
                    if (ops.Count > 0)
                    { errors.AddError(lineNum, 0, "Error: CSECT no acepta operandos"); return false; }
                    break;
                // ---- EXTDEF ----
                case 35:
                    if (hasLabel)
                    { errors.AddError(lineNum, 0, "Error: EXTDEF no acepta etiqueta al inicio"); return false; }
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, "Error: EXTDEF requiere al menos un símbolo"); return false; }
                    break;
                // ---- EXTREF ----
                case 36:
                    if (hasLabel)
                    { errors.AddError(lineNum, 0, "Error: EXTREF no acepta etiqueta al inicio"); return false; }
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, "Error: EXTREF requiere al menos un símbolo"); return false; }
                    break;
            }

            return true;
        }

        static bool IsDecOrHex(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (int.TryParse(s, out _)) return true;
            if (s.EndsWith("H", StringComparison.OrdinalIgnoreCase) &&
                SicLexer.IsHex(s.Substring(0, s.Length - 1))) return true;
            return false;
        }

        static readonly HashSet<int> InsTypes2 = new HashSet<int>
            { 3,6,7,8,9,10,11,12,13,15,16,17,18,19,20,21,30,31,33,34,35,36 };

        static List<SicToken> OperandTokens(List<SicToken> tokens)
        {
            bool passedIns = false;
            var result = new List<SicToken>();
            foreach (var t in tokens)
            {
                if (t.Type == 25) continue;
                if (!passedIns && InsTypes2.Contains(t.Type)) { passedIns = true; continue; }
                if (passedIns &&
                   (t.Type == 1 || t.Type == 2 || t.Type == 4 || t.Type == 5 ||
                    t.Type == 14 || t.Type == 32 || (t.Type >= 23 && t.Type <= 29)))
                    result.Add(t);
            }
            return result;
        }
    }

    public partial class Form1 : Form
    {
        string Archivo = string.Empty;
        List<List<string>> codigo = new List<List<string>>();
        public static List<string> ListaErrores = new List<string>();

        // ================================================================
        //  SECCIONES DE CONTROL
        //  Cada sección tiene su propio TabSim, TabBloques y nombre.
        // ================================================================
        class SeccionControl
        {
            public string Nombre;              // nombre de la sección (START o CSECT label)
            public bool EsPrincipal;           // true = definida con START
            public Dictionary<string, (string Valor, string Tipo, int NoBloque, bool EsSimbExterno)> TabSim
                = new Dictionary<string, (string, string, int, bool)>();
            public Dictionary<string, BloqueInfo> TabBloques = new Dictionary<string, BloqueInfo>();
            public string bloqueActual = "";
            // Índice de la primera fila de esta sección en panelResultados
            public int FilaInicio;
            // Índice de la última fila (se llena al finalizar)
            public int FilaFin;
        }

        List<SeccionControl> Secciones = new List<SeccionControl>();
        SeccionControl seccionActual = null;

        // TabSim activa (la de la sección actual) — acceso rápido
        Dictionary<string, (string Valor, string Tipo, int NoBloque, bool EsSimbExterno)> TabSim
            => seccionActual?.TabSim;

        // TabBloques activa
        Dictionary<string, BloqueInfo> TabBloques
            => seccionActual?.TabBloques;

        string bloqueActual
        {
            get => seccionActual?.bloqueActual ?? "";
            set { if (seccionActual != null) seccionActual.bloqueActual = value; }
        }

        // TablaSimbolos_Panel refleja la sección seleccionada en el combo del popup (sólo lectura visual).
        // La sección activa para Paso1/Paso2 es seccionActual.

        class BloqueInfo
        {
            public int NoBloque;
            public int CP;
            public int Longitud;
            public int DirInicio;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private string originalFileName = "Errores";

        // ---- cargarArchivo_Click -> Cargar Archivo ----
        private void cargarArchivo_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                Title = "Seleccione el archivo SIC/XE"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath = ofd.FileName;
                    string inputText = File.ReadAllText(filePath);

                    originalFileName = Path.GetFileNameWithoutExtension(filePath);

                    rtbCode.Text = inputText;
                    rtbErrors.Clear();
                    rtbCode.SelectAll();
                    rtbCode.SelectionColor = Color.Black;

                    this.Archivo = ofd.FileName;
                    this.codigo = new List<List<string>>();

                    var txt = this.Controls.Find("txtRutaArchivo", true)
                                  .OfType<TextBox>().FirstOrDefault();
                    if (txt != null) txt.Text = filePath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al abrir el archivo:\n" + ex.Message,
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ---- checarSintax_Click Revisar Errores ----
        private void checarSintax_Click(object sender, EventArgs e)
        {
            rtbErrors.Text = "";
            string inputText = rtbCode.Text;
            ListaErrores = new List<string>();

            if (string.IsNullOrWhiteSpace(inputText))
            {
                MessageBox.Show("No hay código para analizar.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var errors = new MyErrorListener();
                var rawLines = inputText.Split(
                    new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                var nonEmpty = rawLines
                    .Select((l, idx) => new { Line = l.Trim(), Idx = idx })
                    .Where(x => !string.IsNullOrEmpty(x.Line) && !x.Line.StartsWith("."))
                    .ToList();

                int startCount = 0, endCount = 0;
                // Para validar posición de EXTDEF/EXTREF: últimas directivas vistas
                bool ultimaEraStartOCSECT = false;
                bool ultimaEraExtDefRef = false;

                for (int i = 0; i < nonEmpty.Count; i++)
                {
                    string trimmed = nonEmpty[i].Line;
                    int lineNum = nonEmpty[i].Idx + 1;
                    bool esIni = (i == 0);
                    bool esFin = (i == nonEmpty.Count - 1);

                    var tokens = SicLexer.Tokenize(trimmed);
                    SicValidator.Validate(tokens, errors, lineNum, trimmed,
                                          esIni, esFin, startCount, endCount);

                    bool esCSECT = tokens.Any(t => t.Type == 34);
                    bool esEXT = tokens.Any(t => t.Type == 35 || t.Type == 36);
                    bool esSTART = tokens.Any(t => t.Type == 7);

                    // Validar que EXTDEF/EXTREF esté inmediatamente después de START o CSECT
                    if (esEXT && !ultimaEraStartOCSECT && !ultimaEraExtDefRef)
                        errors.AddError(lineNum, 0, "Error: EXTDEF/EXTREF debe ir inmediatamente después de START o CSECT");

                    ultimaEraStartOCSECT = esSTART || esCSECT;
                    ultimaEraExtDefRef = esEXT;
                    if (!esSTART && !esCSECT && !esEXT)
                    { ultimaEraStartOCSECT = false; ultimaEraExtDefRef = false; }

                    if (esSTART) startCount++;
                    if (tokens.Any(t => t.Type == 8)) endCount++;
                }

                if (errors.HasErrors)
                {
                    rtbErrors.Text = "Errores encontrados:\n" + errors.ErrorMessages;
                    UnderlineErrors(errors.ErrorList);
                    SaveErrorsToFile(errors.ErrorMessages);
                }
                else
                {
                    rtbErrors.Text = "Análisis completado sin errores.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al analizar:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveErrorsToFile(string errors)
        {
            try
            {
                string errorFileName = $"{originalFileName}_Errors.err";
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, errorFileName);
                File.WriteAllText(filePath, errors);
                MessageBox.Show("Errores guardados en: " + filePath, "Información",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el archivo:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UnderlineErrors(List<SyntaxErrorInfo> errors)
        {
            rtbCode.SelectAll();
            rtbCode.SelectionFont = new Font(rtbCode.Font, FontStyle.Regular);
            rtbCode.SelectionColor = Color.Black;

            foreach (var error in errors)
            {
                try
                {
                    if (error.Line - 1 >= rtbCode.Lines.Length) continue;
                    int lineStart = rtbCode.GetFirstCharIndexFromLine(error.Line - 1);
                    if (lineStart < 0) continue;

                    string lineText = rtbCode.Lines[error.Line - 1];
                    if (error.Column >= lineText.Length) continue;

                    int wordStart = error.Column;
                    while (wordStart > 0 && !char.IsWhiteSpace(lineText[wordStart - 1]))
                        wordStart--;

                    int wordEnd = error.Column;
                    while (wordEnd < lineText.Length && !char.IsWhiteSpace(lineText[wordEnd]))
                        wordEnd++;

                    rtbCode.Select(lineStart + wordStart, wordEnd - wordStart);
                    rtbCode.SelectionColor = Color.Red;
                    rtbCode.SelectionFont = new Font(rtbCode.Font, FontStyle.Regular);

                    rtbCode.Select(lineStart, lineText.Length);
                    rtbCode.SelectionColor = Color.Red;
                    rtbCode.SelectionFont = new Font(rtbCode.Font, FontStyle.Underline);
                }
                catch { }
            }
        }

        //  PASO 1 -----------------------------------------------------------------------------------------------------------------------

        private void IniciarNuevaSeccion(string nombre, bool esPrincipal, int filaInicio)
        {
            var sec = new SeccionControl
            {
                Nombre = nombre,
                EsPrincipal = esPrincipal,
                FilaInicio = filaInicio,
                FilaFin = -1
            };
            sec.TabBloques[""] = new BloqueInfo { NoBloque = 0, CP = 0, Longitud = 0, DirInicio = 0 };
            sec.bloqueActual = "";
            Secciones.Add(sec);
            seccionActual = sec;
        }

        // Verifica que EXTDEF/EXTREF sigan inmediatamente a START o CSECT
        // (sin instrucciones que generen código ni etiquetas entre ellos)
        // Devuelve true si la línea i está en la zona permitida
        private bool EstaEnZonaExtDef(int lineaIdx)
        {
            // Buscar hacia atrás la última directiva START o CSECT
            for (int k = lineaIdx - 1; k >= 0; k--)
            {
                var tk = SicLexer.Tokenize(codigo[k][0]);
                if (tk.Any(t => t.Type == 7 || t.Type == 34)) return true;  // START o CSECT
                if (tk.Any(t => t.Type == 35 || t.Type == 36)) continue;     // otra EXTDEF/EXTREF: ok
                return false; // cualquier otra instrucción: fuera de zona
            }
            return false;
        }

        private void Paso1()
        {
            // La primera sección la crearemos al encontrar START
            // (si el código no empieza con START, la lógica de error se maneja como antes)

            int CP() => seccionActual == null ? 0 : seccionActual.TabBloques[seccionActual.bloqueActual].CP;
            void SetCP(int v) { if (seccionActual != null) seccionActual.TabBloques[seccionActual.bloqueActual].CP = v; }
            void IncCP(int delta) { if (seccionActual != null) seccionActual.TabBloques[seccionActual.bloqueActual].CP += delta; }

            for (int i = 0; i < codigo.Count; i++)
            {
                ListaErrores = new List<string>();

                IList<SicToken> t = SicLexer.Tokenize(codigo[i][0]);

                var errListener = new MyErrorListener();
                bool esInicio = (i == 0);
                bool esFin = (i == codigo.Count - 1);
                SicValidator.Validate(t.ToList(), errListener, i + 1, codigo[i][0],
                                      esInicio, esFin);
                ListaErrores.AddRange(errListener.ErrorList.Select(er => er.ToString()));

                DataGridViewRow r = new DataGridViewRow();
                r.CreateCells(panelResultados);
                r.Cells[0].Value = i;

                int cpActual = seccionActual == null ? 0 : CP();
                int noBlqActual = seccionActual == null ? 0 : seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque;
                r.Cells[3].Value = cpActual.ToString("X4");
                r.Cells[1].Value = "---";
                r.Cells[2].Value = noBlqActual.ToString();

                // ══════════════════════════════════════════════════
                //  PRIMERA LÍNEA (START)
                // ══════════════════════════════════════════════════
                if (i == 0)
                {
                    r.Cells[7].Value = "---";
                    string nombreProg = t.Count > 0 && t[0].Type == 25 ? t[0].Text : "PROG";
                    IniciarNuevaSeccion(nombreProg, true, 0);

                    if (t.Count > 0 && t[0].Type == 25) r.Cells[4].Value = t[0].Text;

                    if (ListaErrores.Count > 0)
                    {
                        r.Cells[7].Value = "Error: Sintaxis";
                        r.Cells[7].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        var insTok = t.FirstOrDefault(x => x.Type == 7);
                        if (insTok != null) r.Cells[5].Value = insTok.Text;

                        List<string> op = RegresarOperandos(t);
                        if (op.Count >= 1)
                        {
                            r.Cells[6].Value = op[0];
                            int startDir = ParseDecOrHex(op[0]);
                            SetCP(startDir);
                            seccionActual.TabBloques[""].DirInicio = startDir;
                        }
                    }
                    r.Cells[3].Value = CP().ToString("X4");
                    r.Cells[2].Value = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque.ToString();
                    panelResultados.Rows.Add(r);
                    continue;
                }

                // ══════════════════════════════════════════════════
                //  ÚLTIMA LÍNEA (END)
                // ══════════════════════════════════════════════════
                if (i == codigo.Count - 1)
                {
                    r.Cells[3].Value = seccionActual.EsPrincipal
                        ? seccionActual.TabBloques[seccionActual.bloqueActual].CP.ToString("X4")
                        : "000A"; // convención: CP de la sección principal
                    r.Cells[2].Value = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque.ToString();
                    r.Cells[7].Value = "---";
                    // Al finalizar la última línea: calcular longitudes/dirs de inicio de la sección activa
                    FinalizarSeccion(seccionActual, i);

                    if (ListaErrores.Count > 0)
                    {
                        r.Cells[7].Value = "Error: Sintaxis";
                        r.Cells[7].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        var insTok = t.FirstOrDefault(x => x.Type >= 3 && x.Type <= 21 || x.Type == 8);
                        if (insTok != null) r.Cells[5].Value = insTok.Text;
                        List<string> op = RegresarOperandos(t);
                        if (op.Count >= 1) r.Cells[6].Value = op[0];
                    }

                    // Tamaño total: suma de todas las secciones
                    int tamTotal = 0;
                    foreach (var sec2 in Secciones)
                    {
                        var ords = sec2.TabBloques.Values.OrderBy(b => b.NoBloque).ToList();
                        if (ords.Count > 0)
                        {
                            var ult2 = ords.Last();
                            tamTotal += ult2.DirInicio + ult2.Longitud;
                        }
                    }
                    numTamProg.Text = tamTotal.ToString("X") + "H";
                    MostrarTablaBloques();

                    panelResultados.Rows.Add(r);
                    continue;
                }

                // ══════════════════════════════════════════════════
                //  CSECT — inicio de nueva sección de control
                // ══════════════════════════════════════════════════
                if (t.Any(tk => tk.Type == 34))
                {
                    // Finalizar la sección actual antes de crear la nueva
                    FinalizarSeccion(seccionActual, i - 1);

                    string nombreCSECT = t.Count > 0 && t[0].Type == 25 ? t[0].Text : "CSECT_" + Secciones.Count;
                    IniciarNuevaSeccion(nombreCSECT, false, panelResultados.Rows.Count + 1);

                    r.Cells[4].Value = t.Count > 0 && t[0].Type == 25 ? t[0].Text : "";
                    r.Cells[5].Value = "CSECT";
                    r.Cells[3].Value = "0000";
                    r.Cells[2].Value = "0";
                    r.Cells[7].Value = "---";
                    if (ListaErrores.Count > 0) { r.Cells[7].Value = "Error: Sintaxis"; r.Cells[7].Style.ForeColor = Color.Red; }
                    panelResultados.Rows.Add(r);
                    continue;
                }

                // ══════════════════════════════════════════════════
                //  EXTREF — insertar símbolos externos en TabSim
                // ══════════════════════════════════════════════════
                if (t.Any(tk => tk.Type == 36))
                {
                    // Validar que esté inmediatamente después de START/CSECT/otro EXTDEF/EXTREF
                    bool zonaOk = EstaEnZonaExtDef(i);

                    r.Cells[5].Value = "EXTREF";
                    r.Cells[7].Value = "---";
                    if (!zonaOk || ListaErrores.Count > 0)
                    {
                        r.Cells[7].Value = "Error: Sintaxis";
                        r.Cells[7].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        // El operando ya viene como tipo 32 (lista), separado por comas
                        var opTok = t.FirstOrDefault(tk => tk.Type == 32);
                        if (opTok != null)
                        {
                            r.Cells[6].Value = opTok.Text;
                            foreach (string sim in opTok.Text.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)))
                            {
                                if (!seccionActual.TabSim.ContainsKey(sim))
                                    InsertarTabSim(sim, "---", "---", -1, true);
                            }
                        }
                    }
                    r.Cells[3].Value = CP().ToString("X4");
                    r.Cells[2].Value = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque.ToString();
                    panelResultados.Rows.Add(r);
                    continue;
                }

                // ══════════════════════════════════════════════════
                //  EXTDEF — solo validar sintaxis en Paso 1
                // ══════════════════════════════════════════════════
                if (t.Any(tk => tk.Type == 35))
                {
                    bool zonaOk = EstaEnZonaExtDef(i);
                    r.Cells[5].Value = "EXTDEF";
                    r.Cells[7].Value = "---";
                    if (!zonaOk || ListaErrores.Count > 0)
                    {
                        r.Cells[7].Value = "Error: Sintaxis";
                        r.Cells[7].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        var opTok = t.FirstOrDefault(tk => tk.Type == 32);
                        if (opTok != null) r.Cells[6].Value = opTok.Text;
                    }
                    r.Cells[3].Value = CP().ToString("X4");
                    r.Cells[2].Value = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque.ToString();
                    panelResultados.Rows.Add(r);
                    continue;
                }

                // ══════════════════════════════════════════════════
                //  LÍNEAS INTERMEDIAS
                // ══════════════════════════════════════════════════
                {
                    r.Cells[7].Value = "---";
                    bool ErrorSimboloDuplicado = false;

                    if (t.Count > 0 && t[0].Type == 25)
                    {
                        r.Cells[4].Value = t[0].Text;
                        bool esEQU_check = t.Any(tk => tk.Type == 31);
                        if (!esEQU_check && seccionActual.TabSim.ContainsKey(t[0].Text))
                            ErrorSimboloDuplicado = true;
                    }

                    string fmt = RegresarFormato(t);
                    r.Cells[1].Value = fmt == "---" ? "---" : fmt;
                    r.Cells[2].Value = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque.ToString();
                    r.Cells[5].Value = RegresarInstruccion(t);
                    List<string> op = RegresarOperandos(t);
                    if (op.Count > 0) r.Cells[6].Value = string.Join(",", op);

                    if (r.Cells[6].Value != null && (fmt == "3" || fmt == "4"))
                    {
                        string operando = r.Cells[6].Value?.ToString() ?? "";
                        if (operando.Contains("#")) r.Cells[7].Value = "Inmediato";
                        else if (operando.Contains("@")) r.Cells[7].Value = "Indirecto";
                        else r.Cells[7].Value = "Simple";
                    }

                    string ins4 = r.Cells[5].Value?.ToString() ?? "";
                    if (ins4 == "RSUB" || ins4 == "+RSUB")
                    {
                        bool sinOp = (r.Cells[6].Value == null || (r.Cells[6].Value?.ToString() ?? "") == "");
                        r.Cells[7].Value = sinOp ? "---" : "Error: Sintaxis";
                        if (!sinOp) r.Cells[7].Style.ForeColor = Color.Red;
                    }

                    if (fmt == "1" && r.Cells[6].Value != null && (r.Cells[6].Value?.ToString() ?? "") != "")
                    {
                        r.Cells[7].Value = "Error: Sintaxis";
                        r.Cells[7].Style.ForeColor = Color.Red;
                    }

                    if (ListaErrores.Count > 0)
                    {
                        r.Cells[7].Value = r.Cells[5].Value?.ToString() == "Error"
                            ? "Error: Instruccion no existe" : "Error: Sintaxis";
                        r.Cells[7].Style.ForeColor = Color.Red;
                    }
                    else if (ErrorSimboloDuplicado)
                    {
                        r.Cells[7].Value = "Error: Simbolo Duplicado";
                        r.Cells[7].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        string currentIns = r.Cells[5].Value?.ToString() ?? "";

                        // -- USE --
                        if (currentIns == "USE")
                        {
                            string nombreBloque = r.Cells[6].Value?.ToString()?.Trim() ?? "";
                            if (!seccionActual.TabBloques.ContainsKey(nombreBloque))
                            {
                                int nuevoNo = seccionActual.TabBloques.Count;
                                seccionActual.TabBloques[nombreBloque] = new BloqueInfo
                                { NoBloque = nuevoNo, CP = 0, Longitud = 0, DirInicio = 0 };
                            }
                            seccionActual.bloqueActual = nombreBloque;
                            r.Cells[3].Value = CP().ToString("X4");
                            r.Cells[1].Value = "---";
                            r.Cells[2].Value = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque.ToString();
                            r.Cells[7].Value = "---";
                            panelResultados.Rows.Add(r);
                            continue;
                        }

                        // -- ORG --
                        if (currentIns == "ORG")
                        {
                            string oper = r.Cells[6].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(oper))
                            {
                                if (oper.EndsWith("H", StringComparison.OrdinalIgnoreCase) &&
                                    SicLexer.IsHex(oper.Substring(0, oper.Length - 1)))
                                    SetCP(Convert.ToInt32(oper.Substring(0, oper.Length - 1), 16));
                                else if (int.TryParse(oper, out int orgDec))
                                    SetCP(orgDec);
                                else
                                {
                                    var evalOrg = EvaluarExpresionEQU(oper);
                                    if (evalOrg.Error) { r.Cells[7].Value = "Error: Expresion ORG no valida"; r.Cells[7].Style.ForeColor = Color.Red; }
                                    else SetCP(evalOrg.Valor);
                                }
                            }
                        }

                        // -- EQU --
                        if (currentIns == "EQU" && t.Count > 0 && t[0].Type == 25)
                        {
                            string labelEqu = t[0].Text;
                            int bloqueEqu = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque;

                            if (seccionActual.TabSim.ContainsKey(labelEqu))
                            { r.Cells[7].Value = "Error: Simbolo Duplicado"; r.Cells[7].Style.ForeColor = Color.Red; }
                            else
                            {
                                string operEqu = r.Cells[6].Value?.ToString() ?? "";

                                // EQU no puede tener símbolos externos
                                if (TieneSimboloExterno(operEqu))
                                {
                                    r.Cells[7].Value = "Error: EQU no puede tener simbolos externos";
                                    r.Cells[7].Style.ForeColor = Color.Red;
                                    InsertarTabSim(labelEqu, "FFFF", "Absoluto", bloqueEqu);
                                }
                                else if (operEqu == "*")
                                {
                                    string hexCP = CP().ToString("X4").ToUpper();
                                    InsertarTabSim(labelEqu, hexCP, "Relativo", bloqueEqu);
                                }
                                else
                                {
                                    bool bloqueOk = ValidarBloqueExpresion(operEqu, bloqueEqu);
                                    if (!bloqueOk)
                                    {
                                        r.Cells[7].Value = "Error: Expresion no valida";
                                        r.Cells[7].Style.ForeColor = Color.Red;
                                        InsertarTabSim(labelEqu, "FFFF", "Absoluto", bloqueEqu);
                                    }
                                    else
                                    {
                                        var evalResult = EvaluarExpresionEQU(operEqu);
                                        if (evalResult.Error)
                                        {
                                            r.Cells[7].Value = "Error: Expresion no valida";
                                            r.Cells[7].Style.ForeColor = Color.Red;
                                            InsertarTabSim(labelEqu, "FFFF", "Absoluto", bloqueEqu);
                                        }
                                        else
                                        {
                                            string hexVal = evalResult.Valor.ToString("X4").ToUpper();
                                            string tipo = evalResult.EsRelativo ? "Relativo" : "Absoluto";
                                            InsertarTabSim(labelEqu, hexVal, tipo, bloqueEqu);
                                        }
                                    }
                                }
                            }
                        }

                        // -- Insertar símbolo en TabSim --
                        if (t.Count > 0 && t[0].Type == 25 && currentIns != "EQU")
                        {
                            string hexVal2 = CP().ToString("X").ToUpper();
                            int noBlq = seccionActual.TabBloques[seccionActual.bloqueActual].NoBloque;
                            InsertarTabSim(t[0].Text, hexVal2, "Relativo", noBlq);
                        }
                    }

                    // -- Incremento CP --
                    string err6 = r.Cells[7].Value?.ToString() ?? "";
                    bool hayError = err6.StartsWith("Error");

                    if (!hayError)
                    {
                        string ins2 = r.Cells[5].Value?.ToString() ?? "";
                        string oper2 = r.Cells[6].Value?.ToString() ?? "";

                        switch (fmt)
                        {
                            case "1": IncCP(1); break;
                            case "2": IncCP(2); break;
                            case "3": IncCP(3); break;
                            case "4": IncCP(4); break;
                            case "---":
                                switch (ins2)
                                {
                                    case "BASE": break;
                                    case "ORG": break;
                                    case "EQU": break;
                                    case "USE": break;
                                    case "RESW": IncCP(ParseDecOrHex(oper2) * 3); break;
                                    case "RESB": IncCP(ParseDecOrHex(oper2)); break;
                                    case "WORD": IncCP(3); break;
                                    case "BYTE": IncCP(CalcByteSize(oper2)); break;
                                }
                                break;
                        }
                    }

                    r.Cells[3].Value = seccionActual.TabBloques[seccionActual.bloqueActual].CP == 0
                        ? "0000" : (seccionActual.TabBloques[seccionActual.bloqueActual].CP - (
                            fmt == "1" ? 1 : fmt == "2" ? 2 : fmt == "3" ? 3 : fmt == "4" ? 4 : 0
                        )).ToString("X4");
                    // Recalcular el CP de la fila (antes del incremento)
                    r.Cells[3].Value = cpActual.ToString("X4");
                }

                panelResultados.Rows.Add(r);
            }

            if (codigo.Count == 0)
                numTamProg.Text = "0H";
        }

        private void FinalizarSeccion(SeccionControl sec, int ultimaFila)
        {
            if (sec == null) return;
            sec.FilaFin = ultimaFila;

            var ordenBloques = sec.TabBloques.Values.OrderBy(b => b.NoBloque).ToList();
            for (int b = 1; b < ordenBloques.Count; b++)
            {
                var prev = ordenBloques[b - 1];
                ordenBloques[b].DirInicio = prev.DirInicio + prev.CP;
            }
            foreach (var bl in ordenBloques)
                bl.Longitud = bl.CP;
        }

        // Devuelve true si la expresión contiene al menos un símbolo externo de la sección activa
        private bool TieneSimboloExterno(string expr)
        {
            if (seccionActual == null || string.IsNullOrWhiteSpace(expr)) return false;
            foreach (var kv in seccionActual.TabSim)
                if (kv.Value.EsSimbExterno && expr.Contains(kv.Key))
                    return true;
            return false;
        }
        // Verifica que todos los símbolos referenciados en una expresión EQU
        //    pertenezcan al mismo número de bloque indicado.
        private bool ValidarBloqueExpresion(string expr, int bloqueEsperado)
        {
            if (string.IsNullOrWhiteSpace(expr)) return true;
            int i = 0; expr = expr.Trim();
            while (i < expr.Length)
            {
                char c = expr[i];
                if (c == ' ' || c == '\t' || c == '+' || c == '-' ||
                    c == '*' || c == '/' || c == '(' || c == ')') { i++; continue; }
                int start = i;
                while (i < expr.Length && expr[i] != ' ' && expr[i] != '\t' &&
                       expr[i] != '+' && expr[i] != '-' && expr[i] != '*' &&
                       expr[i] != '/' && expr[i] != '(' && expr[i] != ')') i++;
                string word = expr.Substring(start, i - start);
                if (string.IsNullOrEmpty(word)) continue;
                // ¿Es número decimal?
                if (int.TryParse(word, out _)) continue;
                // ¿Es número hex (termina en H)?
                if (word.EndsWith("H", StringComparison.OrdinalIgnoreCase) &&
                    SicLexer.IsHex(word.Substring(0, word.Length - 1))) continue;
                // Es símbolo: buscar en TabSim de la sección activa y verificar bloque
                if (seccionActual != null && seccionActual.TabSim.ContainsKey(word))
                {
                    var entrada = seccionActual.TabSim[word];
                    if (entrada.EsSimbExterno) return false; // EQU no puede tener SE
                    if (entrada.Tipo == "Absoluto") continue;
                    if (entrada.NoBloque != bloqueEsperado) return false;
                }
                // Si no está en TabSim aún, no podemos verificar (se evaluará más tarde);
                // dejamos pasar y el evaluador dará error si no existe.
            }
            return true;
        }

        // Muestra la Tabla de Bloques en el DataGridView TablaBloques_Panel
        private void MostrarTablaBloques()
        {
            DataGridView tbp = this.Controls.Find("TablaBloques_Panel", true)
                                   .OfType<DataGridView>().FirstOrDefault();
            if (tbp == null)
            {
                // Crear la tabla de bloques en la ventana
                tbp = new DataGridView();
                tbp.Name = "TablaBloques_Panel";
                tbp.AllowUserToAddRows = false;
                tbp.AllowUserToDeleteRows = false;
                tbp.ReadOnly = true;
                tbp.Font = new Font("Courier New", 9);
                tbp.BackgroundColor = Color.White;
                tbp.GridColor = Color.LightGray;
                tbp.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

                // Columnas de la tablitaaa
                foreach (var (hdr, w) in new[] {
                    ("No.Bloque", 80), ("Nombre", 100), ("Longitud", 90), ("Dir.Inicio", 90) })
                {
                    var col = new DataGridViewTextBoxColumn
                    { HeaderText = hdr, ReadOnly = true, Width = w };
                    tbp.Columns.Add(col);
                }

                // Crear un panel contenedor con etiqueta encima
                var panelTB = new Panel();
                panelTB.Name = "PanelTablaBloques";
                panelTB.BorderStyle = BorderStyle.FixedSingle;
                panelTB.Width = 400;
                panelTB.Height = 180;
                panelTB.Location = new System.Drawing.Point(
                    TablaSimbolos_Panel.Parent?.Width - 420 ?? 10, 30);
                panelTB.Anchor = System.Windows.Forms.AnchorStyles.Top
                               | System.Windows.Forms.AnchorStyles.Right;

                var lblTB = new Label();
                lblTB.Text = "TABLA DE BLOQUES";
                lblTB.Dock = DockStyle.Top;
                lblTB.Height = 26;
                lblTB.BackColor = Color.MediumPurple;
                lblTB.ForeColor = Color.White;
                lblTB.Font = new Font("Arial", 9, FontStyle.Bold);
                lblTB.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                lblTB.Padding = new Padding(4);

                tbp.Dock = DockStyle.Fill;
                panelTB.Controls.Add(tbp);
                panelTB.Controls.Add(lblTB);

                // Añadir al mismo contenedor que TablaSimbolos_Panel
                TablaSimbolos_Panel.Parent?.Controls.Add(panelTB);
            }

            tbp.Rows.Clear();
            var bloquesSec = seccionActual?.TabBloques ?? TabBloques;
            foreach (var bl in bloquesSec.Values.OrderBy(b => b.NoBloque))
            {
                string nombre = bl.NoBloque == 0 ? "(Omisión)" : bl.NoBloque.ToString();
                // Buscar el nombre del bloque por su número
                string nombreBlq = bloquesSec.FirstOrDefault(kv => kv.Value.NoBloque == bl.NoBloque).Key;
                if (string.IsNullOrEmpty(nombreBlq)) nombreBlq = "(Omisión)";

                tbp.Rows.Add(
                    bl.NoBloque.ToString(),
                    nombreBlq,
                    bl.Longitud.ToString("X4"),
                    bl.DirInicio.ToString("X4")
                );
            }
        }

        // ---- Calcula tamaño en bytes de un operando BYTE ----
        private int CalcByteSize(string operando)
        {
            if (string.IsNullOrEmpty(operando)) return 0;

            if (operando.StartsWith("C'", StringComparison.OrdinalIgnoreCase) && operando.EndsWith("'"))
            {
                // 1 byte por carácter
                return operando.Substring(2, operando.Length - 3).Length;
            }
            else if (operando.StartsWith("X'", StringComparison.OrdinalIgnoreCase) && operando.EndsWith("'"))
            {
                // Completar a dígitos pares, cada par = 1 byte
                string hex = operando.Substring(2, operando.Length - 3);
                int digitos = hex.Length % 2 == 0 ? hex.Length : hex.Length + 1;
                return digitos / 2;
            }
            return 0;
        }

        // ---- Genera código objeto para BYTE ----
        private string CalcByteObj(string operando)
        {
            if (operando.StartsWith("C'", StringComparison.OrdinalIgnoreCase) && operando.EndsWith("'"))
            {
                string contenido = operando.Substring(2, operando.Length - 3);
                return string.Concat(contenido.Select(c => ((int)c).ToString("X2")));
            }
            else if (operando.StartsWith("X'", StringComparison.OrdinalIgnoreCase) && operando.EndsWith("'"))
            {
                string hex = operando.Substring(2, operando.Length - 3);
                if (hex.Length % 2 != 0) hex = "0" + hex; // completar a par
                return hex.ToUpper();
            }
            return "---";
        }

        // ---- Parsea decimal o hex (con H al final) ----
        private int ParseDecOrHex(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            if (s.EndsWith("H", StringComparison.OrdinalIgnoreCase))
            {
                string body = s.Substring(0, s.Length - 1);
                if (int.TryParse(body, System.Globalization.NumberStyles.HexNumber, null, out int v))
                    return v;
                return 0;
            }
            if (int.TryParse(s, out int d)) return d;
            return 0;
        }

        // ---- Inserta símbolo en TabSim interno de la sección activa ----
        private void InsertarTabSimInterno(string simbolo, string hexVal, string tipo, int noBloque, bool esSE = false)
        {
            if (seccionActual == null) return;
            seccionActual.TabSim[simbolo] = (hexVal, tipo, noBloque, esSE);
        }

        // ---- Inserta símbolo en TablaSimbolos_Panel (cols: Símbolo, Dirección, Tipo, No.Bloque, SimboloExterno) ----
        private void InsertarTabSim(string simbolo, string hexVal, string tipo, int noBloque, bool esSE = false)
        {
            InsertarTabSimInterno(simbolo, hexVal, tipo, noBloque, esSE);

            // Añadir columna NoBloque si no existe
            if (TablaSimbolos_Panel.Columns["NoBloque"] == null)
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "NoBloque"; col.HeaderText = "No.Bloque"; col.ReadOnly = true; col.Width = 80;
                TablaSimbolos_Panel.Columns.Add(col);
            }
            // Añadir columna SimboloExterno si no existe
            if (TablaSimbolos_Panel.Columns["SimboloExterno"] == null)
            {
                var col2 = new DataGridViewTextBoxColumn();
                col2.Name = "SimboloExterno"; col2.HeaderText = "S.E."; col2.ReadOnly = true; col2.Width = 50;
                TablaSimbolos_Panel.Columns.Add(col2);
            }

            var rs = new DataGridViewRow();
            rs.CreateCells(TablaSimbolos_Panel);
            rs.Cells[0].Value = simbolo;
            rs.Cells[1].Value = esSE ? "---" : hexVal;
            rs.Cells[2].Value = esSE ? "---" : tipo;
            if (TablaSimbolos_Panel.Columns.Count > 3)
                rs.Cells[3].Value = esSE ? "---" : noBloque.ToString();
            if (TablaSimbolos_Panel.Columns.Count > 4)
                rs.Cells[4].Value = esSE ? "Sí" : "No";
            TablaSimbolos_Panel.Rows.Add(rs);
        }

        //  EVALUACIÓN DE EXPRESIONES
        //  Compartida por Paso1 (EQU) y Paso2 (Formato 3/4, WORD)
        private struct EvalResult
        {
            public bool Error;
            public int Valor;
            public bool EsRelativo;
            public int RelCount; // suma algebraica: +rel=+1, -rel=-1
        }

        private enum ExprTokKind { Num, Sym, Plus, Minus, Star, Slash, LParen, RParen }
        private struct ExprToken
        {
            public ExprTokKind Kind;
            public int NumVal;
            public bool EsRel;
            public string Text;
        }

        private delegate EvalResult PrimarioFn(List<ExprToken> toks, ref int pos);

        private EvalResult ParseSumaRaiz(List<ExprToken> toks, ref int pos, PrimarioFn factorFn)
        {
            var inner = ParseSumaInterna(toks, ref pos, factorFn);
            if (inner.Error) return inner;
            int rc = inner.RelCount;
            bool esRel;
            if (rc == 0) esRel = false;
            else if (rc == 1) esRel = true;
            else return new EvalResult { Error = true };
            return new EvalResult { Error = false, Valor = inner.Valor, EsRelativo = esRel, RelCount = rc };
        }

        private EvalResult ParseSumaInterna(List<ExprToken> toks, ref int pos, PrimarioFn factorFn)
        {
            int signo = +1;
            if (pos < toks.Count && toks[pos].Kind == ExprTokKind.Minus) { signo = -1; pos++; }
            else if (pos < toks.Count && toks[pos].Kind == ExprTokKind.Plus) { pos++; }

            var left = factorFn(toks, ref pos);
            if (left.Error) return left;

            int totalVal = signo * left.Valor;
            int relCount = signo * left.RelCount;

            while (pos < toks.Count &&
                   (toks[pos].Kind == ExprTokKind.Plus || toks[pos].Kind == ExprTokKind.Minus))
            {
                int op = toks[pos].Kind == ExprTokKind.Plus ? +1 : -1;
                pos++;
                var right = factorFn(toks, ref pos);
                if (right.Error) return right;
                totalVal += op * right.Valor;
                relCount += op * right.RelCount;
            }
            return new EvalResult { Error = false, Valor = totalVal, EsRelativo = (relCount != 0), RelCount = relCount };
        }

        private EvalResult ParseFactorGen(List<ExprToken> toks, ref int pos, PrimarioFn primFn)
        {
            var left = primFn(toks, ref pos);
            if (left.Error) return left;
            while (pos < toks.Count &&
                   (toks[pos].Kind == ExprTokKind.Star || toks[pos].Kind == ExprTokKind.Slash))
            {
                var opKind = toks[pos].Kind; pos++;
                var right = primFn(toks, ref pos);
                if (right.Error) return right;
                if (left.RelCount != 0 || right.RelCount != 0) return new EvalResult { Error = true };
                int val = opKind == ExprTokKind.Star
                    ? left.Valor * right.Valor
                    : (right.Valor == 0 ? 0 : left.Valor / right.Valor);
                left = new EvalResult { Error = false, Valor = val, EsRelativo = false, RelCount = 0 };
            }
            return left;
        }

        // ---- Paso 1: tokenizador y primario para EQU (lee TabSim) ----
        private List<ExprToken>? TokenizarExpr(string expr)
        {
            var list = new List<ExprToken>();
            int i = 0; expr = expr.Trim();
            while (i < expr.Length)
            {
                char c = expr[i];
                if (c == ' ' || c == '\t') { i++; continue; }
                if (c == '+') { list.Add(new ExprToken { Kind = ExprTokKind.Plus, Text = "+" }); i++; continue; }
                if (c == '-') { list.Add(new ExprToken { Kind = ExprTokKind.Minus, Text = "-" }); i++; continue; }
                if (c == '*') { list.Add(new ExprToken { Kind = ExprTokKind.Star, Text = "*" }); i++; continue; }
                if (c == '/') { list.Add(new ExprToken { Kind = ExprTokKind.Slash, Text = "/" }); i++; continue; }
                if (c == '(') { list.Add(new ExprToken { Kind = ExprTokKind.LParen, Text = "(" }); i++; continue; }
                if (c == ')') { list.Add(new ExprToken { Kind = ExprTokKind.RParen, Text = ")" }); i++; continue; }
                int start = i;
                while (i < expr.Length && expr[i] != ' ' && expr[i] != '\t' &&
                       expr[i] != '+' && expr[i] != '-' && expr[i] != '*' &&
                       expr[i] != '/' && expr[i] != '(' && expr[i] != ')') i++;
                string word = expr.Substring(start, i - start);
                if (string.IsNullOrEmpty(word)) return null;
                if (int.TryParse(word, out int dec))
                { list.Add(new ExprToken { Kind = ExprTokKind.Num, NumVal = dec, EsRel = false, Text = word }); continue; }
                if (word.EndsWith("H", StringComparison.OrdinalIgnoreCase) && SicLexer.IsHex(word.Substring(0, word.Length - 1)))
                {
                    int hv = Convert.ToInt32(word.Substring(0, word.Length - 1), 16);
                    list.Add(new ExprToken { Kind = ExprTokKind.Num, NumVal = hv, EsRel = false, Text = word }); continue;
                }
                if (seccionActual != null && seccionActual.TabSim.ContainsKey(word))
                {
                    var e2 = seccionActual.TabSim[word];
                    if (e2.EsSimbExterno) return null; // EQU no acepta SE
                    list.Add(new ExprToken
                    {
                        Kind = ExprTokKind.Sym,
                        NumVal = e2.Valor == "---" ? 0 : Convert.ToInt32(e2.Valor, 16),
                        EsRel = (e2.Tipo == "Relativo"),
                        Text = word
                    }); continue;
                }
                return null;
            }
            return list;
        }

        private EvalResult ParsePrimario(List<ExprToken> toks, ref int pos)
        {
            if (pos >= toks.Count) return new EvalResult { Error = true };
            var tok = toks[pos];
            if (tok.Kind == ExprTokKind.Num || tok.Kind == ExprTokKind.Sym)
            {
                pos++; int rc = tok.EsRel ? 1 : 0;
                return new EvalResult { Error = false, Valor = tok.NumVal, EsRelativo = tok.EsRel, RelCount = rc };
            }
            if (tok.Kind == ExprTokKind.LParen)
            {
                pos++;
                var inner = ParseSumaInterna(toks, ref pos, (t2, ref p2) => ParseFactorGen(t2, ref p2, ParsePrimario));
                if (inner.Error) return inner;
                if (pos >= toks.Count || toks[pos].Kind != ExprTokKind.RParen) return new EvalResult { Error = true };
                pos++; return inner;
            }
            return new EvalResult { Error = true };
        }

        private EvalResult EvaluarExpresionEQU(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return new EvalResult { Error = true };
            try
            {
                var tokens = TokenizarExpr(expr);
                if (tokens == null) return new EvalResult { Error = true };
                int pos = 0;
                var res = ParseSumaRaiz(tokens, ref pos, (toks, ref p) => ParseFactorGen(toks, ref p, ParsePrimario));
                return (res.Error || pos != tokens.Count) ? new EvalResult { Error = true } : res;
            }
            catch { return new EvalResult { Error = true }; }
        }

        // ---- Paso 2: tokenizador y primario para expresiones (lee TablaSimbolos_Panel) ----
        private List<ExprToken>? TokenizarExprPaso2(string expr)
        {
            var list = new List<ExprToken>();
            int i = 0; expr = expr.Trim();
            while (i < expr.Length)
            {
                char c = expr[i];
                if (c == ' ' || c == '\t') { i++; continue; }
                if (c == '+') { list.Add(new ExprToken { Kind = ExprTokKind.Plus, Text = "+" }); i++; continue; }
                if (c == '-') { list.Add(new ExprToken { Kind = ExprTokKind.Minus, Text = "-" }); i++; continue; }
                if (c == '*') { list.Add(new ExprToken { Kind = ExprTokKind.Star, Text = "*" }); i++; continue; }
                if (c == '/') { list.Add(new ExprToken { Kind = ExprTokKind.Slash, Text = "/" }); i++; continue; }
                if (c == '(') { list.Add(new ExprToken { Kind = ExprTokKind.LParen, Text = "(" }); i++; continue; }
                if (c == ')') { list.Add(new ExprToken { Kind = ExprTokKind.RParen, Text = ")" }); i++; continue; }
                int start = i;
                while (i < expr.Length && expr[i] != ' ' && expr[i] != '\t' &&
                       expr[i] != '+' && expr[i] != '-' && expr[i] != '*' &&
                       expr[i] != '/' && expr[i] != '(' && expr[i] != ')') i++;
                string word = expr.Substring(start, i - start);
                if (string.IsNullOrEmpty(word)) return null;
                if (int.TryParse(word, out int dec))
                { list.Add(new ExprToken { Kind = ExprTokKind.Num, NumVal = dec, EsRel = false, Text = word }); continue; }
                if (word.EndsWith("H", StringComparison.OrdinalIgnoreCase) && SicLexer.IsHex(word.Substring(0, word.Length - 1)))
                {
                    int hv = Convert.ToInt32(word.Substring(0, word.Length - 1), 16);
                    list.Add(new ExprToken { Kind = ExprTokKind.Num, NumVal = hv, EsRel = false, Text = word }); continue;
                }
                bool found = false;
                // Primero buscar en TabSim de la sección activa
                if (seccionActual != null && seccionActual.TabSim.TryGetValue(word, out var entSec))
                {
                    bool sr = !entSec.EsSimbExterno && entSec.Tipo == "Relativo";
                    int sv = entSec.EsSimbExterno ? 0
                            : entSec.Valor == "---" ? 0
                            : (sr ? DireccionAbsoluta(word) : Convert.ToInt32(entSec.Valor, 16));
                    if (sv < 0) sv = 0;
                    list.Add(new ExprToken { Kind = ExprTokKind.Sym, NumVal = sv, EsRel = sr, Text = word });
                    found = true;
                }
                if (!found)
                {
                    foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                    {
                        string nom = row.Cells[0]?.Value?.ToString() ?? "";
                        if (string.Equals(nom, word, StringComparison.OrdinalIgnoreCase))
                        {
                            bool sr = (row.Cells[2]?.Value?.ToString() ?? "") == "Relativo";
                            // Con bloques: usar dirección absoluta para símbolos relativos
                            int sv = sr ? DireccionAbsoluta(word)
                                        : Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "0", 16);
                            if (sv < 0) sv = 0; // símbolo no resuelto
                            list.Add(new ExprToken { Kind = ExprTokKind.Sym, NumVal = sv, EsRel = sr, Text = word });
                            found = true; break;
                        }
                    }
                }
                if (!found) return null;
            }
            return list;
        }

        private EvalResult ParsePrimarioPaso2(List<ExprToken> toks, ref int pos)
        {
            if (pos >= toks.Count) return new EvalResult { Error = true };
            var tok = toks[pos];
            if (tok.Kind == ExprTokKind.Num || tok.Kind == ExprTokKind.Sym)
            {
                pos++; int rc = tok.EsRel ? 1 : 0;
                return new EvalResult { Error = false, Valor = tok.NumVal, EsRelativo = tok.EsRel, RelCount = rc };
            }
            if (tok.Kind == ExprTokKind.LParen)
            {
                pos++;
                var inner = ParseSumaInterna(toks, ref pos, (t2, ref p2) => ParseFactorGen(t2, ref p2, ParsePrimarioPaso2));
                if (inner.Error) return inner;
                if (pos >= toks.Count || toks[pos].Kind != ExprTokKind.RParen) return new EvalResult { Error = true };
                pos++; return inner;
            }
            return new EvalResult { Error = true };
        }

        private EvalResult EvaluarExpresionPaso2(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return new EvalResult { Error = true };
            try
            {
                var tokens = TokenizarExprPaso2(expr);
                if (tokens == null) return new EvalResult { Error = true };
                int pos = 0;
                var res = ParseSumaRaiz(tokens, ref pos, (toks, ref p) => ParseFactorGen(toks, ref p, ParsePrimarioPaso2));
                return (res.Error || pos != tokens.Count) ? new EvalResult { Error = true } : res;
            }
            catch { return new EvalResult { Error = true }; }
        }

        private bool EsExpresionCompuesta(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            return s.IndexOfAny(new char[] { '+', '-', '*', '/', '(', ')' }) >= 0;
        }

        // ---- RegresarFormato ----
        string RegresarFormato(IList<SicToken> t)
        {
            int cont = 0;
            string ret = "---";
            foreach (var token in t)
            {
                switch (token.Type)
                {
                    case 15: ret = "1"; cont++; break;
                    case 16: case 17: case 18: case 19: ret = "2"; cont++; break;
                    case 20: case 3: ret = "3"; cont++; break;
                    case 21: case 6: ret = "4"; cont++; break;
                }
            }
            return cont <= 1 ? ret : "Error";
        }

        // ---- RegresarInstruccion ----
        string RegresarInstruccion(IList<SicToken> t)
        {
            int cont = 0;
            string ret = "";
            foreach (var token in t)
            {
                // Tipos de instrucción/directiva: 3,6,7-13,15-21,30,31,33
                // Excluir tipo 14 (registro) que también cae en el rango
                if (token.Type == 3 || token.Type == 6 ||
                   (token.Type >= 7 && token.Type <= 13) ||
                   (token.Type >= 15 && token.Type <= 21) ||
                   token.Type == 30 || token.Type == 31 || token.Type == 33 ||
                   token.Type == 34 || token.Type == 35 || token.Type == 36)
                { ret = token.Text; cont++; }
            }
            return cont == 1 ? ret : "Error";
        }

        // ---- RegresarOperandos ----
        List<string> RegresarOperandos(IList<SicToken> t)
        {
            var list = new List<string>();
            for (int i = 0; i < t.Count; i++)
            {
                if (i != 0 &&
                   (t[i].Type == 1 || t[i].Type == 2 || t[i].Type == 4 ||
                    t[i].Type == 5 || t[i].Type == 14 || t[i].Type == 32 ||
                   (t[i].Type >= 23 && t[i].Type <= 29)))
                    list.Add(t[i].Text);
            }
            return list;
        }

        // ---- tabla_Click genera TABSIM ----
        private void tabla_Click(object sender, EventArgs e)
        {
            rtbErrors.Text = "";
            TablaSimbolos_Panel.Rows.Clear();
            // Quitar columnas dinámicas de ejecuciones anteriores
            if (TablaSimbolos_Panel.Columns["NoBloque"] != null)
                TablaSimbolos_Panel.Columns.Remove("NoBloque");
            if (TablaSimbolos_Panel.Columns["SimboloExterno"] != null)
                TablaSimbolos_Panel.Columns.Remove("SimboloExterno");
            panelResultados.Rows.Clear();
            // Limpiar tabla de bloques UI si existe
            if (this.Controls.Find("TablaBloques_Panel", true).Length > 0)
            {
                var tbp = this.Controls.Find("TablaBloques_Panel", true)[0] as DataGridView;
                tbp?.Rows.Clear();
            }
            string inputText = rtbCode.Text;
            Secciones = new List<SeccionControl>();
            seccionActual = null;
            codigo = new List<List<string>>();
            ListaErrores = new List<string>();

            for (int i = 0; i < rtbCode.Lines.Length; i++)
            {
                string trim = rtbCode.Lines[i].Trim();
                if (trim != "" && !trim.StartsWith("."))
                    codigo.Add(new List<string> { trim });
            }

            if (string.IsNullOrWhiteSpace(inputText))
            {
                MessageBox.Show("No hay código para analizar.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Paso1();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al analizar:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---- Diccionarios SIC/XE ----
        Dictionary<string, string> CodigosInstruccion = new Dictionary<string, string>
        {
            {"ADD","18"},{"ADDF","58"},{"ADDR","90"},{"AND","40"},{"CLEAR","B4"},
            {"COMP","28"},{"COMPF","88"},{"COMPR","A0"},{"DIV","24"},{"DIVF","64"},
            {"DIVR","9C"},{"FIX","C4"},{"FLOAT","C0"},{"HIO","F4"},{"J","3C"},
            {"JEQ","30"},{"JGT","34"},{"JLT","38"},{"JSUB","48"},{"LDA","00"},
            {"LDB","68"},{"LDCH","50"},{"LDF","70"},{"LDL","08"},{"LDS","6C"},
            {"LDT","74"},{"LDX","04"},{"LPS","D0"},{"MUL","20"},{"MULF","60"},
            {"MULR","98"},{"NORM","C8"},{"OR","44"},{"RD","D8"},{"RMO","AC"},
            {"RSUB","4C"},{"SHIFTL","A4"},{"SHIFTR","A8"},{"SIO","F0"},{"SSK","EC"},
            {"STA","0C"},{"STB","78"},{"STCH","54"},{"STF","80"},{"STI","D4"},
            {"STL","14"},{"STS","7C"},{"STSW","E8"},{"STT","84"},{"STX","10"},
            {"SUB","1C"},{"SUBF","5C"},{"SUBR","94"},{"SVC","B0"},{"TD","E0"},
            {"TIO","F8"},{"TIX","2C"},{"TIXR","B8"},{"WD","DC"}
        };

        // Directivas que no generan codigo objeto.
        List<string> DirectivasNO = new List<string> { "START", "END", "BASE", "RESW", "RESB", "USE" };

        Dictionary<string, string> Registros = new Dictionary<string, string>
            { {"A","0"},{"X","1"},{"L","2"},{"B","3"},{"S","4"},
              {"T","5"},{"F","6"},{"PC","8"},{"SW","9"} };

        string BASE = "-1";

        //  DIRECCIONAMIENTO CON BLOQUES -----------------------------------------------------------------------------------------------------------------------
        //  Con bloques, los CPs en panelResultados y las direcciones en
        //  TabSim son relativos al bloque. Para Paso2 necesitamos las
        //  direcciones absolutas = DirInicio(bloque) + CPrelativo.

        // Devuelve DirInicio del bloque con el número dado, o 0 si no hay bloques definidos. (por que por defualt es omision)
        private int DirInicioDeBloque(int noBloque)
        {
            var tabBloq = seccionActual?.TabBloques;
            if (tabBloq == null || tabBloq.Count == 0) return 0;
            var bl = tabBloq.Values.FirstOrDefault(b => b.NoBloque == noBloque);
            return bl?.DirInicio ?? 0;
        }

        // Devuelve la dirección absoluta de un símbolo:
        //   - Absoluto -> valor tal cual (sin sumar nada)
        //   - Relativo  -> CPrelativo + DirInicio(bloque del símbolo)
        // Si el símbolo no existe en TabSim devuelve -1.
        private int DireccionAbsoluta(string nombreSim)
        {
            // Buscar en TabSim de la sección activa
            if (seccionActual != null && seccionActual.TabSim.TryGetValue(nombreSim, out var entrada))
            {
                if (entrada.EsSimbExterno) return 0; // SE: valor 0 para evaluación
                if (entrada.Valor == "---") return 0;
                int cpRel = Convert.ToInt32(entrada.Valor, 16);
                if (entrada.Tipo == "Absoluto") return cpRel;
                return cpRel + DirInicioDeBloque(entrada.NoBloque);
            }
            // Fallback: TablaSimbolos_Panel
            foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
            {
                if (string.Equals(row.Cells[0]?.Value?.ToString() ?? "",
                                  nombreSim, StringComparison.OrdinalIgnoreCase))
                {
                    string dirStr = row.Cells[1]?.Value?.ToString() ?? "0";
                    if (dirStr == "---") return 0;
                    int cpRel = Convert.ToInt32(dirStr, 16);
                    var tabBloq = seccionActual?.TabBloques;
                    return cpRel + (tabBloq != null && tabBloq.Count > 1
                        ? DirInicioDeBloque(
                            int.TryParse(row.Cells.Count > 3
                                ? row.Cells[3]?.Value?.ToString() : "0", out int nb) ? nb : 0)
                        : 0);
                }
            }
            return -1;
        }

        // Devuelve el CP absoluto correspondiente a la fila indicada de panelResultados.
        // es una directiva (valor "---" para START/END, o número de bloque para el resto).
        // El CP absoluto = CPrelativo(col2) + DirInicio(bloque de esa fila).
        private int CPAbsolutoFila(int filaIndex)
        {
            if (filaIndex < 0 || filaIndex >= panelResultados.Rows.Count) return 0;
            var fila = panelResultados.Rows[filaIndex];

            // Cells[3] = CP relativo al bloque (hex), Cells[2] = NoBloque (entero)
            int cpRel = 0;
            if (!int.TryParse(fila.Cells[3]?.Value?.ToString() ?? "0",
                              System.Globalization.NumberStyles.HexNumber, null, out cpRel))
                cpRel = 0;

            var tabBloqF = seccionActual?.TabBloques;
            if (tabBloqF == null || tabBloqF.Count <= 1) return cpRel;

            // CP_Real = CP_Relativo + DirInicio(BloqueActual)
            string col2 = fila.Cells[2]?.Value?.ToString() ?? "0";
            if (!int.TryParse(col2, out int noBloque)) noBloque = 0;
            return cpRel + DirInicioDeBloque(noBloque);
        }

        //  PASO 2 -----------------------------------------------------------------------------------------------------------------------

        private void paso2()
        {
            BASE = "-1"; // Resetear BASE al iniciar paso2

            // Determinar la sección de control activa según la fila
            SeccionControl SeccionDeFila(int filaIdx)
            {
                // La sección es la última cuya FilaInicio <= filaIdx
                SeccionControl res = Secciones.Count > 0 ? Secciones[0] : null;
                foreach (var s in Secciones)
                    if (s.FilaInicio <= filaIdx) res = s;
                return res;
            }

            for (int i = 0; i < panelResultados.Rows.Count; i++)
            {
                // Actualizar sección activa para que DirInicioDeBloque/DireccionAbsoluta funcionen
                seccionActual = SeccionDeFila(i) ?? seccionActual;

                if (panelResultados.Rows[i].Cells[5].Value == null) continue;

                string instruccion = panelResultados.Rows[i].Cells[5].Value?.ToString() ?? "";
                string panelError = panelResultados.Rows[i].Cells[7]?.Value?.ToString() ?? "";
                string cp = panelResultados.Rows[i].Cells[3]?.Value?.ToString() ?? "";
                string operando = panelResultados.Rows[i].Cells[6]?.Value?.ToString() ?? "";
                int saveHex = 0;
                bool noExisteSim = false;

                // Resetear BASE al inicio de cada sección
                if (instruccion == "CSECT") { BASE = "-1"; panelResultados.Rows[i].Cells[8].Value = "---"; continue; }

                // EXTDEF/EXTREF no generan código objeto
                if (instruccion == "EXTDEF" || instruccion == "EXTREF")
                { panelResultados.Rows[i].Cells[8].Value = "---"; continue; }

                if (panelError.Contains("Error: Sintaxis") ||
                    panelError.Contains("Error: Instruccion no existe"))
                { panelResultados.Rows[i].Cells[8].Value = "---"; continue; }

                if (panelError.Contains("Error: Simbolo Duplicado") &&
                    i + 1 < panelResultados.Rows.Count)
                {
                    if (cp == (panelResultados.Rows[i + 1].Cells[3]?.Value?.ToString() ?? ""))
                    { panelResultados.Rows[i].Cells[8].Value = "---"; continue; }
                }

                // ---- START / RESW / RESB / EQU / ORG / USE no generan código objeto ----
                if (instruccion == "START" || instruccion == "RESW" || instruccion == "RESB" ||
                    instruccion == "EQU" || instruccion == "ORG" || instruccion == "USE")
                { panelResultados.Rows[i].Cells[8].Value = "---"; continue; }

                // ---- END: verificar símbolo ----
                if (instruccion == "END")
                {
                    if (!string.IsNullOrEmpty(operando))
                    {
                        bool existe = TablaSimbolos_Panel.Rows.Cast<DataGridViewRow>()
                                             .Any(row => row.Cells[0]?.Value?.ToString() == operando);
                        if (!existe)
                        {
                            panelResultados.Rows[i].Cells[8].Value = "FFFFF";
                            panelResultados.Rows[i].Cells[9].Value = "Error: Simbolo no existe en TabSim";
                            panelResultados.Rows[i].Cells[9].Style.ForeColor = Color.Red;
                        }
                        else
                            panelResultados.Rows[i].Cells[8].Value = "---";
                    }
                    else
                        panelResultados.Rows[i].Cells[8].Value = "---";
                    continue;
                }

                // ---- BASE: definir registro base ----
                if (instruccion == "BASE")
                {
                    string baseOp = System.Text.RegularExpressions.Regex.Replace(operando, @"\s*,\s*", ",")
                                       .TrimStart('#', '@')
                                       .Replace(",X", "", StringComparison.OrdinalIgnoreCase).Trim();
                    bool found = false;
                    // Buscar en TabSim de la sección activa primero
                    if (seccionActual != null && seccionActual.TabSim.TryGetValue(baseOp, out var entBase))
                    {
                        if (!entBase.EsSimbExterno)
                        {
                            int dirAbsBase = DireccionAbsoluta(baseOp);
                            BASE = dirAbsBase >= 0 ? dirAbsBase.ToString("X") : "-1";
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                        {
                            if (row.Cells[0]?.Value?.ToString() == baseOp)
                            {
                                bool esRelBase = (row.Cells[2]?.Value?.ToString() ?? "") == "Relativo";
                                int dirAbsBase = esRelBase
                                    ? DireccionAbsoluta(baseOp)
                                    : Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "0", 16);
                                BASE = dirAbsBase.ToString("X");
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        BASE = "-1";
                        panelResultados.Rows[i].Cells[9].Value = "Error: Simbolo no existe en TabSim";
                        panelResultados.Rows[i].Cells[9].Style.ForeColor = Color.Red;
                    }
                    panelResultados.Rows[i].Cells[8].Value = "---";
                    continue;
                }

                // ---- WORD: genera 3 bytes, acepta expresiones ----
                if (instruccion == "WORD")
                {
                    string exprWord = operando.Trim();
                    string objWord;
                    bool tieneSimExt = TieneSimboloExterno(exprWord);

                    if (int.TryParse(exprWord, out int valDecW))
                        objWord = (valDecW & 0xFFFFFF).ToString("X6").ToUpper().PadLeft(6, '0');
                    else if (exprWord.EndsWith("H", StringComparison.OrdinalIgnoreCase) &&
                             SicLexer.IsHex(exprWord.Substring(0, exprWord.Length - 1)))
                    {
                        int valHexW = Convert.ToInt32(exprWord.Substring(0, exprWord.Length - 1), 16);
                        objWord = valHexW.ToString("X6").ToUpper().PadLeft(6, '0');
                    }
                    else
                    {
                        // Evaluar con SE = 0
                        var evalW = EvaluarExpresionPaso2(exprWord);
                        if (evalW.Error)
                        {
                            panelResultados.Rows[i].Cells[8].Value = "---";
                            panelResultados.Rows[i].Cells[9].Value = "Error: Expresion no valida";
                            panelResultados.Rows[i].Cells[9].Style.ForeColor = Color.Red;
                            continue;
                        }
                        objWord = (evalW.Valor & 0xFFFFFF).ToString("X6").ToUpper().PadLeft(6, '0');

                        if (tieneSimExt)
                        {
                            // Marcar *SE por cada símbolo externo en la expresión + *R por relativos sin pareja
                            string markers = GenerarMarcadoresWORD(exprWord, evalW);
                            objWord += markers;
                        }
                        else if (evalW.EsRelativo)
                            objWord += "*R";
                    }
                    panelResultados.Rows[i].Cells[8].Value = objWord;
                    continue;
                }

                // ---- BYTE: genera código objeto variable ----
                if (instruccion == "BYTE")
                {
                    panelResultados.Rows[i].Cells[8].Value = CalcByteObj(operando);
                    continue;
                }

                // ---- RSUB ----
                if (instruccion == "RSUB")
                { panelResultados.Rows[i].Cells[8].Value = "4F0000"; continue; }

                string formato = panelResultados.Rows[i].Cells[1]?.Value?.ToString() ?? "";

                // ---- Formato 1 ----
                if (formato == "1")
                {
                    if (CodigosInstruccion.ContainsKey(instruccion))
                        panelResultados.Rows[i].Cells[8].Value = CodigosInstruccion[instruccion];
                }
                // ---- Formato 2 ----
                else if (formato == "2")
                {
                    if (CodigosInstruccion.ContainsKey(instruccion))
                    {
                        string opcode = CodigosInstruccion[instruccion];
                        string[] regsA = operando.Split(',');
                        string r0 = regsA.Length > 0 ? (regsA[0].Trim() ?? "") : "";
                        string r1 = regsA.Length > 1 ? (regsA[1].Trim() ?? "") : "";
                        string reg1 = Registros.ContainsKey(r0) ? Registros[r0] : "0";
                        string reg2 = r1.Length > 0
                                            ? (Registros.ContainsKey(r1) ? Registros[r1] : r1)
                                            : "0";
                        panelResultados.Rows[i].Cells[8].Value = opcode + reg1 + reg2;
                    }
                }
                // ---- Formato 3 ----
                else if (formato == "3" && instruccion != "RSUB")
                {
                    // CP de la instrucción siguiente — con bloques debe ser ABSOLUTO
                    //int CP = CPAbsolutoFila(i + 1);

                    int dirFilaActualRelativa = Convert.ToInt32(panelResultados.Rows[i].Cells[3].Value.ToString(), 16);
                    int noBloqueActual = int.Parse(panelResultados.Rows[i].Cells[2].Value.ToString());

                    // El CP real es: (Dirección actual + 3) + Inicio del bloque actual
                    int CP = (dirFilaActualRelativa + 3) + DirInicioDeBloque(noBloqueActual);

                    // Normalizar: quitar espacios alrededor de la coma (ej. "VALOR, X" → "VALOR,X")
                    string operandoNorm3 = System.Text.RegularExpressions.Regex.Replace(operando, @"\s*,\s*", ",");
                    string operandoLimpio = operandoNorm3.TrimStart('@', '#')
                                               .Replace(",X", "", StringComparison.OrdinalIgnoreCase).Trim();
                    int TA;
                    bool esConstante = false;
                    bool esHex = false;

                    // ¿Expresión compuesta? (operadores, paréntesis)
                    bool esExprFmt3 = EsExpresionCompuesta(operandoLimpio);
                    if (esExprFmt3)
                    {
                        var evalF3 = EvaluarExpresionPaso2(operandoLimpio);
                        if (evalF3.Error)
                        {
                            panelResultados.Rows[i].Cells[9].Value = "Error: Expresion no valida";
                            panelResultados.Rows[i].Cells[9].Style.ForeColor = Color.Red;
                            panelResultados.Rows[i].Cells[8].Value = "---"; continue;
                        }
                        TA = evalF3.Valor;
                        if (TA < 0)
                        {
                            panelResultados.Rows[i].Cells[9].Value = "Error: Operando fuera de rango";
                            panelResultados.Rows[i].Cells[9].Style.ForeColor = Color.Red;
                            panelResultados.Rows[i].Cells[8].Value = "---"; continue;
                        }
                        noExisteSim = false;
                        if (operando.StartsWith("#"))
                            esConstante = (TA <= 4095);
                        else
                            esConstante = (!evalF3.EsRelativo && TA <= 4095);
                    }
                    else if (int.TryParse(operandoLimpio.TrimEnd('H', 'h'), out int numero))
                    {
                        int numeroDecimal;
                        if (operandoLimpio.EndsWith("H", StringComparison.OrdinalIgnoreCase))
                        {
                            esHex = true; saveHex = numero; TA = numero;
                            numeroDecimal = Convert.ToInt32(operandoLimpio.TrimEnd('H', 'h'), 16);
                        }
                        else
                        { TA = Convert.ToInt32(numero.ToString("X"), 16); numeroDecimal = numero; }
                        esConstante = (numeroDecimal >= 0 && numeroDecimal <= 4095);
                    }
                    else
                    {
                        // TA_Real = DirRelativa_Simbolo + DirInicio(BloqueDelSimbolo)
                        TA = 0xFFF;
                        noExisteSim = true;

                        if (seccionActual != null && seccionActual.TabSim.TryGetValue(operandoLimpio, out var entradaSim))
                        {
                            noExisteSim = false;
                            if (entradaSim.EsSimbExterno)
                            {
                                // Símbolo externo: valor 0, se marcará *SE en código objeto
                                TA = 0;
                                esConstante = false;
                            }
                            else
                            {
                                int dirRel = entradaSim.Valor == "---" ? 0 : Convert.ToInt32(entradaSim.Valor, 16);
                                if (entradaSim.Tipo == "Absoluto")
                                {
                                    TA = dirRel;
                                    if (TA >= 0 && TA <= 4095) esConstante = true;
                                }
                                else
                                    TA = dirRel + DirInicioDeBloque(entradaSim.NoBloque);
                                if (TA < 0) TA = 0xFFF;
                            }
                        }
                        else
                        {
                            // Fallback: TablaSimbolos_Panel
                            foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                            {
                                string sim = row.Cells[0]?.Value?.ToString() ?? "";
                                if (string.Equals(operandoLimpio, sim, StringComparison.OrdinalIgnoreCase))
                                {
                                    bool esSímRelativo = (row.Cells[2]?.Value?.ToString() ?? "") == "Relativo";
                                    int dirRel = Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "FFF", 16);
                                    if (esSímRelativo)
                                    {
                                        int nb = int.TryParse(
                                            row.Cells.Count > 3 ? row.Cells[3]?.Value?.ToString() : "0",
                                            out int nbf) ? nbf : 0;
                                        TA = dirRel + DirInicioDeBloque(nb);
                                    }
                                    else
                                    {
                                        TA = dirRel;
                                        if (TA >= 0 && TA <= 4095) esConstante = true;
                                    }
                                    if (TA < 0) TA = 0xFFF;
                                    noExisteSim = false;
                                    break;
                                }
                            }
                        }
                    }
                    string nixbpe = "000000";
                    int desp = TA;
                    // CP ya es absoluto (CPAbsolutoFila suma DirInicio del bloque de la instrucción)
                    // BASE también se guarda como dirección absoluta desde la directiva BASE
                    int baseValor;
                    if (BASE == "-1")
                        baseValor = -1;
                    else
                        baseValor = Convert.ToInt32(BASE, 16);

                    if (noExisteSim)
                        panelResultados.Rows[i].Cells[9].Value = "Error: Simbolo no encontrado en TABSIM";

                    if (operandoNorm3.StartsWith("@"))
                    {
                        if (esConstante) nixbpe = "100000";
                        else
                        {
                            desp = TA - CP; if (desp >= -2048 && desp <= 2047) nixbpe = "100010";
                            else
                            {
                                desp = TA - baseValor; if (desp >= 0 && desp <= 4095) nixbpe = "100100";
                                else
                                {
                                    desp = 0xFFF; nixbpe = "100110";
                                    panelResultados.Rows[i].Cells[9].Value = "Error: No relativo a CP/BASE";
                                }
                            }
                        }
                    }
                    else if (operandoNorm3.StartsWith("#"))
                    {
                        if (esConstante) nixbpe = "010000";
                        else
                        {
                            desp = TA - CP; if (desp >= -2048 && desp <= 2047) nixbpe = "010010";
                            else
                            {
                                desp = TA - baseValor; if (desp >= 0 && desp <= 4095) nixbpe = "010100";
                                else
                                {
                                    desp = 0xFFF; nixbpe = "010110";
                                    panelResultados.Rows[i].Cells[9].Value = "Error: No relativo a CP/BASE";
                                }
                            }
                        }
                    }
                    else if (operandoNorm3.EndsWith(",X", StringComparison.OrdinalIgnoreCase))
                    {
                        if (esConstante) nixbpe = "111000";
                        else
                        {
                            desp = TA - CP; if (desp >= -2048 && desp <= 2047) nixbpe = "111010";
                            else
                            {
                                desp = TA - baseValor; if (desp >= 0 && desp <= 4095) nixbpe = "111100";
                                else
                                {
                                    desp = 0xFFF; nixbpe = "111110";
                                    panelResultados.Rows[i].Cells[9].Value = "Error: No relativo a CP/BASE";
                                }
                            }
                        }
                    }
                    else
                    {
                        if (esConstante) nixbpe = "110000";
                        else
                        {
                            desp = TA - CP; if (desp >= -2048 && desp <= 2047) nixbpe = "110010";
                            else
                            {
                                desp = TA - baseValor; if (desp >= 0 && desp <= 4095) nixbpe = "110100";
                                else
                                {
                                    desp = 0xFFF; nixbpe = "110110";
                                    panelResultados.Rows[i].Cells[9].Value = "Error: No relativo a CP/BASE";
                                }
                            }
                        }
                    }

                    if (CodigosInstruccion.ContainsKey(instruccion))
                    {
                        string CodigoOperacion = CodigosInstruccion[instruccion];
                        char codOp_1p = CodigoOperacion[0];
                        int codOp_2p_bin = Convert.ToInt32(CodigoOperacion[1].ToString(), 16);
                        string codOp_2p_bits = Convert.ToString(codOp_2p_bin, 2).PadLeft(4, '0').Substring(0, 2);
                        string sumaBits = codOp_2p_bits + nixbpe;
                        int byteResultado = Convert.ToInt32(sumaBits, 2);
                        string byteResHex = $"{(byteResultado >> 4):X}{(byteResultado & 0xF):X}";
                        string despResHex = esConstante ? TA.ToString("X3") : (desp & 0xFFF).ToString("X3");
                        if (esHex) despResHex = saveHex.ToString("X3");
                        string codFmt3 = $"{codOp_1p}{byteResHex}{despResHex}".ToUpper();
                        // Si el operando es símbolo externo, marcar *SE
                        bool esSE3 = seccionActual != null
                            && seccionActual.TabSim.TryGetValue(operandoLimpio, out var entSE3)
                            && entSE3.EsSimbExterno;
                        panelResultados.Rows[i].Cells[8].Value = esSE3 ? codFmt3 + " *SE" : codFmt3;
                    }
                }
                // ---- Formato 4 ----
                else if (formato == "4")
                {
                    if (!instruccion.StartsWith("+")) continue;
                    instruccion = instruccion.Substring(1) ?? "";

                    // Normalizar: quitar espacios alrededor de la coma en indexado
                    string operandoNorm4 = System.Text.RegularExpressions.Regex.Replace(operando, @"\s*,\s*", ",");
                    string operandoLimpio = operandoNorm4.TrimStart('@', '#')
                                               .Replace(",X", "", StringComparison.OrdinalIgnoreCase).Trim();
                    int TA = 0xFFFFF;
                    bool esM = false;

                    if (operandoLimpio.EndsWith("H", StringComparison.OrdinalIgnoreCase))
                    {
                        // Constante hex: si valor > 4095 es dirección de memoria, si no es constante
                        string nHex = operandoLimpio.TrimEnd('H', 'h');
                        if (int.TryParse(nHex, System.Globalization.NumberStyles.HexNumber, null, out int valorHex))
                        {
                            int valorDecimal = Convert.ToInt32(nHex, 16);
                            if (valorDecimal > 4095) { TA = valorHex; esM = true; }
                        }
                    }
                    else if (int.TryParse(operandoLimpio, out int numDec))
                    {
                        // Constante decimal: si valor > 4095 es dirección de memoria
                        if (numDec > 4095) { TA = numDec; esM = true; }
                    }
                    else
                    {
                        noExisteSim = true; // asumir no encontrado hasta encontrarlo
                        // Primero buscar en TabSim de la sección activa
                        if (seccionActual != null && seccionActual.TabSim.TryGetValue(operandoLimpio, out var entFmt4))
                        {
                            noExisteSim = false;
                            if (entFmt4.EsSimbExterno)
                            {
                                TA = 0; esM = true; // SE: dirección 0, se resolverá en ligado
                            }
                            else
                            {
                                bool esSímRelFmt4 = entFmt4.Tipo == "Relativo";
                                TA = esSímRelFmt4
                                    ? DireccionAbsoluta(operandoLimpio)
                                    : (entFmt4.Valor == "---" ? 0xFFFFF : Convert.ToInt32(entFmt4.Valor, 16));
                                if (TA < 0) TA = 0xFFFFF;
                                esM = true;
                            }
                        }
                        else
                        {
                            foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                            {
                                string sim = row.Cells[0]?.Value?.ToString() ?? "";
                                if (string.Equals(operandoLimpio, sim, StringComparison.OrdinalIgnoreCase))
                                {
                                    bool esSímRelFmt4 = (row.Cells[2]?.Value?.ToString() ?? "") == "Relativo";
                                    // Con bloques: usar dirección absoluta para símbolos relativos
                                    TA = esSímRelFmt4
                                        ? DireccionAbsoluta(operandoLimpio)
                                        : Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "FFFFF", 16);
                                    if (TA < 0) TA = 0xFFFFF;
                                    noExisteSim = false; esM = true; break;
                                }
                            }
                        }
                    }

                    if (noExisteSim)
                        panelResultados.Rows[i].Cells[9].Value = "Error: Simbolo no encontrado en TABSIM";
                    if (!esM && !noExisteSim)
                    { TA = 0xFFFFF; panelResultados.Rows[i].Cells[9].Value = "Error: No existe combinacion MD"; }

                    string nixbpe2 = "000001";
                    if (operandoNorm4.StartsWith("@")) nixbpe2 = noExisteSim ? "010111" : "100001";
                    else if (operandoNorm4.StartsWith("#")) nixbpe2 = noExisteSim ? "010111" : "010001";
                    else if (operandoNorm4.EndsWith(",X", StringComparison.OrdinalIgnoreCase))
                        nixbpe2 = noExisteSim ? "111111" : "111001";
                    else nixbpe2 = noExisteSim ? "100111" : "100001";

                    if (CodigosInstruccion.ContainsKey(instruccion))
                    {
                        string CodigoOperacion = CodigosInstruccion[instruccion];
                        char codOp_1p = CodigoOperacion[0];
                        int codOp_2p_bin = Convert.ToInt32(CodigoOperacion[1].ToString(), 16);
                        string codOp_2p_bits = Convert.ToString(codOp_2p_bin, 2).PadLeft(4, '0').Substring(0, 2);
                        string sumaBits = codOp_2p_bits + nixbpe2;
                        int byteResultado = Convert.ToInt32(sumaBits, 2);
                        string byteResHex = $"{(byteResultado >> 4):X}{(byteResultado & 0xF):X}";
                        string dirResHex = TA.ToString("X5");

                        bool esSE4 = seccionActual != null && seccionActual.TabSim.TryGetValue(
                            System.Text.RegularExpressions.Regex.Replace(operando, @"\s*,\s*", ",")
                                .TrimStart('@', '#').Replace(",X", "", StringComparison.OrdinalIgnoreCase).Trim(),
                            out var entSE4) && entSE4.EsSimbExterno;

                        string marker4 = esSE4 ? "*SE" : (dirResHex == "FFFFF" ? "" : "*R");
                        string val4 = $"{codOp_1p}{byteResHex}{dirResHex}".ToUpper();
                        panelResultados.Rows[i].Cells[8].Value = dirResHex == "FFFFF" ? val4 : val4 + marker4;
                    }
                }
            }
        }

        // Genera marcadores *SE y *R para una expresión WORD con símbolos externos
        private string GenerarMarcadoresWORD(string expr, EvalResult eval)
        {
            if (seccionActual == null) return eval.EsRelativo ? "*R" : "";
            var markers = new System.Text.StringBuilder();
            // Contar términos relativos internos sin pareja (relCount neto)
            // Por cada SE: *SE; por cada relativo sin pareja: *R
            // Estrategia simple: un *SE por SE encontrado, un *R si relCount neto != 0
            int relNet = eval.RelCount;
            foreach (var kv in seccionActual.TabSim)
            {
                if (kv.Value.EsSimbExterno && ContienePalabra(expr, kv.Key))
                    markers.Append(" *SE");
            }
            if (relNet != 0) markers.Append(" *R");
            return markers.ToString();
        }

        private bool ContienePalabra(string expr, string palabra)
        {
            int idx = 0;
            while ((idx = expr.IndexOf(palabra, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                bool antesOk = (idx == 0 || !char.IsLetterOrDigit(expr[idx - 1]));
                bool despuesOk = (idx + palabra.Length >= expr.Length || !char.IsLetterOrDigit(expr[idx + palabra.Length]));
                if (antesOk && despuesOk) return true;
                idx++;
            }
            return false;
        }

        private void CodigoObj_Click(object sender, EventArgs e) => paso2();

        private void btnVerBloques_Click(object sender, EventArgs e)
        {
            if (Secciones.Count == 0)
            {
                MessageBox.Show("No hay bloques. Ejecute Paso 1 primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var win = new Form();
            win.Text = "Tabla de Bloques";
            win.Size = new System.Drawing.Size(560, 380);
            win.StartPosition = FormStartPosition.CenterParent;
            win.FormBorderStyle = FormBorderStyle.FixedDialog;
            win.MaximizeBox = false; win.MinimizeBox = false;

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(6) };
            var lblSel = new Label { Text = "Sección:", AutoSize = true, Location = new System.Drawing.Point(6, 10), Font = new Font("Arial", 9, FontStyle.Bold) };
            var cmbSec = new ComboBox { Location = new System.Drawing.Point(75, 7), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9) };
            foreach (var s in Secciones) cmbSec.Items.Add(s.Nombre);
            if (cmbSec.Items.Count > 0) cmbSec.SelectedIndex = 0;
            panelTop.Controls.Add(lblSel); panelTop.Controls.Add(cmbSec);

            var lbl = new Label
            {
                Text = "TABLA DE BLOQUES",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.MediumPurple,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            var lblTam = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Padding = new Padding(6, 0, 0, 0)
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                Font = new Font("Courier New", 9),
                BackgroundColor = Color.White,
                GridColor = Color.LightGray,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "No.Bloque", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Longitud", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dir.Inicio", ReadOnly = true });

            void CargarBloques(string nombre)
            {
                dgv.Rows.Clear();
                var sec = Secciones.FirstOrDefault(s => s.Nombre == nombre);
                if (sec == null) return;
                foreach (var bl in sec.TabBloques.Values.OrderBy(b => b.NoBloque))
                {
                    string nblq = sec.TabBloques.FirstOrDefault(kv => kv.Value.NoBloque == bl.NoBloque).Key;
                    if (string.IsNullOrEmpty(nblq)) nblq = "(Omisión)";
                    dgv.Rows.Add(bl.NoBloque.ToString(), nblq, bl.Longitud.ToString("X4"), bl.DirInicio.ToString("X4"));
                }
                var ords = sec.TabBloques.Values.OrderBy(b => b.NoBloque).ToList();
                int tam = ords.Count > 0 ? ords.Last().DirInicio + ords.Last().Longitud : 0;
                lblTam.Text = $"  Tamaño de la sección: {tam:X}H";
            }

            cmbSec.SelectedIndexChanged += (s2, e2) => CargarBloques(cmbSec.SelectedItem?.ToString() ?? "");
            if (cmbSec.Items.Count > 0) CargarBloques(Secciones[0].Nombre);

            win.Controls.Add(dgv);
            win.Controls.Add(lblTam);
            win.Controls.Add(lbl);
            win.Controls.Add(panelTop);
            win.ShowDialog(this);
        }

        #region ObjArch
        // ---- objArchivo / FileGenerattor_Click ----
        private void objArchivo()
        {
            rtbObjArchivo.Clear();
            int total = panelResultados.Rows.Count;
            if (total == 0)
            { MessageBox.Show("No hay resultados. Ejecute Paso 1 y Paso 2 primero."); return; }

            // Si no hay secciones (programa sin CSECT), generar como antes con sección virtual
            if (Secciones.Count == 0)
            { MessageBox.Show("Ejecute Paso 1 primero.", "Aviso"); return; }

            var todosObjetos = new List<string>(); // acumulado de todos los programas objeto
            var cortanT = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "RESB","RESW","ORG","USE","CSECT","EXTDEF","EXTREF","START","END","EQU","BASE" };

            foreach (var sec in Secciones)
            {
                seccionActual = sec; // contexto para DireccionAbsoluta
                var lineas = new List<string>();

                // Rango de filas de esta sección
                int filaIni = sec.FilaInicio;
                int filaFin = (sec == Secciones.Last()) ? total - 1 : sec.FilaFin;
                if (filaFin < 0) filaFin = total - 1;

                // ── H ──────────────────────────────────────────────
                string nomSec = sec.Nombre.Length > 6 ? sec.Nombre.Substring(0, 6) : sec.Nombre.PadRight(6);
                var ords = sec.TabBloques.Values.OrderBy(b => b.NoBloque).ToList();
                int longSec = ords.Count > 0 ? ords.Last().DirInicio + ords.Last().Longitud : 0;
                lineas.Add($"H{nomSec}000000{longSec:X6}");

                // ── D y R (en orden de aparición en el código fuente) ──
                for (int fi = filaIni; fi <= filaFin; fi++)
                {
                    if (fi >= total) break;
                    string ins = panelResultados.Rows[fi].Cells[5]?.Value?.ToString() ?? "";
                    string oper = panelResultados.Rows[fi].Cells[6]?.Value?.ToString() ?? "";
                    if (ins == "EXTDEF" && !string.IsNullOrEmpty(oper))
                    {
                        // D record: por cada símbolo EXTDEF incluir nombre+dirección relativa absoluta
                        var sb = new System.Text.StringBuilder("D");
                        foreach (string sim in oper.Split(',').Select(s2 => s2.Trim()).Where(s2 => !string.IsNullOrEmpty(s2)))
                        {
                            string simPad = sim.Length > 6 ? sim.Substring(0, 6) : sim.PadRight(6);
                            int dirAbsSim = DireccionAbsoluta(sim);
                            if (dirAbsSim < 0) dirAbsSim = 0;
                            sb.Append($"{simPad}{dirAbsSim:X6}");
                        }
                        lineas.Add(sb.ToString());
                    }
                    else if (ins == "EXTREF" && !string.IsNullOrEmpty(oper))
                    {
                        // R record: cada símbolo referenciado externamente
                        var sb = new System.Text.StringBuilder("R");
                        foreach (string sim in oper.Split(',').Select(s2 => s2.Trim()).Where(s2 => !string.IsNullOrEmpty(s2)))
                        {
                            string simPad = sim.Length > 6 ? sim.Substring(0, 6) : sim.PadRight(6);
                            sb.Append(simPad);
                        }
                        lineas.Add(sb.ToString());
                    }
                }

                // ── T ──────────────────────────────────────────────
                int i = filaIni;
                while (i <= filaFin && i < total)
                {
                    while (i <= filaFin && i < total)
                    {
                        string ins = panelResultados.Rows[i].Cells[5]?.Value?.ToString() ?? "";
                        string cod = panelResultados.Rows[i].Cells[8]?.Value?.ToString() ?? "---";
                        string codLimpio = StripMarkers(cod);
                        if (cortanT.Contains(ins)) { i++; continue; }
                        if (codLimpio == "---" || codLimpio.Length == 0 ||
                            codLimpio.StartsWith("Error") || codLimpio.Length % 2 != 0)
                        { i++; continue; }
                        break;
                    }
                    if (i > filaFin || i >= total) break;

                    int cpInicialAbs = CPAbsolutoFila(i);
                    var contenidoT = new System.Text.StringBuilder();

                    while (i <= filaFin && i < total)
                    {
                        string ins = panelResultados.Rows[i].Cells[5]?.Value?.ToString() ?? "";
                        string cod = panelResultados.Rows[i].Cells[8]?.Value?.ToString() ?? "---";
                        string codLimpio = StripMarkers(cod);
                        if (cortanT.Contains(ins)) break;
                        if (codLimpio == "---" || codLimpio.Length == 0 ||
                            codLimpio.StartsWith("Error") || codLimpio.Length % 2 != 0)
                        { i++; continue; }
                        if (contenidoT.Length + codLimpio.Length > 60) break;
                        contenidoT.Append(codLimpio);
                        i++;
                    }

                    if (contenidoT.Length > 0)
                        lineas.Add($"T{cpInicialAbs:X6}{contenidoT.Length / 2:X2}{contenidoT}");
                    else i++;
                }

                // ── M ──────────────────────────────────────────────
                for (int j = filaIni; j <= filaFin && j < total; j++)
                {
                    string cod = panelResultados.Rows[j].Cells[8]?.Value?.ToString() ?? "";
                    string ins2 = panelResultados.Rows[j].Cells[5]?.Value?.ToString() ?? "";
                    string fmt = panelResultados.Rows[j].Cells[1]?.Value?.ToString() ?? "";
                    int cpAbsM = CPAbsolutoFila(j);

                    // *R → relocalización por inicio de sección
                    if (cod.Contains("*R"))
                    {
                        if (fmt == "4")
                            lineas.Add($"M{(cpAbsM + 1):X6}05+{sec.Nombre.Substring(0, Math.Min(6, sec.Nombre.Length))}");
                        else if (ins2 == "WORD")
                            lineas.Add($"M{cpAbsM:X6}06+{sec.Nombre.Substring(0, Math.Min(6, sec.Nombre.Length))}");
                    }
                    // *SE → símbolo externo específico
                    if (cod.Contains("*SE"))
                    {
                        // Determinar qué símbolo(s) externos aplican
                        string oper = panelResultados.Rows[j].Cells[6]?.Value?.ToString() ?? "";
                        GenerarRegistrosM_SE(lineas, oper, cpAbsM, fmt, ins2, sec);
                    }
                }

                // ── E ──────────────────────────────────────────────
                if (sec.EsPrincipal)
                {
                    string dirEjecucion = "000000";
                    for (int j = 0; j < total; j++)
                    {
                        if ((panelResultados.Rows[j].Cells[5]?.Value?.ToString() ?? "") != "END") continue;
                        string operEnd = panelResultados.Rows[j].Cells[6]?.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(operEnd))
                        {
                            int d = DireccionAbsoluta(operEnd);
                            if (d >= 0) dirEjecucion = d.ToString("X6").PadLeft(6, '0');
                        }
                        break;
                    }
                    lineas.Add($"E{dirEjecucion}");
                }
                else
                    lineas.Add("E");

                // Acumular y mostrar
                foreach (string linea in lineas)
                {
                    Color c = linea.StartsWith("H") ? Color.Cyan
                            : linea.StartsWith("D") ? Color.LightSkyBlue
                            : linea.StartsWith("R") ? Color.Plum
                            : linea.StartsWith("T") ? Color.LimeGreen
                            : linea.StartsWith("M") ? Color.Orange
                            : linea.StartsWith("E") ? Color.Yellow
                            : Color.White;
                    AgregarLineaColoreada(linea, c);
                }
                AgregarLineaColoreada("", Color.White); // línea vacía entre secciones
                todosObjetos.AddRange(lineas);

                // Guardar archivo por sección
                string rutaArchivo = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, $"{sec.Nombre}.obj");
                try { System.IO.File.WriteAllLines(rutaArchivo, lineas); }
                catch (Exception ex)
                { MessageBox.Show("No se pudo guardar el archivo:\n" + ex.Message); }
            }

            MessageBox.Show($"Programa(s) objeto generado(s) en:\n{AppDomain.CurrentDomain.BaseDirectory}",
                            "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Quita todos los marcadores al final del código objeto para el registro T
        private string StripMarkers(string cod)
        {
            if (string.IsNullOrEmpty(cod)) return cod;
            // Eliminar " *SE", "*SE", "*R", "*" al final
            string s = cod;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s*\*SE", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\*R", "");
            s = s.TrimEnd('*').Trim();
            return s;
        }

        // Genera registros M para símbolos externos en una expresión WORD o fmt4
        private void GenerarRegistrosM_SE(List<string> lineas, string expr, int cpAbs, string fmt, string ins2, SeccionControl sec)
        {
            if (sec == null) return;
            foreach (var kv in sec.TabSim)
            {
                if (!kv.Value.EsSimbExterno) continue;
                if (!ContienePalabra(expr, kv.Key)) continue;
                // Determinar signo: buscar si el símbolo aparece con signo + o - en la expresión
                char signo = '+';
                int idx = expr.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    for (int k = idx - 1; k >= 0; k--)
                    {
                        if (expr[k] == '-') { signo = '-'; break; }
                        if (expr[k] == '+') { signo = '+'; break; }
                        if (!char.IsWhiteSpace(expr[k])) break;
                    }
                }
                string simName = kv.Key.Length > 6 ? kv.Key.Substring(0, 6) : kv.Key;
                if (fmt == "4")
                    lineas.Add($"M{(cpAbs + 1):X6}05{signo}{simName}");
                else if (ins2 == "WORD")
                    lineas.Add($"M{cpAbs:X6}06{signo}{simName}");
            }
        }

        private void AgregarLineaColoreada(string texto, Color color)
        {
            int start = rtbObjArchivo.TextLength;
            rtbObjArchivo.AppendText(texto + "\n");
            rtbObjArchivo.Select(start, texto.Length);
            rtbObjArchivo.SelectionColor = color;
            rtbObjArchivo.SelectionLength = 0;
        }

        private void FileGenerattor_Click(object sender, EventArgs e) => objArchivo();
        #endregion


        // ---- Ver TABSIM (ventana emergente con selector de sección) ----
        private void btnVerTabSim_Click(object sender, EventArgs e)
        {
            if (Secciones.Count == 0 || Secciones.All(s => s.TabSim.Count == 0))
            {
                MessageBox.Show("No hay símbolos en TABSIM. Ejecute Paso 1 primero.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var win = new Form();
            win.Text = "Tabla de Símbolos (TABSIM)";
            win.Size = new System.Drawing.Size(640, 520);
            win.StartPosition = FormStartPosition.CenterParent;
            win.FormBorderStyle = FormBorderStyle.FixedDialog;
            win.MaximizeBox = false; win.MinimizeBox = false;

            // Selector de sección
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(6) };
            var lblSel = new Label { Text = "Sección:", AutoSize = true, Location = new System.Drawing.Point(6, 10), Font = new Font("Arial", 9, FontStyle.Bold) };
            var cmbSec = new ComboBox { Location = new System.Drawing.Point(75, 7), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 9) };
            foreach (var s in Secciones) cmbSec.Items.Add(s.Nombre);
            if (cmbSec.Items.Count > 0) cmbSec.SelectedIndex = 0;
            panelTop.Controls.Add(lblSel);
            panelTop.Controls.Add(cmbSec);

            var lbl = new Label
            {
                Text = "TABLA DE SÍMBOLOS (TABSIM)",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.LightGreen,
                ForeColor = Color.DarkGreen,
                Font = new Font("Arial", 10, FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                Font = new Font("Courier New", 9),
                BackgroundColor = Color.White,
                GridColor = Color.LightGray,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Símbolo", ReadOnly = true, Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dirección", ReadOnly = true, Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tipo", ReadOnly = true, Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "No.Bloque", ReadOnly = true, Width = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "S.E.", ReadOnly = true, Width = 50 });

            void CargarSeccion(string nombre)
            {
                dgv.Rows.Clear();
                var sec = Secciones.FirstOrDefault(s => s.Nombre == nombre);
                if (sec == null) return;
                foreach (var kv in sec.TabSim)
                {
                    var (val, tipo, nb, esSE) = kv.Value;
                    var row2 = new DataGridViewRow(); row2.CreateCells(dgv);
                    row2.Cells[0].Value = kv.Key;
                    row2.Cells[1].Value = esSE ? "---" : val;
                    row2.Cells[2].Value = esSE ? "---" : tipo;
                    row2.Cells[3].Value = esSE ? "---" : nb.ToString();
                    row2.Cells[4].Value = esSE ? "Sí" : "No";
                    if (esSE) for (int ci = 0; ci < 5; ci++) row2.Cells[ci].Style.ForeColor = Color.Gray;
                    dgv.Rows.Add(row2);
                }
            }

            cmbSec.SelectedIndexChanged += (s2, e2) => CargarSeccion(cmbSec.SelectedItem?.ToString() ?? "");
            if (cmbSec.Items.Count > 0) CargarSeccion(Secciones[0].Nombre);

            win.Controls.Add(dgv);
            win.Controls.Add(lbl);
            win.Controls.Add(panelTop);
            win.ShowDialog(this);
        }

        // ---- Ver Código Objeto (ventana emergente) ----
        private void btnVerCodObj_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(rtbObjArchivo.Text))
            {
                MessageBox.Show("No hay código objeto. Ejecute 'Generar Programa Objeto' primero.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var win = new Form();
            string secNames = Secciones.Count > 0
                ? string.Join(", ", Secciones.Select(s => s.Nombre))
                : "PROG";
            win.Text = $"Código Objeto (.OBJ) — Secciones: {secNames}";
            win.Size = new System.Drawing.Size(660, 520);
            win.StartPosition = FormStartPosition.CenterParent;
            win.FormBorderStyle = FormBorderStyle.FixedDialog;
            win.MaximizeBox = false;
            win.MinimizeBox = false;

            var lbl = new Label();
            lbl.Text = "CÓDIGO OBJETO (.OBJ)";
            lbl.Dock = DockStyle.Top;
            lbl.Height = 30;
            lbl.BackColor = Color.Goldenrod;
            lbl.ForeColor = Color.White;
            lbl.Font = new Font("Arial", 10, FontStyle.Bold);
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            var rtb = new RichTextBox();
            rtb.Dock = DockStyle.Fill;
            rtb.ReadOnly = true;
            rtb.Font = new Font("Courier New", 10);
            rtb.BackColor = Color.Black;
            rtb.ForeColor = Color.LimeGreen;
            rtb.ScrollBars = RichTextBoxScrollBars.Both;
            rtb.WordWrap = false;
            rtb.Rtf = rtbObjArchivo.Rtf;

            win.Controls.Add(rtb);
            win.Controls.Add(lbl);
            win.ShowDialog(this);
        }

    }
}