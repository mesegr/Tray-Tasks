using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TareasBandeja.Views;

/// <summary>Selector propio: superficie pintada por la app y menú emergente sin controles nativos.</summary>
internal sealed class SelectorRedondeado : Control
{
    private readonly Form menu = new();
    private readonly Panel opcionesMenu = new();
    private readonly MenuScrollBar barraMenu;
    private readonly int paddingMenu = 7;
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

        opcionesMenu.AutoScroll = false;
        opcionesMenu.BackColor = Ui.Tarjeta;
        opcionesMenu.MouseWheel += (_, e) => DesplazarMenu(e.Delta);
        barraMenu = new MenuScrollBar(this);
        barraMenu.Visible = false;
        menu.FormBorderStyle = FormBorderStyle.None;
        menu.ShowInTaskbar = false;
        menu.StartPosition = FormStartPosition.Manual;
        menu.AutoSize = false;
        menu.Padding = Padding.Empty;
        menu.BackColor = Ui.Tarjeta;
        menu.Controls.AddRange(new Control[] { opcionesMenu, barraMenu });
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

        opcionesMenu.Controls.Clear();
        barraMenu.Value = 0;
        for (int i = 0; i < Items.Count; i++)
        {
            var opcion = new OpcionSelector(Items[i]?.ToString() ?? string.Empty, i == selectedIndex, Font);
            int indice = i;
            opcion.Width = Math.Max(0, Width - paddingMenu * 2);
            opcion.Location = new Point(paddingMenu, paddingMenu + i * 42);
            opcion.Click += (_, _) => { SelectedIndex = indice; menu.Hide(); Focus(); };
            opcion.MouseWheel += (_, e) => DesplazarMenu(e.Delta);
            opcionesMenu.Controls.Add(opcion);
        }

        var origen = PointToScreen(Point.Empty);
        var anfitrion = FindForm();
        Rectangle limites = anfitrion != null && anfitrion.IsHandleCreated
            ? anfitrion.RectangleToScreen(anfitrion.ClientRectangle)
            : Screen.FromControl(this).WorkingArea;
        int alturaDeseada = Items.Count * 42 + paddingMenu * 2;
        int espacioArriba = origen.Y - limites.Top - 4;
        int alturaMaximaCompacta = (int)(6.5 * 42 + paddingMenu * 2);
        int alturaMenu = Math.Min(alturaDeseada, Math.Min(alturaMaximaCompacta, Math.Max(42, espacioArriba)));
        menu.ClientSize = new Size(Width, alturaMenu);
        bool requiereScroll = alturaDeseada > alturaMenu;
        int anchoBarra = requiereScroll ? 14 : 0;
        opcionesMenu.Bounds = new Rectangle(0, 0, Width - anchoBarra, alturaMenu);
        barraMenu.Bounds = new Rectangle(Width - anchoBarra, 0, anchoBarra, alturaMenu);
        barraMenu.Visible = requiereScroll;
        int anchoOpcion = Width - anchoBarra - paddingMenu * 2;
        foreach (Control opcion in opcionesMenu.Controls) opcion.Width = Math.Max(0, anchoOpcion);
        barraMenu.Configure(Items.Count * 42 + paddingMenu * 2, alturaMenu);
        ActualizarDesplazamientoMenu();
        menu.Location = new Point(origen.X, origen.Y - menu.Height - 4);
        if (menu.Right > limites.Right) menu.Left = limites.Right - menu.Width;
        if (menu.Left < limites.Left) menu.Left = limites.Left;
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

    private void DesplazarMenu(int delta)
    {
        if (!barraMenu.Visible || delta == 0) return;
        barraMenu.Value = Math.Clamp(barraMenu.Value - Math.Sign(delta) * 42, 0, barraMenu.Maximum);
        ActualizarDesplazamientoMenu();
    }

    private void ActualizarDesplazamientoMenu()
    {
        for (int i = 0; i < opcionesMenu.Controls.Count; i++)
            opcionesMenu.Controls[i].Top = paddingMenu + i * 42 - barraMenu.Value;
        barraMenu.Invalidate();
    }

    private sealed class MenuScrollBar : Control
    {
        private readonly SelectorRedondeado owner;
        private bool hover;
        private bool dragging;
        private int dragOffset;
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Value { get; set; }
        public int Maximum { get; private set; }
        private int contentHeight;

        public MenuScrollBar(SelectorRedondeado owner)
        {
            this.owner = owner;
            BackColor = Ui.Tarjeta;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public void Configure(int contentHeight, int viewportHeight)
        {
            this.contentHeight = contentHeight;
            Maximum = Math.Max(0, contentHeight - viewportHeight);
            Value = Math.Clamp(Value, 0, Maximum);
            Invalidate();
        }

        private (int top, int height) Thumb()
        {
            if (Maximum <= 0 || Height <= 0) return (0, Height);
            int thumbHeight = Math.Clamp((int)Math.Round(Height * (Height / (double)contentHeight)), 24, Height);
            int track = Math.Max(1, Height - thumbHeight - 12);
            int top = 6 + (int)Math.Round(track * Value / (double)Maximum);
            return (top, thumbHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var fondo = new SolidBrush(Ui.Tarjeta)) e.Graphics.FillRectangle(fondo, ClientRectangle);
            if (Maximum <= 0) return;
            var (top, height) = Thumb();
            int width = hover || dragging ? 4 : 3;
            using var brush = new SolidBrush(Color.FromArgb(hover || dragging ? 190 : 125, Ui.Gris));
            using var shape = Ui.RoundedRect(new Rectangle((Width - width) / 2, top, width, height), width / 2);
            e.Graphics.FillPath(brush, shape);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (!dragging) hover = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || Maximum <= 0) return;
            var (top, height) = Thumb();
            if (e.Y >= top && e.Y <= top + height)
            {
                dragging = true;
                dragOffset = e.Y - top;
                Capture = true;
            }
            else
            {
                Value = Math.Clamp(Value + (e.Y < top ? -Height : Height), 0, Maximum);
                owner.ActualizarDesplazamientoMenu();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            var (_, height) = Thumb();
            int track = Math.Max(1, Height - height - 12);
            int top = Math.Clamp(e.Y - dragOffset - 6, 0, track);
            Value = Math.Clamp((int)Math.Round(top * Maximum / (double)track), 0, Maximum);
            owner.ActualizarDesplazamientoMenu();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
            Capture = false;
            Invalidate();
        }
    }
}
