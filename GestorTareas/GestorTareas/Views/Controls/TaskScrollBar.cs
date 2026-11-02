using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

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
