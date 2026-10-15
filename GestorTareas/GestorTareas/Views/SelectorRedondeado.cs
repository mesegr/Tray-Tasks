using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TareasBandeja.Views;

/// <summary>Selector propio: superficie pintada por la app y menú emergente sin controles nativos.</summary>
internal sealed class SelectorRedondeado : Control
{
    private readonly Form menu = new();
    private readonly FlowLayoutPanel opcionesMenu = new();
    private bool hover;
    private int selectedIndex = -1;

    public List<object> Items { get; } = new();
    public event EventHandler? SelectedIndexChanged;

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            int nuevo = value >= 0 && value < Items.Count ? value : -1;
            if (selectedIndex == nuevo) return;
            selectedIndex = nuevo;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public object? SelectedItem => selectedIndex >= 0 ? Items[selectedIndex] : null;

    public SelectorRedondeado()
    {
        BackColor = Ui.Hover;
        ForeColor = Ui.Texto;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;

        opcionesMenu.FlowDirection = FlowDirection.TopDown;
        opcionesMenu.WrapContents = false;
        opcionesMenu.AutoSize = true;
        opcionesMenu.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        opcionesMenu.Padding = new Padding(7);
        opcionesMenu.BackColor = Ui.Tarjeta;
        menu.FormBorderStyle = FormBorderStyle.None;
        menu.ShowInTaskbar = false;
        menu.StartPosition = FormStartPosition.Manual;
        menu.AutoSize = true;
        menu.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        menu.Padding = Padding.Empty;
        menu.BackColor = Ui.Tarjeta;
        menu.Controls.Add(opcionesMenu);
        menu.Deactivate += (_, _) => menu.Hide();
        menu.SizeChanged += (_, _) =>
        {
            if (menu.Width > 0 && menu.Height > 0)
            {
                using var path = Ui.RoundedRect(new Rectangle(0, 0, menu.Width, menu.Height), Ui.RadioEsquina);
                menu.Region = new Region(path);
            }
        };
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

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ActualizarRecorte();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        AlternarMenu();
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Down or Keys.Up or Keys.Enter or Keys.Space or Keys.Escape || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space or Keys.Down)
        {
            AlternarMenu();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Up && Items.Count > 0)
        {
            SelectedIndex = selectedIndex <= 0 ? Items.Count - 1 : selectedIndex - 1;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape && menu.Visible)
        {
            menu.Hide();
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = Ui.RoundedRect(bounds, Ui.RadioEsquina))
        using (var fondo = new SolidBrush(hover ? Ui.Hover : BackColor)) g.FillPath(fondo, path);

        var areaTexto = new Rectangle(16, 0, Math.Max(0, Width - 48), Height);
        var texto = SelectedItem?.ToString() ?? string.Empty;
        TextRenderer.DrawText(g, texto, Font, areaTexto, Ui.Texto,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        float cx = Width - 19;
        float cy = Height / 2f;
        using var flecha = new Pen(Ui.Gris, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(flecha, new[] { new PointF(cx - 3, cy - 1), new PointF(cx, cy + 2), new PointF(cx + 3, cy - 1) });
    }

    // Mantiene el pintado estándar, pero el Region limita el fondo a la forma
    // redondeada para que no queden esquinas rectangulares ni restos del control inferior.
    protected override void OnPaintBackground(PaintEventArgs e) => base.OnPaintBackground(e);

    private void ActualizarRecorte()
    {
        if (Width < 2 || Height < 2) return;
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Ui.RadioEsquina);
        var regionAnterior = Region;
        Region = new Region(path);
        regionAnterior?.Dispose();
    }

    private void AlternarMenu()
    {
        if (menu.Visible) { menu.Hide(); return; }
        if (Items.Count == 0 || !IsHandleCreated) return;

        opcionesMenu.SuspendLayout();
        opcionesMenu.Controls.Clear();
        for (int i = 0; i < Items.Count; i++)
        {
            var opcion = new OpcionSelector(Items[i]?.ToString() ?? string.Empty, i == selectedIndex, Font);
            int indice = i;
            opcion.Width = Math.Max(80, Width - 8);
            opcion.Click += (_, _) => { SelectedIndex = indice; menu.Hide(); Focus(); };
            opcionesMenu.Controls.Add(opcion);
        }
        opcionesMenu.ResumeLayout(true);

        menu.Width = Width;
        menu.Location = PointToScreen(new Point(0, Height + 4));
        var pantalla = Screen.FromControl(this).WorkingArea;
        if (menu.Bottom > pantalla.Bottom)
            menu.Top = PointToScreen(Point.Empty).Y - menu.Height - 4;
        if (menu.Right > pantalla.Right) menu.Left = pantalla.Right - menu.Width;
        menu.Show(FindForm());
        menu.BringToFront();
    }

    private sealed class OpcionSelector : Control
    {
        private readonly string texto;
        private readonly bool seleccionada;
        private bool hover;

        public OpcionSelector(string texto, bool seleccionada, Font font)
        {
            this.texto = texto;
            this.seleccionada = seleccionada;
            Font = font;
            Height = 42;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (hover || seleccionada)
            {
                using var path = Ui.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 2), Ui.RadioEsquina);
                using var fondo = new SolidBrush(hover ? Ui.Hover : Color.FromArgb(
                    (Ui.Tarjeta.R + Ui.Hover.R) / 2, (Ui.Tarjeta.G + Ui.Hover.G) / 2, (Ui.Tarjeta.B + Ui.Hover.B) / 2));
                g.FillPath(fondo, path);
            }

            int left = seleccionada ? 32 : 16;
            TextRenderer.DrawText(g, texto, Font, new Rectangle(left, 0, Math.Max(0, Width - left - 10), Height),
                Ui.Texto, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (seleccionada)
            {
                using var pen = new Pen(Ui.Azul, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(pen, new[] { new PointF(15, Height / 2f), new PointF(18, Height / 2f + 3), new PointF(24, Height / 2f - 3) });
            }
        }
    }
}
