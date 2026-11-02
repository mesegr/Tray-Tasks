using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
