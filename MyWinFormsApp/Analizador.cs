using System;
using System.Collections.Generic;
using System.Linq;

namespace MyWinFormsApp
{
    public class InstruccionInfo
    {
        public string Nombre { get; set; }
        public int Formato { get; set; }
        public string TipoOperando { get; set; }
        public int NumOperandos { get; set; }

        public InstruccionInfo(string nombre, int formato, string tipo, int numOps)
        {
            Nombre = nombre;
            Formato = formato;
            TipoOperando = tipo;
            NumOperandos = numOps;
        }
    }

    public class TablaCodigos
    {
        private Dictionary<string, InstruccionInfo> tabla = new();

        public TablaCodigos()
        {
            tabla["FIX"]   = new InstruccionInfo("FIX",   1, "ninguno", 0);
            tabla["FLOAT"] = new InstruccionInfo("FLOAT", 1, "ninguno", 0);
            tabla["HIO"]   = new InstruccionInfo("HIO",   1, "ninguno", 0);
            tabla["NORM"]  = new InstruccionInfo("NORM",  1, "ninguno", 0);
            tabla["SIO"]   = new InstruccionInfo("SIO",   1, "ninguno", 0);
            tabla["TIO"]   = new InstruccionInfo("TIO",   1, "ninguno", 0);

            tabla["ADDR"]  = new InstruccionInfo("ADDR",  2, "r1,r2", 2);
            tabla["CLEAR"] = new InstruccionInfo("CLEAR", 2, "r1",    1);
            tabla["COMPR"] = new InstruccionInfo("COMPR", 2, "r1,r2", 2);
            tabla["DIVR"]  = new InstruccionInfo("DIVR",  2, "r1,r2", 2);
            tabla["MULR"]  = new InstruccionInfo("MULR",  2, "r1,r2", 2);
            tabla["RMO"]   = new InstruccionInfo("RMO",   2, "r1,r2", 2);
            tabla["SHIFTL"]= new InstruccionInfo("SHIFTL",2, "r1,n",  2);
            tabla["SHIFTR"]= new InstruccionInfo("SHIFTR",2, "r1,n",  2);
            tabla["SUBR"]  = new InstruccionInfo("SUBR",  2, "r1,r2", 2);
            tabla["SVC"]   = new InstruccionInfo("SVC",   2, "n",     1);
            tabla["TIXR"]  = new InstruccionInfo("TIXR",  2, "r1",    1);

            tabla["ADD"]   = new InstruccionInfo("ADD",   34, "m", 1);
            tabla["ADDF"]  = new InstruccionInfo("ADDF",  34, "m", 1);
            tabla["AND"]   = new InstruccionInfo("AND",   34, "m", 1);
            tabla["COMP"]  = new InstruccionInfo("COMP",  34, "m", 1);
            tabla["COMPF"] = new InstruccionInfo("COMPF", 34, "m", 1);
            tabla["DIV"]   = new InstruccionInfo("DIV",   34, "m", 1);
            tabla["DIVF"]  = new InstruccionInfo("DIVF",  34, "m", 1);
            tabla["J"]     = new InstruccionInfo("J",     34, "m", 1);
            tabla["JEQ"]   = new InstruccionInfo("JEQ",   34, "m", 1);
            tabla["JGT"]   = new InstruccionInfo("JGT",   34, "m", 1);
            tabla["JLT"]   = new InstruccionInfo("JLT",   34, "m", 1);
            tabla["JSUB"]  = new InstruccionInfo("JSUB",  34, "m", 1);
            tabla["LDA"]   = new InstruccionInfo("LDA",   34, "m", 1);
            tabla["LDB"]   = new InstruccionInfo("LDB",   34, "m", 1);
            tabla["LDCH"]  = new InstruccionInfo("LDCH",  34, "m", 1);
            tabla["LDF"]   = new InstruccionInfo("LDF",   34, "m", 1);
            tabla["LDL"]   = new InstruccionInfo("LDL",   34, "m", 1);
            tabla["LDS"]   = new InstruccionInfo("LDS",   34, "m", 1);
            tabla["LDT"]   = new InstruccionInfo("LDT",   34, "m", 1);
            tabla["LDX"]   = new InstruccionInfo("LDX",   34, "m", 1);
            tabla["LPS"]   = new InstruccionInfo("LPS",   34, "m", 1);
            tabla["MUL"]   = new InstruccionInfo("MUL",   34, "m", 1);
            tabla["MULF"]  = new InstruccionInfo("MULF",  34, "m", 1);
            tabla["OR"]    = new InstruccionInfo("OR",    34, "m", 1);
            tabla["RD"]    = new InstruccionInfo("RD",    34, "m", 1);
            tabla["RSUB"]  = new InstruccionInfo("RSUB",  34, "ninguno", 0);
            tabla["SSK"]   = new InstruccionInfo("SSK",   34, "m", 1);
            tabla["STA"]   = new InstruccionInfo("STA",   34, "m", 1);
            tabla["STB"]   = new InstruccionInfo("STB",   34, "m", 1);
            tabla["STCH"]  = new InstruccionInfo("STCH",  34, "m", 1);
            tabla["STF"]   = new InstruccionInfo("STF",   34, "m", 1);
            tabla["STI"]   = new InstruccionInfo("STI",   34, "m", 1);
            tabla["STL"]   = new InstruccionInfo("STL",   34, "m", 1);
            tabla["STS"]   = new InstruccionInfo("STS",   34, "m", 1);
            tabla["STSW"]  = new InstruccionInfo("STSW",  34, "m", 1);
            tabla["STT"]   = new InstruccionInfo("STT",   34, "m", 1);
            tabla["STX"]   = new InstruccionInfo("STX",   34, "m", 1);
            tabla["SUB"]   = new InstruccionInfo("SUB",   34, "m", 1);
            tabla["SUBF"]  = new InstruccionInfo("SUBF",  34, "m", 1);
            tabla["TD"]    = new InstruccionInfo("TD",    34, "m", 1);
            tabla["TIX"]   = new InstruccionInfo("TIX",   34, "m", 1);
            tabla["WD"]    = new InstruccionInfo("WD",    34, "m", 1);

            tabla["START"] = new InstruccionInfo("START", 0, "numero",  1);
            tabla["END"]   = new InstruccionInfo("END",   0, "etiqueta",1);
            tabla["BYTE"]  = new InstruccionInfo("BYTE",  0, "byte",    1);
            tabla["WORD"]  = new InstruccionInfo("WORD",  0, "numero",  1);
            tabla["RESB"]  = new InstruccionInfo("RESB",  0, "numero",  1);
            tabla["RESW"]  = new InstruccionInfo("RESW",  0, "numero",  1);
            tabla["BASE"]  = new InstruccionInfo("BASE",  0, "etiqueta",1);
        }

        public InstruccionInfo? Obtener(string nombre)
        {
            return tabla.TryGetValue(nombre.ToUpper(), out var info) ? info : null;
        }

        public bool Existe(string nombre)
        {
            return tabla.ContainsKey(nombre.ToUpper());
        }
    }

    public class LineaProcesada
    {
        public int    NumeroLinea  { get; set; }
        public string Contador     { get; set; } = "";
        public string Etiqueta     { get; set; } = "";
        public string Operacion    { get; set; } = "";
        public string Operandos    { get; set; } = "";
        public string Formato      { get; set; } = "";
        public string CodigoObjeto { get; set; } = "---";
        // ErrorMessage holds Paso-2 warnings (símbolo no encontrado, no relativo, etc.)
        public string ErrorMessage { get; set; } = "";
        public List<string> Errores { get; set; }
        public bool  Valida        { get; set; }

        public LineaProcesada()
        {
            Errores = new List<string>();
            Valida  = true;
        }
    }

    public class AnalizadorSicXe
    {
        private TablaCodigos tabla;
        private List<LineaProcesada> lineasProcesadas;
        private List<string> erroresGlobales;

        // TabSim stores symbol -> decimal address (as int)
        private Dictionary<string, int> tablaSimbolos;

        private int contadorPrograma = 0;
        private int contadorInicio   = 0;
        private int contadorFinal    = 0;

        // BASE register value: -1 means not set; otherwise decimal address
        private int baseRegistro = -1;

        // ============================================================
        // Opcode table (2-hex chars per instruction)
        // ============================================================
        private static readonly Dictionary<string, string> SicxeInstructions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"ADD","18"},{"ADDF","58"},{"ADDR","90"},{"AND","40"},
            {"CLEAR","B4"},{"COMP","28"},{"COMPF","88"},{"COMPR","A0"},
            {"DIV","24"},{"DIVF","64"},{"DIVR","9C"},
            {"FIX","C4"},{"FLOAT","C0"},{"HIO","F4"},
            {"J","3C"},{"JEQ","30"},{"JGT","34"},{"JLT","38"},{"JSUB","48"},
            {"LDA","00"},{"LDB","68"},{"LDCH","50"},{"LDF","70"},{"LDL","08"},
            {"LDS","6C"},{"LDT","74"},{"LDX","04"},{"LPS","D0"},
            {"MUL","20"},{"MULF","60"},{"MULR","98"},
            {"NORM","C8"},{"OR","44"},
            {"RD","D8"},{"RMO","AC"},{"RSUB","4C"},
            {"SHIFTL","A4"},{"SHIFTR","A8"},{"SIO","F0"},{"SSK","EC"},
            {"STA","0C"},{"STB","78"},{"STCH","54"},{"STF","80"},{"STI","D4"},
            {"STL","14"},{"STS","7C"},{"STSW","E8"},{"STT","84"},{"STX","10"},
            {"SUB","1C"},{"SUBF","5C"},{"SUBR","94"},{"SVC","B0"},
            {"TD","E0"},{"TIO","F8"},{"TIX","2C"},{"TIXR","B8"},{"WD","DC"}
        };

        private static readonly Dictionary<string, string> Registros =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"A","0"},{"X","1"},{"L","2"},{"B","3"},
            {"S","4"},{"T","5"},{"F","6"},{"PC","8"},{"SW","9"}
        };

        private static readonly HashSet<string> NoObj =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "START","END","RESW","RESB","BASE","WORD","BYTE"
        };

        // ============================================================
        public AnalizadorSicXe()
        {
            tabla             = new TablaCodigos();
            lineasProcesadas  = new List<LineaProcesada>();
            erroresGlobales   = new List<string>();
            tablaSimbolos     = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        // ============================================================
        // Helpers
        // ============================================================

        /// <summary>
        /// Parses a value that may end with 'H' (hex) or be a plain decimal.
        /// Returns -1 on failure.
        /// </summary>
        private static int ParseNumero(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return -1;
            valor = valor.Trim();
            if (valor.EndsWith("H", StringComparison.OrdinalIgnoreCase))
            {
                try { return Convert.ToInt32(valor[..^1], 16); }
                catch { return -1; }
            }
            if (int.TryParse(valor, out int dec)) return dec;
            return -1;
        }

        private string ContadorEnHex() => contadorPrograma.ToString("X4");

        // ============================================================
        // Public API
        // ============================================================

        public void Analizar(string contenido)
        {
            lineasProcesadas.Clear();
            erroresGlobales.Clear();
            tablaSimbolos.Clear();
            contadorPrograma = 0;
            contadorInicio   = 0;
            contadorFinal    = 0;
            baseRegistro     = -1;

            string[] lineas = contenido.Split(
                new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // PASO 1: tokenize, assign CP, fill TabSim
            foreach (var linea in lineas)
                ProcesarLinea(linea);

            // PASO 2: generate object code
            GenerarCodigoObjeto();
        }

        public List<LineaProcesada>       ObtenerLineasProcesadas() => lineasProcesadas;
        public List<string>               ObtenerErrores()          => erroresGlobales;
        public Dictionary<string, string> ObtenerTablaSimbolos()
        {
            // Return as hex-string values for display
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in tablaSimbolos)
                d[kv.Key] = kv.Value.ToString("X4");
            return d;
        }
        public int ObtenerContadorInicio()          => contadorInicio;
        public int ObtenerContadorFinal()            => contadorFinal;
        public int ObtenerTamanioProgramaTotal()     => contadorFinal - contadorInicio;
        public bool HayErrores() =>
            lineasProcesadas.Any(l =>
                !l.Valida || l.Errores.Count > 0 ||
                (!string.IsNullOrEmpty(l.ErrorMessage) && l.ErrorMessage.Contains("Error")));

        // ============================================================
        // PASO 1
        // ============================================================
        private void ProcesarLinea(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea) || linea.TrimStart().StartsWith("."))
                return;

            string[] campos = linea.Split(
                new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var resultado = new LineaProcesada
            {
                Contador    = ContadorEnHex(),
                NumeroLinea = lineasProcesadas.Count,  // 0-based to match example
                Etiqueta    = ""
            };

            int indice = 0;

            if (campos.Length > 0)
            {
                string primerCampo = campos[0].Trim();
                string instrPosible = primerCampo.StartsWith("+")
                    ? primerCampo[1..].ToUpper()
                    : primerCampo.ToUpper();

                if (tabla.Existe(instrPosible))
                {
                    resultado.Etiqueta = "";
                    indice = 0;
                }
                else
                {
                    if (ValidarEtiqueta(primerCampo))
                    {
                        resultado.Etiqueta = primerCampo;
                        indice = 1;
                    }
                    else
                    {
                        resultado.Errores.Add($"[Léxico] Etiqueta inválida: '{primerCampo}'");
                        resultado.Valida = false;
                        lineasProcesadas.Add(resultado);
                        return;
                    }
                }
            }

            if (indice >= campos.Length)
            {
                lineasProcesadas.Add(resultado);
                return;
            }

            string campOp        = campos[indice].Trim();
            bool   esFormato4    = campOp.StartsWith("+");
            string operacionRaw  = campOp;
            string campOpLimpio  = esFormato4 ? campOp[1..].ToUpper() : campOp.ToUpper();

            resultado.Operacion = operacionRaw;
            resultado.Formato   = DeterminarFormato(operacionRaw);

            resultado.Operandos = indice + 1 < campos.Length
                ? string.Join(" ", campos.Skip(indice + 1)).Trim()
                : "";

            InstruccionInfo? info = tabla.Obtener(campOpLimpio);

            if (info == null)
            {
                resultado.Errores.Add($"[Léxico] Instrucción no existe: '{campOpLimpio}'");
                resultado.Valida = false;
                lineasProcesadas.Add(resultado);
                return;
            }

            // ---- START: set initial CP ----
            if (campOpLimpio == "START")
            {
                int ini = ParseNumero(resultado.Operandos);
                if (ini == -1) ini = 0;
                contadorInicio   = ini;
                contadorPrograma = ini;
                resultado.Contador = contadorPrograma.ToString("X4");
            }

            // ---- Register symbol in TabSim (decimal) ----
            if (!string.IsNullOrEmpty(resultado.Etiqueta)
                && campOpLimpio != "START"
                && campOpLimpio != "END")
            {
                if (tablaSimbolos.ContainsKey(resultado.Etiqueta))
                {
                    resultado.Errores.Add($"[Semántico] Símbolo duplicado: '{resultado.Etiqueta}'");
                    resultado.Valida = false;
                }
                else
                {
                    tablaSimbolos[resultado.Etiqueta] = contadorPrograma; // store decimal
                }
            }

            // ---- Advance CP (only when no syntax errors) ----
            int incremento = CalcularIncremento(info, resultado.Operandos, esFormato4, resultado.Errores);

            if (resultado.Errores.Count == 0)
            {
                resultado.Valida  = true;
                contadorPrograma += incremento;
                contadorFinal     = contadorPrograma;
            }
            else
            {
                resultado.Valida = false;
            }

            lineasProcesadas.Add(resultado);
        }

        /// <summary>
        /// Returns how many bytes this line occupies (for advancing CP).
        /// WORD always = 3 bytes (not value*3).
        /// </summary>
        private int CalcularIncremento(InstruccionInfo info, string operandos,
                                       bool esFormato4, List<string> errores)
        {
            switch (info.Nombre.ToUpper())
            {
                case "START":
                case "END":
                case "BASE":
                    return 0;

                case "WORD":
                    return 3;   // always 3 bytes regardless of value

                case "RESW":
                {
                    int n = ParseNumero(operandos);
                    if (n < 0) { errores.Add($"[Sintáctico] '{operandos}' no es un número válido"); return 0; }
                    return n * 3;
                }

                case "RESB":
                {
                    int n = ParseNumero(operandos);
                    if (n < 0) { errores.Add($"[Sintáctico] '{operandos}' no es un número válido"); return 0; }
                    return n;
                }

                case "BYTE":
                {
                    if (operandos.StartsWith("C'") && operandos.EndsWith("'"))
                        return operandos.Length - 3;  // chars between quotes
                    if (operandos.StartsWith("X'") && operandos.EndsWith("'"))
                    {
                        string hex = operandos[2..^1];
                        return (hex.Length + 1) / 2;
                    }
                    errores.Add("[Sintáctico] BYTE debe ser C'texto' o X'hex'");
                    return 0;
                }
            }

            if (info.Formato == 1) return 1;
            if (info.Formato == 2)
            {
                if (!ValidarFormato2(info, operandos, errores)) return 0;
                return 2;
            }
            if (info.Formato == 34)
            {
                if (info.Nombre == "RSUB") return esFormato4 ? 4 : 3;
                if (string.IsNullOrEmpty(operandos))
                {
                    errores.Add($"[Sintáctico] {info.Nombre} requiere 1 operando");
                    return 0;
                }
                return esFormato4 ? 4 : 3;
            }
            return 0;
        }

        private bool ValidarFormato2(InstruccionInfo info, string operandos, List<string> errores)
        {
            if (string.IsNullOrEmpty(operandos))
            {
                errores.Add($"[Sintáctico] {info.Nombre} requiere operandos");
                return false;
            }
            string[] ops = operandos.Split(',');
            if (info.TipoOperando == "r1" && ops.Length == 1)
            {
                if (!Registros.ContainsKey(ops[0].Trim()))
                { errores.Add($"[Sintáctico] {ops[0].Trim()} no es registro válido"); return false; }
                return true;
            }
            if ((info.TipoOperando == "r1,r2" || info.TipoOperando == "r1,n") && ops.Length == 2)
            {
                if (!Registros.ContainsKey(ops[0].Trim()))
                { errores.Add($"[Sintáctico] Registro inválido: {ops[0].Trim()}"); return false; }
                // second operand can be a register or a number
                return true;
            }
            errores.Add($"[Sintáctico] Operandos inválidos para {info.Nombre}");
            return false;
        }

        private bool ValidarEtiqueta(string etiqueta)
        {
            if (string.IsNullOrEmpty(etiqueta)) return false;
            return char.IsLetter(etiqueta[0]) &&
                   etiqueta.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
        }

        private string DeterminarFormato(string operacion)
        {
            operacion = operacion.ToUpper();
            bool f4 = operacion.StartsWith("+");
            if (f4) operacion = operacion[1..];

            if (new[] {"FIX","FLOAT","HIO","NORM","SIO","TIO"}.Contains(operacion))
                return "1";
            if (new[] {"ADDR","CLEAR","COMPR","DIVR","MULR","RMO",
                       "SHIFTL","SHIFTR","SUBR","SVC","TIXR"}.Contains(operacion))
                return "2";
            if (new[] {"ADD","ADDF","AND","COMP","COMPF","DIV","DIVF",
                       "J","JEQ","JGT","JLT","JSUB",
                       "LDA","LDB","LDCH","LDF","LDL","LDS","LDT","LDX","LPS",
                       "MUL","MULF","OR","RD","RSUB","SSK",
                       "STA","STB","STCH","STF","STI","STL","STS","STSW","STT","STX",
                       "SUB","SUBF","TD","TIX","WD"}.Contains(operacion))
                return f4 ? "4" : "3";
            if (new[] {"START","END","BASE","BYTE","RESB","RESW","WORD"}.Contains(operacion))
                return "---";
            return "?";
        }

        // ============================================================
        // PASO 2 — Generate object code (mirrors paso2() from reference)
        // ============================================================
        private void GenerarCodigoObjeto()
        {
            for (int i = 0; i < lineasProcesadas.Count; i++)
            {
                var linea = lineasProcesadas[i];

                // Syntax errors → no object code
                if (linea.Errores.Count > 0)
                {
                    linea.CodigoObjeto = "---";
                    linea.ErrorMessage = string.Join(", ", linea.Errores);
                    continue;
                }

                string instruccion = linea.Operacion.ToUpper();
                string operando    = linea.Operandos;
                bool   esF4        = instruccion.StartsWith("+");
                if (esF4) instruccion = instruccion[1..];

                string formato = linea.Formato;

                // ---- RSUB ----
                if (instruccion == "RSUB")
                {
                    linea.CodigoObjeto = "4F0000";
                    continue;
                }

                // ---- Directivas ----
                if (NoObj.Contains(instruccion))
                {
                    switch (instruccion.ToUpper())
                    {
                        case "BASE":
                        {
                            string baseOp = operando.TrimStart('#','@').TrimEnd(',','X');
                            if (tablaSimbolos.TryGetValue(baseOp, out int bval))
                                baseRegistro = bval;
                            linea.CodigoObjeto = "---";
                            break;
                        }
                        case "WORD":
                        {
                            // Convert value and store as 6-hex-digit object code
                            int val = ParseNumero(operando);
                            linea.CodigoObjeto = val < 0 ? "---" : val.ToString("X6");
                            break;
                        }
                        case "BYTE":
                        {
                            if (operando.StartsWith("X'") && operando.EndsWith("'"))
                                linea.CodigoObjeto = operando[2..^1].ToUpper();
                            else if (operando.StartsWith("C'") && operando.EndsWith("'"))
                            {
                                string txt = operando[2..^1];
                                linea.CodigoObjeto =
                                    string.Concat(txt.Select(c => ((int)c).ToString("X2")));
                            }
                            else
                                linea.CodigoObjeto = "---";
                            break;
                        }
                        default:
                            linea.CodigoObjeto = "---";
                            break;
                    }
                    continue;
                }

                // ---- Formato 1 ----
                if (formato == "1")
                {
                    linea.CodigoObjeto = SicxeInstructions.TryGetValue(instruccion, out var op1)
                        ? op1 : "---";
                    continue;
                }

                // ---- Formato 2 ----
                if (formato == "2")
                {
                    if (!SicxeInstructions.TryGetValue(instruccion, out var op2))
                    { linea.CodigoObjeto = "---"; continue; }

                    string[] regs = operando.Split(',');
                    string r1 = regs.Length > 0 && Registros.TryGetValue(regs[0].Trim(), out var rv1)
                                ? rv1 : "0";
                    string r2 = regs.Length > 1
                                ? (Registros.TryGetValue(regs[1].Trim(), out var rv2) ? rv2 : regs[1].Trim())
                                : "0";
                    linea.CodigoObjeto = op2 + r1 + r2;
                    continue;
                }

                // ---- Formato 3 ----
                if (formato == "3")
                {
                    linea.CodigoObjeto = GenerarF3(linea, i);
                    continue;
                }

                // ---- Formato 4 ----
                if (formato == "4")
                {
                    linea.CodigoObjeto = GenerarF4(linea, i);
                    continue;
                }

                linea.CodigoObjeto = "---";
            }
        }

        // ============================================================
        // Formato 3 code generation  (mirrors paso2 F3 block)
        // ============================================================
        private string GenerarF3(LineaProcesada linea, int idx)
        {
            string instruccion = linea.Operacion.ToUpper();
            if (instruccion.StartsWith("+")) instruccion = instruccion[1..];
            if (!SicxeInstructions.TryGetValue(instruccion, out var codOp)) return "---";

            // CP = address of NEXT instruction (same as reference: dGV_int.Rows[i+1])
            int CP = 0;
            if (idx + 1 < lineasProcesadas.Count)
                CP = Convert.ToInt32(lineasProcesadas[idx + 1].Contador, 16);

            string operando       = linea.Operandos;
            string operandoLimpio = operando.TrimStart('@','#').Replace(",X","").Trim();

            int  TA          = 0;
            bool esConstante = false;
            bool esHex       = false;
            int  saveHex     = 0;
            bool noSeEncontro = false;

            if (int.TryParse(operandoLimpio.TrimEnd('H','h'), out int numParsed))
            {
                if (operandoLimpio.EndsWith("H", StringComparison.OrdinalIgnoreCase))
                {
                    esHex    = true;
                    saveHex  = numParsed;
                    TA       = numParsed;  // already the hex value
                    int decVal = Convert.ToInt32(operandoLimpio[..^1], 16);
                    esConstante = (decVal >= 0 && decVal <= 4095);
                }
                else
                {
                    TA          = Convert.ToInt32(numParsed.ToString("X"), 16);
                    esConstante = (numParsed >= 0 && numParsed <= 4095);
                }
            }
            else
            {
                TA = 0xFFF;  // default if symbol not found
                noSeEncontro = true;
                foreach (var sym in tablaSimbolos)
                {
                    if (string.Equals(sym.Key, operandoLimpio, StringComparison.OrdinalIgnoreCase))
                    {
                        TA           = sym.Value;  // decimal (same as hex display value)
                        noSeEncontro = false;
                        break;
                    }
                }
            }

            if (noSeEncontro)
                linea.ErrorMessage = "Error: Simbolo no encontrado en TABSIM";

            int desp     = TA;
            string nixbpe = "000000";
            int baseValor = baseRegistro; // -1 if not set

            // Determine addressing mode bits
            if (operando.StartsWith("@"))
            {
                if (esConstante)
                    nixbpe = "100000";
                else
                {
                    desp = TA - CP;
                    if (desp >= -2048 && desp <= 2047)
                        nixbpe = "100010";
                    else
                    {
                        desp = TA - baseValor;
                        if (baseValor >= 0 && desp >= 0 && desp <= 4095)
                            nixbpe = "100100";
                        else
                        {
                            desp   = 0xFFF;
                            nixbpe = "100110";
                            linea.ErrorMessage = "Error: No relativo a CP/BASE";
                        }
                    }
                }
            }
            else if (operando.StartsWith("#"))
            {
                if (esConstante)
                    nixbpe = "010000";
                else
                {
                    desp = TA - CP;
                    if (desp >= -2048 && desp <= 2047)
                        nixbpe = "010010";
                    else
                    {
                        desp = TA - baseValor;
                        if (baseValor >= 0 && desp >= 0 && desp <= 4095)
                            nixbpe = "010100";
                        else
                        {
                            desp   = 0xFFF;
                            nixbpe = "010110";
                            linea.ErrorMessage = "Error: No relativo a CP/BASE";
                        }
                    }
                }
            }
            else if (operando.EndsWith(",X", StringComparison.OrdinalIgnoreCase))
            {
                if (esConstante)
                    nixbpe = "111000";
                else
                {
                    desp = TA - CP;
                    if (desp >= -2048 && desp <= 2047)
                        nixbpe = "111010";
                    else
                    {
                        desp = TA - baseValor;
                        if (baseValor >= 0 && desp >= 0 && desp <= 4095)
                            nixbpe = "111100";
                        else
                        {
                            desp   = 0xFFF;
                            nixbpe = "111110";
                            linea.ErrorMessage = "Error: No relativo a CP/BASE";
                        }
                    }
                }
            }
            else  // Simple
            {
                if (esConstante)
                    nixbpe = "110000";
                else
                {
                    desp = TA - CP;
                    if (desp >= -2048 && desp <= 2047)
                        nixbpe = "110010";
                    else
                    {
                        desp = TA - baseValor;
                        if (baseValor >= 0 && desp >= 0 && desp <= 4095)
                            nixbpe = "110100";
                        else
                        {
                            desp   = 0xFFF;
                            nixbpe = "110110";
                            linea.ErrorMessage = "Error: No relativo a CP/BASE";
                        }
                    }
                }
            }

            // Assemble: codOp[0] + second nibble bits merged with nixbpe
            char   codOp1     = codOp[0];
            int    codOp2bin  = Convert.ToInt32(codOp[1].ToString(), 16);
            string codOp2bits = Convert.ToString(codOp2bin, 2).PadLeft(4,'0')[..2];
            string byteStr    = codOp2bits + nixbpe;
            int    byteFinal  = Convert.ToInt32(byteStr, 2);
            string byteFinalHex = $"{(byteFinal >> 4):X}{(byteFinal & 0xF):X}";

            // Displacement: use saved hex value when operand ended in H
            string despHex = esConstante
                ? TA.ToString("X3")
                : (desp & 0xFFF).ToString("X3");
            if (esHex)
                despHex = saveHex.ToString("X3");

            return $"{codOp1}{byteFinalHex}{despHex}".ToUpper();
        }

        // ============================================================
        // Formato 4 code generation  (mirrors paso2 F4 block)
        // ============================================================
        private string GenerarF4(LineaProcesada linea, int idx)
        {
            string instruccion = linea.Operacion.ToUpper();
            if (!instruccion.StartsWith("+")) return "---";
            instruccion = instruccion[1..];
            if (!SicxeInstructions.TryGetValue(instruccion, out var codOp)) return "---";

            string operando       = linea.Operandos;
            string operandoLimpio = operando.TrimStart('@','#').Replace(",X","").Trim();

            int  TA          = 0xFFFFF;
            bool esM         = false;
            bool noSeEncontro = false;

            if (operandoLimpio.EndsWith("H", StringComparison.OrdinalIgnoreCase))
            {
                string numHex = operandoLimpio[..^1];
                if (int.TryParse(numHex, System.Globalization.NumberStyles.HexNumber,
                                 null, out int hexVal))
                {
                    int decVal = Convert.ToInt32(numHex, 16);
                    if (decVal > 4095)
                    {
                        TA  = hexVal;
                        esM = true;
                    }
                    else
                    {
                        // Hex value ≤ 4095: not a valid F4 memory combo
                        TA = 0xFFFFF;
                        linea.ErrorMessage = "Error: No combinacion";
                    }
                }
            }
            else if (int.TryParse(operandoLimpio, out int decNum))
            {
                TA = Convert.ToInt32(decNum.ToString("X"), 16);
                // Decimal constant in F4 → no-combo error per reference
                linea.ErrorMessage = "Error: No combinacion";
            }
            else
            {
                // Look up in TabSim
                noSeEncontro = true;
                foreach (var sym in tablaSimbolos)
                {
                    if (string.Equals(sym.Key, operandoLimpio, StringComparison.OrdinalIgnoreCase))
                    {
                        TA           = sym.Value;
                        noSeEncontro = false;
                        esM          = true;
                        break;
                    }
                }
                if (noSeEncontro)
                {
                    TA = 0xFFFFF;
                    linea.ErrorMessage = "Error: Simbolo no encontrado en TABSIM";
                }
            }

            // nixbpe: e=1 always; adjust n,i,x,b,p based on mode and errors
            string nixbpe;
            if (operando.StartsWith("@"))
                nixbpe = noSeEncontro ? "010111" : "100001";
            else if (operando.StartsWith("#"))
                nixbpe = noSeEncontro ? "010111" : "010001";
            else if (operando.EndsWith(",X", StringComparison.OrdinalIgnoreCase))
                nixbpe = noSeEncontro ? "111111" : "111001";
            else
                nixbpe = noSeEncontro ? "100111" : "110001";

            // Assemble
            char   codOp1    = codOp[0];
            int    c2bin     = Convert.ToInt32(codOp[1].ToString(), 16);
            string c2bits    = Convert.ToString(c2bin, 2).PadLeft(4,'0')[..2];
            string byteStr   = c2bits + nixbpe;
            int    byteFinal = Convert.ToInt32(byteStr, 2);
            string byteHex   = $"{(byteFinal >> 4):X}{(byteFinal & 0xF):X}";
            string dirHex    = (TA & 0xFFFFF).ToString("X5");

            string resultado = $"{codOp1}{byteHex}{dirHex}".ToUpper();

            // '*' marks relocation entries
            return esM ? resultado + "*" : resultado;
        }
    }
}