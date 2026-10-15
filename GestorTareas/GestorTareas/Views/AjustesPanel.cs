namespace TareasBandeja.Views;

internal readonly record struct ConfiguracionAtajo(uint Modificadores, Keys Tecla);

internal sealed class AjustesPanel : RoundedPanel
{
    readonly Label encabezado = new();
    readonly Label descripcion = new();
    readonly Label etiquetaAtajo = new();
    readonly TextBox capturaAtajo = new();
    ConfiguracionAtajo configuracion;
    readonly Button guardar;
    readonly Button cancelar;

    public event EventHandler<ConfiguracionAtajo>? Guardado;
    public event EventHandler? Cancelado;

    public AjustesPanel()
    {
        BackColor = Ui.Tarjeta;
        Fill = Ui.Tarjeta;
        Radius = 16;

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
        capturaAtajo.Enter += (_, _) => capturaAtajo.BackColor = Color.FromArgb(64, 64, 66);
        capturaAtajo.Leave += (_, _) => capturaAtajo.BackColor = Ui.Hover;

        guardar = CrearBoton("Guardar", Ui.Azul, 0, 0, 86);
        guardar.Click += (_, _) => GuardarCambios();
        cancelar = CrearBoton("Cancelar", Ui.Hover, 0, 0, 84);
        cancelar.Click += (_, _) => Cancelado?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { encabezado, descripcion, etiquetaAtajo, capturaAtajo, guardar, cancelar });
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
        capturaAtajo.Width = Math.Max(120, Width - 36);
        const int ajusteVertical = 21;
        cancelar.Location = new Point(Math.Max(margen, Width - margen - cancelar.Width),
            Math.Max(margen, Height - margen - cancelar.Height) + ajusteVertical);
        guardar.Location = new Point(Math.Max(margen, cancelar.Left - separacion - guardar.Width), cancelar.Top);
        using var region = Ui.RoundedRect(new Rectangle(0, 0, capturaAtajo.Width, capturaAtajo.Height), capturaAtajo.Height / 2);
        capturaAtajo.Region = new Region(region);
    }

    public void Editar(ConfiguracionAtajo config)
    {
        configuracion = config;
        capturaAtajo.Text = FormatearAtajo(config);
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

    void GuardarCambios()
    {
        if (configuracion.Tecla == Keys.None || configuracion.Modificadores == 0) return;
        Guardado?.Invoke(this, configuracion);
    }
}
