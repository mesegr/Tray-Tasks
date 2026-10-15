namespace TareasBandeja.Views;

internal readonly record struct ConfiguracionAtajo(uint Modificadores, Keys Tecla);

internal sealed class AjustesPanel : RoundedPanel
{
    readonly Label encabezado = new();
    readonly Label descripcion = new();
    readonly Label etiquetaAtajo = new();
    readonly TextBox capturaAtajo = new();
    readonly SelectorRedondeado avisoPrevio = new();
    readonly SelectorRedondeado duracionAviso = new();
    readonly SelectorRedondeado ubicacionAviso = new();
    readonly Label etiquetaAviso = new();
    readonly Label etiquetaDuracion = new();
    readonly Label etiquetaUbicacion = new();
    readonly CheckBox sonido = new();
    readonly CheckBox inicioAutomatico = new();
    readonly CheckBox temaClaro = new();
    readonly CheckBox mostrarAlIniciar = new();
    readonly Panel contenido = new();
    ConfiguracionAtajo configuracion;
    readonly Button guardar;
    readonly Button cancelar;

    public event EventHandler<ConfiguracionAtajo>? Guardado;
    public event EventHandler? Cancelado;
    public int MinutosAviso => avisoPrevio.SelectedIndex == 0 ? 30 : 60;
    public int DuracionNotificacion => duracionAviso.SelectedIndex switch { 0 => 2000, 1 => 4000, _ => 6000 };
    public int UbicacionNotificacion => ubicacionAviso.SelectedIndex;
    public bool SonidoHabilitado => sonido.Checked;
    public bool InicioAutomaticoHabilitado => inicioAutomatico.Checked;
    public bool TemaClaroHabilitado => temaClaro.Checked;
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
        descripcion.Location = new Point(18, 50);

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

        ConfigurarEtiqueta(etiquetaAviso, "Aviso previo", new Point(18, 172));
        ConfigurarEtiqueta(etiquetaDuracion, "Duración", new Point(162, 172));
        ConfigurarEtiqueta(etiquetaUbicacion, "Posición de las notificaciones", new Point(18, 242));
        ConfigurarSelector(avisoPrevio, new Point(18, 192), 129, "30 min", "60 min");
        ConfigurarSelector(duracionAviso, new Point(161, 192), 129, "2 segundos", "4 segundos", "6 segundos");
        ConfigurarSelector(ubicacionAviso, new Point(18, 262), 272, "Abajo a la derecha", "Abajo a la izquierda", "Arriba a la derecha", "Arriba a la izquierda");
        sonido.Text = "Reproducir sonido";
        inicioAutomatico.Text = "Iniciar con Windows";
        temaClaro.Text = "Usar tema claro";
        mostrarAlIniciar.Text = "Mostrar ventana al iniciar Windows";
        foreach (var opcion in new[] { sonido, inicioAutomatico, temaClaro, mostrarAlIniciar })
        {
            opcion.AutoSize = true;
            opcion.ForeColor = Color.White;
            opcion.BackColor = Ui.Tarjeta;
            opcion.Font = new Font("Segoe UI", 11f);
            opcion.Left = 18;
        }
        sonido.Top = 314;
        inicioAutomatico.Top = 340;
        temaClaro.Top = 366;
        mostrarAlIniciar.Top = 392;

        guardar = CrearBoton("Guardar", Ui.Azul, 0, 0, 86);
        guardar.Click += (_, _) => GuardarCambios();
        cancelar = CrearBoton("Cancelar", Ui.Hover, 0, 0, 84);
        cancelar.Click += (_, _) => Cancelado?.Invoke(this, EventArgs.Empty);

        contenido.Location = Point.Empty;
        contenido.BackColor = Ui.Tarjeta;
        contenido.AutoScroll = true;
        contenido.AutoScrollMinSize = new Size(0, 500);
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

    public void CargarPreferencias(int minutos, int duracion, bool activarSonido, bool iniciar, bool claro, int ubicacion = 0, bool mostrar = false)
    {
        avisoPrevio.SelectedIndex = minutos <= 30 ? 0 : 1;
        duracionAviso.SelectedIndex = duracion <= 2000 ? 0 : duracion <= 4000 ? 1 : 2;
        sonido.Checked = activarSonido;
        inicioAutomatico.Checked = iniciar;
        temaClaro.Checked = claro;
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
