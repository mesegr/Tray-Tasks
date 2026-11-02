using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
