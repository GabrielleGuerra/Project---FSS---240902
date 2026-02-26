using System;
using System.Collections.Generic;
using System.Linq;

namespace MyWinFormsApp
{
    public class InstruccionInfo
    {
        // formato de una instruccion
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
            // FORMATO 1 - Sin operandos
            tabla["FIX"] = new InstruccionInfo("FIX", 1, "ninguno", 0);
            tabla["FLOAT"] = new InstruccionInfo("FLOAT", 1, "ninguno", 0);
            tabla["HIO"] = new InstruccionInfo("HIO", 1, "ninguno", 0);
            tabla["NORM"] = new InstruccionInfo("NORM", 1, "ninguno", 0);
            tabla["SIO"] = new InstruccionInfo("SIO", 1, "ninguno", 0);
            tabla["TIO"] = new InstruccionInfo("TIO", 1, "ninguno", 0);

            // FORMATO 2 - Con registros
            tabla["ADDR"] = new InstruccionInfo("ADDR", 2, "r1,r2", 2);
            tabla["CLEAR"] = new InstruccionInfo("CLEAR", 2, "r1", 1);
            tabla["COMPR"] = new InstruccionInfo("COMPR", 2, "r1,r2", 2);
            tabla["DIVR"] = new InstruccionInfo("DIVR", 2, "r1,r2", 2);
            tabla["MULR"] = new InstruccionInfo("MULR", 2, "r1,r2", 2);
            tabla["RMO"] = new InstruccionInfo("RMO", 2, "r1,r2", 2);
            tabla["SHIFTL"] = new InstruccionInfo("SHIFTL", 2, "r1,n", 2);
            tabla["SUBR"] = new InstruccionInfo("SUBR", 2, "r1,r2", 2);
            tabla["SVC"] = new InstruccionInfo("SVC", 2, "n", 1);
            tabla["TIXR"] = new InstruccionInfo("TIXR", 2, "r1", 1);

            // FORMATO 3/4 - Con operandos de memoria (m)
            tabla["ADD"] = new InstruccionInfo("ADD", 34, "m", 1);
            tabla["ADDF"] = new InstruccionInfo("ADDF", 34, "m", 1);
            tabla["AND"] = new InstruccionInfo("AND", 34, "m", 1);
            tabla["COMP"] = new InstruccionInfo("COMP", 34, "m", 1);
            tabla["COMPF"] = new InstruccionInfo("COMPF", 34, "m", 1);
            tabla["DIV"] = new InstruccionInfo("DIV", 34, "m", 1);
            tabla["DIVF"] = new InstruccionInfo("DIVF", 34, "m", 1);
            tabla["J"] = new InstruccionInfo("J", 34, "m", 1);
            tabla["JEQ"] = new InstruccionInfo("JEQ", 34, "m", 1);
            tabla["JGT"] = new InstruccionInfo("JGT", 34, "m", 1);
            tabla["JLT"] = new InstruccionInfo("JLT", 34, "m", 1);
            tabla["JSUB"] = new InstruccionInfo("JSUB", 34, "m", 1);
            tabla["LDA"] = new InstruccionInfo("LDA", 34, "m", 1);
            tabla["LDB"] = new InstruccionInfo("LDB", 34, "m", 1);
            tabla["LDCH"] = new InstruccionInfo("LDCH", 34, "m", 1);
            tabla["LDF"] = new InstruccionInfo("LDF", 34, "m", 1);
            tabla["LDL"] = new InstruccionInfo("LDL", 34, "m", 1);
            tabla["LDS"] = new InstruccionInfo("LDS", 34, "m", 1);
            tabla["LDT"] = new InstruccionInfo("LDT", 34, "m", 1);
            tabla["LDX"] = new InstruccionInfo("LDX", 34, "m", 1);
            tabla["LPS"] = new InstruccionInfo("LPS", 34, "m", 1);
            tabla["MUL"] = new InstruccionInfo("MUL", 34, "m", 1);
            tabla["MULF"] = new InstruccionInfo("MULF", 34, "m", 1);
            tabla["OR"] = new InstruccionInfo("OR", 34, "m", 1);
            tabla["RD"] = new InstruccionInfo("RD", 34, "m", 1);
            tabla["RSUB"] = new InstruccionInfo("RSUB", 34, "ninguno", 0);
            tabla["SSK"] = new InstruccionInfo("SSK", 34, "m", 1);
            tabla["STA"] = new InstruccionInfo("STA", 34, "m", 1);
            tabla["STB"] = new InstruccionInfo("STB", 34, "m", 1);
            tabla["STCH"] = new InstruccionInfo("STCH", 34, "m", 1);
            tabla["STF"] = new InstruccionInfo("STF", 34, "m", 1);
            tabla["STI"] = new InstruccionInfo("STI", 34, "m", 1);
            tabla["STL"] = new InstruccionInfo("STL", 34, "m", 1);
            tabla["STS"] = new InstruccionInfo("STS", 34, "m", 1);
            tabla["STSW"] = new InstruccionInfo("STSW", 34, "m", 1);
            tabla["STT"] = new InstruccionInfo("STT", 34, "m", 1);
            tabla["STX"] = new InstruccionInfo("STX", 34, "m", 1);
            tabla["SUB"] = new InstruccionInfo("SUB", 34, "m", 1);
            tabla["SUBF"] = new InstruccionInfo("SUBF", 34, "m", 1);
            tabla["TD"] = new InstruccionInfo("TD", 34, "m", 1);
            tabla["TIX"] = new InstruccionInfo("TIX", 34, "m", 1);
            tabla["WD"] = new InstruccionInfo("WD", 34, "m", 1);

            // DIRECTIVAS
            tabla["START"] = new InstruccionInfo("START", 0, "numero", 1);
            tabla["END"] = new InstruccionInfo("END", 0, "etiqueta", 1);
            tabla["BYTE"] = new InstruccionInfo("BYTE", 0, "byte", 1);
            tabla["WORD"] = new InstruccionInfo("WORD", 0, "numero", 1);
            tabla["RESB"] = new InstruccionInfo("RESB", 0, "numero", 1);
            tabla["RESW"] = new InstruccionInfo("RESW", 0, "numero", 1);
            tabla["BASE"] = new InstruccionInfo("BASE", 0, "etiqueta", 1);
        }

        //obtenemos la info de una instriccion por su nombre, si no existe devuelve null
        public InstruccionInfo? Obtener(string nombre)
        {
            // vuelvo todo mayuscula para evitar problemas de busqueda
            return tabla.TryGetValue(nombre.ToUpper(), out var info) ? info : null;
        }

        //checa que una instruccion exista en la tabla, devuelve true o false
        public bool Existe(string nombre)
        {
            return tabla.ContainsKey(nombre.ToUpper());
        }
    }

    public class LineaProcesada
    {
        //estructura de una linea ya procesada, con su numero, contador, etiqueta, operacion, operandos, errores y si es valida o no
        public int NumeroLinea { get; set; }
        public string Contador { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public string Operacion { get; set; } = "";
        public string Operandos { get; set; } = "";
        public List<string> Errores { get; set; }
        public bool Valida { get; set; }

        public LineaProcesada()
        {
            Errores = new List<string>();
            Valida = true;
        }
    }

    //=======================================================================================
    // CLASE PRINCIPAL
    //aqui analizamos el codigo, linea por linea, identificando etiquetas, operaciones, operandos y errores, y construyendo la tabla de simbolos 
    //=======================================================================================
    public class AnalizadorSicXe 
    {
        private TablaCodigos tabla;
        private List<LineaProcesada> lineasProcesadas;
        private List<string> erroresGlobales;
        private Dictionary<string, string> tablaSimbolos;
        private int contadorPrograma = 0;

        public AnalizadorSicXe()
        {
            tabla = new TablaCodigos();
            lineasProcesadas = new List<LineaProcesada>();
            erroresGlobales = new List<string>();
            tablaSimbolos = new Dictionary<string, string>();
        }

        public void Analizar(string contenido)
        {
            //limpiar todos los datos anteriores para un nuevo análisis
            lineasProcesadas.Clear();
            erroresGlobales.Clear();
            tablaSimbolos.Clear();
            contadorPrograma = 0;

            //dividimos el contenido en lineas, eliminando las vacias
            string[] lineas = contenido.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            //procesamos cada linea individualmente
            foreach (var linea in lineas)
            {
                ProcesarLinea(linea);
            }
        }
    
        private void ProcesarLinea(string linea)
        {
            //ignoramos lineas vacias 
            if (string.IsNullOrWhiteSpace(linea) || linea.TrimStart().StartsWith("."))
                return;

            //dividimos la linea en campos, usando tabulaciones y espacios como separadores, y eliminando vacios    
            string[] campos = linea.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //almacenamos la linea procesada
            var resultado = new LineaProcesada
            {
                Contador = ContadorEnHex(),
                NumeroLinea = lineasProcesadas.Count + 1,
                Etiqueta = ""
            };

            int indice = 0;

            // PASO 1: Identificar etiqueta
            if (campos.Length > 0)
            {
                string primerCampo = campos[0].Trim();

                // Obtener el nombre de instrucción sin el +
                string instruccionPosible = primerCampo;
                if (primerCampo.StartsWith("+"))
                {
                    instruccionPosible = primerCampo.Substring(1).ToUpper();
                }
                else
                {
                    instruccionPosible = primerCampo.ToUpper();
                }

                // Verificar si es una instrucción conocida
                if (tabla.Existe(instruccionPosible))
                {
                    // Es instrucción, no etiqueta
                    resultado.Etiqueta = "";
                    indice = 0;
                }
                else if (!string.IsNullOrEmpty(primerCampo))
                {
                    // Es etiqueta
                    if (ValidarEtiqueta(primerCampo))
                    {
                        resultado.Etiqueta = primerCampo;
                        indice = 1;
                    }
                    else
                    {
                        // si es etiqueta no valida, registramos el error y no procesamos mas esta linea
                        resultado.Errores.Add($"[Léxico] Etiqueta inválida: '{primerCampo}'");
                        resultado.Valida = false;
                        lineasProcesadas.Add(resultado);
                        foreach (var error in resultado.Errores)
                        {
                            erroresGlobales.Add($"[Línea {resultado.NumeroLinea}] {error}");
                        }
                        return;
                    }
                }
                else
                {
                    resultado.Etiqueta = "";
                    indice = 1;
                }
            }

            // PASO 2: Extraer operación  (si no hay etiqueta, el primer campo es la operación, si hay etiqueta, el segundo campo es la operación)
            if (indice < campos.Length)
            {
                // detectar si la operación tiene formato 4 (empieza con +)
                string campo = campos[indice].Trim();

                bool esFormato4 = campo.StartsWith("+");
                string operacionOriginal = campo; // guardamos la operacion
                
                if (esFormato4)
                {
                    campo = campo.Substring(1); //quitamos el +
                }

                campo = campo.ToUpper();
                resultado.Operacion = operacionOriginal;

                // Extraer operandos completos
                if (indice + 1 < campos.Length)
                {
                    resultado.Operandos = string.Join(" ", campos.Skip(indice + 1)).Trim();
                }
                else
                {
                    resultado.Operandos = "";
                }

                // PASO 3: Validar instrucción
                InstruccionInfo info = tabla.Obtener(campo); // obtenemos la info de la instruccion, si no existe, es un error léxico

                if (info == null) // instrucción no existe, mostrar error
                {
                    resultado.Errores.Add($"[Léxico] Instrucción no existe: '{campo}'");
                    resultado.Valida = false;
                }
                else
                {
                    // Registrar etiqueta en tabla de símbolos si es válida
                    if (!string.IsNullOrEmpty(resultado.Etiqueta) && info.Nombre != "START" && info.Nombre != "END")
                    {
                        if (tablaSimbolos.ContainsKey(resultado.Etiqueta))
                        {
                            //etiqueta ya existe, error semántico
                            resultado.Errores.Add($"[Semántico] Símbolo duplicado: '{resultado.Etiqueta}'");
                            resultado.Valida = false;
                        }
                        else
                        {
                            //etiqueta válida, la agregamos a la tabla de símbolos con su dirección actual
                            tablaSimbolos[resultado.Etiqueta] = resultado.Contador;
                        }
                    }

                    // Validar instrucción e incrementar contador de programa según el formato y tipo de instrucción
                    int incremento = ValidarInstruccion(info, resultado.Operandos, esFormato4, resultado.Errores);

                    if (resultado.Errores.Count == 0)
                    {
                        resultado.Valida = true;
                        contadorPrograma += incremento; // solo incrementamos si no hay errores, para evitar confusiones en la dirección de las etiquetas siguientes
                    }
                    else
                    {
                        resultado.Valida = false;
                    }
                }
            }

            // Guardamos el resultado de esta línea procesada en la lista de líneas procesadas y acumulamos los errores globales con su número de línea correspondiente
            lineasProcesadas.Add(resultado);
            foreach (var error in resultado.Errores)
            {
                erroresGlobales.Add($"[Línea {resultado.NumeroLinea}] {error}");
            }
        }

        // funcion que valida la instruccion y devuelve el incremento del contador de programa segun su formato
        // o 0 si hay errores (en ese caso se registran los errores en la lista de errores pasada como parametro)
        private int ValidarInstruccion(InstruccionInfo info, string operandos, bool esFormato4, List<string> errores)
        {
            // Directivas que no incrementan
            if (info.Nombre == "START" || info.Nombre == "END" || info.Nombre == "BASE")
            {
                return 0;
            }

            // BYTE especial: solo C'...' o X'...'
            if (info.Nombre == "BYTE")
            {
                if (!operandos.StartsWith("C'") && !operandos.StartsWith("X'"))
                {
                    errores.Add($"[Sintáctico] BYTE debe ser C'texto' o X'hexadecimal'");
                    return 0;
                }

                //Calcular el tamaño de BYTE según su formato
                if (operandos.StartsWith("C'") && operandos.EndsWith("'"))
                {
                    string contenido = operandos.Substring(2, operandos.Length - 3);
                    return contenido.Length;
                }
                else if (operandos.StartsWith("X'") && operandos.EndsWith("'"))
                {
                    string hex = operandos.Substring(2, operandos.Length - 3);
                    return (hex.Length + 1) / 2; //va de 2 en 2 caracteres hexadecimales por byte, redondeando hacia arriba si es impar
                }
                else
                {
                    errores.Add($"[Sintáctico] BYTE debe ser C'texto' o X'hexadecimal'");
                    return 0;
                }
            }

            // RESW, RESB, WORD
            if (info.Nombre == "RESW") // cada palabra son 3 bytes
            {
                if (int.TryParse(operandos, out int num))
                {
                    return num * 3;
                }
                else
                {
                    errores.Add($"[Sintáctico] RESW requiere un número");
                    return 0;
                }
            }

            if (info.Nombre == "RESB") //guarda num bytes
            {
                if (int.TryParse(operandos, out int num))
                {
                    return num;
                }
                else
                {
                    errores.Add($"[Sintáctico] RESB requiere un número");
                    return 0;
                }
            }

            if (info.Nombre == "WORD") // cada palabra son 3 bytes
            {
                if (int.TryParse(operandos, out int num))
                {
                    return num * 3;
                }
                else
                {
                    errores.Add($"[Sintáctico] WORD requiere un número");
                    return 0;
                }
            }

            // FORMATO 1: Sin operandos
            if (info.Formato == 1)
            {
                if (!string.IsNullOrEmpty(operandos))
                {
                    errores.Add($"[Sintáctico] {info.Nombre} no puede tener operandos");
                    return 0;
                }
                return 1;
            }

            // FORMATO 2: Registros r1 y r2, o r1 y n, o solo r1, o solo n
            if (info.Formato == 2)
            {
                if (!ValidarFormato2(info, operandos, errores))
                {
                    return 0;
                }
                return 2;
            }

            // FORMATO 3/4: Memoria (m)
            if (info.Formato == 34)
            {
                // RSUB es especial por que no tiene operandos, aunque es formato 3/4, asi que lo validamos aparte
                if (info.Nombre == "RSUB")
                {
                    if (!string.IsNullOrEmpty(operandos))
                    {
                        errores.Add($"[Sintáctico] {info.Nombre} no puede tener operandos");
                        return 0;
                    }
                    return esFormato4 ? 4 : 3;
                }

                // Otras instrucciones formato 3/4 con (m)
                if (string.IsNullOrEmpty(operandos))
                {
                    errores.Add($"[Sintáctico] {info.Nombre} requiere 1 operando");
                    return 0;
                }

                // Validar que no haya ## ni @@ (dobles)
                if (operandos.Contains("##") || operandos.Contains("@@"))
                {
                    errores.Add($"[Sintáctico] Operando inválido: ## y @@ no son válidos");
                    return 0;
                }

                // Validar operandos: puede ser "LABEL" o "LABEL,X"
                // NO puede ser "LABEL,Y" o "LABEL,Z"
                if (operandos.Contains(","))
                {
                    string[] partes = operandos.Split(',');
                    
                    // Solo puede haber exactamente 2 partes
                    if (partes.Length != 2)
                    {
                        errores.Add($"[Sintáctico] {info.Nombre} solo puede tener 1 operando");
                        return 0;
                    }

                    string segunda = partes[1].Trim().ToUpper();
                    if (segunda != "X")
                    {
                        errores.Add($"[Sintáctico] {info.Nombre} solo puede ser indexado con ,X");
                        return 0;
                    }
                }

                // Validar que @ tenga algo después
                string primerOperando = operandos.Contains(",") ? operandos.Split(',')[0].Trim() : operandos.Trim();
                if (primerOperando.StartsWith("@"))
                {
                    if (primerOperando.Length <= 1 || primerOperando == "@")
                    {
                        errores.Add($"[Sintáctico] @ debe ir seguido de un operando");
                        return 0;
                    }
                }

                return esFormato4 ? 4 : 3; //+op = formato 4, op = formato 3
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

            // validar segun el tipo requerido.
            if (info.TipoOperando == "r1" && ops.Length == 1)
            {
                if (!EsRegistroValido(ops[0].Trim()))
                {
                    errores.Add($"[Sintáctico] {ops[0].Trim()} no es un registro válido");
                    return false;
                }
                return true;
            }
            else if (info.TipoOperando == "r1,r2" && ops.Length == 2)
            {
                if (!EsRegistroValido(ops[0].Trim()) || !EsRegistroValido(ops[1].Trim()))
                {
                    errores.Add($"[Sintáctico] Registros inválidos");
                    return false;
                }
                return true;
            }
            else if (info.TipoOperando == "n" && ops.Length == 1)
            {
                if (!int.TryParse(ops[0].Trim(), out _))
                {
                    errores.Add($"[Sintáctico] Debe ser un número");
                    return false;
                }
                return true;
            }
            else if (info.TipoOperando == "r1,n" && ops.Length == 2)
            {
                if (!EsRegistroValido(ops[0].Trim()) || !int.TryParse(ops[1].Trim(), out _))
                {
                    errores.Add($"[Sintáctico] Formato inválido");
                    return false;
                }
                return true;
            }

            errores.Add($"[Sintáctico] Operandos inválidos para {info.Nombre}");
            return false;
        }

        private bool EsRegistroValido(string reg) //registros validos de SICXE
        {
            var registros = new[] { "A", "X", "L", "B", "S", "T", "F", "PC", "SW" };
            return registros.Contains(reg.ToUpper());
        }

        private bool ValidarEtiqueta(string etiqueta)
        {
            if (string.IsNullOrEmpty(etiqueta))
                return false;

            return char.IsLetter(etiqueta[0]) && etiqueta.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
        }

        private string ContadorEnHex()
        {
            return contadorPrograma.ToString("X4"); //contador hex a 4 digitos, por ejemplo 0000, 0003, 0012, etc.
        }

        public List<LineaProcesada> ObtenerLineasProcesadas() => lineasProcesadas;
        public bool HayErrores() => erroresGlobales.Count > 0;
        public List<string> ObtenerErrores() => erroresGlobales;
        public Dictionary<string, string> ObtenerTablaSimbolos() => tablaSimbolos;
    }
}