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
    // ================================================================

    public class SicToken
    {
        public int    Type { get; }
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
            { "START","END","BASE","RESW","RESB","WORD","BYTE" };

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
            string ins     = parts[idx];
            idx++;
            bool   isPlus  = ins.StartsWith("+");
            string insCore = isPlus ? ins.Substring(1) : ins;

            if      (ins.Equals("RSUB",  StringComparison.OrdinalIgnoreCase))
                result.Add(new SicToken(3,  ins.ToUpper()));
            else if (ins.Equals("+RSUB", StringComparison.OrdinalIgnoreCase))
                result.Add(new SicToken(6,  ins.ToUpper()));
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
                    TokenizeOperand(operandRaw, result);
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
                case "END":   return 8;
                case "BASE":  return 9;
                case "RESW":  return 10;
                case "RESB":  return 11;
                case "WORD":  return 12;
                case "BYTE":  return 13;
                default:      return 0;
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
            string body   = raw;
            if (raw.StartsWith("@") || raw.StartsWith("#"))
            {
                prefix = raw.Substring(0, 1);
                body   = raw.Substring(1);
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
        public int    Line    { get; }
        public int    Column  { get; }
        public string Message { get; }
        public SyntaxErrorInfo(int line, int column, string message)
        { Line = line; Column = column; Message = message; }
        public override string ToString() => $"Línea {Line}, Columna {Column}: {Message}";
    }

    public class MyErrorListener
    {
        public List<SyntaxErrorInfo> ErrorList  { get; } = new List<SyntaxErrorInfo>();
        public bool   HasErrors     => ErrorList.Count > 0;
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
            { 3,6,7,8,9,10,11,12,13,15,16,17,18,19,20,21 };

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

            var  ins      = insToks[0];
            var  ops      = OperandTokens(tokens);
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
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, "Error: END requiere un símbolo de primera instrucción"); return false; }
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
                    if (ops.Count == 0 || !IsDecOrHex(ops[0].Text))
                    { errors.AddError(lineNum, 0, $"Error: operando de WORD inválido o faltante"); return false; }
                    break;

                // ---- BYTE ----
                case 13:
                    if (ops.Count == 0)
                    { errors.AddError(lineNum, 0, "Error: BYTE requiere C'...' o X'...'"); return false; }
                    {
                        string bval     = ops[0].Text;
                        bool   validByte =
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
            { 3,6,7,8,9,10,11,12,13,15,16,17,18,19,20,21 };

        static List<SicToken> OperandTokens(List<SicToken> tokens)
        {
            bool passedIns = false;
            var result = new List<SicToken>();
            foreach (var t in tokens)
            {
                if (t.Type == 25) continue; // etiqueta al inicio, nunca es operando
                if (!passedIns && InsTypes2.Contains(t.Type)) { passedIns = true; continue; }
                if (passedIns &&
                   (t.Type == 1 || t.Type == 2 || t.Type == 4 || t.Type == 5 ||
                    t.Type == 14 || (t.Type >= 23 && t.Type <= 29)))
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
        string             Archivo;
        List<List<string>> codigo;
        public static List<string> ListaErrores = new List<string>();
        Dictionary<string, string> TabSim;

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
                Title  = "Seleccione el archivo SIC/XE"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath  = ofd.FileName;
                    string inputText = File.ReadAllText(filePath);

                    originalFileName = Path.GetFileNameWithoutExtension(filePath);

                    rtbCode.Text = inputText;
                    rtbErrors.Clear();
                    rtbCode.SelectAll();
                    rtbCode.SelectionColor = Color.Black;

                    this.Archivo = ofd.FileName;
                    this.codigo  = new List<List<string>>();

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
            ListaErrores     = new List<string>();

            if (string.IsNullOrWhiteSpace(inputText))
            {
                MessageBox.Show("No hay código para analizar.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var errors   = new MyErrorListener();
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
                    int    lineNum = nonEmpty[i].Idx + 1;
                    bool   esIni   = (i == 0);
                    bool   esFin   = (i == nonEmpty.Count - 1);

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
            rtbCode.SelectionFont  = new Font(rtbCode.Font, FontStyle.Regular);
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
                    rtbCode.SelectionFont  = new Font(rtbCode.Font, FontStyle.Regular);

                    rtbCode.Select(lineStart, lineText.Length);
                    rtbCode.SelectionColor = Color.Red;
                    rtbCode.SelectionFont  = new Font(rtbCode.Font, FontStyle.Underline);
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

                var  errListener = new MyErrorListener();
                bool esInicio    = (i == 0);
                bool esFin       = (i == codigo.Count - 1);
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
                        if (TabSim.ContainsKey(t[0].Text))
                            ErrorSimboloDuplicado = true;
                    }

                    r.Cells[1].Value = RegresarFormato(t);
                    r.Cells[4].Value = RegresarInstruccion(t);
                    List<string> op  = RegresarOperandos(t);
                    if (op.Count > 0)
                        r.Cells[5].Value = string.Join(",", op);

                    // Modo de direccionamiento (fmt 3/4)
                    string fmt = r.Cells[1].Value?.ToString() ?? "";
                    if (r.Cells[5].Value != null && (fmt == "3" || fmt == "4"))
                    {
                        string operando = r.Cells[5].Value.ToString();
                        if      (operando.Contains("#")) r.Cells[6].Value = "Inmediato";
                        else if (operando.Contains("@")) r.Cells[6].Value = "Indirecto";
                        else                             r.Cells[6].Value = "Simple";
                    }

                    // RSUB sin operandos
                    string ins4 = r.Cells[4].Value?.ToString() ?? "";
                    if (ins4 == "RSUB" || ins4 == "+RSUB")
                    {
                        bool sinOp = (r.Cells[5].Value == null || r.Cells[5].Value.ToString() == "");
                        r.Cells[6].Value = sinOp ? "---" : "Error: Sintaxis";
                        if (!sinOp) r.Cells[6].Style.ForeColor = Color.Red;
                    }

                    // Formato 1 sin operandos
                    if (fmt == "1" && r.Cells[5].Value != null && r.Cells[5].Value.ToString() != "")
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
                        // Insertar símbolo en TabSim
                        if (t.Count > 0 && t[0].Type == 25)
                        {
                            TabSim[t[0].Text] = ContadorPrograma.ToString();
                            var rs = new DataGridViewRow();
                            rs.CreateCells(TablaSimbolos_Panel);
                            rs.Cells[0].Value = t[0].Text;
                            rs.Cells[1].Value = ContadorPrograma.ToString("X").ToUpper();
                            TablaSimbolos_Panel.Rows.Add(rs);
                        }
                    }

                    // ---- Incremento CP ----
                    string err6    = r.Cells[6].Value?.ToString() ?? "";
                    bool   hayError = err6.StartsWith("Error");

                    if (!hayError)
                    {
                        string ins2  = r.Cells[4].Value?.ToString() ?? "";
                        string oper  = r.Cells[5].Value?.ToString() ?? "";

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
                                    case "RESW": ContadorPrograma += ParseDecOrHex(oper) * 3; break;
                                    case "RESB": ContadorPrograma += ParseDecOrHex(oper);     break;
                                    case "WORD": ContadorPrograma += 3; break; // siempre 3 bytes
                                    case "BYTE": ContadorPrograma += CalcByteSize(oper);      break;
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
                string hex     = operando.Substring(2, operando.Length - 3);
                int    digitos = hex.Length % 2 == 0 ? hex.Length : hex.Length + 1;
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

        // ---- RegresarFormato ----
        string RegresarFormato(IList<SicToken> t)
        {
            int    cont = 0;
            string ret  = "---";
            foreach (var token in t)
            {
                switch (token.Type)
                {
                    case 15: ret = "1"; cont++; break;
                    case 16: case 17: case 18: case 19: ret = "2"; cont++; break;
                    case 20: case  3: ret = "3"; cont++; break;
                    case 21: case  6: ret = "4"; cont++; break;
                }
            }
            return cont <= 1 ? ret : "Error";
        }

        // ---- RegresarInstruccion ----
        string RegresarInstruccion(IList<SicToken> t)
        {
            int    cont = 0;
            string ret  = "";
            foreach (var token in t)
            {
                // Tipos de instrucción/directiva: 3,6,7-13,15-21
                // Excluir tipo 14 (registro) que también cae en el rango
                if (token.Type == 3 || token.Type == 6 ||
                   (token.Type >= 7  && token.Type <= 13) ||
                   (token.Type >= 15 && token.Type <= 21))
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
                    t[i].Type == 5 || t[i].Type == 14 ||
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
            TabSim       = new Dictionary<string, string>();
            codigo       = new List<List<string>>();
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
        List<string> DirectivasNO = new List<string> { "START","END","BASE","RESW","RESB" };

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

                string instruccion  = panelResultados.Rows[i].Cells[4].Value.ToString();
                string panelError = panelResultados.Rows[i].Cells[6]?.Value?.ToString() ?? "";
                string cp           = panelResultados.Rows[i].Cells[2]?.Value?.ToString() ?? "";
                string operando     = panelResultados.Rows[i].Cells[5]?.Value?.ToString() ?? "";
                int    saveHex      = 0;
                bool   noExisteSim = false;

                if (panelError.Contains("Error: Sintaxis") ||
                    panelError.Contains("Error: Instruccion no existe"))
                { panelResultados.Rows[i].Cells[7].Value = "---"; continue; }

                if (panelError.Contains("Error: Simbolo Duplicado") &&
                    i + 1 < panelResultados.Rows.Count)
                {
                    if (cp == (panelResultados.Rows[i + 1].Cells[2]?.Value?.ToString() ?? ""))
                    { panelResultados.Rows[i].Cells[7].Value = "---"; continue; }
                }

                // ---- START / RESW / RESB no generan código objeto ----
                if (instruccion == "START" || instruccion == "RESW" || instruccion == "RESB")
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
                    string baseOp = operando.TrimStart('#', '@').Replace(",X", "").Trim();
                    bool   found  = false;
                    foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                    {
                        if (row.Cells[0]?.Value?.ToString() == baseOp)
                        {
                            BASE  = row.Cells[1].Value.ToString();
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

                // ---- WORD: genera 3 bytes ----
                if (instruccion == "WORD")
                {
                    string obj;
                    if (operando.EndsWith("H", StringComparison.OrdinalIgnoreCase))
                        obj = operando.TrimEnd('H', 'h').PadLeft(6, '0').ToUpper();
                    else if (int.TryParse(operando, out int valDec))
                        obj = valDec.ToString("X6");
                    else
                        obj = "000000";
                    panelResultados.Rows[i].Cells[7].Value = obj;
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
                        string opcode  = CodigosInstruccion[instruccion];
                        string[] regsA = operando.Split(',');
                        string reg1    = regsA.Length > 0 && Registros.ContainsKey(regsA[0].Trim())
                                            ? Registros[regsA[0].Trim()] : "0";
                        string reg2    = regsA.Length > 1
                                            ? (Registros.ContainsKey(regsA[1].Trim())
                                                ? Registros[regsA[1].Trim()] : regsA[1].Trim())
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

                    string operandoLimpio = operando.TrimStart('@', '#').Replace(",X", "");
                    int    TA;
                    bool   esConstante = false;
                    bool   esHex       = false;

                    if (int.TryParse(operandoLimpio.TrimEnd('H', 'h'), out int numero))
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
                        foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                        {
                            string sim = row.Cells[0]?.Value?.ToString() ?? "";
                            if (operandoLimpio == sim)
                            { TA = Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "FFF", 16);
                              noExisteSim = false; break; }
                            else noExisteSim = true;
                        }
                    }

                    int    desp      = TA;
                    string nixbpe    = "000000";
                    int    baseValor = BASE == "-1" ? -1 : Convert.ToInt32(BASE, 16);

                    if (noExisteSim)
                        panelResultados.Rows[i].Cells[8].Value = "Error: Simbolo no encontrado en TABSIM";

                    if (operando.StartsWith("@"))
                    {
                        if (esConstante) nixbpe = "100000";
                        else { desp=TA-CP; if(desp>=-2048&&desp<=2047) nixbpe="100010";
                               else { desp=TA-baseValor; if(desp>=0&&desp<=4095) nixbpe="100100";
                                      else { desp=0xFFF; nixbpe="100110";
                                             panelResultados.Rows[i].Cells[8].Value="Error: No relativo a CP/BASE"; }}}
                    }
                    else if (operando.StartsWith("#"))
                    {
                        if (esConstante) nixbpe = "010000";
                        else { desp=TA-CP; if(desp>=-2048&&desp<=2047) nixbpe="010010";
                               else { desp=TA-baseValor; if(desp>=0&&desp<=4095) nixbpe="010100";
                                      else { desp=0xFFF; nixbpe="010110";
                                             panelResultados.Rows[i].Cells[8].Value="Error: No relativo a CP/BASE"; }}}
                    }
                    else if (operando.EndsWith(",X"))
                    {
                        if (esConstante) nixbpe = "111000";
                        else { desp=TA-CP; if(desp>=-2048&&desp<=2047) nixbpe="111010";
                               else { desp=TA-baseValor; if(desp>=0&&desp<=4095) nixbpe="111100";
                                      else { desp=0xFFF; nixbpe="111110";
                                             panelResultados.Rows[i].Cells[8].Value="Error: No relativo a CP/BASE"; }}}
                    }
                    else
                    {
                        if (esConstante) nixbpe = "110000";
                        else { desp=TA-CP; if(desp>=-2048&&desp<=2047) nixbpe="110010";
                               else { desp=TA-baseValor; if(desp>=0&&desp<=4095) nixbpe="110100";
                                      else { desp=0xFFF; nixbpe="110110";
                                             panelResultados.Rows[i].Cells[8].Value="Error: No relativo a CP/BASE"; }}}
                    }

                    if (CodigosInstruccion.ContainsKey(instruccion))
                    {
                        string CodigoOperacion = CodigosInstruccion[instruccion];
                        char   codOp_1p = CodigoOperacion[0];
                        int    codOp_2p_bin = Convert.ToInt32(CodigoOperacion[1].ToString(), 16);
                        string codOp_2p_bits  = Convert.ToString(codOp_2p_bin, 2).PadLeft(4, '0').Substring(0, 2);
                        string sumaBits = codOp_2p_bits + nixbpe;
                        int    byteResultado = Convert.ToInt32(sumaBits, 2);
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
                    instruccion = instruccion.Substring(1);

                    string operandoLimpio = operando.TrimStart('@', '#').Replace(",X", "");
                    int TA = 0xFFFFF;
                    bool esM = false;

                    if (operandoLimpio.EndsWith("H", StringComparison.OrdinalIgnoreCase))
                    {
                        // Constante hex: si valor > 4095 es dirección de memoria, si no es constante
                        string nHex = operandoLimpio.TrimEnd('H', 'h');
                        if (int.TryParse(nHex, System.Globalization.NumberStyles.HexNumber,null, out int valorHex))
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
                        foreach (DataGridViewRow row in TablaSimbolos_Panel.Rows)
                        {
                            string sim = row.Cells[0]?.Value?.ToString() ?? "";
                            if (operandoLimpio == sim)
                            { TA = Convert.ToInt32(row.Cells[1]?.Value?.ToString() ?? "FFFFF", 16);
                              noExisteSim = false; esM = true; break; }
                            else noExisteSim = true;
                        }
                    }

                    if (noExisteSim)
                        panelResultados.Rows[i].Cells[8].Value = "Error: Simbolo no encontrado en TABSIM";
                    if (!esM && !noExisteSim)
                    { TA = 0xFFFFF; panelResultados.Rows[i].Cells[8].Value = "Error: No existe combinacion MD"; }

                    string nixbpe2 = "000001";
                    if      (operando.StartsWith("@")) nixbpe2 = noExisteSim ? "010111" : "100001";
                    else if (operando.StartsWith("#")) nixbpe2 = noExisteSim ? "010111" : "010001";
                    else if (operando.EndsWith(",X")) nixbpe2  = noExisteSim ? "111111" : "111001";
                    else                              nixbpe2  = noExisteSim ? "100111" : "100001";

                    if (CodigosInstruccion.ContainsKey(instruccion))
                    {
                        string CodigoOperacion        = CodigosInstruccion[instruccion];
                        char   codOp_1p       = CodigoOperacion[0];
                        int    codOp_2p_bin   = Convert.ToInt32(CodigoOperacion[1].ToString(), 16);
                        string codOp_2p_bits  = Convert.ToString(codOp_2p_bin, 2).PadLeft(4, '0').Substring(0, 2);
                        string sumaBits     = codOp_2p_bits + nixbpe2;
                        int    byteResultado    = Convert.ToInt32(sumaBits, 2);
                        string byteResHex = $"{(byteResultado >> 4):X}{(byteResultado & 0xF):X}";
                        string dirResHex       = TA.ToString("X5");

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

            string primeraEtiqueta    = panelResultados.Rows[0].Cells[3]?.Value?.ToString() ?? "";
            string primeraEtiquetaRaw = primeraEtiqueta;
            primeraEtiqueta = primeraEtiqueta.Length > 6
                ? primeraEtiqueta.Substring(0, 6)
                : primeraEtiqueta.PadRight(6, '_');

            string direccionFinal =
                (panelResultados.Rows[panelResultados.Rows.Count - 1].Cells[2]?.Value?.ToString() ?? "000000")
                .PadLeft(6, '0');

            lineas.Add($"H{primeraEtiqueta}000000{direccionFinal}");

            int i = 0;
            while (i < panelResultados.Rows.Count)
            {
                while (i < panelResultados.Rows.Count &&
                       (panelResultados.Rows[i].Cells[7]?.Value?.ToString() ?? "---") == "---")
                    i++;
                if (i >= panelResultados.Rows.Count) break;

                string cpInicial       = (panelResultados.Rows[i].Cells[2]?.Value?.ToString() ?? "000000").PadLeft(6, '0');
                string contenidoT      = "";
                int    caracteresUsados = 0;

                while (i < panelResultados.Rows.Count)
                {
                    string codigoObjeto = panelResultados.Rows[i].Cells[7]?.Value?.ToString() ?? "---";
                    string ins2         = panelResultados.Rows[i].Cells[4]?.Value?.ToString() ?? "";
                    if (codigoObjeto == "---")
                    { if (ins2 == "RESB" || ins2 == "RESW") break; }
                    else
                    { contenidoT += codigoObjeto.TrimEnd('*'); caracteresUsados += codigoObjeto.Length; }
                    if (caracteresUsados >= 60) break;
                    i++;
                }

                if (contenidoT.Length > 0)
                {
                    int    longBytes = (int)Math.Ceiling(contenidoT.Length / 2.0);
                    string longHex   = longBytes.ToString("X2");
                    lineas.Add($"T{cpInicial}{longHex}{contenidoT}");
                }
            }

            for (int j = 0; j < panelResultados.Rows.Count; j++)
            {
                string codigoObjeto = panelResultados.Rows[j].Cells[7]?.Value?.ToString() ?? "";
                if (codigoObjeto.EndsWith("*"))
                {
                    int    cpValor = Convert.ToInt32(panelResultados.Rows[j].Cells[2]?.Value?.ToString() ?? "0", 16);
                    string cpMod   = (cpValor + 1).ToString("X6");
                    lineas.Add($"M{cpMod}05+{primeraEtiqueta}");
                }
            }

            string direccionInicio = "";
            for (int j = 0; j < panelResultados.Rows.Count; j++)
            {
                string ins2 = panelResultados.Rows[j].Cells[4]?.Value?.ToString() ?? "";
                if (ins2 != "START" && ins2 != "END"  && ins2 != "BASE" &&
                    ins2 != "RESW"  && ins2 != "RESB" && ins2 != "WORD" && ins2 != "BYTE")
                {
                    direccionInicio = (panelResultados.Rows[j].Cells[2]?.Value?.ToString() ?? "000000").PadLeft(6, '0');
                    break;
                }
            }

            lineas.Add($"E{direccionInicio}");

            string rutaArchivo = $"{primeraEtiquetaRaw}.obj";
            File.WriteAllLines(rutaArchivo, lineas);
            MessageBox.Show($"Archivo objeto generado correctamente en {rutaArchivo}");
        }

        private void FileGenerattor_Click(object sender, EventArgs e) => objArchivo();
        #endregion
    }
}