using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
