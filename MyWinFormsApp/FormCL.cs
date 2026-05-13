using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MyWinFormsApp
{
	/// <summary>
	/// Formulario de resultados del Cargador-Ligador.
	/// Muestra el mapa de memoria y la TABSE generados por los algoritmos
	/// de Paso 1 (TABSE) y Paso 2 (carga en memoria).
	/// </summary>
	public class FormCL : Form
	{
		// --- Propiedades públicas (se asignan antes de Show()) -------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<string> RutasArchivos { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string DIRPROG { get; set; } = "0000";

		// --- Variables internas del cargador-ligador -------------------------------------------------------------
		private string DIRSC;
		private string DIREJ;

		// --- Controles -------------------------------------------------------------
		private DataGridView dgv_MemoryMap;
		private DataGridView dgv_TABSE;
		private Label labelEjec;
		private Label labelLonsc;

		// --- Constructor -------------------------------------------------------------
		public FormCL()
		{
			InicializarComponentes();
		}

		// --- Carga del formulario -------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			try
			{
				EjecutarPaso1();
				EjecutarPaso2();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al ejecutar el cargador-ligador:\n" + ex.Message,
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		// ===================================================================================
		//  UTILIDADES HEX
		// ===================================================================================
		private int HexToInt(string hex) => Convert.ToInt32(hex, 16);
		private string IntToHex(int value, int digits = 6) =>
			value.ToString("X").PadLeft(digits, '0');
		private string SumHex(string hex1, string hex2) =>
			IntToHex(HexToInt(hex1) + HexToInt(hex2));
		private string SubHex(string hex1, string hex2) =>
			IntToHex(HexToInt(hex1) - HexToInt(hex2));

		// ===================================================================================
		//  PASO 1 — Construir TABSE
		// ===================================================================================
		private void EjecutarPaso1()
		{
			DIRSC = DIRPROG;

			foreach (string ruta in RutasArchivos)
			{
				if (!File.Exists(ruta))
				{
					MessageBox.Show($"Archivo no encontrado:\n{ruta}", "Advertencia",
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
					continue;
				}

				var lineas = File.ReadAllLines(ruta);
				int i = 0;

				while (i < lineas.Length)
				{
					string linea = lineas[i];

					if (linea.StartsWith("H"))
					{
						string nombreSeccion = linea.Substring(1, 6).TrimEnd('_').Trim();
						string longitudSC = linea.Substring(linea.Length - 6, 6);

						bool duplicado = dgv_TABSE.Rows
							.Cast<DataGridViewRow>()
							.Any(row => row.Cells[0].Value?.ToString() == nombreSeccion);

						if (!duplicado)
							dgv_TABSE.Rows.Add(nombreSeccion, "---", DIRSC, longitudSC);

						i++;

						while (i < lineas.Length && !lineas[i].StartsWith("E"))
						{
							string subLinea = lineas[i];
							if (subLinea.StartsWith("D"))
							{
								string datos = subLinea.Substring(1);
								for (int j = 0; j + 12 <= datos.Length; j += 12)
								{
									string simbolo = datos.Substring(j, 6).TrimEnd('_').Trim();
									string dirRelativa = datos.Substring(j + 6, 6);
									string direccionAbs = SumHex(DIRSC, dirRelativa);

									bool simboloDuplicado = dgv_TABSE.Rows
										.Cast<DataGridViewRow>()
										.Any(row => row.Cells[1].Value?.ToString() == simbolo);

									if (!simboloDuplicado)
										dgv_TABSE.Rows.Add("---", simbolo, direccionAbs, "---");
								}
							}
							i++;
						}

						DIRSC = SumHex(DIRSC, longitudSC);
					}
					else
						i++;
				}
			}
		}

		// ===================================================================================
		//  PASO 2 — Cargar en mapa de memoria y aplicar registros M
		// ===================================================================================
		private void EjecutarPaso2()
		{
			DIRSC = DIRPROG;
			DIREJ = DIRPROG;

			Color[] coloresM = {
				Color.Yellow, Color.LightBlue, Color.MediumPurple, Color.Pink,
				Color.LightSalmon, Color.LightGreen, Color.Khaki, Color.Lavender
			};

			foreach (string ruta in RutasArchivos)
			{
				if (!File.Exists(ruta)) continue;

				var lineas = File.ReadAllLines(ruta);
				int i = 0;

				while (i < lineas.Length)
				{
					if (lineas[i].StartsWith("H"))
					{
						string longitudSC = lineas[i].Substring(lineas[i].Length - 6, 6);
						i++;

						while (i < lineas.Length && !lineas[i].StartsWith("E"))
						{
							string linea = lineas[i];

							if (linea.StartsWith("T"))
							{
								string dirRel = linea.Substring(1, 6);
								string direccionMem = SumHex(DIRSC, dirRel);
								string contenido = linea.Substring(9);

								for (int j = 0; j < contenido.Length; j += 2)
								{
									string byteHex = contenido.Substring(j, 2);
									string direccion = IntToHex(HexToInt(direccionMem) + (j / 2), 4);
									EscribirEnMapaMemoria(direccion, byteHex);
								}
							}
							else if (linea.StartsWith("M"))
							{
								string dirRel = linea.Substring(1, 6);
								string direccion = SumHex(DIRSC, dirRel);

								string operacion = linea.Substring(9, 1);
								string etiqueta = linea.Substring(10).TrimEnd('_').Trim();

								string valorEtiqueta = BuscarDireccionEnTABSE(etiqueta);
								if (valorEtiqueta == null)
								{
									MessageBox.Show($"Símbolo no definido: {etiqueta}",
										"Error en registro M", MessageBoxButtons.OK, MessageBoxIcon.Warning);
									i++;
									continue;
								}

								string valorOriginal = LeerBytesDesdeMapa(direccion, 3);
								string nuevoValor = operacion == "+"
									? SumHex(valorOriginal, valorEtiqueta)
									: SubHex(valorOriginal, valorEtiqueta);

								EscribirBytesEnMapa(direccion, nuevoValor);

								// Colorear las celdas modificadas
								var rand = new Random(Guid.NewGuid().GetHashCode());
								var colorM = coloresM[rand.Next(coloresM.Length)];
								ColorearCeldas(direccion, 3, colorM);
							}
							i++;
						}

						// Registro E — dirección de ejecución
						if (i < lineas.Length && lineas[i].StartsWith("E") &&
							lineas[i].Length > 1 && lineas[i].Length >= 7)
						{
							string dirEsp = lineas[i].Substring(1, 6);
							DIREJ = SumHex(DIRSC, dirEsp);
						}
						i++;

						DIRSC = SumHex(DIRSC, longitudSC);
					}
					else
						i++;
				}
			}

			// Resaltar dirección de inicio de ejecución en verde
			string baseAddr4 = IntToHex((HexToInt(DIREJ) / 16) * 16, 4);
			int startCol = Convert.ToInt32(DIREJ.Substring(DIREJ.Length > 4 ? 5 : 3, 1), 16) + 1;
			foreach (DataGridViewRow row in dgv_MemoryMap.Rows)
			{
				if (row.Cells[0].Value?.ToString() == baseAddr4)
				{
					if (startCol < row.Cells.Count)
						row.Cells[startCol].Style.BackColor = Color.LimeGreen;
					break;
				}
			}

			// Mostrar info en labels
			string direjShow = DIREJ.TrimStart('0');
			string lonscShow = DIRSC.TrimStart('0');
			if (string.IsNullOrEmpty(direjShow)) direjShow = "0";
			if (string.IsNullOrEmpty(lonscShow)) lonscShow = "0";

			labelEjec.Text = "Ejecución inicia en: " + direjShow.ToUpper() + "H";
			labelLonsc.Text = "Longitud total: " + lonscShow.ToUpper() + "H";

			// Post-formateo de TABSE: quitar ceros a la izquierda en dirección y longitud
			QuitarCerosTabse();

			// Auto-escalar grids
			EscalarGrids();
		}

		// ===================================================================================
		//  HELPERS — Mapa de memoria
		// ===================================================================================
		private void EscribirEnMapaMemoria(string direccion, string byteHex)
		{
			// direccion es 4 chars: XXX0-XXXF; col 0 = base addr, cols 1-16 = bytes
			string baseDir = direccion.Substring(0, 3) + "0";
			int rowIndex = -1;

			foreach (DataGridViewRow row in dgv_MemoryMap.Rows)
			{
				if (row.Cells[0].Value?.ToString() == baseDir)
				{
					rowIndex = row.Index; break;
				}
			}

			if (rowIndex == -1)
			{
				rowIndex = dgv_MemoryMap.Rows.Add();
				dgv_MemoryMap.Rows[rowIndex].Cells[0].Value = baseDir;
			}

			int colIndex = Convert.ToInt32(direccion.Substring(3, 1), 16) + 1;
			dgv_MemoryMap.Rows[rowIndex].Cells[colIndex].Value = byteHex.ToUpper();
		}

		private string LeerBytesDesdeMapa(string direccion, int byteCount)
		{
			string resultado = "";
			for (int i = 0; i < byteCount; i++)
			{
				string actualDir = IntToHex(HexToInt(direccion) + i, 4);
				string baseDir = actualDir.Substring(0, 3) + "0";
				int rowIndex = -1;

				foreach (DataGridViewRow row in dgv_MemoryMap.Rows)
				{
					if (row.Cells[0].Value?.ToString() == baseDir)
					{ rowIndex = row.Index; break; }
				}

				if (rowIndex != -1)
				{
					int colIndex = Convert.ToInt32(actualDir.Substring(3, 1), 16) + 1;
					resultado += dgv_MemoryMap.Rows[rowIndex].Cells[colIndex].Value?.ToString() ?? "00";
				}
				else
					resultado += "00";
			}
			return resultado;
		}

		private void EscribirBytesEnMapa(string direccion, string valorHex)
		{
			for (int i = 0; i < valorHex.Length; i += 2)
			{
				string actualDir = IntToHex(HexToInt(direccion) + (i / 2), 4);
				EscribirEnMapaMemoria(actualDir, valorHex.Substring(i, 2));
			}
		}

		private void ColorearCeldas(string direccionBase, int byteCount, Color color)
		{
			for (int b = 0; b < byteCount; b++)
			{
				string modDir = IntToHex(HexToInt(direccionBase) + b, 4);
				string baseDir = modDir.Substring(0, 3) + "0";
				foreach (DataGridViewRow row in dgv_MemoryMap.Rows)
				{
					if (row.Cells[0].Value?.ToString() == baseDir)
					{
						int colIndex = Convert.ToInt32(modDir.Substring(3, 1), 16) + 1;
						if (colIndex < row.Cells.Count)
							row.Cells[colIndex].Style.BackColor = color;
						break;
					}
				}
			}
		}

		// ===================================================================================
		//  HELPERS — TABSE
		// ===================================================================================
		private string BuscarDireccionEnTABSE(string simbolo)
		{
			foreach (DataGridViewRow row in dgv_TABSE.Rows)
			{
				if (row.Cells[1].Value?.ToString() == simbolo ||
					row.Cells[0].Value?.ToString() == simbolo)
					return row.Cells[2].Value?.ToString();
			}
			return null;
		}

		private void QuitarCerosTabse()
		{
			foreach (DataGridViewRow row in dgv_TABSE.Rows)
			{
				for (int c = 2; c <= 3; c++)
				{
					if (row.Cells[c].Value == null) continue;
					string v = row.Cells[c].Value.ToString();
					if (v != "---" && v.Length >= 2)
					{
						v = v.TrimStart('0');
						row.Cells[c].Value = string.IsNullOrEmpty(v) ? "0" : v;
					}
				}
			}
		}

		private void EscalarGrids()
		{
			// Memory map — fuente grande para facilitar lectura
			float escMap = 2.4f;
			var fMap = dgv_MemoryMap.Font;
			dgv_MemoryMap.Font = new Font(fMap.FontFamily, fMap.Size * escMap, fMap.Style);
			dgv_MemoryMap.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
			dgv_MemoryMap.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

			// TABSE — fuente mediana
			float escTab = 1.4f;
			var fTab = dgv_TABSE.Font;
			dgv_TABSE.Font = new Font(fMap.FontFamily, fTab.Size * escTab, fTab.Style);
			dgv_TABSE.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
			dgv_TABSE.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
		}

		// ===================================================================================
		//  ACCIONES — Botones
		// ===================================================================================
		private void MostrarContenidoArchivos()
		{
			if (RutasArchivos == null || RutasArchivos.Count == 0)
			{ MessageBox.Show("No hay archivos cargados."); return; }

			var sb = new System.Text.StringBuilder();
			foreach (string ruta in RutasArchivos)
			{
				if (!File.Exists(ruta)) { sb.AppendLine($"[No encontrado: {ruta}]"); continue; }
				sb.AppendLine($"===== {Path.GetFileName(ruta)} =====");
				sb.AppendLine(File.ReadAllText(ruta));
				sb.AppendLine();
			}

			string texto = sb.ToString();
			// Mostrar en ventana emergente
			var winArch = new Form
			{
				Text = "Contenido de los archivos OBJ",
				Size = new Size(700, 500),
				StartPosition = FormStartPosition.CenterParent
			};
			var rtb = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Font = new Font("Courier New", 9),
				Text = texto,
				ScrollBars = RichTextBoxScrollBars.Both,
				WordWrap = false
			};
			winArch.Controls.Add(rtb);
			winArch.ShowDialog(this);
		}

		private void AbrirArchivosEnNotepad()
		{
			if (RutasArchivos == null || RutasArchivos.Count == 0)
			{ MessageBox.Show("No hay archivos cargados."); return; }

			foreach (string ruta in RutasArchivos)
			{
				if (File.Exists(ruta))
					Process.Start("notepad.exe", ruta);
				else
					MessageBox.Show($"Archivo no encontrado:\n{ruta}",
						"Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void Reordenar()
		{
			var selector = new FormOrdenArchivos(RutasArchivos ?? new List<string>());
			selector.Show();
			this.Close();
		}

		// ===================================================================================
		//  DISEÑO DEL FORMULARIO
		// ===================================================================================
		private void InicializarComponentes()
		{
			this.Text = "Cargador-Ligador — Resultado";
			this.Size = new Size(1400, 780);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.WindowState = FormWindowState.Maximized;
			this.BackColor = Color.FromArgb(245, 245, 250);

			// --- Panel superior — título + info -------------------------------------------
			var panelTop = new Panel
			{
				Dock = DockStyle.Top,
				Height = 80,
				BackColor = Color.FromArgb(25, 50, 100),
				Padding = new Padding(10, 8, 10, 8)
			};

			var lblTitulo = new Label
			{
				Text = "CARGADOR LIGADOR — MAPA DE MEMORIA",
				AutoSize = true,
				Font = new Font("Arial", 13, FontStyle.Bold),
				ForeColor = Color.White,
				Location = new Point(12, 8)
			};

			labelEjec = new Label
			{
				Text = "Ejecución inicia en: ---",
				AutoSize = true,
				Font = new Font("Arial", 10, FontStyle.Bold),
				ForeColor = Color.LimeGreen,
				Location = new Point(12, 42)
			};

			labelLonsc = new Label
			{
				Text = "Longitud total: ---",
				AutoSize = true,
				Font = new Font("Arial", 10, FontStyle.Bold),
				ForeColor = Color.Cyan,
				Location = new Point(380, 42)
			};

			panelTop.Controls.Add(lblTitulo);
			panelTop.Controls.Add(labelEjec);
			panelTop.Controls.Add(labelLonsc);

			// --- Panel inferior — botones -------------------------------------------
			var panelBottom = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 56,
				BackColor = Color.FromArgb(230, 230, 240)
			};

			var btnVerArch = CrearBotonCL("Ver archivos", 12, Color.SteelBlue);
			btnVerArch.Click += (s, e) => MostrarContenidoArchivos();

			var btnAbrirNotepad = CrearBotonCL("Abrir en Notepad", 162, Color.FromArgb(80, 80, 80));
			btnAbrirNotepad.Click += (s, e) => AbrirArchivosEnNotepad();

			var btnReordenar = CrearBotonCL("Reordenar archivos", 312, Color.FromArgb(120, 60, 160));
			btnReordenar.Click += (s, e) => Reordenar();

			var btnCerrar = CrearBotonCL("Cerrar", 490, Color.Crimson);
			btnCerrar.Click += (s, e) => this.Close();

			panelBottom.Controls.Add(btnVerArch);
			panelBottom.Controls.Add(btnAbrirNotepad);
			panelBottom.Controls.Add(btnReordenar);
			panelBottom.Controls.Add(btnCerrar);

			// --- SplitContainer principal — mapa de memoria | TABSE -------------------------------------------
			var split = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Vertical,
				SplitterDistance = 800,
				BackColor = Color.FromArgb(245, 245, 250)
			};

			// --- Panel izquierdo: Mapa de Memoria -------------------------------------------
			var panelMem = new Panel
			{
				Dock = DockStyle.Fill,
				BorderStyle = BorderStyle.None
			};

			var lblMem = new Label
			{
				Text = "MAPA DE MEMORIA",
				Dock = DockStyle.Top,
				Height = 28,
				BackColor = Color.FromArgb(0, 100, 60),
				ForeColor = Color.White,
				Font = new Font("Arial", 9, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleCenter
			};

			dgv_MemoryMap = new DataGridView
			{
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = true,
				Font = new Font("Courier New", 8),
				BackgroundColor = Color.FromArgb(245, 245, 250),
				GridColor = Color.FromArgb(245, 245, 250),
				DefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.White,
					ForeColor = Color.Black,
					SelectionBackColor = Color.FromArgb(151, 174, 185),
					SelectionForeColor = Color.White
				},
				ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.FromArgb(91, 112, 127),
					ForeColor = Color.White,
					Font = new Font("Courier New", 8, FontStyle.Bold)
				},
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
				EnableHeadersVisualStyles = false,
				ScrollBars = ScrollBars.Both
			};

			// Columna de dirección base
			dgv_MemoryMap.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Dirección",
				ReadOnly = true,
				Width = 70,
				DefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.FromArgb(245, 245, 250),
					ForeColor = Color.Black,
					Font = new Font("Courier New", 8, FontStyle.Bold)
				}
			});

			// Columnas 0-F
			string[] nibbles = { "0", "1", "2", "3", "4", "5", "6", "7",
								  "8", "9", "A", "B", "C", "D", "E", "F" };
			foreach (string n in nibbles)
			{
				dgv_MemoryMap.Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = n,
					ReadOnly = true,
					Width = 42
				});
			}

			panelMem.Controls.Add(dgv_MemoryMap);
			panelMem.Controls.Add(lblMem);

			// --- Panel derecho: TABSE -------------------------------------------
			var panelTab = new Panel { Dock = DockStyle.Fill };

			var lblTab = new Label
			{
				Text = "TABLA DE SECCIONES EXTERNAS (TABSE)",
				Dock = DockStyle.Top,
				Height = 28,
				BackColor = Color.FromArgb(127, 99, 71),
				ForeColor = Color.White,
				Font = new Font("Arial", 9, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleCenter
			};

			dgv_TABSE = new DataGridView
			{
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = true,
				Font = new Font("Courier New", 8),
				BackgroundColor = Color.White,
				GridColor = Color.White,
				DefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.White,
					ForeColor = Color.Black,
					SelectionBackColor = Color.FromArgb(255, 230, 191),
					SelectionForeColor = Color.Black
				},
				ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
				{
					BackColor = Color.FromArgb(80, 40, 0),
					ForeColor = Color.White,
					Font = new Font("Courier New", 8, FontStyle.Bold)
				},
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
				EnableHeadersVisualStyles = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
			};

			dgv_TABSE.Columns.Add(new DataGridViewTextBoxColumn
			{ HeaderText = "Sección", ReadOnly = true, Width = 80 });
			dgv_TABSE.Columns.Add(new DataGridViewTextBoxColumn
			{ HeaderText = "Símbolo", ReadOnly = true, Width = 80 });
			dgv_TABSE.Columns.Add(new DataGridViewTextBoxColumn
			{ HeaderText = "Dirección", ReadOnly = true, Width = 80 });
			dgv_TABSE.Columns.Add(new DataGridViewTextBoxColumn
			{ HeaderText = "Longitud", ReadOnly = true, Width = 80 });

			panelTab.Controls.Add(dgv_TABSE);
			panelTab.Controls.Add(lblTab);

			split.Panel1.Controls.Add(panelMem);
			split.Panel2.Controls.Add(panelTab);

			// --- Ensamblar -------------------------------------------
			this.Controls.Add(split);
			this.Controls.Add(panelBottom);
			this.Controls.Add(panelTop);
		}

		private Button CrearBotonCL(string texto, int x, Color color)
		{
			var btn = new Button
			{
				Text = texto,
				Location = new Point(x, 10),
				Size = new Size(140, 36),
				BackColor = color,
				ForeColor = Color.White,
				Font = new Font("Arial", 9),
				FlatStyle = FlatStyle.Flat,
				Cursor = Cursors.Hand
			};
			btn.FlatAppearance.BorderSize = 0;
			return btn;
		}
	}
}