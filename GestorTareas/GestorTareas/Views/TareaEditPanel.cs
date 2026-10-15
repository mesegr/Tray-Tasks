using TareasBandeja.Models;

namespace TareasBandeja.Views;

internal sealed class TareaEditPanel : RoundedPanel
{
    private readonly Label encabezado = new();
    private readonly TextBox titulo = new();
    private readonly DateTimePicker fecha = new();
    private readonly DateTimePicker hora = new();
    private readonly Chip conHora = new();
    private readonly Chip importante = new();
    private readonly Button guardar;
    private readonly Button cancelar;
    private Tarea? original;

    public event EventHandler<Tarea>? Guardado;
    public event EventHandler? Cancelado;

    public TareaEditPanel()
    {
        Padding = new Padding(16);
        BackColor = Ui.Tarjeta;
        Fill = Ui.Tarjeta;
        Radius = 16;

        encabezado.Text = "Editar tarea";
        encabezado.ForeColor = Color.White;
        encabezado.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        encabezado.AutoSize = true;
        encabezado.Location = new Point(18, 16);

        titulo.Bounds = new Rectangle(18, 58, 272, 32);
        titulo.BackColor = Ui.Hover;
        titulo.ForeColor = Color.White;
        titulo.BorderStyle = BorderStyle.None;
        titulo.Font = new Font("Segoe UI", 10.5f);
        titulo.Padding = new Padding(10, 4, 10, 0);
        titulo.Margin = new Padding(0);
        titulo.MaxLength = 100;

        fecha.Bounds = new Rectangle(18, 108, 130, 32);
        fecha.Format = DateTimePickerFormat.Short;
        fecha.BackColor = Ui.Hover;
        fecha.ForeColor = Color.White;
        fecha.Font = new Font("Segoe UI", 9.5f);
        fecha.CalendarMonthBackground = Ui.Tarjeta;
        fecha.CalendarTitleBackColor = Ui.Azul;
        fecha.CalendarForeColor = Color.White;
        fecha.CalendarTitleForeColor = Color.White;

        conHora.Text = "Con hora";
        conHora.ConCasilla = true;
        conHora.ColorInactivo = Ui.Tarjeta;
        conHora.SoloCirculoMarcado = true;
        conHora.SinResaltadoHover = true;
        conHora.Font = new Font("Segoe UI", 9.5f);
        conHora.AutoSize = true;
        conHora.Location = new Point(154, 108);
        conHora.Height = 32;
        conHora.Click += (_, _) => hora.Enabled = conHora.Checked;

        hora.Bounds = new Rectangle(18, 152, 130, 32);
        hora.Format = DateTimePickerFormat.Custom;
        hora.CustomFormat = "HH:mm";
        hora.ShowUpDown = true;
        hora.BackColor = Ui.Hover;
        hora.ForeColor = Color.White;
        hora.Font = new Font("Segoe UI", 9.5f);
        hora.CalendarMonthBackground = Ui.Tarjeta;
        hora.CalendarForeColor = Color.White;

        importante.Text = "Importante";
        importante.ConCasilla = true;
        importante.ColorActivo = Ui.Naranja;
        importante.ColorInactivo = Ui.Tarjeta;
        importante.SoloCirculoMarcado = true;
        importante.SinResaltadoHover = true;
        importante.Font = new Font("Segoe UI", 9.5f);
        importante.AutoSize = true;
        importante.Location = new Point(154, 152);
        importante.Height = 32;

        guardar = CrearBoton("Guardar", Ui.Azul, 112, 228, 86);
        guardar.Click += (_, _) => GuardarCambios();
        cancelar = CrearBoton("Cancelar", Ui.Hover, 206, 228, 84);
        cancelar.Click += (_, _) => Cancelado?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { encabezado, titulo, fecha, conHora, hora, importante, guardar, cancelar });
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        titulo.Width = Math.Max(100, Width - 36);
        titulo.Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, titulo.Width, titulo.Height), titulo.Height / 2));
        cancelar.Left = Math.Max(18, Width - cancelar.Width - 18);
        guardar.Left = Math.Max(18, cancelar.Left - guardar.Width - 8);
    }

    public void Editar(Tarea tarea)
    {
        original = tarea;
        titulo.Text = tarea.Titulo;
        fecha.Value = tarea.Fecha.Date;
        conHora.Checked = tarea.ConHora;
        hora.Value = tarea.ConHora ? tarea.Fecha : DateTime.Today.AddHours(12);
        hora.Enabled = tarea.ConHora;
        importante.Checked = tarea.Importante;
        titulo.Focus();
        titulo.SelectAll();
    }

    private Button CrearBoton(string texto, Color fondo, int x, int y, int ancho) => new EditorButton
    {
        Text = texto,
        Bounds = new Rectangle(x, y, ancho, 34),
        BackColor = fondo,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 9.5f),
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 }
    };

    private void GuardarCambios()
    {
        if (original == null || string.IsNullOrWhiteSpace(titulo.Text)) return;

        DateTime nuevaFecha = fecha.Value.Date;
        if (conHora.Checked) nuevaFecha = nuevaFecha.Add(hora.Value.TimeOfDay);

        Guardado?.Invoke(this, new Tarea
        {
            Titulo = titulo.Text.Trim(),
            Fecha = nuevaFecha,
            ConHora = conHora.Checked,
            Importante = importante.Checked,
            Creada = original.Creada,
            Hecha = original.Hecha
        });
    }
}

internal sealed class EditorButton : Button
{
    public EditorButton()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Ui.Fondo);
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
