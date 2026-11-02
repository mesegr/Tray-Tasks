using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
