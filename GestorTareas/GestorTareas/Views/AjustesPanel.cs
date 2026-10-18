using System.Drawing.Drawing2D;

namespace TareasBandeja.Views;

internal readonly record struct ConfiguracionAtajo(uint Modificadores, Keys Tecla);

internal sealed class SelectorTema : Control
{
    private bool claro;
    private float progreso;
    private float destino;
    private readonly System.Windows.Forms.Timer animador = new() { Interval = 15 };

    public event EventHandler? TemaCambiado;

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Claro
    {
        get => claro;
        set
        {
            if (claro == value && destino == (value ? 1f : 0f)) return;
            claro = value;
            destino = value ? 1f : 0f;
            if (!IsHandleCreated || !Visible)
            {
                progreso = destino;
                animador.Stop();
                Invalidate();
                return;
            }
            animador.Start();
        }
    }

    public SelectorTema()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
        TabStop = true;
        BackColor = Ui.Tarjeta;
        ForeColor = Ui.Texto;
        Font = new Font("Segoe UI", 10.5f);
        Size = new Size(72, 40);
        animador.Tick += (_, _) =>
        {
            progreso += (destino - progreso) * 0.28f;
            if (Math.Abs(destino - progreso) < 0.01f)
            {
                progreso = destino;
                animador.Stop();
            }
            Invalidate();
        };
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        AlternarTema();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            AlternarTema();
            e.Handled = true;
        }
    }

    private void AlternarTema()
    {
        Claro = !Claro;
        TemaCambiado?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle((Width - 72) / 2, (Height - 36) / 2, 72, 36);
        using (var trackPath = Ui.RoundedRect(track, 18))
        using (var trackBrush = new SolidBrush(Mezclar(Ui.Hover, Ui.Azul, progreso)))
            g.FillPath(trackBrush, trackPath);

        int thumbX = (int)Math.Round(track.Left + 3 + progreso * (track.Width - 36));
        var thumb = new Rectangle(thumbX, track.Top + 3, 30, 30);
        using var thumbBrush = new SolidBrush(Color.White);
        g.FillEllipse(thumbBrush, thumb);

        var centro = new PointF(thumb.Left + thumb.Width / 2f, thumb.Top + thumb.Height / 2f);
        bool mostrarSol = progreso >= 0.5f;
        using var icono = new Pen(mostrarSol ? Ui.Azul : Ui.Gris, 1.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        if (mostrarSol)
        {
            g.DrawEllipse(icono, centro.X - 4, centro.Y - 4, 8, 8);
            for (int angulo = 0; angulo < 360; angulo += 45)
            {
                double radianes = angulo * Math.PI / 180;
                float x1 = centro.X + (float)Math.Cos(radianes) * 6;
                float y1 = centro.Y + (float)Math.Sin(radianes) * 6;
                float x2 = centro.X + (float)Math.Cos(radianes) * 8;
                float y2 = centro.Y + (float)Math.Sin(radianes) * 8;
                g.DrawLine(icono, x1, y1, x2, y2);
            }
        }
        else
        {
            using var luna = new SolidBrush(Ui.Gris);
            using var recorte = new SolidBrush(Color.White);
            g.FillEllipse(luna, centro.X - 6, centro.Y - 7, 13, 13);
            g.FillEllipse(recorte, centro.X - 2, centro.Y - 9, 13, 13);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) animador.Dispose();
        base.Dispose(disposing);
    }

    static Color Mezclar(Color inicio, Color fin, float cantidad) => Color.FromArgb(
        (int)(inicio.A + (fin.A - inicio.A) * cantidad),
        (int)(inicio.R + (fin.R - inicio.R) * cantidad),
        (int)(inicio.G + (fin.G - inicio.G) * cantidad),
        (int)(inicio.B + (fin.B - inicio.B) * cantidad));
}

internal sealed class SelectorAvisosMultiples : Control
{
    private static readonly (int Minutos, string Texto)[] opciones =
    {
        (1440, "24 horas"), (720, "12 horas"), (480, "8 horas"), (240, "4 horas"),
        (120, "2 horas"), (60, "1 hora"), (45, "45 min"), (30, "30 min"), (15, "15 min")
    };

    private readonly HashSet<int> seleccionados = new();
    private readonly Dictionary<int, AppCheckBox> checks = new();
    private readonly Form menu = new();
    private readonly FlowLayoutPanel lista = new();
    private bool hover;

    public int[] Seleccionados => seleccionados.OrderByDescending(x => x).ToArray();

    public SelectorAvisosMultiples()
    {
        BackColor = Ui.Hover;
        ForeColor = Ui.Texto;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;

        lista.FlowDirection = FlowDirection.TopDown;
        lista.WrapContents = false;
        lista.Dock = DockStyle.Fill;
        lista.Padding = new Padding(8);
        lista.BackColor = Ui.Tarjeta;
        menu.FormBorderStyle = FormBorderStyle.None;
        menu.ShowInTaskbar = false;
        menu.StartPosition = FormStartPosition.Manual;
        menu.BackColor = Ui.Tarjeta;
        menu.ClientSize = new Size(196, 9 * 32 + 16);
        menu.Controls.Add(lista);
        menu.Deactivate += (_, _) => menu.Hide();
        menu.SizeChanged += (_, _) =>
        {
            if (menu.Width > 0 && menu.Height > 0)
            {
                using var path = Ui.RoundedRect(new Rectangle(0, 0, menu.Width, menu.Height), Ui.RadioEsquina);
                menu.Region = new Region(path);
            }
        };

        foreach (var (minutos, texto) in opciones)
        {
            var check = new AppCheckBox
            {
                Text = texto,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Ui.Tarjeta,
                ForeColor = Ui.Texto,
                Size = new Size(menu.ClientSize.Width - 16, 30),
                Margin = new Padding(0, 1, 0, 1)
            };
            check.CheckedChanged += (_, _) =>
            {
                if (check.Checked) seleccionados.Add(minutos);
                else seleccionados.Remove(minutos);
                Invalidate();
            };
            checks.Add(minutos, check);
            lista.Controls.Add(check);
        }
    }

    public void Establecer(IEnumerable<int> valores)
    {
        var permitidos = opciones.Select(o => o.Minutos).ToHashSet();
        seleccionados.Clear();
        foreach (int valor in valores)
            if (permitidos.Contains(valor)) seleccionados.Add(valor);
        foreach (var (minutos, check) in checks)
            check.Checked = seleccionados.Contains(minutos);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
    protected override void OnClick(EventArgs e) { base.OnClick(e); AlternarMenu(); }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ActualizarRecorte();
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Down or Keys.Enter or Keys.Space or Keys.Escape || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Down or Keys.Enter or Keys.Space)
        {
            AlternarMenu();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape && menu.Visible)
        {
            menu.Hide();
            e.Handled = true;
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) menu.Hide();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Ui.RadioEsquina))
        using (var fondo = new SolidBrush(hover ? Ui.Hover : BackColor)) g.FillPath(fondo, path);

        string texto = seleccionados.Count switch
        {
            0 => "Desactivado",
            1 => "1 aviso",
            _ => $"{seleccionados.Count} avisos"
        };
        TextRenderer.DrawText(g, texto, Font, new Rectangle(16, 0, Math.Max(0, Width - 48), Height),
            Ui.Texto, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        float cx = Width - 19, cy = Height / 2f;
        using var flecha = new Pen(Ui.Gris, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(flecha, new[] { new PointF(cx - 3, cy - 1), new PointF(cx, cy + 2), new PointF(cx + 3, cy - 1) });
    }

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
        if (!IsHandleCreated) return;

        foreach (var (minutos, check) in checks)
            check.Checked = seleccionados.Contains(minutos);

        menu.ClientSize = new Size(Width, menu.Height);
        foreach (Control check in lista.Controls) check.Width = Width - 16;

        var origen = PointToScreen(new Point(0, Height + 4));
        var pantalla = Screen.FromControl(this).WorkingArea;
        menu.Location = origen;
        if (menu.Bottom > pantalla.Bottom)
            menu.Top = PointToScreen(Point.Empty).Y - menu.Height - 4;
        if (menu.Right > pantalla.Right) menu.Left = pantalla.Right - menu.Width;
        menu.Show(FindForm());
        menu.BringToFront();
    }
}

internal sealed class AjustesPanel : RoundedPanel
{
    readonly Label encabezado = new();
    readonly Label descripcion = new();
    readonly Label etiquetaAtajo = new();
    readonly AppTextBox capturaAtajo = new();
    readonly SelectorAvisosMultiples avisoPrevio = new();
    readonly SelectorRedondeado duracionAviso = new();
    readonly SelectorRedondeado ubicacionAviso = new();
    readonly Label etiquetaAviso = new();
    readonly Label etiquetaDuracion = new();
    readonly Label etiquetaUbicacion = new();
    readonly AppCheckBox sonido = new();
    readonly AppCheckBox inicioAutomatico = new();
    readonly SelectorTema temaClaro = new();
    readonly AppCheckBox mostrarAlIniciar = new();
    readonly Panel contenido = new();
    ConfiguracionAtajo configuracion;
    readonly Button guardar;
    readonly Button cancelar;

    public event EventHandler<ConfiguracionAtajo>? Guardado;
    public event EventHandler? Cancelado;
    public event EventHandler? TemaCambiado;
    public int[] AvisosPrevios => avisoPrevio.Seleccionados;
    public int DuracionNotificacion => duracionAviso.SelectedIndex switch { 0 => 2000, 1 => 4000, _ => 6000 };
    public int UbicacionNotificacion => ubicacionAviso.SelectedIndex;
    public bool SonidoHabilitado => sonido.Checked;
    public bool InicioAutomaticoHabilitado => inicioAutomatico.Checked;
    public bool TemaClaroHabilitado => temaClaro.Claro;
    public bool MostrarAlIniciarHabilitado => mostrarAlIniciar.Checked;

    public AjustesPanel()
    {
        BackColor = Ui.Tarjeta;
        Fill = Ui.Tarjeta;
        Radius = Ui.RadioEsquina;

        encabezado.Text = "Ajustes";
        encabezado.AutoSize = true;
        encabezado.ForeColor = Color.White;
        encabezado.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
        encabezado.Location = new Point(18, 16);

        descripcion.Text = "Personaliza el comportamiento de la aplicación";
        descripcion.AutoSize = true;
        descripcion.ForeColor = Ui.Gris;
        descripcion.Font = new Font("Segoe UI", 9.5f);
        descripcion.Location = new Point(18, 56);

        etiquetaAtajo.Text = "Atajo para mostrar u ocultar la app";
        etiquetaAtajo.AutoSize = true;
        etiquetaAtajo.ForeColor = Color.White;
        etiquetaAtajo.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        etiquetaAtajo.Location = new Point(18, 82);

        capturaAtajo.Bounds = new Rectangle(18, 120, 272, 40);
        capturaAtajo.BackColor = Ui.Hover;
        capturaAtajo.ForeColor = Color.White;
        capturaAtajo.BorderStyle = BorderStyle.None;
        capturaAtajo.Font = new Font("Segoe UI", 11f);
        capturaAtajo.ReadOnly = true;
        capturaAtajo.ShortcutsEnabled = false;
        capturaAtajo.TabStop = true;
        capturaAtajo.TextAlign = HorizontalAlignment.Center;
        capturaAtajo.Cursor = Cursors.Hand;
        capturaAtajo.KeyDown += CapturarAtajo;
        capturaAtajo.PreviewKeyDown += (_, e) => e.IsInputKey = true;
        capturaAtajo.Enter += (_, _) => capturaAtajo.BackColor = Ui.Hover;
        capturaAtajo.Leave += (_, _) => capturaAtajo.BackColor = Ui.Hover;

        ConfigurarEtiqueta(etiquetaAviso, "Avisos previos", new Point(18, 172));
        ConfigurarEtiqueta(etiquetaDuracion, "Duración", new Point(162, 172));
        ConfigurarEtiqueta(etiquetaUbicacion, "Posición de las notificaciones", new Point(18, 242));
        avisoPrevio.Location = new Point(18, 192);
        avisoPrevio.Size = new Size(129, 36);
        avisoPrevio.BackColor = Ui.Hover;
        avisoPrevio.ForeColor = Color.White;
        avisoPrevio.Font = new Font("Segoe UI", 11f);
        avisoPrevio.Establecer(new[] { 60, 15 });
        ConfigurarSelector(duracionAviso, new Point(161, 192), 129, "2 segundos", "4 segundos", "6 segundos");
        ConfigurarSelector(ubicacionAviso, new Point(18, 262), 272, "Abajo a la derecha", "Abajo a la izquierda", "Arriba a la derecha", "Arriba a la izquierda");
        sonido.Text = "Reproducir sonido";
        inicioAutomatico.Text = "Iniciar con Windows";
        mostrarAlIniciar.Text = "Mostrar ventana al iniciar Windows";
        foreach (var opcion in new[] { sonido, inicioAutomatico, mostrarAlIniciar })
        {
            opcion.AutoSize = false;
            opcion.ForeColor = Color.White;
            opcion.BackColor = Ui.Tarjeta;
            opcion.Font = new Font("Segoe UI", 11f);
            opcion.Left = 18;
            opcion.Size = new Size(272, 26);
        }
        sonido.Top = 314;
        inicioAutomatico.Top = 340;
        temaClaro.Location = new Point(Width - 90, 8);
        temaClaro.TemaCambiado += (_, _) => TemaCambiado?.Invoke(this, EventArgs.Empty);
        mostrarAlIniciar.Top = 366;

        guardar = CrearBoton("Guardar", Ui.Azul, 0, 0, 86);
        guardar.Click += (_, _) => GuardarCambios();
        cancelar = CrearBoton("Cancelar", Ui.Hover, 0, 0, 84);
        cancelar.Click += (_, _) => Cancelado?.Invoke(this, EventArgs.Empty);

        contenido.Location = Point.Empty;
        contenido.BackColor = Ui.Tarjeta;
        contenido.AutoScroll = false;
        contenido.AutoScrollMinSize = Size.Empty;
        contenido.BorderStyle = BorderStyle.None;
        contenido.Controls.AddRange(new Control[] { encabezado, descripcion, etiquetaAtajo, capturaAtajo,
            etiquetaAviso, etiquetaDuracion, etiquetaUbicacion, avisoPrevio, duracionAviso, ubicacionAviso,
            sonido, inicioAutomatico, temaClaro, mostrarAlIniciar });
        Controls.AddRange(new Control[] { contenido, guardar, cancelar });
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        RecolocarBotones();
    }

    public void RecolocarBotones()
    {
        const int margen = 18;
        const int separacion = 8;
        contenido.Bounds = new Rectangle(0, 0, Width, Math.Max(100, Height - 58));
        capturaAtajo.Width = Math.Max(120, Width - 36);
        foreach (var opcion in new[] { sonido, inicioAutomatico, mostrarAlIniciar })
            opcion.Width = Math.Max(140, Width - 36);
        temaClaro.Size = new Size(72, 40);
        temaClaro.Left = Math.Max(18, Width - 18 - temaClaro.Width);
        temaClaro.Top = encabezado.Top + Math.Max(0, (encabezado.Height - temaClaro.Height) / 2);
        int anchoInterior = Math.Max(140, Width - 36);
        int anchoColumna = Math.Max(80, (anchoInterior - 14) / 2);
        avisoPrevio.Width = anchoColumna;
        duracionAviso.Left = 18 + anchoColumna + 14;
        duracionAviso.Width = anchoColumna;
        etiquetaDuracion.Left = duracionAviso.Left;
        ubicacionAviso.Width = anchoInterior;
        cancelar.Location = new Point(Math.Max(margen, Width - margen - cancelar.Width),
            Math.Max(margen, Height - margen - cancelar.Height));
        guardar.Location = new Point(Math.Max(margen, cancelar.Left - separacion - guardar.Width), cancelar.Top);
        using var region = Ui.RoundedRect(new Rectangle(0, 0, capturaAtajo.Width, capturaAtajo.Height), Ui.RadioEsquina);
        capturaAtajo.Region = new Region(region);
        using var regionContenido = Ui.RoundedRect(new Rectangle(0, 0, contenido.Width, contenido.Height), Ui.RadioEsquina);
        contenido.Region = new Region(regionContenido);
    }

    public void Editar(ConfiguracionAtajo config)
    {
        configuracion = config;
        capturaAtajo.Text = FormatearAtajo(config);
    }

    public void CargarPreferencias(IEnumerable<int> avisos, int duracion, bool activarSonido, bool iniciar, bool claro, int ubicacion = 0, bool mostrar = false)
    {
        avisoPrevio.Establecer(avisos);
        duracionAviso.SelectedIndex = duracion <= 2000 ? 0 : duracion <= 4000 ? 1 : 2;
        sonido.Checked = activarSonido;
        inicioAutomatico.Checked = iniciar;
        temaClaro.Claro = claro;
        ubicacionAviso.SelectedIndex = Math.Clamp(ubicacion, 0, 3);
        mostrarAlIniciar.Checked = mostrar;
    }

    void CapturarAtajo(object? sender, KeyEventArgs e)
    {
        Keys tecla = e.KeyCode;
        if (tecla is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin)
            return;

        uint modificadores = 0;
        if ((e.Modifiers & Keys.Control) != Keys.None) modificadores |= 0x0002;
        if ((e.Modifiers & Keys.Alt) != Keys.None) modificadores |= 0x0001;
        if ((e.Modifiers & Keys.Shift) != Keys.None) modificadores |= 0x0004;
        if ((Control.ModifierKeys & (Keys.LWin | Keys.RWin)) != Keys.None)
            modificadores |= 0x0008;
        if (modificadores == 0) return;

        configuracion = new ConfiguracionAtajo(modificadores, tecla);
        capturaAtajo.Text = FormatearAtajo(configuracion);
        e.SuppressKeyPress = true;
        e.Handled = true;
    }

    static string FormatearAtajo(ConfiguracionAtajo config)
    {
        var partes = new List<string>();
        if ((config.Modificadores & 0x0002) != 0) partes.Add("Ctrl");
        if ((config.Modificadores & 0x0001) != 0) partes.Add("Alt");
        if ((config.Modificadores & 0x0004) != 0) partes.Add("Shift");
        if ((config.Modificadores & 0x0008) != 0) partes.Add("Win");
        partes.Add(config.Tecla.ToString());
        return string.Join(" + ", partes);
    }

    Button CrearBoton(string texto, Color fondo, int x, int y, int ancho) => new EditorButton
    {
        Text = texto,
        Bounds = new Rectangle(x, y, ancho, 34),
        BackColor = fondo,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 9.5f),
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 }
    };

    static void ConfigurarSelector(SelectorRedondeado selector, Point ubicacion, int ancho, params string[] opciones)
    {
        selector.Location = ubicacion;
        selector.Size = new Size(ancho, 36);
        selector.BackColor = Ui.Hover;
        selector.ForeColor = Color.White;
        selector.Font = new Font("Segoe UI", 11f);
        selector.Items.AddRange(opciones);
        selector.SelectedIndex = 0;
    }

    static void ConfigurarEtiqueta(Label etiqueta, string texto, Point ubicacion)
    {
        etiqueta.Text = texto;
        etiqueta.AutoSize = true;
        etiqueta.Location = ubicacion;
        etiqueta.ForeColor = Ui.Gris;
        etiqueta.Font = new Font("Segoe UI", 8f);
    }

    void GuardarCambios()
    {
        if (configuracion.Tecla == Keys.None || configuracion.Modificadores == 0) return;
        Guardado?.Invoke(this, configuracion);
    }
}
