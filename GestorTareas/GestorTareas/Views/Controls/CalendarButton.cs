using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
