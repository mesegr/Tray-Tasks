using System.Drawing.Drawing2D;

namespace TrayTasks.Views;

/// <summary>Etiqueta de ajustes con un indicador circular propio.</summary>
internal sealed class AppCheckBox : Control
{
    private bool hover;
    private bool marcado;

    public event EventHandler? CheckedChanged;

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => marcado;
        set
        {
            if (marcado == value) return;
            marcado = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public AppCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Ui.Tarjeta;
        ForeColor = Ui.Texto;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hover = false;
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override bool IsInputKey(Keys keyData) => keyData == Keys.Space || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode != Keys.Space) return;
        Checked = !Checked;
        e.Handled = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Ui.Fondo);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int lado = Math.Min(19, Math.Max(15, Height - 5));
        int y = (Height - lado) / 2;
        var caja = new Rectangle(0, y, lado, lado);
        if (Checked)
        {
            using var fondo = new SolidBrush(Ui.Azul);
            g.FillEllipse(fondo, caja);
            using var tick = new Pen(Color.White, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(tick, new[]
            {
                new PointF(4.5f, y + lado * 0.53f),
                new PointF(lado * 0.42f, y + lado - 5),
                new PointF(lado - 4, y + 5)
            });
        }
        else
        {
            using var fondo = new SolidBrush(Ui.Hover);
            using var borde = new Pen(hover ? Ui.AzulClaro : Ui.Separador, 1.2f);
            g.FillEllipse(fondo, caja);
            g.DrawEllipse(borde, caja);
        }

        var texto = new Rectangle(lado + 9, 0, Math.Max(0, Width - lado - 9), Height);
        TextRenderer.DrawText(g, Text, Font, texto, Ui.Texto,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
