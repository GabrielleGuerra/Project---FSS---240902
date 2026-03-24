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
            { "START","END","BASE","RESW","RESB","WORD","BYTE", "ORG", "EQU" };

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
                    if (esEQU || esWORD)
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

    // ================================================================
    //  ERROR INFO
    // ================================================================
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

    // ================================================================
    //  VALIDADOR
    // ================================================================
    public static class SicValidator
    {
        static readonly HashSet<int> InsTypes = new HashSet<int>
            { 3,6,7,8,9,10,11,12,13,15,16,17,18,19,20,21,30,31 };

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
            { 3,6,7,8,9,10,11,12,13,15,16,17,18,19,20,21,30,31 };

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

    // ================================================================
    //  FORM1
    // ================================================================
    public partial class Form1 : Form
    {
        string Archivo = string.Empty;
        List<List<string>> codigo = new List<List<string>>();
        public static List<string> ListaErrores = new List<string>();
        Dictionary<string, (string Valor, string Tipo)> TabSim = new Dictionary<string, (string Valor, string Tipo)>();

        public Form1()
        {
            InitializeComponent();
        }

        private string originalFileName = "Errores";

        // ---- cargarArchivo_Click → Cargar Archivo ----
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

                // Solo líneas no vacías y no comentarios
                var nonEmpty = rawLines
                    .Select((l, idx) => new { Line = l.Trim(), Idx = idx })
                    .Where(x => !string.IsNullOrEmpty(x.Line) && !x.Line.StartsWith("."))
                    .ToList();

                int startCount = 0, endCount = 0;

                for (int i = 0; i < nonEmpty.Count; i++)
                {
                    string trimmed = nonEmpty[i].Line;
                    int lineNum = nonEmpty[i].Idx + 1;
                    bool esIni = (i == 0);
                    bool esFin = (i == nonEmpty.Count - 1);

                    var tokens = SicLexer.Tokenize(trimmed);
                    SicValidator.Validate(tokens, errors, lineNum, trimmed,
                                          esIni, esFin, startCount, endCount);

                    if (tokens.Any(t => t.Type == 7)) startCount++;
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

        // ================================================================
        //  PASO 1
        // ================================================================
        private void Paso1()
        {
            int ContadorPrograma = 0;

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
                r.Cells[2].Value = ContadorPrograma.ToString("X4");

                // ============================================================
                //  PRIMERA LÍNEA (START)
                // ============================================================
                if (i == 0)
                {
                    r.Cells[1].Value = "---";
                    r.Cells[6].Value = "---";

                    if (t.Count > 0 && t[0].Type == 25)
                        r.Cells[3].Value = t[0].Text;

                    if (ListaErrores.Count > 0)
                    {
                        r.Cells[6].Value = "Error: Sintaxis";
                        r.Cells[6].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        var insTok = t.FirstOrDefault(x => x.Type == 7);
                        if (insTok != null) r.Cells[4].Value = insTok.Text;

                        List<string> op = RegresarOperandos(t);
                        if (op.Count >= 1)
                        {
                            r.Cells[5].Value = op[0];
                            string num = op[0];
                            if (num.EndsWith("H", StringComparison.OrdinalIgnoreCase))
                                ContadorPrograma += ParseDecOrHex(num);
                            else if (int.TryParse(num, out int parsed))
                                ContadorPrograma += parsed;
                        }
                    }
                }
                // ============================================================
                //  ÚLTIMA LÍNEA (END)
                // ============================================================
                else if (i == codigo.Count - 1)
                {
                    r.Cells[1].Value = "---";
                    r.Cells[6].Value = "---";

                    if (ListaErrores.Count > 0)
                    {
                        r.Cells[6].Value = "Error: Sintaxis";
                        r.Cells[6].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        var insTok = t.FirstOrDefault(x => x.Type >= 3 && x.Type <= 21);
                        if (insTok != null) r.Cells[4].Value = insTok.Text;

                        List<string> op = RegresarOperandos(t);
                        if (op.Count >= 1)
                            r.Cells[5].Value = op[0];
                        // Validación de existencia del símbolo se hace en Paso2
                    }
                }
                // ============================================================
                //  LÍNEAS INTERMEDIAS
                // ============================================================
                else
                {
                    r.Cells[6].Value = "---";
                    bool ErrorSimboloDuplicado = false;

                    if (t.Count > 0 && t[0].Type == 25)
                    {
                        r.Cells[3].Value = t[0].Text;
                        bool esEQU_check = t.Any(tk => tk.Type == 31);
                        if (!esEQU_check && TabSim.ContainsKey(t[0].Text))
                            ErrorSimboloDuplicado = true;
                    }

                    r.Cells[1].Value = RegresarFormato(t);
                    r.Cells[4].Value = RegresarInstruccion(t);
                    List<string> op = RegresarOperandos(t);
                    if (op.Count > 0)
                        r.Cells[5].Value = string.Join(",", op);

                    // Modo de direccionamiento (fmt 3/4)
                    string fmt = r.Cells[1].Value?.ToString() ?? "";
                    if (r.Cells[5].Value != null && (fmt == "3" || fmt == "4"))
                    {
                        string operando = r.Cells[5].Value?.ToString() ?? "";
                        if (operando.Contains("#")) r.Cells[6].Value = "Inmediato";
                        else if (operando.Contains("@")) r.Cells[6].Value = "Indirecto";
                        else r.Cells[6].Value = "Simple";
                    }

                    // RSUB sin operandos
                    string ins4 = r.Cells[4].Value?.ToString() ?? "";
                    if (ins4 == "RSUB" || ins4 == "+RSUB")
                    {
                        bool sinOp = (r.Cells[5].Value == null || (r.Cells[5].Value?.ToString() ?? "") == "");
                        r.Cells[6].Value = sinOp ? "---" : "Error: Sintaxis";
                        if (!sinOp) r.Cells[6].Style.ForeColor = Color.Red;
                    }

                    // Formato 1 sin operandos
                    if (fmt == "1" && r.Cells[5].Value != null && (r.Cells[5].Value?.ToString() ?? "") != "")
                    {
                        r.Cells[6].Value = "Error: Sintaxis";
                        r.Cells[6].Style.ForeColor = Color.Red;
                    }

                    // Errores de validación
                    if (ListaErrores.Count > 0)
                    {
                        r.Cells[6].Value =
                            r.Cells[4].Value?.ToString() == "Error"
                                ? "Error: Instruccion no existe"
                                : "Error: Sintaxis";
                        r.Cells[6].Style.ForeColor = Color.Red;
                    }
                    else if (ErrorSimboloDuplicado)
                    {
                        r.Cells[6].Value = "Error: Simbolo Duplicado";
                        r.Cells[6].Style.ForeColor = Color.Red;
                    }
                    else
                    {
                        string currentIns = r.Cells[4].Value?.ToString() ?? "";

                        // ---- Handle ORG ----
                        if (currentIns == "ORG")
                        {
                            string oper = r.Cells[5].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(oper))
                                ContadorPrograma = ParseDecOrHex(oper);
                        }

                        // ---- Handle EQU ----
                        if (currentIns == "EQU" && t.Count > 0 && t[0].Type == 25)
                        {
                            string labelEqu = t[0].Text;
                            if (TabSim.ContainsKey(labelEqu))
                            {
                                r.Cells[6].Value = "Error: Simbolo Duplicado";
                                r.Cells[6].Style.ForeColor = Color.Red;
                            }
                            else
                            {
                                string operEqu = r.Cells[5].Value?.ToString() ?? "";
                                if (operEqu == "*")
                                {
                                    string hexCP = ContadorPrograma.ToString("X4").ToUpper();
                                    TabSim[labelEqu] = (hexCP, "Relativo");
                                    InsertarTabSim(labelEqu, hexCP, "Relativo");
                                }
                                else
                                {
                                    var evalResult = EvaluarExpresionEQU(operEqu);
                                    if (evalResult.Error)
                                    {
                                        r.Cells[6].Value = "Error: Expresion no valida";
                                        r.Cells[6].Style.ForeColor = Color.Red;
                                        TabSim[labelEqu] = ("FFFF", "Absoluto");
                                        InsertarTabSim(labelEqu, "FFFF", "Absoluto");
                                    }
                                    else
                                    {
                                        string hexVal = evalResult.Valor.ToString("X4").ToUpper();
                                        string tipo = evalResult.EsRelativo ? "Relativo" : "Absoluto";
                                        TabSim[labelEqu] = (hexVal, tipo);
                                        InsertarTabSim(labelEqu, hexVal, tipo);
                                    }
                                }
                            }
                        }

                        // ---- Insertar símbolo en TabSim (solo etiquetas no-EQU) ----
                        if (t.Count > 0 && t[0].Type == 25 && currentIns != "EQU")
                        {
                            string hexVal = ContadorPrograma.ToString("X").ToUpper();
                            TabSim[t[0].Text] = (hexVal, "Relativo");
                            InsertarTabSim(t[0].Text, hexVal, "Relativo");
                        }
                    }

                    // ---- Incremento CP ----
                    string err6 = r.Cells[6].Value?.ToString() ?? "";
                    bool hayError = err6.StartsWith("Error");

                    if (!hayError)
                    {
                        string ins2 = r.Cells[4].Value?.ToString() ?? "";
                        string oper = r.Cells[5].Value?.ToString() ?? "";

                        switch (fmt)
                        {
                            case "1": ContadorPrograma += 1; break;
                            case "2": ContadorPrograma += 2; break;
                            case "3": ContadorPrograma += 3; break;
                            case "4": ContadorPrograma += 4; break;
                            case "---":
                                switch (ins2)
                                {
                                    case "BASE": break;
                                    case "ORG": break;
                                    case "EQU": break;
                                    case "RESW": ContadorPrograma += ParseDecOrHex(oper) * 3; break;
                                    case "RESB": ContadorPrograma += ParseDecOrHex(oper); break;
                                    case "WORD": ContadorPrograma += 3; break;
                                    case "BYTE": ContadorPrograma += CalcByteSize(oper); break;
                                }
                                break;
                        }
                    }
                }

                panelResultados.Rows.Add(r);
            }

            numTamProg.Text = ContadorPrograma.ToString("X") + "H";
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

        // ---- Inserta símbolo en TablaSimbolos_Panel (cols: Símbolo, Dirección, Tipo) ----
        private void InsertarTabSim(string simbolo, string hexVal, string tipo)
        {
            var rs = new DataGridViewRow();
            rs.CreateCells(TablaSimbolos_Panel);
            rs.Cells[0].Value = simbolo;
            rs.Cells[1].Value = hexVal;
            rs.Cells[2].Value = tipo;
            TablaSimbolos_Panel.Rows.Add(rs);
        }

        // ================================================================
        //  INFRAESTRUCTURA DE EVALUACIÓN DE EXPRESIONES
        //  Compartida por Paso1 (EQU) y Paso2 (Formato 3/4, WORD)
        // ================================================================
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
                if (TabSim.ContainsKey(word))
                {
                    var e2 = TabSim[word];
                    list.Add(new ExprToken
                    {
                        Kind = ExprTokKind.Sym,
                        NumVal = Convert.ToInt32(e2.Valor, 16),
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
                foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                {
                    string nom = row.Cells[0]?.Value?.ToString() ?? "";
                    if (string.Equals(nom, word, StringComparison.OrdinalIgnoreCase))
                    {
                        int sv = Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "0", 16);
                        bool sr = (row.Cells[2]?.Value?.ToString() ?? "") == "Relativo";
                        list.Add(new ExprToken { Kind = ExprTokKind.Sym, NumVal = sv, EsRel = sr, Text = word });
                        found = true; break;
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
                // Tipos de instrucción/directiva: 3,6,7-13,15-21,30,31
                // Excluir tipo 14 (registro) que también cae en el rango
                if (token.Type == 3 || token.Type == 6 ||
                   (token.Type >= 7 && token.Type <= 13) ||
                   (token.Type >= 15 && token.Type <= 21) ||
                   token.Type == 30 || token.Type == 31)
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
            panelResultados.Rows.Clear();
            string inputText = rtbCode.Text;
            TabSim = new Dictionary<string, (string Valor, string Tipo)>();
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
        List<string> DirectivasNO = new List<string> { "START", "END", "BASE", "RESW", "RESB" };

        Dictionary<string, string> Registros = new Dictionary<string, string>
            { {"A","0"},{"X","1"},{"L","2"},{"B","3"},{"S","4"},
              {"T","5"},{"F","6"},{"PC","8"},{"SW","9"} };

        string BASE = "-1";

        // ================================================================
        //  PASO 2
        // ================================================================
        private void paso2()
        {
            BASE = "-1"; // Resetear BASE al iniciar paso2

            for (int i = 0; i < panelResultados.Rows.Count; i++)
            {
                if (panelResultados.Rows[i].Cells[4].Value == null) continue;

                string instruccion = panelResultados.Rows[i].Cells[4].Value?.ToString() ?? "";
                string panelError = panelResultados.Rows[i].Cells[6]?.Value?.ToString() ?? "";
                string cp = panelResultados.Rows[i].Cells[2]?.Value?.ToString() ?? "";
                string operando = panelResultados.Rows[i].Cells[5]?.Value?.ToString() ?? "";
                int saveHex = 0;
                bool noExisteSim = false;

                if (panelError.Contains("Error: Sintaxis") ||
                    panelError.Contains("Error: Instruccion no existe"))
                { panelResultados.Rows[i].Cells[7].Value = "---"; continue; }

                if (panelError.Contains("Error: Simbolo Duplicado") &&
                    i + 1 < panelResultados.Rows.Count)
                {
                    if (cp == (panelResultados.Rows[i + 1].Cells[2]?.Value?.ToString() ?? ""))
                    { panelResultados.Rows[i].Cells[7].Value = "---"; continue; }
                }

                // ---- START / RESW / RESB / EQU / ORG no generan código objeto ----
                if (instruccion == "START" || instruccion == "RESW" || instruccion == "RESB" ||
                    instruccion == "EQU" || instruccion == "ORG")
                { panelResultados.Rows[i].Cells[7].Value = "---"; continue; }

                // ---- END: verificar símbolo ----
                if (instruccion == "END")
                {
                    if (!string.IsNullOrEmpty(operando))
                    {
                        bool existe = TablaSimbolos_Panel.Rows.Cast<DataGridViewRow>()
                                             .Any(row => row.Cells[0]?.Value?.ToString() == operando);
                        if (!existe)
                        {
                            panelResultados.Rows[i].Cells[7].Value = "FFFFF";
                            panelResultados.Rows[i].Cells[8].Value = "Error: Simbolo no existe en TabSim";
                            panelResultados.Rows[i].Cells[8].Style.ForeColor = Color.Red;
                        }
                        else
                            panelResultados.Rows[i].Cells[7].Value = "---";
                    }
                    else
                        panelResultados.Rows[i].Cells[7].Value = "---";
                    continue;
                }

                // ---- BASE: definir registro base ----
                if (instruccion == "BASE")
                {
                    string baseOp = System.Text.RegularExpressions.Regex.Replace(operando, @"\s*,\s*", ",")
                                       .TrimStart('#', '@')
                                       .Replace(",X", "", StringComparison.OrdinalIgnoreCase).Trim();
                    bool found = false;
                    foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                    {
                        if (row.Cells[0]?.Value?.ToString() == baseOp)
                        {
                            BASE = row.Cells[1]?.Value?.ToString() ?? "-1";
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        BASE = "-1";
                        panelResultados.Rows[i].Cells[8].Value = "Error: Simbolo no existe en TabSim";
                        panelResultados.Rows[i].Cells[8].Style.ForeColor = Color.Red;
                    }
                    panelResultados.Rows[i].Cells[7].Value = "---";
                    continue;
                }

                // ---- WORD: genera 3 bytes, acepta expresiones ----
                if (instruccion == "WORD")
                {
                    string exprWord = operando.Trim();
                    string objWord;
                    if (int.TryParse(exprWord, out int valDecW))
                        objWord = valDecW.ToString("X6").ToUpper().PadLeft(6, '0');
                    else if (exprWord.EndsWith("H", StringComparison.OrdinalIgnoreCase) &&
                             SicLexer.IsHex(exprWord.Substring(0, exprWord.Length - 1)))
                    {
                        int valHexW = Convert.ToInt32(exprWord.Substring(0, exprWord.Length - 1), 16);
                        objWord = valHexW.ToString("X6").ToUpper().PadLeft(6, '0');
                    }
                    else
                    {
                        var evalW = EvaluarExpresionPaso2(exprWord);
                        if (evalW.Error)
                        {
                            panelResultados.Rows[i].Cells[7].Value = "---";
                            panelResultados.Rows[i].Cells[8].Value = "Error: Expresion no valida";
                            panelResultados.Rows[i].Cells[8].Style.ForeColor = Color.Red;
                            continue;
                        }
                        objWord = (evalW.Valor & 0xFFFFFF).ToString("X6").ToUpper().PadLeft(6, '0');
                        if (evalW.EsRelativo) objWord += "*";
                    }
                    panelResultados.Rows[i].Cells[7].Value = objWord;
                    continue;
                }

                // ---- BYTE: genera código objeto variable ----
                if (instruccion == "BYTE")
                {
                    panelResultados.Rows[i].Cells[7].Value = CalcByteObj(operando);
                    continue;
                }

                // ---- RSUB ----
                if (instruccion == "RSUB")
                { panelResultados.Rows[i].Cells[7].Value = "4F0000"; continue; }

                string formato = panelResultados.Rows[i].Cells[1]?.Value?.ToString() ?? "";

                // ---- Formato 1 ----
                if (formato == "1")
                {
                    if (CodigosInstruccion.ContainsKey(instruccion))
                        panelResultados.Rows[i].Cells[7].Value = CodigosInstruccion[instruccion];
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
                        panelResultados.Rows[i].Cells[7].Value = opcode + reg1 + reg2;
                    }
                }
                // ---- Formato 3 ----
                else if (formato == "3" && instruccion != "RSUB")
                {
                    int CP = (i + 1 < panelResultados.Rows.Count)
                        ? Convert.ToInt32(panelResultados.Rows[i + 1].Cells[2]?.Value?.ToString() ?? "0", 16)
                        : 0;

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
                            panelResultados.Rows[i].Cells[8].Value = "Error: Expresion no valida";
                            panelResultados.Rows[i].Cells[8].Style.ForeColor = Color.Red;
                            panelResultados.Rows[i].Cells[7].Value = "---"; continue;
                        }
                        TA = evalF3.Valor;
                        if (TA < 0)
                        {
                            panelResultados.Rows[i].Cells[8].Value = "Error: Operando fuera de rango";
                            panelResultados.Rows[i].Cells[8].Style.ForeColor = Color.Red;
                            panelResultados.Rows[i].Cells[7].Value = "---"; continue;
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
                        TA = 0xFFF;
                        noExisteSim = true; // asumir no encontrado hasta encontrarlo
                        foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                        {
                            string sim = row.Cells[0]?.Value?.ToString() ?? "";
                            if (string.Equals(operandoLimpio, sim, StringComparison.OrdinalIgnoreCase))
                            {
                                TA = Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "FFF", 16);
                                noExisteSim = false;
                                // Símbolo Absoluto en rango 0-4095 → constante; Relativo → dirección
                                string tipoSim = row.Cells[2]?.Value?.ToString() ?? "";
                                if (tipoSim == "Absoluto" && TA >= 0 && TA <= 4095)
                                    esConstante = true;
                                break;
                            }
                        }
                    }
                    string nixbpe = "000000";
                    int desp = TA;
                    int baseValor = BASE == "-1" ? -1 : Convert.ToInt32(BASE, 16);

                    if (noExisteSim)
                        panelResultados.Rows[i].Cells[8].Value = "Error: Simbolo no encontrado en TABSIM";

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
                                    panelResultados.Rows[i].Cells[8].Value = "Error: No relativo a CP/BASE";
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
                                    panelResultados.Rows[i].Cells[8].Value = "Error: No relativo a CP/BASE";
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
                                    panelResultados.Rows[i].Cells[8].Value = "Error: No relativo a CP/BASE";
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
                                    panelResultados.Rows[i].Cells[8].Value = "Error: No relativo a CP/BASE";
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
                        panelResultados.Rows[i].Cells[7].Value = $"{codOp_1p}{byteResHex}{despResHex}".ToUpper();
                    }
                }
                // ---- Formato 4 ----
                else if (formato == "4")
                {
                    if (!instruccion.StartsWith("+")) continue;
                    instruccion = instruccion.Substring(1) ?? "";

                    // Normalizar: quitar espacios alrededor de la coma (ej. "VALOR, X" → "VALOR,X")
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
                        foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                        {
                            string sim = row.Cells[0]?.Value?.ToString() ?? "";
                            if (string.Equals(operandoLimpio, sim, StringComparison.OrdinalIgnoreCase))
                            {
                                TA = Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "FFFFF", 16);
                                noExisteSim = false; esM = true; break;
                            }
                        }
                    }

                    if (noExisteSim)
                        panelResultados.Rows[i].Cells[8].Value = "Error: Simbolo no encontrado en TABSIM";
                    if (!esM && !noExisteSim)
                    { TA = 0xFFFFF; panelResultados.Rows[i].Cells[8].Value = "Error: No existe combinacion MD"; }

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

                        panelResultados.Rows[i].Cells[7].Value = dirResHex == "FFFFF"
                            ? $"{codOp_1p}{byteResHex}{dirResHex}".ToUpper()
                            : $"{codOp_1p}{byteResHex}{dirResHex}*".ToUpper();
                    }
                }
            }
        }

        private void CodigoObj_Click(object sender, EventArgs e) => paso2();

        #region ObjArch
        // ---- objArchivo / FileGenerattor_Click ----
        private void objArchivo()
        {
            var lineas = new List<string>();
            rtbObjArchivo.Clear();

            int total = panelResultados.Rows.Count;
            if (total == 0)
            { MessageBox.Show("No hay resultados. Ejecute Paso 1 y Paso 2 primero."); return; }

            // ── Registro H ──────────────────────────────────────────────────────
            string primeraEtiquetaRaw = panelResultados.Rows[0].Cells[3]?.Value?.ToString() ?? "";
            string nombreProg = primeraEtiquetaRaw.Length > 6
                ? primeraEtiquetaRaw.Substring(0, 6)
                : primeraEtiquetaRaw.PadRight(6, '_');
            string dirInicioProg = (panelResultados.Rows[0].Cells[2]?.Value?.ToString() ?? "0").PadLeft(6, '0');
            string longProg = (panelResultados.Rows[total - 1].Cells[2]?.Value?.ToString() ?? "0").PadLeft(6, '0');
            lineas.Add($"H{nombreProg}{dirInicioProg}{longProg}");

            // ── Registros T ──────────────────────────────────────────────────────
            var cortanT = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "RESB", "RESW", "ORG" };

            int i = 0;
            while (i < total)
            {
                // Avanzar hasta la primera fila con código objeto válido que NO corte T
                while (i < total)
                {
                    string ins = panelResultados.Rows[i].Cells[4]?.Value?.ToString() ?? "";
                    string cod = panelResultados.Rows[i].Cells[7]?.Value?.ToString() ?? "---";
                    string codLimpio = cod.TrimEnd('*');
                    if (cortanT.Contains(ins)) { i++; continue; }
                    if (codLimpio == "---" || codLimpio.Length == 0 ||
                        codLimpio.StartsWith("Error") || codLimpio.Length % 2 != 0)
                    { i++; continue; }
                    break;
                }
                if (i >= total) break;

                string cpInicial = (panelResultados.Rows[i].Cells[2]?.Value?.ToString() ?? "0").PadLeft(6, '0');
                var contenidoT = new System.Text.StringBuilder();

                while (i < total)
                {
                    string ins = panelResultados.Rows[i].Cells[4]?.Value?.ToString() ?? "";
                    string cod = panelResultados.Rows[i].Cells[7]?.Value?.ToString() ?? "---";
                    string codLimpio = cod.TrimEnd('*');
                    if (cortanT.Contains(ins)) break;
                    if (codLimpio == "---" || codLimpio.Length == 0 ||
                        codLimpio.StartsWith("Error") || codLimpio.Length % 2 != 0)
                    { i++; continue; }
                    if (contenidoT.Length + codLimpio.Length > 60) break;
                    contenidoT.Append(codLimpio);
                    i++;
                }

                if (contenidoT.Length > 0)
                    lineas.Add($"T{cpInicial}{contenidoT.Length / 2:X2}{contenidoT}");
                else
                    i++; // evitar loop infinito
            }

            // ── Registros M ──────────────────────────────────────────────────────
            for (int j = 0; j < total; j++)
            {
                string cod = panelResultados.Rows[j].Cells[7]?.Value?.ToString() ?? "";
                if (!cod.EndsWith("*")) continue;
                string ins2 = panelResultados.Rows[j].Cells[4]?.Value?.ToString() ?? "";
                string fmt = panelResultados.Rows[j].Cells[1]?.Value?.ToString() ?? "";
                if (!int.TryParse(panelResultados.Rows[j].Cells[2]?.Value?.ToString() ?? "0",
                        System.Globalization.NumberStyles.HexNumber, null, out int cpValor))
                    cpValor = 0;
                if (fmt == "4")
                    lineas.Add($"M{(cpValor + 1):X6}05+{nombreProg}");
                else if (ins2 == "WORD")
                    lineas.Add($"M{cpValor:X6}06+{nombreProg}");
            }

            // ── Registro E ───────────────────────────────────────────────────────
            string dirEjecucion = dirInicioProg;
            for (int j = 0; j < total; j++)
            {
                string ins2 = panelResultados.Rows[j].Cells[4]?.Value?.ToString() ?? "";
                if (ins2 != "END") continue;
                string operandoEnd = panelResultados.Rows[j].Cells[5]?.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(operandoEnd))
                {
                    foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                    {
                        if (string.Equals(row.Cells[0]?.Value?.ToString() ?? "",
                                          operandoEnd, StringComparison.OrdinalIgnoreCase))
                        { dirEjecucion = (row.Cells[1]?.Value?.ToString() ?? "0").PadLeft(6, '0'); break; }
                    }
                }
                break;
            }
            lineas.Add($"E{dirEjecucion}");

            // ── Guardar archivo .obj ──────────────────────────────────────────────
            string rutaArchivo = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, $"{primeraEtiquetaRaw}.obj");
            try { System.IO.File.WriteAllLines(rutaArchivo, lineas); }
            catch (Exception ex)
            { MessageBox.Show("No se pudo guardar el archivo:\n" + ex.Message); return; }

            // ── Mostrar en rtbObjArchivo con colores ──────────────────────────────
            foreach (string linea in lineas)
            {
                Color c = linea.StartsWith("H") ? Color.Cyan
                        : linea.StartsWith("T") ? Color.LimeGreen
                        : linea.StartsWith("M") ? Color.Orange
                        : linea.StartsWith("E") ? Color.Yellow
                        : Color.White;
                AgregarLineaColoreada(linea, c);
            }
            MessageBox.Show($"Archivo objeto generado en:\n{rutaArchivo}",
                            "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
}