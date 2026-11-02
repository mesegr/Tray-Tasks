namespace TrayTasks.Views;

internal sealed class ProjectManagerPanel : RoundedPanel
{
    private readonly Label titulo = new();
    private readonly Label descripcion = new();
    private readonly Label error = new();
    private readonly SelectorRedondeado selector = new();
    private readonly AppTextBox nombre = new();
    private readonly EditorButton guardar;
    private readonly EditorButton nuevo;
    private readonly EditorButton eliminar;
    private readonly EditorButton cancelar;
    private bool modoNuevo;

    public event Action<string>? CrearSolicitado;
    public event Action<string, string>? RenombrarSolicitado;
    public event Action<string>? EliminarSolicitado;
    public event EventHandler? Cancelado;

    public ProjectManagerPanel()
    {
        Padding = new Padding(16);
        Radius = Ui.RadioEsquina;
        Fill = BackColor = Ui.Tarjeta;

        titulo.Text = "Proyectos";
        titulo.AutoSize = true;
        titulo.ForeColor = Ui.Texto;
        titulo.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
        titulo.Location = new Point(18, 16);
        descripcion.Text = "Organiza tus tareas en proyectos.";
        descripcion.AutoSize = true;
        descripcion.ForeColor = Ui.Gris;
        descripcion.Font = new Font("Segoe UI", 9f);
        descripcion.Location = new Point(18, 48);

        selector.Bounds = new Rectangle(18, 84, 300, 36);
        selector.BackColor = Ui.Hover;
        selector.Font = new Font("Segoe UI", 10f);
        selector.SelectedIndexChanged += (_, _) => CargarNombreSeleccionado();

        nombre.Bounds = new Rectangle(18, 132, 300, 42);
        nombre.BorderStyle = BorderStyle.None;
        nombre.BackColor = Ui.Hover;
        nombre.ForeColor = Ui.Texto;
        nombre.Font = new Font("Segoe UI", 10.5f);
        nombre.MaxLength = 40;
        error.Location = new Point(18, 180);
        error.AutoSize = true;
        error.ForeColor = Ui.Rojo;
        error.Font = new Font("Segoe UI", 8.5f);

        nuevo = CrearBoton("Nuevo", Ui.Hover, 18, 218, 76);
        nuevo.Click += (_, _) => IniciarNuevo();
        guardar = CrearBoton("Guardar", Ui.Azul, 102, 218, 86);
        guardar.Click += (_, _) => Guardar();
        eliminar = CrearBoton("Eliminar", Ui.Hover, 196, 218, 82);
        eliminar.AccessibleDescription = "Eliminar el proyecto seleccionado";
        eliminar.Click += (_, _) =>
        {
            if (selector.SelectedIndex < 0) return;
            EliminarSolicitado?.Invoke(selector.SelectedItem?.ToString() ?? "");
        };
        cancelar = CrearBoton("Cerrar", Ui.Hover, 290, 218, 82);
        cancelar.Click += (_, _) => Cancelado?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { titulo, descripcion, selector, nombre, error, nuevo, guardar, eliminar, cancelar });
        Resize += (_, _) => Recolocar();
        Recolocar();
    }

    public void CargarProyectos(IEnumerable<string> proyectos, string? seleccionar = null)
    {
        modoNuevo = false;
        selector.Items.Clear();
        selector.Items.AddRange(proyectos.Cast<object>().ToArray());
        int indice = selector.Items.FindIndex(p => string.Equals(p?.ToString(), seleccionar, StringComparison.CurrentCultureIgnoreCase));
        selector.SelectedIndex = indice >= 0 ? indice : selector.Items.Count > 0 ? 0 : -1;
        selector.Enabled = selector.Items.Count > 0;
        eliminar.Enabled = selector.Items.Count > 0;
        CargarNombreSeleccionado();
        ActualizarBotones();
        error.Text = "";
    }

    public void MostrarError(string mensaje) => error.Text = mensaje;
    public void Cancelar() => Cancelado?.Invoke(this, EventArgs.Empty);

    private void CargarNombreSeleccionado()
    {
        if (!modoNuevo && selector.SelectedIndex >= 0)
            nombre.Text = selector.SelectedItem?.ToString() ?? "";
        else if (!modoNuevo)
            nombre.Clear();
    }

    private void IniciarNuevo()
    {
        modoNuevo = true;
        selector.SelectedIndex = -1;
        nombre.Clear();
        error.Text = "";
        ActualizarBotones();
        nombre.Focus();
    }

    private void Guardar()
    {
        string valor = nombre.Text.Trim();
        if (valor.Length == 0) { MostrarError("Escribe un nombre para el proyecto."); nombre.Focus(); return; }
        if (valor.Length > 40) { MostrarError("Máximo 40 caracteres."); return; }
        if (modoNuevo) CrearSolicitado?.Invoke(valor);
        else if (selector.SelectedIndex >= 0) RenombrarSolicitado?.Invoke(selector.SelectedItem?.ToString() ?? "", valor);
    }

    private void ActualizarBotones()
    {
        eliminar.Visible = true;
        guardar.Text = modoNuevo ? "Crear" : "Guardar";
        eliminar.Enabled = selector.SelectedIndex >= 0 && !modoNuevo;
        Recolocar();
    }

    private EditorButton CrearBoton(string texto, Color fondo, int x, int y, int ancho) => new()
    {
        Text = texto,
        Bounds = new Rectangle(x, y, ancho, 34),
        BackColor = fondo,
        ForeColor = Ui.Texto,
        Font = new Font("Segoe UI", 9.5f),
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 }
    };

    private void Recolocar()
    {
        if (Width < 80 || Height < 80) return;
        int ancho = Math.Max(100, Width - 36);
        selector.Width = nombre.Width = ancho;
        nombre.Region?.Dispose();
        using var path = Ui.RoundedRect(new Rectangle(0, 0, nombre.Width, nombre.Height), Ui.RadioEsquina);
        nombre.Region = new Region(path);
        error.MaximumSize = new Size(ancho, 40);
        int top = Math.Max(178, Height - 52);
        int gap = 8;
        guardar.Left = Math.Max(18, Width - 18 - guardar.Width);
        cancelar.Left = Math.Max(18, guardar.Left - gap - cancelar.Width);
        eliminar.Left = Math.Max(18, cancelar.Left - gap - eliminar.Width);
        nuevo.Left = Math.Max(18, eliminar.Left - gap - nuevo.Width);
        nuevo.Top = guardar.Top = eliminar.Top = cancelar.Top = top;
    }
}
