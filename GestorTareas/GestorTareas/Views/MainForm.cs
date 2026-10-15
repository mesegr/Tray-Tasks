using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;
using TareasBandeja.Controllers;
using TareasBandeja.Models;

namespace TareasBandeja.Views;

// =====================================================================
//  Estilo y controles personalizados (look iOS oscuro)
// =====================================================================
static class Ui
{
    public static int RadioEsquina { get; private set; } = 16;

    public static Color Fondo { get; private set; } = Color.FromArgb(28, 28, 30);
    public static Color Tarjeta { get; private set; } = Color.FromArgb(44, 44, 46);
    public static Color Hover { get; private set; } = Color.FromArgb(54, 54, 56);
    public static Color Separador { get; private set; } = Color.FromArgb(62, 62, 64);
    public static Color Gris { get; private set; } = Color.FromArgb(142, 142, 147);
    public static Color Azul { get; private set; } = Color.FromArgb(10, 132, 255);
    public static Color AzulClaro { get; private set; } = Color.FromArgb(64, 156, 255);
    public static Color Verde { get; private set; } = Color.FromArgb(48, 209, 88);
    public static Color Rojo { get; private set; } = Color.FromArgb(255, 69, 58);
    public static Color Naranja { get; private set; } = Color.FromArgb(255, 159, 10);
    public static Color Texto { get; private set; } = Color.White;

    public static void EstablecerRadioEsquina(int radio) => RadioEsquina = Math.Max(1, radio);

    public static void EstablecerTema(bool claro)
    {
        Fondo = claro ? Color.FromArgb(242, 242, 247) : Color.FromArgb(28, 28, 30);
        Tarjeta = claro ? Color.White : Color.FromArgb(44, 44, 46);
        Hover = claro ? Color.FromArgb(229, 229, 234) : Color.FromArgb(54, 54, 56);
        Separador = claro ? Color.FromArgb(209, 209, 214) : Color.FromArgb(62, 62, 64);
        Gris = claro ? Color.FromArgb(99, 99, 102) : Color.FromArgb(142, 142, 147);
        Texto = claro ? Color.FromArgb(28, 28, 30) : Color.White;
        Azul = Color.FromArgb(10, 132, 255);
        AzulClaro = Color.FromArgb(0, 122, 255);
        Verde = Color.FromArgb(48, 209, 88);
        Rojo = Color.FromArgb(255, 69, 58);
        Naranja = Color.FromArgb(255, 159, 10);
    }

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

    // Dibuja una flecha ‹ o ›
    public static void Chevron(Graphics g, float cx, float cy, float ancho, float alto, bool derecha, Color color, float grosor)
    {
        float dir = derecha ? -1f : 1f;
        using var pen = new Pen(color, grosor)
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

// Panel con fondo redondeado (las "tarjetas" de iOS)
class RoundedPanel : Panel
{
    int radius = Ui.RadioEsquina;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius
    {
        get => radius;
        set
        {
            radius = Math.Max(0, value);
            ActualizarRecorte();
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Fill { get; set; } = Ui.Tarjeta;

    public RoundedPanel() { DoubleBuffered = true; }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ActualizarRecorte();
    }

    void ActualizarRecorte()
    {
        if (Width < 2 || Height < 2) return;
        if (radius == 0)
        {
            var regionAnteriorBorrar = Region;
            Region = null;
            regionAnteriorBorrar?.Dispose();
            return;
        }
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), radius);
        var regionAnterior = Region;
        Region = new Region(path);
        regionAnterior?.Dispose();
    }

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

        Ui.Chevron(g, Width / 2f, Height / 2f, Width * 0.08f, Width * 0.19f, Derecha, Ui.Azul, Width / 14f);
    }
}

// Botón de cierre discreto para la cabecera
class CloseButton : Button
{
    bool hover, down;

    public CloseButton()
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
        g.Clear(Parent?.BackColor ?? Ui.Fondo);

        if (hover || down)
        {
            using var fondo = new SolidBrush(down ? Ui.Hover : Ui.Tarjeta);
            g.FillEllipse(fondo, 0, 0, Width - 1, Height - 1);
        }

        float cx = Width / 2f, cy = Height / 2f, medio = Width * 0.16f;
        using var pen = new Pen(Ui.Rojo, Width / 15f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLine(pen, cx - medio, cy - medio, cx + medio, cy + medio);
        g.DrawLine(pen, cx - medio, cy + medio, cx + medio, cy - medio);
    }
}

class SettingsButton : Button
{
    bool hover, down;

    public SettingsButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Ui.Fondo;
        ForeColor = Ui.Gris;
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
        g.Clear(Parent?.BackColor ?? Ui.Fondo);
        if (hover || down)
        {
            using var fondo = new SolidBrush(down ? Ui.Hover : Ui.Tarjeta);
            g.FillEllipse(fondo, 0, 0, Width - 1, Height - 1);
        }

        using var fuente = new Font("Segoe UI Symbol", 17f);
        TextRenderer.DrawText(g, "⚙", fuente, ClientRectangle, down ? Color.White : Ui.Gris,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

// "Chip" redondo: sirve de botón (fecha) o de casilla (importante)
class Chip : Control
{
    bool hover, marcado;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ConCasilla { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorActivo { get; set; } = Ui.Azul;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorInactivo { get; set; } = Ui.Tarjeta;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SoloCirculoMarcado { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SinResaltadoHover { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => marcado;
        set { marcado = value; Invalidate(); }
    }

    public Chip()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    public override Size GetPreferredSize(Size proposedSize)
    {
        if (!ConCasilla)
            return base.GetPreferredSize(proposedSize);

        int alto = Height > 0 ? Height : 32;
        int anchoTexto = TextRenderer.MeasureText(Text ?? string.Empty, Font).Width;
        return new Size(anchoTexto + alto + (int)(alto * 0.55f), alto);
    }

    protected override void OnClick(EventArgs e)
    {
        if (ConCasilla) Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Ui.Fondo);

        using (var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Ui.RadioEsquina))
        {
            var colorFondo = marcado && !SoloCirculoMarcado
                ? ColorActivo
                : hover && !SinResaltadoHover ? Ui.Hover : ColorInactivo;
            using var fondo = new SolidBrush(colorFondo);
            g.FillPath(fondo, path);
        }

        var colTexto = marcado && !SoloCirculoMarcado ? Color.White : ConCasilla ? Ui.Gris : Color.White;
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

        if (ConCasilla)
        {
            float d = Height * 0.5f;
            var c = new RectangleF(Height * 0.3f, (Height - d) / 2f, d, d);
            if (marcado)
            {
                using var fondoCirculo = new SolidBrush(SoloCirculoMarcado ? ColorActivo : Color.White);
                g.FillEllipse(fondoCirculo, c);
                using var pen = new Pen(SoloCirculoMarcado ? Color.White : ColorActivo, d / 8f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                g.DrawLines(pen, new[]
                {
                    new PointF(c.X + c.Width * 0.27f, c.Y + c.Height * 0.52f),
                    new PointF(c.X + c.Width * 0.45f, c.Y + c.Height * 0.70f),
                    new PointF(c.X + c.Width * 0.74f, c.Y + c.Height * 0.32f)
                });
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(99, 99, 102), Math.Max(1.5f, d / 10f));
                g.DrawEllipse(pen, c);
            }

            int x = (int)(c.Right + Height * 0.18f);
            var rect = new Rectangle(x, 0, Width - x - (int)(Height * 0.2f), Height);
            TextRenderer.DrawText(g, Text, Font, rect, colTexto, flags | TextFormatFlags.Left);
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), colTexto,
                flags | TextFormatFlags.HorizontalCenter);
        }
    }
}

// Calendario oscuro para elegir el día
class MiniCalendar : Control
{
    static readonly CultureInfo es = new("es-ES");

    DateTime mes = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    DateTime sel = DateTime.Today;
    int hoverDia = -1;
    int hoverNav = 0;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<DateTime, bool>? TieneTareas { get; set; }

    public event EventHandler<DateTime>? DiaElegido;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 16;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime Seleccionado
    {
        get => sel;
        set
        {
            sel = value.Date;
            mes = new DateTime(sel.Year, sel.Month, 1);
            Invalidate();
        }
    }

    public MiniCalendar() { DoubleBuffered = true; }

    // --- Geometría ---
    float CabH => Height * 0.15f;
    float SemH => Height * 0.08f;
    float Arriba => CabH + SemH;
    float CeldaW => Width / 7f;
    float CeldaH => (Height - Arriba - Height * 0.03f) / 6f;
    int Offset => ((int)mes.DayOfWeek + 6) % 7;   // la semana empieza en lunes
    int DiasMes => DateTime.DaysInMonth(mes.Year, mes.Month);

    RectangleF Celda(int dia)
    {
        int i = dia - 1 + Offset;
        return new RectangleF((i % 7) * CeldaW, Arriba + (i / 7) * CeldaH, CeldaW, CeldaH);
    }

    int DiaEn(Point p)
    {
        if (p.Y < Arriba) return -1;
        int col = (int)(p.X / CeldaW);
        int fila = (int)((p.Y - Arriba) / CeldaH);
        if (col < 0 || col > 6 || fila < 0 || fila > 5) return -1;
        int d = fila * 7 + col - Offset + 1;
        return d >= 1 && d <= DiasMes ? d : -1;
    }

    int NavEn(Point p)
    {
        if (p.Y > CabH) return 0;
        if (p.X < Width * 0.2f) return -1;
        if (p.X > Width * 0.8f) return 1;
        return 0;
    }

    // --- Dibujo ---
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Ui.Fondo);

        using (var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
        using (var fondo = new SolidBrush(Ui.Tarjeta))
            g.FillPath(fondo, path);

        using var fTit = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var fSem = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fDia = new Font("Segoe UI", 10.5f);
        var centro = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;

        // Mes y año
        string titulo = mes.ToString("MMMM yyyy", es);
        titulo = char.ToUpper(titulo[0]) + titulo[1..];
        TextRenderer.DrawText(g, titulo, fTit, new Rectangle(0, 0, Width, (int)CabH), Ui.Texto, centro);

        // Flechas de mes
        float r = CabH * 0.4f;
        foreach (int lado in new[] { -1, 1 })
        {
            float cx = lado == -1 ? Width * 0.10f : Width * 0.90f;
            float cy = CabH / 2f;
            if (hoverNav == lado)
            {
                using var bg = new SolidBrush(Ui.Hover);
                g.FillEllipse(bg, cx - r, cy - r, r * 2, r * 2);
            }
            Ui.Chevron(g, cx, cy, r * 0.22f, r * 0.5f, lado == 1, Ui.Azul, Math.Max(2f, r / 6f));
        }

        // Días de la semana
        string[] sem = { "L", "M", "X", "J", "V", "S", "D" };
        for (int i = 0; i < 7; i++)
        {
            var rs = new Rectangle((int)(i * CeldaW), (int)CabH, (int)CeldaW, (int)SemH);
            TextRenderer.DrawText(g, sem[i], fSem, rs, Ui.Gris, centro);
        }

        // Días del mes
        for (int d = 1; d <= DiasMes; d++)
        {
            var celda = Celda(d);
            var fecha = new DateTime(mes.Year, mes.Month, d);
            float diam = Math.Min(celda.Width, celda.Height) * 0.82f;
            var circ = new RectangleF(celda.X + (celda.Width - diam) / 2f, celda.Y + (celda.Height - diam) / 2f, diam, diam);

            var colTexto = Ui.Texto;
            if (fecha == sel.Date)
            {
                using var azul = new SolidBrush(Ui.Azul);
                g.FillEllipse(azul, circ);
                colTexto = Color.White;
            }
            else
            {
                if (d == hoverDia)
                {
                    using var gris = new SolidBrush(Ui.Hover);
                    g.FillEllipse(gris, circ);
                }
                if (fecha == DateTime.Today) colTexto = Ui.Azul;
            }

            TextRenderer.DrawText(g, d.ToString(), fDia, Rectangle.Round(celda), colTexto, centro);
            if (TieneTareas?.Invoke(fecha) == true)
            {
                float punto = Math.Max(3f, Math.Min(5f, celda.Height * 0.11f));
                using var marca = new SolidBrush(fecha == sel.Date ? Color.White : Ui.Naranja);
                g.FillEllipse(marca, Width / 2f - punto / 2f, celda.Bottom - punto * 1.6f, punto, punto);
            }
        }
    }

    // --- Ratón ---
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int dh = DiaEn(e.Location);
        int nh = NavEn(e.Location);
        if (dh != hoverDia || nh != hoverNav)
        {
            hoverDia = dh;
            hoverNav = nh;
            Cursor = (dh > 0 || nh != 0) ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hoverDia = -1;
        hoverNav = 0;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        int nav = NavEn(e.Location);
        if (nav != 0)
        {
            mes = mes.AddMonths(nav);
            Invalidate();
            return;
        }

        int d = DiaEn(e.Location);
        if (d > 0)
        {
            sel = new DateTime(mes.Year, mes.Month, d);
            Invalidate();
            DiaElegido?.Invoke(this, sel);
        }
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
    const int IdAtajoVentana = 7001;
    const int WM_HOTKEY = 0x0312;
    const uint MOD_CONTROL = 0x0002;
    const uint MOD_ALT = 0x0001;
    const uint MOD_SHIFT = 0x0004;
    const uint MOD_WIN = 0x0008;

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

    readonly TareaController controller = new();
    readonly Stack<Tarea> borradas = new();
    List<Tarea> visibles = new();     // lo que se muestra en la lista del día actual
    DateTime dia = DateTime.Today;    // día que se está viendo
    DateTime fechaNueva = DateTime.Today; // fecha elegida para la tarea que se va a crear

    readonly NotifyIcon tray = new();
    readonly Label lblTitulo = new();
    readonly Label lblSub = new();
    readonly Label lblVacio = new();
    readonly ChevronButton btnPrev = new();
    readonly ChevronButton btnNext = new();
    readonly SettingsButton btnAjustes = new();
    readonly CloseButton btnCerrar = new();
    readonly RoundedPanel tarjeta = new();
    readonly RoundedPanel barraFiltros = new();
    readonly RoundedPanel entrada = new();
    readonly RoundedPanel pildoraHora = new();
    readonly BufferedListBox lista = new();
    readonly TextBox txtBuscar = new();
    readonly SelectorRedondeado filtroTareas = new();
    readonly TextBox txtNueva = new();
    readonly MaskedTextBox txtHora = new();
    readonly PlusButton btnAnadir = new();
    readonly Chip chipFecha = new();
    readonly Chip chipImportante = new();
    readonly MiniCalendar calendario = new();
    readonly TareaEditPanel panelEdicion = new();
    readonly AjustesPanel panelAjustes = new();
    readonly UndoSnackbar snackbar = new();
    readonly System.Windows.Forms.Timer reloj = new();
    readonly System.Windows.Forms.Timer animacionFilas = new();
    readonly Dictionary<Guid, int> opacidadesFilas = new();

    readonly Font fNormal = new("Segoe UI", 11f);
    readonly Font fSub = new("Segoe UI", 8.5f);

    readonly float k;   // factor de escala según el zoom de Windows (100% = 1, 150% = 1.5...)
    int S(int v) => (int)Math.Round(v * k);
    float Sf(float v) => v * k;

    Icon? iconoActual;
    DateTime ultimoDia = DateTime.Today;
    int hover = -1;
    int avisoPrevioMinutos = 60;
    int duracionNotificacion = 2000;
    int alturaVentanaNormal;
    bool sonidoHabilitado = true;
    bool inicioAutomaticoHabilitado = true;
    bool temaClaroHabilitado;
    bool mostrarVentanaAlIniciar;
    int ubicacionNotificaciones;
    readonly bool iniciadoPorWindows;
    bool salir;
    bool primeraVez = true;
    readonly HashSet<(Guid Id, DateTime Fecha, int MinutosAntes)> recordatoriosMostrados = new();
    ToolStripMenuItem? itemDeshacerBorrado;
    Tarea? tareaEnEdicion;
    Tarea? tareaContextual;
    ConfiguracionAtajo configuracionAtajo;

    public MainForm(bool iniciadoPorWindows = false)
    {
        this.iniciadoPorWindows = iniciadoPorWindows;
        k = DeviceDpi / 96f;
        Ui.EstablecerRadioEsquina(S(16));
        configuracionAtajo = CargarConfiguracionAtajo();

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
        btnAjustes.Text = "⚙";
        btnAjustes.Font = new Font("Segoe UI Symbol", 13f);
        btnAjustes.ForeColor = Ui.Gris;
        btnAjustes.BackColor = Ui.Fondo;
        btnAjustes.FlatStyle = FlatStyle.Flat;
        btnAjustes.FlatAppearance.BorderSize = 0;
        btnAjustes.Cursor = Cursors.Hand;
        btnAjustes.TabStop = false;
        btnAjustes.Bounds = new Rectangle(ancho - margen - S(68), S(14), S(32), S(32));
        btnCerrar.Bounds = new Rectangle(ancho - margen - S(32), S(14), S(32), S(32));
        btnNext.Bounds = new Rectangle(btnAjustes.Left - S(36), S(14), S(32), S(32));
        btnPrev.Bounds = new Rectangle(btnNext.Left - S(48), S(14), S(32), S(32));

        int y = lblSub.Bottom + S(8);

        barraFiltros.Bounds = new Rectangle(margen, y, ancho - 2 * margen, S(34));
        barraFiltros.Radius = Ui.RadioEsquina;
        barraFiltros.Fill = Ui.Tarjeta;
        barraFiltros.BackColor = Ui.Fondo;
        txtBuscar.Bounds = new Rectangle(S(12), 0, S(132), S(34));
        txtBuscar.BorderStyle = BorderStyle.None;
        txtBuscar.BackColor = Ui.Tarjeta;
        txtBuscar.ForeColor = Color.White;
        txtBuscar.Font = new Font("Segoe UI", 9.5f);
        txtBuscar.PlaceholderText = "Buscar tareas";
        txtBuscar.Location = new Point(S(12), (barraFiltros.Height - txtBuscar.PreferredHeight) / 2);
        filtroTareas.Bounds = new Rectangle(S(150), 0, Math.Max(S(120), barraFiltros.Width - S(162)), S(34));
        filtroTareas.BackColor = Ui.Tarjeta;
        filtroTareas.ForeColor = Color.White;
        filtroTareas.Font = new Font("Segoe UI", 11f);
        filtroTareas.Items.AddRange(new object[] { "Todas", "Pendientes", "Hechas", "Importantes", "Vencidas" });
        filtroTareas.SelectedIndex = 0;
        barraFiltros.Controls.AddRange(new Control[] { txtBuscar, filtroTareas });

        y = barraFiltros.Bottom + S(8);

        // --- Tarjeta con la lista ---
        tarjeta.Bounds = new Rectangle(margen, y, ancho - 2 * margen, S(280));
        tarjeta.Padding = new Padding(0, S(10), 0, S(10));
        tarjeta.Radius = Ui.RadioEsquina;
        tarjeta.BackColor = Ui.Fondo;

        lista.Dock = DockStyle.Fill;
        lista.BorderStyle = BorderStyle.None;
        lista.BackColor = Ui.Tarjeta;
        lista.DrawMode = DrawMode.OwnerDrawFixed;
        lista.ItemHeight = S(52);
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
        entrada.Radius = Ui.RadioEsquina;
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

        y = entrada.Bottom + S(10);

        // --- Fila de opciones: fecha, hora e importante ---
        int altoChip = S(32);
        int separacion = S(8);

        chipFecha.Font = new Font("Segoe UI", 9.5f);
        chipFecha.Bounds = new Rectangle(margen, y, S(108), altoChip);

        pildoraHora.Bounds = new Rectangle(chipFecha.Right + separacion, y, S(70), altoChip);
        pildoraHora.Radius = Ui.RadioEsquina;
        pildoraHora.BackColor = Ui.Fondo;

        txtHora.Mask = "00:00";
        txtHora.PromptChar = '-';
        txtHora.HidePromptOnLeave = false;
        txtHora.BorderStyle = BorderStyle.None;
        txtHora.BackColor = Ui.Tarjeta;
        txtHora.ForeColor = Color.White;
        txtHora.Font = new Font("Segoe UI", 10f);
        txtHora.TextAlign = HorizontalAlignment.Center;
        txtHora.Width = S(46);
        txtHora.Location = new Point((pildoraHora.Width - txtHora.Width) / 2, (pildoraHora.Height - txtHora.Height) / 2);
        pildoraHora.Controls.Add(txtHora);

        chipImportante.Font = new Font("Segoe UI", 9.5f);
        chipImportante.Text = "Importante";
        chipImportante.ConCasilla = true;
        chipImportante.ColorActivo = Ui.Naranja;
        int xImp = pildoraHora.Right + separacion;
        chipImportante.Bounds = new Rectangle(xImp, y, ancho - margen - xImp, altoChip);

        ClientSize = new Size(ancho, chipFecha.Bottom + margen);
        alturaVentanaNormal = ClientSize.Height;

        // --- Calendario (aparece encima de la lista al pulsar la fecha) ---
        calendario.Bounds = tarjeta.Bounds;
        calendario.Radius = Ui.RadioEsquina;
        calendario.Font = new Font("Segoe UI", 10f);
        calendario.TieneTareas = controller.TieneTareas;
        calendario.Visible = false;

        panelEdicion.Bounds = new Rectangle(tarjeta.Left, tarjeta.Top, tarjeta.Width, S(304));
        panelEdicion.Radius = Ui.RadioEsquina;
        panelEdicion.Visible = false;
        panelEdicion.Guardado += (_, tareaEditada) =>
        {
            if (tareaEnEdicion == null) return;
            controller.Actualizar(tareaEnEdicion, tareaEditada);
            tareaEnEdicion = null;
            panelEdicion.Visible = false;
            Refrescar();
            AnimarTarea(tareaEditada);
        };
        panelEdicion.Cancelado += (_, _) =>
        {
            tareaEnEdicion = null;
            panelEdicion.Visible = false;
        };

        panelAjustes.Bounds = new Rectangle(tarjeta.Left, tarjeta.Top, tarjeta.Width,
            Math.Max(S(320), alturaVentanaNormal - tarjeta.Top - margen));
        panelAjustes.Radius = Ui.RadioEsquina;
        panelAjustes.RecolocarBotones();
        panelAjustes.Visible = false;
        panelAjustes.Editar(configuracionAtajo);
        CargarPreferencias();
        panelAjustes.Guardado += (_, nuevoAtajo) =>
        {
            if (!AplicarAtajo(nuevoAtajo))
            {
                MessageBox.Show(this, "Esa combinación ya está siendo utilizada por otra aplicación.",
                    "Atajo no disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            configuracionAtajo = nuevoAtajo;
            GuardarConfiguracionAtajo(nuevoAtajo);
            avisoPrevioMinutos = panelAjustes.MinutosAviso;
            duracionNotificacion = panelAjustes.DuracionNotificacion;
            sonidoHabilitado = panelAjustes.SonidoHabilitado;
            inicioAutomaticoHabilitado = panelAjustes.InicioAutomaticoHabilitado;
            temaClaroHabilitado = panelAjustes.TemaClaroHabilitado;
            mostrarVentanaAlIniciar = panelAjustes.MostrarAlIniciarHabilitado;
            ubicacionNotificaciones = panelAjustes.UbicacionNotificacion;
            Notificador.DuracionAviso = duracionNotificacion;
            Notificador.Posicion = ubicacionNotificaciones;
            GuardarPreferencias();
            EstablecerInicioAutomatico(inicioAutomaticoHabilitado);
            AplicarTema(temaClaroHabilitado);
            MostrarAjustes(false);
        };
        panelAjustes.Cancelado += (_, _) => MostrarAjustes(false);

        snackbar.Bounds = new Rectangle(S(12), ClientSize.Height - S(52), ClientSize.Width - S(24), S(42));
        snackbar.DeshacerSolicitado += (_, _) => DeshacerBorrado();

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblSub, btnPrev, btnNext, btnAjustes, btnCerrar, barraFiltros, tarjeta, entrada, btnAnadir,
            chipFecha, pildoraHora, chipImportante, calendario, panelEdicion, panelAjustes, snackbar
        });
        btnPrev.BringToFront();
        btnNext.BringToFront();
        btnAjustes.BringToFront();
        btnCerrar.BringToFront();
        AplicarTema(temaClaroHabilitado);
        AcceptButton = btnAnadir; // Enter = añadir
        Shown += (_, _) =>
        {
            if (!this.iniciadoPorWindows || !mostrarVentanaAlIniciar) return;
            var area = Screen.PrimaryScreen!.WorkingArea;
            Location = new Point(area.Right - Width - S(12), area.Bottom - Height - S(12));
        };

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
        menu.Items.Add("Exportar copia…", null, (s, e) => ExportarCopia());
        menu.Items.Add("Restaurar copia…", null, (s, e) => RestaurarCopia());
        menu.Items.Add("Acerca de Tareas", null, (s, e) => MessageBox.Show(this,
            "Tareas\nRecordatorios sencillos desde la bandeja de Windows.", "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information));
        menu.Items.Add(new ToolStripSeparator());
        itemDeshacerBorrado = new ToolStripMenuItem("Deshacer borrado");
        menu.Items.Add(itemDeshacerBorrado);
        itemDeshacerBorrado.Enabled = false;
        itemDeshacerBorrado.Click += (s, e) => DeshacerBorrado();
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (s, e) => { salir = true; tray.Visible = false; Application.Exit(); });
        tray.ContextMenuStrip = menu;
        // Clic en el icono: abre el panel; otro clic en el icono: lo cierra
        tray.MouseClick += (s, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (Visible) Hide(); else Mostrar();
        };

        // --- Datos ---
        try
        {
            controller.Cargar();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudieron cargar las tareas: " + ex.Message);
        }
        ActualizarChipFecha();
        Refrescar();
        tray.Visible = true;

        // Arrancar automáticamente con Windows si la opción sigue activada.
        EstablecerInicioAutomatico(inicioAutomaticoHabilitado);

        // Aviso al arrancar si tienes tareas pendientes hoy
        int pendientes = PendientesHoy();
        if (pendientes > 0)
            Notificador.Mostrar("Tareas de hoy", $"Tienes {pendientes} tarea(s) pendiente(s)", sonidoHabilitado, duracionNotificacion);

        // --- Eventos ---
        btnPrev.Click += (s, e) => IrADia(dia.AddDays(-1));
        btnNext.Click += (s, e) => IrADia(dia.AddDays(1));
        btnAjustes.Click += (s, e) =>
        {
            if (panelAjustes.Visible)
            {
                MostrarAjustes(false);
                return;
            }

            calendario.Visible = false;
            panelEdicion.Visible = false;
            panelAjustes.Editar(configuracionAtajo);
            panelAjustes.CargarPreferencias(avisoPrevioMinutos, duracionNotificacion, sonidoHabilitado,
                inicioAutomaticoHabilitado, temaClaroHabilitado, ubicacionNotificaciones, mostrarVentanaAlIniciar);
            MostrarAjustes(true);
        };
        btnCerrar.Click += (s, e) => Hide();
        lblTitulo.Click += (s, e) => IrADia(DateTime.Today);
        btnAnadir.Click += (s, e) => Anadir();
        chipFecha.Click += (s, e) => AlternarCalendario();
        calendario.DiaElegido += (s, d) =>
        {
            fechaNueva = d.Date;
            ActualizarChipFecha();
            calendario.Visible = false;
            txtNueva.Focus();
        };
        txtHora.TextChanged += (s, e) => txtHora.ForeColor = Color.White;

        lista.DrawItem += DibujarItem;
        txtBuscar.TextChanged += (_, _) => Refrescar();
        filtroTareas.SelectedIndexChanged += (_, _) => Refrescar();
        lista.MouseMove += (s, e) =>
        {
            int i = lista.IndexFromPoint(e.Location);
            if (i != hover) { hover = i; lista.Invalidate(); }
        };
        lista.MouseLeave += (s, e) => { hover = -1; lista.Invalidate(); };
        lista.MouseDown += Lista_MouseDown;
        lista.DoubleClick += Lista_DoubleClick;
        ConfigurarMenuTareas();
        animacionFilas.Interval = 16;
        animacionFilas.Tick += (_, _) =>
        {
            foreach (var id in opacidadesFilas.Keys.ToList())
            {
                opacidadesFilas[id] = Math.Min(255, opacidadesFilas[id] + 34);
                if (opacidadesFilas[id] >= 255) opacidadesFilas.Remove(id);
            }
            lista.Invalidate();
            if (opacidadesFilas.Count == 0) animacionFilas.Stop();
        };

        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (calendario.Visible) calendario.Visible = false;
                else if (panelAjustes.Visible) MostrarAjustes(false);
                else if (panelEdicion.Visible) panelEdicion.Visible = false;
                else if (Visible) Hide();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.F) { txtBuscar.Focus(); txtBuscar.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.N) { txtNueva.Focus(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Z) { DeshacerBorrado(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Left) { IrADia(dia.AddDays(-1)); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Right) { IrADia(dia.AddDays(1)); e.Handled = true; }
            else if (e.KeyCode == Keys.F2 && hover >= 0 && hover < visibles.Count) { AbrirEdicion(visibles[hover]); e.Handled = true; }
            else if (e.KeyCode == Keys.Space && lista.Focused && hover >= 0 && hover < visibles.Count)
            {
                var tarea = visibles[hover];
                controller.MarcarHecha(tarea, dia, !tarea.Hecha);
                Refrescar();
                AnimarTarea(tarea);
                e.Handled = true;
            }
        };

        // Cada minuto comprueba si ha cambiado el día (medianoche)
        reloj.Interval = 60_000;
        reloj.Tick += (s, e) =>
        {
            if (DateTime.Today != ultimoDia)
            {
                ultimoDia = DateTime.Today;
                IrADia(DateTime.Today);
            }

            ComprobarRecordatorios();
        };
        ComprobarRecordatorios(alIniciar: true);
        reloj.Start();
    }

    // ---------- Comportamiento de la ventana ----------

    // La app arranca oculta (solo el icono de bandeja)
    protected override void SetVisibleCore(bool value)
    {
        if (primeraVez)
        {
            primeraVez = false;
            value = value && iniciadoPorWindows && mostrarVentanaAlIniciar;
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
        RegisterHotKey(Handle, IdAtajoVentana, configuracionAtajo.Modificadores, (uint)configuracionAtajo.Tecla);
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            int redondeo = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(Handle, 33, ref redondeo, sizeof(int));
        }
        else
        {
            using var path = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina);
            Region = new Region(path);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (IsHandleCreated && Environment.OSVersion.Version.Build < 22000)
        {
            using var path = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina);
            Region = new Region(path);
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (IsHandleCreated)
            UnregisterHotKey(Handle, IdAtajoVentana);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == IdAtajoVentana)
        {
            if (Visible) Hide();
            else Mostrar();
            return;
        }

        base.WndProc(ref m);
    }

    bool AplicarAtajo(ConfiguracionAtajo nuevo)
    {
        UnregisterHotKey(Handle, IdAtajoVentana);
        if (RegisterHotKey(Handle, IdAtajoVentana, nuevo.Modificadores, (uint)nuevo.Tecla))
            return true;

        RegisterHotKey(Handle, IdAtajoVentana, configuracionAtajo.Modificadores, (uint)configuracionAtajo.Tecla);
        return false;
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
        calendario.Visible = false;
        IrADia(DateTime.Today);
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - S(12), area.Bottom - Height - S(12));
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        SetForegroundWindow(Handle);
        txtNueva.Focus();
    }

    void MostrarAjustes(bool mostrar)
    {
        panelAjustes.Visible = mostrar;
        snackbar.Bounds = new Rectangle(S(12), ClientSize.Height - S(52), ClientSize.Width - S(24), S(42));
        if (Visible)
        {
            var area = Screen.FromControl(this).WorkingArea;
            Location = new Point(Math.Min(Location.X, area.Right - Width - S(12)), area.Bottom - Height - S(12));
        }
        if (mostrar) panelAjustes.BringToFront();
    }

    // ---------- Textos de fechas ----------
    static string FechaTxt(DateTime f)
    {
        var hoy = DateTime.Today;
        if (f.Date == hoy) return "hoy";
        if (f.Date == hoy.AddDays(1)) return "mañana";
        if (f.Date == hoy.AddDays(-1)) return "ayer";
        return f.ToString("d MMM yyyy", es).Replace(".", "");
    }

    void ActualizarChipFecha()
    {
        chipFecha.Text = fechaNueva.ToString("d MMM yyyy", es).Replace(".", "");
    }

    void AlternarCalendario()
    {
        if (calendario.Visible)
        {
            calendario.Visible = false;
            return;
        }
        calendario.Seleccionado = fechaNueva;
        calendario.Visible = true;
        calendario.BringToFront();
    }

    // ---------- Dibujo de cada fila de la lista ----------
    bool EsDestacada(Tarea t) => TareaController.EsDestacada(t, dia);

    // Texto pequeño bajo el título (hora o entrega) y su color
    (string? texto, Color color) Subtitulo(Tarea t, bool destacada)
    {
        string hora = t.ConHora ? " · " + t.Fecha.ToString("HH:mm") : "";

        if (destacada)
        {
            bool vencida = t.ConHora ? t.Fecha < DateTime.Now : t.Fecha.Date < DateTime.Today;
            return vencida
                ? ($"Vencida · {FechaTxt(t.Fecha)}{hora}", Ui.Rojo)
                : ($"Entrega {FechaTxt(t.Fecha)}{hora}", Ui.Naranja);
        }
        if (t.Importante) return ($"Entrega {FechaTxt(t.Fecha)}{hora}", Ui.Gris);
        if (t.ConHora) return (t.Fecha.ToString("HH:mm"), Ui.Gris);
        return (null, Ui.Gris);
    }

    void DibujarItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= visibles.Count) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = visibles[e.Index];
        var r = e.Bounds;
        bool destacada = EsDestacada(t);

        // Fondo (con un tono más claro al pasar el ratón)
        using (var fondo = new SolidBrush(e.Index == hover ? Ui.Hover : Ui.Tarjeta))
            g.FillRectangle(fondo, r);

        // Línea separadora
        if (e.Index < visibles.Count - 1)
        {
            using var sep = new Pen(Ui.Separador, 1f);
            g.DrawLine(sep, r.Left + S(40), r.Bottom - 1, r.Right - S(12), r.Bottom - 1);
        }

        // Indicador circular a juego con las casillas de Ajustes.
        int lado = S(19);
        var p = new Rectangle(r.Left + S(12), r.Top + (r.Height - lado) / 2, lado, lado);
        if (t.Hecha)
        {
            using var fondoPunto = new SolidBrush(Ui.Azul);
            g.FillEllipse(fondoPunto, p);
            using var tick = new Pen(Color.White, Sf(1.8f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(tick, new[]
            {
                new PointF(p.Left + lado * 0.24f, p.Top + lado * 0.53f),
                new PointF(p.Left + lado * 0.43f, p.Top + lado * 0.70f),
                new PointF(p.Left + lado * 0.76f, p.Top + lado * 0.32f)
            });
        }
        else
        {
            using var fondoPunto = new SolidBrush(Ui.Hover);
            using var bordePunto = new Pen(t.Importante ? Ui.Naranja : Ui.Separador, Sf(1.4f));
            g.FillEllipse(fondoPunto, p);
            g.DrawEllipse(bordePunto, p);
        }

        // Título y, si hay, el texto pequeño de debajo
        int xTexto = r.Left + S(40);
        int anchoTexto = r.Width - S(40) - S(40);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        var (sub, colorSub) = Subtitulo(t, destacada);
        int opacidad = opacidadesFilas.GetValueOrDefault(t.Id, 255);
        Color colorTitulo = Mezclar(Ui.Tarjeta, t.Hecha ? Ui.Gris : Ui.Texto, opacidad);
        if (sub == null)
        {
            TextRenderer.DrawText(g, t.Titulo, fNormal, new Rectangle(xTexto, r.Top, anchoTexto, r.Height), colorTitulo, flags);
        }
        else
        {
            TextRenderer.DrawText(g, t.Titulo, fNormal, new Rectangle(xTexto, r.Top + S(6), anchoTexto, S(24)), colorTitulo, flags);
            TextRenderer.DrawText(g, sub, fSub, new Rectangle(xTexto, r.Top + S(28), anchoTexto, S(18)), colorSub, flags);
        }
        if (t.Hecha)
        {
            var tituloTamano = TextRenderer.MeasureText(t.Titulo, fNormal, new Size(anchoTexto, S(24)), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            float yTachado = sub == null ? r.Top + r.Height / 2f : r.Top + S(18);
            using var tachado = new Pen(Ui.Gris, Math.Max(1f, Sf(1f)));
            g.DrawLine(tachado, xTexto, yTachado, xTexto + Math.Min(anchoTexto, tituloTamano.Width), yTachado);
        }

        // Aspa para borrar: siempre visible en gris y roja al pasar el ratón por la fila
        {
            float cx = r.Right - S(24), cy = r.Top + r.Height / 2f, medio = Sf(5f);
            using var pen = new Pen(e.Index == hover ? Ui.Rojo : Color.FromArgb(99, 99, 102), Sf(1.8f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLine(pen, cx - medio, cy - medio, cx + medio, cy + medio);
            g.DrawLine(pen, cx - medio, cy + medio, cx + medio, cy - medio);
        }
    }

    void Lista_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        int i = lista.IndexFromPoint(e.Location);
        if (i < 0 || i >= visibles.Count) return;

        if (e.X <= S(34))
        {
            var tarea = visibles[i];
            controller.MarcarHecha(tarea, dia, !tarea.Hecha);
            Refrescar();
            AnimarTarea(tarea);
            return;
        }

        // El aspa sigue siendo el acceso rápido para borrar.
        if (e.X <= lista.ClientSize.Width - S(44)) return;

        var eliminada = visibles[i];
        controller.Eliminar(eliminada);
        borradas.Push(eliminada);
        if (itemDeshacerBorrado != null) itemDeshacerBorrado.Enabled = true;
        Refrescar();
        snackbar.Mostrar(eliminada.Titulo);
    }

    void Lista_DoubleClick(object? sender, EventArgs e)
    {
        int i = lista.IndexFromPoint(lista.PointToClient(Cursor.Position));
        if (i < 0 || i >= visibles.Count) return;

        var punto = lista.PointToClient(Cursor.Position);
        if (punto.X <= S(34) || punto.X > lista.ClientSize.Width - S(44)) return;

        AbrirEdicion(visibles[i]);
    }

    void AbrirEdicion(Tarea tarea)
    {
        tareaEnEdicion = tarea;
        panelEdicion.Editar(tareaEnEdicion);
        panelEdicion.Visible = true;
        panelEdicion.BringToFront();
    }

    void ConfigurarMenuTareas()
    {
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Renderer = new ToolStripProfessionalRenderer(new ColoresOscuros()) { RoundedEdges = false },
            BackColor = Ui.Tarjeta,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f)
        };
        menu.Items.Add("Marcar como hecha / pendiente", null, (_, _) =>
        {
            if (tareaContextual == null) return;
            controller.MarcarHecha(tareaContextual, dia, !tareaContextual.Hecha);
            Refrescar();
            AnimarTarea(tareaContextual);
        });
        menu.Items.Add("Editar tarea", null, (_, _) => { if (tareaContextual != null) AbrirEdicion(tareaContextual); });
        menu.Items.Add("Eliminar tarea", null, (_, _) =>
        {
            if (tareaContextual == null) return;
            controller.Eliminar(tareaContextual);
            borradas.Push(tareaContextual);
            if (itemDeshacerBorrado != null) itemDeshacerBorrado.Enabled = true;
            snackbar.Mostrar(tareaContextual.Titulo);
            Refrescar();
        });
        menu.Opening += (_, e) => e.Cancel = tareaContextual == null;
        lista.ContextMenuStrip = menu;
        lista.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            int i = lista.IndexFromPoint(e.Location);
            tareaContextual = i >= 0 && i < visibles.Count ? visibles[i] : null;
            if (tareaContextual == null) menu.Close();
        };
    }

    void DeshacerBorrado()
    {
        if (borradas.Count == 0) return;

        controller.Anadir(borradas.Pop());
        if (itemDeshacerBorrado != null)
            itemDeshacerBorrado.Enabled = borradas.Count > 0;
        Refrescar();
    }

    void ExportarCopia()
    {
        using var dialogo = new SaveFileDialog
        {
            Title = "Guardar copia de seguridad",
            Filter = "Copia de tareas (*.json)|*.json",
            FileName = $"tareas-{DateTime.Now:yyyy-MM-dd}.json",
            DefaultExt = "json",
            AddExtension = true
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialogo.FileName, JsonSerializer.Serialize(controller.Tareas, new JsonSerializerOptions { WriteIndented = true }));
    }

    void RestaurarCopia()
    {
        using var dialogo = new OpenFileDialog
        {
            Title = "Restaurar copia de seguridad",
            Filter = "Copia de tareas (*.json)|*.json|Todos los archivos (*.*)|*.*"
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(this, "La restauración reemplazará la lista actual de tareas. ¿Continuar?", "Restaurar copia",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            controller.ReemplazarDesdeArchivo(dialogo.FileName);
            Refrescar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo restaurar la copia: " + ex.Message, "Error al restaurar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---------- Lógica de tareas ----------

    // Una tarea importante sale TODOS los días (desde que se creó) hasta que se elimine
    // Lista de un día: primero las importantes (por fecha de entrega) y después las del día
    List<Tarea> TareasDelDia(DateTime d)
        => controller.ObtenerParaDia(d);

    int PendientesHoy() => controller.PendientesHoy();

    void IrADia(DateTime d)
    {
        dia = d.Date;
        fechaNueva = dia;
        ActualizarChipFecha();
        Refrescar();
    }

    void Anadir()
    {
        string titulo = txtNueva.Text.Trim();
        if (titulo == "") return;

        // Fecha elegida + hora (si se ha escrito)
        var fecha = fechaNueva.Date;
        bool conHora = false;

        if (txtHora.Text.Any(char.IsDigit))
        {
            if (txtHora.MaskCompleted &&
                TimeOnly.TryParseExact(txtHora.Text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            {
                fecha = fecha.Add(hora.ToTimeSpan());
                conHora = true;
            }
            else
            {
                txtHora.ForeColor = Ui.Rojo; // hora incompleta o inválida
                txtHora.Focus();
                return;
            }
        }

        var nueva = new Tarea
        {
            Titulo = titulo,
            Fecha = fecha,
            ConHora = conHora,
            Importante = chipImportante.Checked,
            Creada = DateTime.Today
        };
        controller.Anadir(nueva);

        txtNueva.Clear();
        txtHora.Clear();
        chipImportante.Checked = false;
        // Si la tarea no se ve en el día actual, saltamos al día de la tarea para que se vea dónde ha caído
        if (!TareasDelDia(dia).Contains(nueva))
            IrADia(fecha.Date);
        else
            Refrescar();
        AnimarTarea(nueva);
    }

    void Refrescar()
    {
        hover = -1;
        lista.Items.Clear();
        IEnumerable<Tarea> resultado = TareasDelDia(dia);
        string consulta = txtBuscar.Text.Trim();
        if (consulta.Length > 0)
            resultado = resultado.Where(t => t.Titulo.Contains(consulta, StringComparison.CurrentCultureIgnoreCase));
        resultado = (filtroTareas.SelectedIndex) switch
        {
            1 => resultado.Where(t => !t.Hecha),
            2 => resultado.Where(t => t.Hecha),
            3 => resultado.Where(t => t.Importante),
            4 => resultado.Where(t => !t.Hecha && (t.ConHora ? t.Fecha < DateTime.Now : t.Fecha.Date < dia.Date)),
            _ => resultado
        };
        visibles = resultado.ToList();
        foreach (var t in visibles)
            lista.Items.Add(t.Titulo);

        lblVacio.Text = txtBuscar.TextLength > 0 || filtroTareas.SelectedIndex > 0
            ? "No hay resultados"
            : "No hay tareas";
        lblVacio.Visible = visibles.Count == 0;
        if (lblVacio.Visible) lblVacio.BringToFront();

        ActualizarCabecera();
        ActualizarIcono();
    }

    void AnimarTarea(Tarea tarea)
    {
        if (!visibles.Any(t => t.Id == tarea.Id)) return;
        opacidadesFilas[tarea.Id] = 0;
        if (!animacionFilas.Enabled) animacionFilas.Start();
    }

    static Color Mezclar(Color desde, Color hasta, int opacidad)
    {
        float p = Math.Clamp(opacidad / 255f, 0f, 1f);
        return Color.FromArgb(
            (int)(desde.R + (hasta.R - desde.R) * p),
            (int)(desde.G + (hasta.G - desde.G) * p),
            (int)(desde.B + (hasta.B - desde.B) * p));
    }

    void ActualizarCabecera()
    {
        var hoy = DateTime.Today;
        lblTitulo.Text =
            dia.Date == hoy ? "Hoy" :
            dia.Date == hoy.AddDays(1) ? "Mañana" :
            dia.Date == hoy.AddDays(-1) ? "Ayer" :
            dia.ToString("d MMM", es).Replace(".", "");

        string fechaTxt = dia.ToString("dddd d 'de' MMMM", es);
        fechaTxt = char.ToUpper(fechaTxt[0]) + fechaTxt[1..];

        int pendientes = visibles.Count(t => !t.Hecha);
        int total = visibles.Count;
        lblSub.Text = total == 0 ? fechaTxt : pendientes == total
            ? $"{fechaTxt} · {total} tarea{(total == 1 ? "" : "s")}"
            : $"{fechaTxt} · {pendientes} pendientes · {total} tareas";
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

            using (var fondo = new SolidBrush(Ui.Azul))
            using (var forma = Ui.RoundedRect(new Rectangle(1, 1, 30, 30), 8))
                g.FillPath(fondo, forma);
            using (var tick = new Pen(Color.White, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLines(tick, new[] { new Point(6, 16), new Point(12, 22), new Point(24, 9) });

            if (n > 0)
            {
                using var fondoNumero = new SolidBrush(Ui.Rojo);
                g.FillEllipse(fondoNumero, 15, 0, 17, 17);
                string texto = n > 9 ? "9+" : n.ToString();
                using var fuente = new Font("Segoe UI", texto.Length == 1 ? 10f : 7.5f, FontStyle.Bold, GraphicsUnit.Pixel);
                using var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(texto, fuente, Brushes.White, new RectangleF(15, 0, 17, 17), formato);
            }
        }

        IntPtr handle = bmp.GetHicon();
        var nuevo = (Icon)Icon.FromHandle(handle).Clone();
        DestroyIcon(handle);

        tray.Icon = nuevo;
        iconoActual?.Dispose();
        iconoActual = nuevo;

        tray.Text = n == 0 ? "Sin tareas pendientes hoy" : $"{n} tarea(s) pendiente(s) hoy";
    }

    // Las tareas sin hora no tienen un instante concreto para calcular un aviso.
    // Se comprueba cada minuto y cada aviso se muestra como máximo una vez por tarea.
    void ComprobarRecordatorios(bool alIniciar = false)
    {
        DateTime ahora = DateTime.Now;

        var ocurrenciasHoy = controller.ObtenerParaDia(ahora.Date)
            .Concat(controller.ObtenerParaDia(ahora.Date.AddDays(1)))
            .Where(t => t.ConHora && !t.Hecha)
            .DistinctBy(t => (t.Id, t.Fecha));

        foreach (var tarea in ocurrenciasHoy)
        {
            if (alIniciar)
            {
                // Si la aplicación se inicia tarde, no se pierde el aviso:
                // se muestra el recordatorio que todavía sea relevante.
                TimeSpan tiempoRestante = tarea.Fecha - ahora;
                if (tiempoRestante > TimeSpan.Zero && tiempoRestante <= TimeSpan.FromMinutes(15))
                    MostrarRecordatorio(tarea, 15);
                else if (tiempoRestante > TimeSpan.FromMinutes(15) && tiempoRestante <= TimeSpan.FromMinutes(avisoPrevioMinutos))
                    MostrarRecordatorio(tarea, avisoPrevioMinutos);

                continue;
            }

            MostrarRecordatorioSiCorresponde(tarea, ahora, avisoPrevioMinutos);
            MostrarRecordatorioSiCorresponde(tarea, ahora, 15);
        }
    }

    void MostrarRecordatorioSiCorresponde(Tarea tarea, DateTime ahora, int minutosAntes)
    {
        DateTime momentoAviso = tarea.Fecha.AddMinutes(-minutosAntes);
        bool dentroDeLaVentana = ahora >= momentoAviso && ahora < momentoAviso.AddMinutes(2);
        var clave = (tarea.Id, tarea.Fecha, minutosAntes);

        if (!dentroDeLaVentana || !recordatoriosMostrados.Add(clave)) return;

        MostrarRecordatorio(tarea, minutosAntes);
    }

    void MostrarRecordatorio(Tarea tarea, int minutosAntes)
    {
        recordatoriosMostrados.Add((tarea.Id, tarea.Fecha, minutosAntes));

        string texto = minutosAntes == 60
            ? $"Falta menos de 1 hora para: {tarea.Titulo}"
            : $"Faltan menos de {minutosAntes} minutos para: {tarea.Titulo}";

        Notificador.Mostrar("Recordatorio de tarea", texto, sonidoHabilitado, duracionNotificacion,
            () => BeginInvoke(new Action(() =>
            {
                Mostrar();
                IrADia(tarea.Fecha.Date);
            })));
    }

    // ---------- Inicio automático con Windows ----------
    // Cada vez que se abre la app se registra la ruta de ese .exe para el próximo arranque
    static void EstablecerInicioAutomatico(bool habilitado)
    {
        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(ClaveInicio);
            if (habilitado)
                clave?.SetValue(NombreApp, $"\"{Application.ExecutablePath}\" --inicio-windows");
            else
                clave?.DeleteValue(NombreApp, false);
        }
        catch
        {
            // Si no se puede escribir en el registro, la app sigue funcionando igual
        }
    }

    void CargarPreferencias()
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey(@"Software\TareasBandeja");
            avisoPrevioMinutos = Convert.ToInt32(clave?.GetValue("AvisoPrevio") ?? 60) <= 30 ? 30 : 60;
            int duracion = Convert.ToInt32(clave?.GetValue("DuracionNotificacion") ?? 2000);
            duracionNotificacion = duracion <= 2000 ? 2000 : duracion <= 4000 ? 4000 : 6000;
            sonidoHabilitado = Convert.ToInt32(clave?.GetValue("SonidoHabilitado") ?? 1) != 0;
            inicioAutomaticoHabilitado = Convert.ToInt32(clave?.GetValue("InicioAutomatico") ?? 1) != 0;
            temaClaroHabilitado = Convert.ToInt32(clave?.GetValue("TemaClaro") ?? 0) != 0;
            ubicacionNotificaciones = Math.Clamp(Convert.ToInt32(clave?.GetValue("UbicacionNotificaciones") ?? 0), 0, 3);
            mostrarVentanaAlIniciar = Convert.ToInt32(clave?.GetValue("MostrarAlIniciar") ?? 0) != 0;
        }
        catch { }
        Notificador.DuracionAviso = duracionNotificacion;
        Notificador.Posicion = ubicacionNotificaciones;
        panelAjustes.CargarPreferencias(avisoPrevioMinutos, duracionNotificacion, sonidoHabilitado,
            inicioAutomaticoHabilitado, temaClaroHabilitado, ubicacionNotificaciones, mostrarVentanaAlIniciar);
    }

    void GuardarPreferencias()
    {
        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(@"Software\TareasBandeja");
            clave?.SetValue("AvisoPrevio", avisoPrevioMinutos, RegistryValueKind.DWord);
            clave?.SetValue("DuracionNotificacion", duracionNotificacion, RegistryValueKind.DWord);
            clave?.SetValue("SonidoHabilitado", sonidoHabilitado ? 1 : 0, RegistryValueKind.DWord);
            clave?.SetValue("InicioAutomatico", inicioAutomaticoHabilitado ? 1 : 0, RegistryValueKind.DWord);
            clave?.SetValue("TemaClaro", temaClaroHabilitado ? 1 : 0, RegistryValueKind.DWord);
            clave?.SetValue("UbicacionNotificaciones", ubicacionNotificaciones, RegistryValueKind.DWord);
            clave?.SetValue("MostrarAlIniciar", mostrarVentanaAlIniciar ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }

    void AplicarTema(bool claro)
    {
        var fondoAnterior = Ui.Fondo;
        var tarjetaAnterior = Ui.Tarjeta;
        var hoverAnterior = Ui.Hover;
        var separadorAnterior = Ui.Separador;
        var grisAnterior = Ui.Gris;
        var textoAnterior = Ui.Texto;
        Ui.EstablecerTema(claro);
        BackColor = Ui.Fondo;
        AplicarPaleta(this, fondoAnterior, tarjetaAnterior, hoverAnterior, separadorAnterior, grisAnterior, textoAnterior);
        Invalidate(true);
    }

    static void AplicarPaleta(Control control, Color fondoViejo, Color tarjetaVieja, Color hoverViejo,
        Color separadorViejo, Color grisViejo, Color textoViejo)
    {
        Color fondoOriginal = control.BackColor;
        bool superficieNeutra = fondoOriginal == fondoViejo || fondoOriginal == tarjetaVieja || fondoOriginal == hoverViejo;
        if (fondoOriginal == fondoViejo) control.BackColor = Ui.Fondo;
        else if (fondoOriginal == tarjetaVieja) control.BackColor = Ui.Tarjeta;
        else if (fondoOriginal == hoverViejo) control.BackColor = Ui.Hover;
        else if (fondoOriginal == separadorViejo) control.BackColor = Ui.Separador;
        if (control.ForeColor == grisViejo) control.ForeColor = Ui.Gris;
        else if (control.ForeColor == textoViejo &&
            (superficieNeutra || control is Label or TextBox or MaskedTextBox or ComboBox or CheckBox or Chip))
            control.ForeColor = Ui.Texto;
        if (control is RoundedPanel panel && panel.Fill == tarjetaVieja) panel.Fill = Ui.Tarjeta;
        if (control is Chip chip && chip.ColorInactivo == tarjetaVieja) chip.ColorInactivo = Ui.Tarjeta;
        if (control is RoundedPanel rounded) rounded.Invalidate();
        foreach (Control hijo in control.Controls) AplicarPaleta(hijo, fondoViejo, tarjetaVieja, hoverViejo, separadorViejo, grisViejo, textoViejo);
    }

    static ConfiguracionAtajo CargarConfiguracionAtajo()
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey(@"Software\TareasBandeja");
            uint modificadores = Convert.ToUInt32(clave?.GetValue("HotkeyModifiers") ?? (MOD_CONTROL | MOD_ALT));
            var tecla = (Keys)Convert.ToInt32(clave?.GetValue("HotkeyKey") ?? (int)Keys.T);
            if (tecla == Keys.None || modificadores == 0) throw new InvalidDataException();
            return new ConfiguracionAtajo(modificadores, tecla);
        }
        catch
        {
            return new ConfiguracionAtajo(MOD_CONTROL | MOD_ALT, Keys.T);
        }
    }

    static void GuardarConfiguracionAtajo(ConfiguracionAtajo config)
    {
        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(@"Software\TareasBandeja");
            clave?.SetValue("HotkeyModifiers", config.Modificadores, RegistryValueKind.DWord);
            clave?.SetValue("HotkeyKey", (int)config.Tecla, RegistryValueKind.DWord);
        }
        catch
        {
            // La app sigue funcionando aunque Windows no permita guardar el ajuste.
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tray.Dispose();
            iconoActual?.Dispose();
            reloj.Dispose();
            animacionFilas.Dispose();
            fNormal.Dispose();
            fSub.Dispose();
        }
        base.Dispose(disposing);
    }
}
