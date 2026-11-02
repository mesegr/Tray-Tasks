using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
