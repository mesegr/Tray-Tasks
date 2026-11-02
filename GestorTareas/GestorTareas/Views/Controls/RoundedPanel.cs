using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
