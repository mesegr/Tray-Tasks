using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace TareasBandeja;

// ---------- Modelo ----------
public class Tarea
{
    public string Titulo { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public bool Hecha { get; set; }
}

// ---------- Programa ----------
static class Program
{
    [STAThread]
    static void Main()
    {
        // Evita abrir dos copias (si no, saldrían dos iconos)
        using var mutex = new Mutex(true, "TareasBandejaUnica", out bool esLaPrimera);
        if (!esLaPrimera) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

// =====================================================================
//  Estilo y controles personalizados (look iOS oscuro)
// =====================================================================
static class Ui
{
    public static readonly Color Fondo = Color.FromArgb(28, 28, 30);
    public static readonly Color Tarjeta = Color.FromArgb(44, 44, 46);
    public static readonly Color Hover = Color.FromArgb(54, 54, 56);
    public static readonly Color Separador = Color.FromArgb(62, 62, 64);
    public static readonly Color Gris = Color.FromArgb(142, 142, 147);
    public static readonly Color Azul = Color.FromArgb(10, 132, 255);
    public static readonly Color AzulClaro = Color.FromArgb(64, 156, 255);
    public static readonly Color Verde = Color.FromArgb(48, 209, 88);
    public static readonly Color Rojo = Color.FromArgb(255, 69, 58);

    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

// Panel con fondo redondeado (las "tarjetas" de iOS)
class RoundedPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 16;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Fill { get; set; } = Ui.Tarjeta;

    public RoundedPanel() { DoubleBuffered = true; }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
        using var brush = new SolidBrush(Fill);
        e.Graphics.FillPath(brush, path);
    }
}

// Botón redondo azul con un "+"
class PlusButton : Button
{
    bool hover, down;

    public PlusButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Ui.Fondo;
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(ControlStyles.Selectable, false);
        DoubleBuffered = true;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Ui.Fondo);

        var color = down ? Color.FromArgb(0, 105, 215) : hover ? Ui.AzulClaro : Ui.Azul;
        using var fondo = new SolidBrush(color);
        g.FillEllipse(fondo, 0, 0, Width - 1, Height - 1);

        float cx = Width / 2f, cy = Height / 2f, medio = Width * 0.18f;
        using var pen = new Pen(Color.White, Width / 16f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, cx - medio, cy, cx + medio, cy);
        g.DrawLine(pen, cx, cy - medio, cx, cy + medio);
    }
}

// Flecha ‹ › dibujada a mano (así no depende de la fuente ni del zoom de pantalla)
class ChevronButton : Button
{
    bool hover, down;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Derecha { get; set; }

    public ChevronButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Ui.Fondo;
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(ControlStyles.Selectable, false);
        DoubleBuffered = true;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Ui.Fondo);

        if (hover || down)
        {
            using var fondo = new SolidBrush(down ? Ui.Hover : Ui.Tarjeta);
            g.FillEllipse(fondo, 0, 0, Width - 1, Height - 1);
        }

        float cx = Width / 2f, cy = Height / 2f;
        float ancho = Width * 0.08f, alto = Width * 0.19f;
        float dir = Derecha ? -1f : 1f;

        using var pen = new Pen(Ui.Azul, Width / 14f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawLines(pen, new[]
        {
            new PointF(cx + dir * ancho, cy - alto),
            new PointF(cx - dir * ancho, cy),
            new PointF(cx + dir * ancho, cy + alto)
        });
    }
}

// ListBox sin parpadeo
class BufferedListBox : ListBox
{
    public BufferedListBox() { DoubleBuffered = true; }
}

// Colores oscuros para el menú del icono de bandeja
class ColoresOscuros : ProfessionalColorTable
{
    static readonly Color Bg = Ui.Tarjeta;
    static readonly Color Sel = Color.FromArgb(72, 72, 74);

    public override Color ToolStripDropDownBackground => Bg;
    public override Color ImageMarginGradientBegin => Bg;
    public override Color ImageMarginGradientMiddle => Bg;
    public override Color ImageMarginGradientEnd => Bg;
    public override Color MenuBorder => Ui.Separador;
    public override Color MenuItemBorder => Sel;
    public override Color MenuItemSelected => Sel;
    public override Color MenuItemSelectedGradientBegin => Sel;
    public override Color MenuItemSelectedGradientEnd => Sel;
}

// =====================================================================
//  Ventana principal + icono de bandeja
// =====================================================================
public class MainForm : Form
{
    const string ClaveInicio = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string NombreApp = "TareasBandeja";
    static readonly CultureInfo es = new("es-ES");

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool DestroyIcon(IntPtr handle);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hWnd, string appName, string? idList);

    readonly string rutaJson = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TareasBandeja", "tareas.json");

    readonly List<Tarea> tareas = new();
    List<Tarea> visibles = new();   // tareas del día que se está mostrando
    DateTime dia = DateTime.Today;  // día que se está viendo

    readonly NotifyIcon tray = new();
    readonly Label lblTitulo = new();
    readonly Label lblSub = new();
    readonly Label lblVacio = new();
    readonly ChevronButton btnPrev = new();
    readonly ChevronButton btnNext = new();
    readonly RoundedPanel tarjeta = new();
    readonly RoundedPanel entrada = new();
    readonly BufferedListBox lista = new();
    readonly TextBox txtNueva = new();
    readonly PlusButton btnAnadir = new();
    readonly System.Windows.Forms.Timer reloj = new();

    readonly Font fNormal = new("Segoe UI", 11f);
    readonly Font fHecha = new("Segoe UI", 11f, FontStyle.Strikeout);

    readonly float k;   // factor de escala según el zoom de Windows (100% = 1, 150% = 1.5...)
    int S(int v) => (int)Math.Round(v * k);
    float Sf(float v) => v * k;

    Icon? iconoActual;
    DateTime ultimoDia = DateTime.Today;
    DateTime ocultadoEn = DateTime.MinValue;
    int hover = -1;
    bool salir;
    bool primeraVez = true;

    public MainForm()
    {
        k = DeviceDpi / 96f;

        // --- Ventana: sin bordes, oscura, tipo "popover" ---
        AutoScaleMode = AutoScaleMode.None; // la escala la hacemos nosotros con S()
        Text = "Tareas";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Ui.Fondo;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        DoubleBuffered = true;

        int ancho = S(340);
        int margen = S(16);
        int hueco = S(10);
        int boton = S(40);

        // --- Cabecera ---
        lblTitulo.AutoSize = false;
        lblTitulo.Font = new Font("Segoe UI", 24f, FontStyle.Bold);
        lblTitulo.ForeColor = Color.White;
        lblTitulo.Cursor = Cursors.Hand;
        lblTitulo.Text = "Hoy";
        lblTitulo.Bounds = new Rectangle(S(20), S(8), S(220), lblTitulo.PreferredHeight);

        lblSub.AutoSize = false;
        lblSub.Font = new Font("Segoe UI", 10f);
        lblSub.ForeColor = Ui.Gris;
        lblSub.Text = "Fecha";
        lblSub.Bounds = new Rectangle(S(22), lblTitulo.Bottom - S(2), S(300), lblSub.PreferredHeight);

        btnNext.Derecha = true;
        btnNext.Bounds = new Rectangle(ancho - margen - S(32), S(14), S(32), S(32));
        btnPrev.Bounds = new Rectangle(btnNext.Left - S(36), S(14), S(32), S(32));

        int y = lblSub.Bottom + S(10);

        // --- Tarjeta con la lista ---
        tarjeta.Bounds = new Rectangle(margen, y, ancho - 2 * margen, S(300));
        tarjeta.Padding = new Padding(0, S(10), 0, S(10));
        tarjeta.Radius = S(16);
        tarjeta.BackColor = Ui.Fondo;

        lista.Dock = DockStyle.Fill;
        lista.BorderStyle = BorderStyle.None;
        lista.BackColor = Ui.Tarjeta;
        lista.DrawMode = DrawMode.OwnerDrawFixed;
        lista.ItemHeight = S(46);
        lista.SelectionMode = SelectionMode.None;
        lista.IntegralHeight = false;
        lista.HandleCreated += (s, e) => SetWindowTheme(lista.Handle, "DarkMode_Explorer", null); // scrollbar oscura

        lblVacio.Dock = DockStyle.Fill;
        lblVacio.Text = "No hay tareas";
        lblVacio.Font = new Font("Segoe UI", 10.5f);
        lblVacio.ForeColor = Ui.Gris;
        lblVacio.BackColor = Ui.Tarjeta;
        lblVacio.TextAlign = ContentAlignment.MiddleCenter;
        lblVacio.Visible = false;

        tarjeta.Controls.Add(lista);
        tarjeta.Controls.Add(lblVacio);

        y = tarjeta.Bottom + S(12);

        // --- Campo de texto en forma de píldora + botón "+" ---
        entrada.Bounds = new Rectangle(margen, y, ancho - 2 * margen - boton - hueco, boton);
        entrada.Radius = boton / 2;
        entrada.BackColor = Ui.Fondo;

        txtNueva.BorderStyle = BorderStyle.None;
        txtNueva.BackColor = Ui.Tarjeta;
        txtNueva.ForeColor = Color.White;
        txtNueva.Font = new Font("Segoe UI", 11f);
        txtNueva.PlaceholderText = "Nueva tarea";
        txtNueva.MaxLength = 100;
        txtNueva.Width = entrada.Width - S(36);
        txtNueva.Location = new Point(S(18), (entrada.Height - txtNueva.Height) / 2);
        entrada.Controls.Add(txtNueva);

        btnAnadir.Bounds = new Rectangle(entrada.Right + hueco, y, boton, boton);

        ClientSize = new Size(ancho, entrada.Bottom + margen);

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblSub, btnPrev, btnNext, tarjeta, entrada, btnAnadir
        });
        AcceptButton = btnAnadir; // Enter = añadir

        // --- Icono de bandeja y menú oscuro ---
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Renderer = new ToolStripProfessionalRenderer(new ColoresOscuros()) { RoundedEdges = false },
            BackColor = Ui.Tarjeta,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f)
        };
        menu.Items.Add("Abrir", null, (s, e) => BeginInvoke(new Action(Mostrar)));
        menu.Items.Add("Salir", null, (s, e) => { salir = true; tray.Visible = false; Application.Exit(); });
        tray.ContextMenuStrip = menu;
        tray.MouseClick += (s, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (Visible) { Hide(); return; }
            // Si acaba de ocultarse porque el clic en el icono le quitó el foco, no lo reabrimos
            if ((DateTime.UtcNow - ocultadoEn).TotalMilliseconds < 300) return;
            Mostrar();
        };

        // --- Datos ---
        Cargar();
        Refrescar();
        tray.Visible = true;

        // Arrancar automáticamente con Windows
        RegistrarInicioAutomatico();

        // Aviso al arrancar si tienes tareas pendientes hoy
        int pendientes = PendientesHoy();
        if (pendientes > 0)
            tray.ShowBalloonTip(4000, "Tareas de hoy", $"Tienes {pendientes} tarea(s) pendiente(s)", ToolTipIcon.Info);

        // --- Eventos ---
        btnPrev.Click += (s, e) => CambiarDia(-1);
        btnNext.Click += (s, e) => CambiarDia(1);
        lblTitulo.Click += (s, e) => { dia = DateTime.Today; Refrescar(); };
        btnAnadir.Click += (s, e) => Anadir();
        lista.DrawItem += DibujarItem;
        lista.MouseMove += (s, e) =>
        {
            int i = lista.IndexFromPoint(e.Location);
            if (i != hover) { hover = i; lista.Invalidate(); }
        };
        lista.MouseLeave += (s, e) => { hover = -1; lista.Invalidate(); };
        lista.MouseDown += Lista_MouseDown;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Hide(); };

        // Cada minuto comprueba si ha cambiado el día (medianoche)
        reloj.Interval = 60_000;
        reloj.Tick += (s, e) =>
        {
            if (DateTime.Today == ultimoDia) return;
            ultimoDia = DateTime.Today;
            dia = DateTime.Today;
            Refrescar();
        };
        reloj.Start();
    }

    // ---------- Comportamiento de la ventana ----------

    // La app arranca oculta (solo el icono de bandeja)
    protected override void SetVisibleCore(bool value)
    {
        if (primeraVez)
        {
            primeraVez = false;
            value = false;
            if (!IsHandleCreated) CreateHandle();
        }
        base.SetVisibleCore(value);
    }

    // Sombra para la ventana sin bordes
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
            return cp;
        }
    }

    // Esquinas redondeadas (Windows 11 nativo; en Windows 10 con una región)
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            int redondeo = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(Handle, 33, ref redondeo, sizeof(int));
        }
        else
        {
            using var path = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), S(14));
            Region = new Region(path);
        }
    }

    // Como un popover de iOS: se oculta al hacer clic fuera
    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (Visible)
        {
            Hide();
            ocultadoEn = DateTime.UtcNow;
        }
    }

    // Al cerrar no se cierra la app: se oculta
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!salir && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    void Mostrar()
    {
        dia = DateTime.Today;
        Refrescar();
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - S(12), area.Bottom - Height - S(12));
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        SetForegroundWindow(Handle);
        txtNueva.Focus();
    }

    // ---------- Dibujo de cada fila de la lista ----------
    void DibujarItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= visibles.Count) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = visibles[e.Index];
        var r = e.Bounds;

        // Fondo (con un tono más claro al pasar el ratón)
        using (var fondo = new SolidBrush(e.Index == hover ? Ui.Hover : Ui.Tarjeta))
            g.FillRectangle(fondo, r);

        // Línea separadora
        if (e.Index < visibles.Count - 1)
        {
            using var sep = new Pen(Ui.Separador, 1f);
            g.DrawLine(sep, r.Left + S(48), r.Bottom - 1, r.Right - S(12), r.Bottom - 1);
        }

        // Círculo (vacío o azul con check)
        int circ = S(22);
        var c = new Rectangle(r.Left + S(14), r.Top + (r.Height - circ) / 2, circ, circ);
        if (t.Hecha)
        {
            using var relleno = new SolidBrush(Ui.Azul);
            g.FillEllipse(relleno, c);
            using var pen = new Pen(Color.White, Sf(2f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(pen, new[]
            {
                new PointF(c.X + c.Width * 0.27f, c.Y + c.Height * 0.50f),
                new PointF(c.X + c.Width * 0.45f, c.Y + c.Height * 0.68f),
                new PointF(c.X + c.Width * 0.73f, c.Y + c.Height * 0.32f)
            });
        }
        else
        {
            using var pen = new Pen(Color.FromArgb(99, 99, 102), Sf(1.8f));
            g.DrawEllipse(pen, c.X + 1, c.Y + 1, c.Width - 2, c.Height - 2);
        }

        // Texto (tachado y gris si está hecha)
        var rectTexto = new Rectangle(r.Left + S(48), r.Top, r.Width - S(48) - S(40), r.Height);
        TextRenderer.DrawText(g, t.Titulo, t.Hecha ? fHecha : fNormal, rectTexto,
            t.Hecha ? Ui.Gris : Color.White,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        // Aspa para borrar al pasar el ratón
        if (e.Index == hover)
        {
            float cx = r.Right - S(24), cy = r.Top + r.Height / 2f, medio = Sf(5f);
            using var pen = new Pen(Ui.Gris, Sf(1.8f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, cx - medio, cy - medio, cx + medio, cy + medio);
            g.DrawLine(pen, cx - medio, cy + medio, cx + medio, cy - medio);
        }
    }

    void Lista_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        int i = lista.IndexFromPoint(e.Location);
        if (i < 0 || i >= visibles.Count) return;

        if (e.X > lista.ClientSize.Width - S(44))
        {
            // Clic en el aspa: eliminar
            tareas.Remove(visibles[i]);
            Guardar();
            Refrescar();
        }
        else
        {
            // Clic en cualquier otro sitio de la fila: marcar / desmarcar
            visibles[i].Hecha = !visibles[i].Hecha;
            Guardar();
            lista.Invalidate();
            ActualizarCabecera();
            ActualizarIcono();
        }
    }

    // ---------- Lógica de tareas ----------
    int PendientesHoy() => tareas.Count(t => t.Fecha.Date == DateTime.Today && !t.Hecha);

    void CambiarDia(int delta)
    {
        dia = dia.AddDays(delta);
        Refrescar();
    }

    void Anadir()
    {
        string titulo = txtNueva.Text.Trim();
        if (titulo == "") return;

        tareas.Add(new Tarea { Titulo = titulo, Fecha = dia.Date });
        txtNueva.Clear();
        Guardar();
        Refrescar();
    }

    void Refrescar()
    {
        hover = -1;
        lista.Items.Clear();
        visibles = tareas
            .Where(t => t.Fecha.Date == dia.Date)
            .OrderBy(t => t.Hecha)
            .ToList();
        foreach (var t in visibles)
            lista.Items.Add(t.Titulo);

        lblVacio.Visible = visibles.Count == 0;
        if (lblVacio.Visible) lblVacio.BringToFront();

        ActualizarCabecera();
        ActualizarIcono();
    }

    void ActualizarCabecera()
    {
        var hoy = DateTime.Today;
        lblTitulo.Text =
            dia.Date == hoy ? "Hoy" :
            dia.Date == hoy.AddDays(1) ? "Mañana" :
            dia.Date == hoy.AddDays(-1) ? "Ayer" :
            dia.ToString("d MMM", es);

        string fechaTxt = dia.ToString("dddd d 'de' MMMM", es);
        fechaTxt = char.ToUpper(fechaTxt[0]) + fechaTxt[1..];

        int pend = visibles.Count(t => !t.Hecha);
        lblSub.Text =
            visibles.Count == 0 ? fechaTxt :
            pend == 0 ? $"{fechaTxt} · todo hecho" :
            $"{fechaTxt} · {pend} pendiente{(pend == 1 ? "" : "s")}";
    }

    // ---------- Icono con número ----------
    void ActualizarIcono()
    {
        int n = PendientesHoy();

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            // Verde si no queda nada, rojo si hay pendientes
            using var fondo = new SolidBrush(n == 0 ? Ui.Verde : Ui.Rojo);
            g.FillEllipse(fondo, 0, 0, 31, 31);

            string texto = n > 99 ? "99+" : n.ToString();
            float tam = texto.Length == 1 ? 22 : texto.Length == 2 ? 18 : 12;
            using var fuente = new Font("Segoe UI", tam, FontStyle.Bold, GraphicsUnit.Pixel);
            using var formato = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(texto, fuente, Brushes.White, new RectangleF(0, 0, 32, 33), formato);
        }

        IntPtr handle = bmp.GetHicon();
        var nuevo = (Icon)Icon.FromHandle(handle).Clone();
        DestroyIcon(handle);

        tray.Icon = nuevo;
        iconoActual?.Dispose();
        iconoActual = nuevo;

        tray.Text = n == 0 ? "Sin tareas pendientes hoy" : $"{n} tarea(s) pendiente(s) hoy";
    }

    // ---------- Guardar / cargar ----------
    void Cargar()
    {
        try
        {
            if (!File.Exists(rutaJson)) return;
            var datos = JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(rutaJson));
            if (datos != null) tareas.AddRange(datos);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudieron cargar las tareas: " + ex.Message);
        }
    }

    void Guardar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaJson)!);
        File.WriteAllText(rutaJson, JsonSerializer.Serialize(tareas));
    }

    // ---------- Inicio automático con Windows ----------
    // Cada vez que se abre la app se registra la ruta de ese .exe para el próximo arranque
    static void RegistrarInicioAutomatico()
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey(ClaveInicio, true);
            clave?.SetValue(NombreApp, $"\"{Application.ExecutablePath}\"");
        }
        catch
        {
            // Si no se puede escribir en el registro, la app sigue funcionando igual
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tray.Dispose();
            iconoActual?.Dispose();
            reloj.Dispose();
            fNormal.Dispose();
            fHecha.Dispose();
        }
        base.Dispose(disposing);
    }
}