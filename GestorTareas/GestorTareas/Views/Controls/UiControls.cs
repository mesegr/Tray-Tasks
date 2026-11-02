using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

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

// Botón discreto para abrir la vista de calendario.
class CalendarButton : Button
{
    bool hover, down;

    public CalendarButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Ui.Fondo;
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(ControlStyles.Selectable, false);
        DoubleBuffered = true;
        AccessibleName = "Abrir calendario";
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

        float size = Math.Min(Width, Height) * 0.46f;
        float x = (Width - size) / 2f;
        float y = (Height - size) / 2f + size * 0.06f;
        float radio = Math.Max(2f, size * 0.12f);
        using var forma = Ui.RoundedRect(Rectangle.Round(new RectangleF(x, y, size, size * 0.82f)), (int)radio);
        using var linea = new Pen(Ui.Gris, Math.Max(1.3f, size * 0.075f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawPath(linea, forma);
        g.DrawLine(linea, x + size * 0.25f, y - size * 0.02f, x + size * 0.25f, y + size * 0.18f);
        g.DrawLine(linea, x + size * 0.75f, y - size * 0.02f, x + size * 0.75f, y + size * 0.18f);
        g.DrawLine(linea, x + size * 0.04f, y + size * 0.27f, x + size * 0.96f, y + size * 0.27f);
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

        float tamano = Math.Min(Width, Height) * 0.48f;
        float radio = tamano / 2f;
        float cx = Width / 2f;
        float cy = Height / 2f;
        var puntos = new PointF[32];
        for (int i = 0; i < puntos.Length; i++)
        {
            float radioPunto = radio * (i % 4 is 0 or 1 ? 0.76f : 1f);
            double angulo = -Math.PI / 2 + i * Math.PI / 16;
            puntos[i] = new PointF(cx + (float)Math.Cos(angulo) * radioPunto,
                cy + (float)Math.Sin(angulo) * radioPunto);
        }

        using var engranaje = new GraphicsPath();
        engranaje.AddPolygon(puntos);
        var color = down ? Color.White : Ui.Gris;
        using var linea = new Pen(color, Math.Max(1.2f, tamano * 0.095f))
        {
            LineJoin = LineJoin.Round
        };
        g.DrawPath(linea, engranaje);
        float radioCentro = tamano * 0.17f;
        using var centro = new Pen(color, Math.Max(1.1f, tamano * 0.085f));
        g.DrawEllipse(centro, cx - radioCentro, cy - radioCentro, radioCentro * 2, radioCentro * 2);
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
    public Func<DateTime, int>? CantidadTareas { get; set; }

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
            int cantidad = CantidadTareas?.Invoke(fecha) ?? 0;
            if (cantidad > 0)
            {
                float punto = Math.Max(3f, Math.Min(5f, celda.Height * 0.11f));
                using var marca = new SolidBrush(cantidad > 3 ? Ui.Rojo : Ui.Naranja);
                g.FillEllipse(marca, celda.X + celda.Width / 2f - punto / 2f, celda.Bottom - punto * 1.6f, punto, punto);
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

    public event EventHandler? ScrollPositionChanged;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0x0115 or 0x020A or 0x0100) // WM_VSCROLL, WM_MOUSEWHEEL, WM_KEYDOWN
            ScrollPositionChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Barra vertical minimalista para la lista de tareas.</summary>
sealed class TaskScrollBar : Control
{
    private BufferedListBox? lista;
    private bool arrastrando;
    private int desplazamientoPuntero;

    public TaskScrollBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Ui.Tarjeta;
        Cursor = Cursors.Hand;
    }

    public void Conectar(BufferedListBox list)
    {
        lista = list;
        lista.ScrollPositionChanged += (_, _) => Invalidate();
        lista.Resize += (_, _) => Actualizar();
        Actualizar();
    }

    public void Actualizar()
    {
        if (lista != null)
        {
            int visibles = Math.Max(1, lista.ClientSize.Height / Math.Max(1, lista.ItemHeight));
            Visible = lista.Items.Count > visibles;
        }
        Invalidate();
    }

    private (int top, int height, int maxTop) Geometria()
    {
        if (lista == null || lista.Items.Count == 0 || lista.ItemHeight <= 0) return (0, Height, 0);
        int visibles = Math.Max(1, lista.ClientSize.Height / lista.ItemHeight);
        if (lista.Items.Count <= visibles) return (0, Height, 0);

        int altoPista = Math.Max(1, Height - 12);
        int altoPulgar = Math.Clamp(altoPista * visibles / lista.Items.Count, 24, altoPista);
        int maxTop = Math.Max(1, lista.Items.Count - visibles);
        int maxY = altoPista - altoPulgar;
        int top = 6 + (int)Math.Round(maxY * Math.Clamp(lista.TopIndex, 0, maxTop) / (double)maxTop);
        return (top, altoPulgar, maxTop);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var fondo = new SolidBrush(Ui.Tarjeta)) e.Graphics.FillRectangle(fondo, ClientRectangle);
        if (lista == null || lista.Items.Count == 0 || Geometria().maxTop == 0) return;

        var (top, alto, _) = Geometria();
        int centro = Width / 2;
        int anchoPulgar = hover || arrastrando ? 4 : 3;
        using var pulgar = new SolidBrush(hover || arrastrando
            ? Color.FromArgb(190, Ui.Gris)
            : Color.FromArgb(125, Ui.Gris));
        using var formaPulgar = Ui.RoundedRect(new Rectangle(centro - anchoPulgar / 2, top, anchoPulgar, alto), anchoPulgar / 2);
        e.Graphics.FillPath(pulgar, formaPulgar);
    }

    private bool hover;

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!arrastrando) hover = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || lista == null) return;
        var (top, alto, maxTop) = Geometria();
        if (maxTop == 0) return;
        if (e.Y >= top && e.Y <= top + alto)
        {
            arrastrando = true;
            desplazamientoPuntero = e.Y - top;
            Capture = true;
        }
        else if (e.Y < top)
            lista.TopIndex = Math.Max(0, lista.TopIndex - Math.Max(1, lista.ClientSize.Height / lista.ItemHeight));
        else
            lista.TopIndex = Math.Min(maxTop, lista.TopIndex + Math.Max(1, lista.ClientSize.Height / lista.ItemHeight));
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!arrastrando || lista == null) return;
        var (_, alto, maxTop) = Geometria();
        int altoPista = Math.Max(1, Height - 12);
        int maxY = Math.Max(1, altoPista - alto);
        int y = Math.Clamp(e.Y - desplazamientoPuntero - 6, 0, maxY);
        int visibles = Math.Max(1, lista.ClientSize.Height / lista.ItemHeight);
        int maxIndice = Math.Max(0, lista.Items.Count - visibles);
        lista.TopIndex = Math.Clamp((int)Math.Round(y * maxIndice / (double)maxY), 0, maxTop);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        arrastrando = false;
        Capture = false;
        Invalidate();
    }
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
