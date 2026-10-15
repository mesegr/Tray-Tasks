using System.Globalization;
using TareasBandeja.Models;

namespace TareasBandeja.Views;

internal sealed class TareaEditPanel : RoundedPanel
{
    private readonly Label encabezado = new();
    private readonly TextBox titulo = new();
    private readonly TextBox fechaVisible = new();
    private readonly MaskedTextBox hora = new();
    private readonly Form calendarioPopup = new();
    private readonly MiniCalendar calendario = new();
    private readonly Chip conHora = new();
    private readonly Chip importante = new();
    private readonly Label etiquetaRecurrencia = new();
    private readonly SelectorRedondeado recurrencia = new();
    private readonly Button guardar;
    private readonly Button cancelar;
    private Tarea? original;
    private DateTime fechaElegida = DateTime.Today;

    public event EventHandler<Tarea>? Guardado;
    public event EventHandler? Cancelado;

    public TareaEditPanel()
    {
        Padding = new Padding(16);
        BackColor = Ui.Tarjeta;
        Fill = Ui.Tarjeta;
        Radius = Ui.RadioEsquina;

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

        fechaVisible.Bounds = new Rectangle(18, 108, 130, 36);
        fechaVisible.BackColor = Ui.Hover;
        fechaVisible.ForeColor = Ui.Texto;
        fechaVisible.BorderStyle = BorderStyle.None;
        fechaVisible.Font = new Font("Segoe UI", 11f);
        fechaVisible.ReadOnly = true;
        fechaVisible.TextAlign = HorizontalAlignment.Center;
        fechaVisible.Cursor = Cursors.Hand;
        fechaVisible.Click += (_, _) => MostrarCalendario();

        calendario.Radius = Ui.RadioEsquina;
        calendario.Bounds = new Rectangle(0, 0, 300, 270);
        calendario.Seleccionado = fechaElegida;
        calendario.DiaElegido += (_, seleccion) =>
        {
            fechaElegida = seleccion.Date;
            fechaVisible.Text = fechaElegida.ToString("dd/MM/yyyy");
            calendarioPopup.Hide();
        };
        calendarioPopup.FormBorderStyle = FormBorderStyle.None;
        calendarioPopup.ShowInTaskbar = false;
        calendarioPopup.StartPosition = FormStartPosition.Manual;
        calendarioPopup.Size = new Size(300, 270);
        calendarioPopup.BackColor = Ui.Fondo;
        calendarioPopup.TopMost = true;
        calendarioPopup.Controls.Add(calendario);
        calendarioPopup.Deactivate += (_, _) => calendarioPopup.Hide();

        conHora.Text = "Con hora";
        conHora.ConCasilla = true;
        conHora.ColorInactivo = Ui.Tarjeta;
        conHora.SoloCirculoMarcado = true;
        conHora.SinResaltadoHover = true;
        conHora.Font = new Font("Segoe UI", 11f);
        conHora.AutoSize = true;
        conHora.Location = new Point(154, 108);
        conHora.Height = 36;
        conHora.Click += (_, _) => hora.ReadOnly = !conHora.Checked;

        hora.Bounds = new Rectangle(18, 152, 130, 36);
        hora.Mask = "00:00";
        hora.PromptChar = '_';
        hora.HidePromptOnLeave = false;
        hora.BackColor = Ui.Hover;
        hora.ForeColor = Ui.Texto;
        hora.BorderStyle = BorderStyle.None;
        hora.Font = new Font("Segoe UI", 11f);
        hora.TextAlign = HorizontalAlignment.Center;
        hora.Text = "12:00";
        hora.ReadOnly = true;

        importante.Text = "Importante";
        importante.ConCasilla = true;
        importante.ColorActivo = Ui.Naranja;
        importante.ColorInactivo = Ui.Tarjeta;
        importante.SoloCirculoMarcado = true;
        importante.SinResaltadoHover = true;
        importante.Font = new Font("Segoe UI", 11f);
        importante.AutoSize = true;
        importante.Location = new Point(154, 152);
        importante.Height = 36;

        etiquetaRecurrencia.Text = "Repetición";
        etiquetaRecurrencia.AutoSize = true;
        etiquetaRecurrencia.ForeColor = Ui.Gris;
        etiquetaRecurrencia.Font = new Font("Segoe UI", 8.5f);
        etiquetaRecurrencia.Location = new Point(18, 198);

        recurrencia.Bounds = new Rectangle(18, 218, 254, 36);
        recurrencia.BackColor = Ui.Hover;
        recurrencia.ForeColor = Color.White;
        recurrencia.Font = new Font("Segoe UI", 11f);
        recurrencia.Items.AddRange(new object[] { "No se repite", "Cada día", "Cada semana", "Cada mes" });
        recurrencia.SelectedIndex = 0;

        guardar = CrearBoton("Guardar", Ui.Azul, 112, 252, 86);
        guardar.Click += (_, _) => GuardarCambios();
        cancelar = CrearBoton("Cancelar", Ui.Hover, 206, 252, 84);
        cancelar.Click += (_, _) => Cancelado?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { encabezado, titulo, fechaVisible, conHora, hora, importante, etiquetaRecurrencia, recurrencia, guardar, cancelar });
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        titulo.Width = Math.Max(100, Width - 36);
        titulo.Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, titulo.Width, titulo.Height), Ui.RadioEsquina));
        fechaVisible.Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, fechaVisible.Width, fechaVisible.Height), Ui.RadioEsquina));
        hora.Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, hora.Width, hora.Height), Ui.RadioEsquina));
        recurrencia.Width = Math.Max(140, Width - 36);
        cancelar.Left = Math.Max(18, Width - cancelar.Width - 18);
        guardar.Left = Math.Max(18, cancelar.Left - guardar.Width - 8);
        guardar.Top = cancelar.Top = Math.Max(18, Height - 18 - cancelar.Height);
    }

    public void Editar(Tarea tarea)
    {
        original = tarea;
        titulo.Text = tarea.Titulo;
        fechaElegida = tarea.Fecha.Date;
        fechaVisible.Text = fechaElegida.ToString("dd/MM/yyyy");
        calendario.Seleccionado = fechaElegida;
        conHora.Checked = tarea.ConHora;
        hora.Text = (tarea.ConHora ? tarea.Fecha : DateTime.Today.AddHours(12)).ToString("HH:mm");
        hora.ReadOnly = !tarea.ConHora;
        importante.Checked = tarea.Importante;
        recurrencia.SelectedIndex = Math.Clamp((int)tarea.Recurrencia, 0, 3);
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

        DateTime nuevaFecha = fechaElegida.Date;
        if (conHora.Checked)
        {
            if (!TimeSpan.TryParseExact(hora.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var horaElegida)) return;
            nuevaFecha = nuevaFecha.Add(horaElegida);
        }

        Guardado?.Invoke(this, new Tarea
        {
            Titulo = titulo.Text.Trim(),
            Fecha = nuevaFecha,
            ConHora = conHora.Checked,
            Importante = importante.Checked,
            Creada = original.Creada,
            Hecha = original.Hecha,
            Recurrencia = (RecurrenciaTarea)Math.Max(0, recurrencia.SelectedIndex),
            FechasCompletadas = original.FechasCompletadas
        });
    }

    private void MostrarCalendario()
    {
        calendario.Seleccionado = fechaElegida;
        var ubicacion = fechaVisible.PointToScreen(new Point(0, fechaVisible.Height + 4));
        var area = Screen.FromControl(this).WorkingArea;
        if (ubicacion.Y + calendarioPopup.Height > area.Bottom)
            ubicacion.Y = fechaVisible.PointToScreen(Point.Empty).Y - calendarioPopup.Height - 4;
        if (ubicacion.X + calendarioPopup.Width > area.Right)
            ubicacion.X = area.Right - calendarioPopup.Width;
        calendarioPopup.Location = ubicacion;
        if (!calendarioPopup.Visible) calendarioPopup.Show(FindForm());
        calendarioPopup.BringToFront();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) calendarioPopup.Hide();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) calendarioPopup.Dispose();
        base.Dispose(disposing);
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
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Ui.RadioEsquina);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
