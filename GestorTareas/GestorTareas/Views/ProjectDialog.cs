namespace TrayTasks.Views;

internal sealed class ProjectCreatePanel : RoundedPanel
{
    private readonly AppTextBox nombre = new();
    private readonly Label error = new();
    private string[] existentes;
    private readonly EditorButton guardar;
    private readonly EditorButton cancelar;

    public string NombreProyecto { get; private set; } = "";
    public event Action<string>? Guardado;
    public event EventHandler? Cancelado;

    public ProjectCreatePanel(IEnumerable<string> proyectosExistentes)
    {
        existentes = proyectosExistentes.ToArray();
        Padding = new Padding(16);
        Radius = Ui.RadioEsquina;
        Fill = Ui.Tarjeta;
        BackColor = Ui.Tarjeta;
        ForeColor = Ui.Texto;

        var titulo = new Label
        {
            Text = "Nuevo proyecto",
            ForeColor = Ui.Texto,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            Location = new Point(18, 16),
            Size = new Size(340, 32)
        };
        var descripcion = new Label
        {
            Text = "Ponle un nombre para agrupar tareas relacionadas.",
            ForeColor = Ui.Gris,
            Font = new Font("Segoe UI", 9f),
            Location = new Point(18, 52),
            Size = new Size(380, 26)
        };
        nombre.Bounds = new Rectangle(18, 92, 300, 42);
        nombre.BorderStyle = BorderStyle.None;
        nombre.BackColor = Ui.Hover;
        nombre.ForeColor = Ui.Texto;
        nombre.Font = new Font("Segoe UI", 10.5f);
        nombre.MaxLength = 40;
        nombre.Location = new Point(18, 92);
        nombre.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Cancelar();
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode != Keys.Enter) return;
            GuardarProyecto();
            e.SuppressKeyPress = true;
        };

        error.ForeColor = Ui.Rojo;
        error.Font = new Font("Segoe UI", 8.5f);
        error.Location = new Point(18, 138);
        error.Size = new Size(360, 22);

        guardar = new EditorButton
        {
            Text = "Crear",
            Bounds = new Rectangle(190, 188, 86, 34),
            BackColor = Ui.Azul,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 }
        };
        guardar.Click += (_, _) => GuardarProyecto();
        cancelar = new EditorButton
        {
            Text = "Cancelar",
            Bounds = new Rectangle(284, 188, 88, 34),
            BackColor = Ui.Hover,
            ForeColor = Ui.Texto,
            Font = new Font("Segoe UI", 9.5f),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 }
        };
        cancelar.Click += (_, _) => Cancelado?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { titulo, descripcion, nombre, error, guardar, cancelar });
        Resize += (_, _) => RecolocarControles();
        RecolocarControles();
    }

    private void GuardarProyecto()
    {
        string texto = nombre.Text.Trim();
        if (texto.Length == 0)
        {
            error.Text = "Escribe un nombre para el proyecto.";
            nombre.Focus();
            return;
        }
        if (existentes.Contains(texto, StringComparer.CurrentCultureIgnoreCase))
        {
            error.Text = "Ya existe un proyecto con ese nombre.";
            nombre.SelectAll();
            nombre.Focus();
            return;
        }
        if (texto.Equals("Sin proyecto", StringComparison.CurrentCultureIgnoreCase) ||
            texto.Equals("＋ Crear proyecto…", StringComparison.CurrentCultureIgnoreCase) ||
            texto.Equals("⚙ Gestionar proyectos…", StringComparison.CurrentCultureIgnoreCase))
        {
            error.Text = "Ese nombre está reservado por la aplicación.";
            nombre.SelectAll();
            nombre.Focus();
            return;
        }

        NombreProyecto = texto;
        Guardado?.Invoke(texto);
    }

    public void ReconfigurarProyectos(IEnumerable<string> proyectosExistentes)
    {
        existentes = proyectosExistentes.ToArray();
        nombre.Clear();
        error.Text = "";
        NombreProyecto = "";
    }

    public void EnfocarNombre()
    {
        nombre.Focus();
    }

    public void Cancelar() => Cancelado?.Invoke(this, EventArgs.Empty);

    private void RecolocarControles()
    {
        if (Width < 80 || Height < 80) return;
        int ancho = Math.Max(100, Width - 36);
        nombre.Width = ancho;
        error.Width = ancho;
        nombre.Region?.Dispose();
        using var path = Ui.RoundedRect(new Rectangle(0, 0, nombre.Width, nombre.Height), Ui.RadioEsquina);
        nombre.Region = new Region(path);
        int botonTop = Math.Max(188, Height - 52);
        cancelar.Location = new Point(Math.Max(18, Width - 18 - cancelar.Width), botonTop);
        guardar.Location = new Point(Math.Max(18, cancelar.Left - guardar.Width - 8), botonTop);
    }
}
