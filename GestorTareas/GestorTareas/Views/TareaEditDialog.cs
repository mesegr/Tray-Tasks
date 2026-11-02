using TrayTasks.Models;

namespace TrayTasks.Views;

internal sealed class TareaEditDialog : Form
{
    private readonly AppTextBox titulo = new();
    private readonly DateTimePicker fecha = new();
    private readonly DateTimePicker hora = new();
    private readonly CheckBox conHora = new();
    private readonly CheckBox importante = new();

    public Tarea? Resultado { get; private set; }

    public TareaEditDialog(Tarea tarea)
    {
        Text = "Editar tarea";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Ui.Fondo;
        ForeColor = Color.White;
        ClientSize = new Size(360, 245);

        var lblTitulo = CrearEtiqueta("Tarea", 18, 16);
        titulo.Bounds = new Rectangle(18, 40, 324, 38);
        titulo.BackColor = Ui.Tarjeta;
        titulo.ForeColor = Color.White;
        titulo.BorderStyle = BorderStyle.FixedSingle;
        titulo.Text = tarea.Titulo;
        titulo.MaxLength = 100;

        var lblFecha = CrearEtiqueta("Fecha", 18, 80);
        fecha.Bounds = new Rectangle(18, 104, 150, 30);
        fecha.Format = DateTimePickerFormat.Short;
        fecha.Value = tarea.Fecha.Date;

        conHora.Text = "Con hora";
        conHora.AutoSize = true;
        conHora.Location = new Point(190, 108);
        conHora.Checked = tarea.ConHora;
        conHora.CheckedChanged += (_, _) => hora.Enabled = conHora.Checked;

        hora.Bounds = new Rectangle(18, 150, 150, 30);
        hora.Format = DateTimePickerFormat.Custom;
        hora.CustomFormat = "HH:mm";
        hora.ShowUpDown = true;
        hora.Value = tarea.ConHora ? tarea.Fecha : DateTime.Today.AddHours(12);
        hora.Enabled = tarea.ConHora;

        importante.Text = "Importante";
        importante.AutoSize = true;
        importante.Location = new Point(190, 154);
        importante.Checked = tarea.Importante;

        var aceptar = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Bounds = new Rectangle(178, 200, 78, 30) };
        var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(264, 200, 78, 30) };
        aceptar.Click += (_, _) => Guardar(tarea);

        Controls.AddRange(new Control[] { lblTitulo, titulo, lblFecha, fecha, conHora, hora, importante, aceptar, cancelar });
        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    private Label CrearEtiqueta(string texto, int x, int y) => new()
    {
        Text = texto,
        ForeColor = Ui.Gris,
        AutoSize = true,
        Location = new Point(x, y)
    };

    private void Guardar(Tarea original)
    {
        string texto = titulo.Text.Trim();
        if (texto.Length == 0)
        {
            DialogResult = DialogResult.None;
            return;
        }

        DateTime nuevaFecha = fecha.Value.Date;
        if (conHora.Checked) nuevaFecha = nuevaFecha.Add(hora.Value.TimeOfDay);

        Resultado = new Tarea
        {
            Titulo = texto,
            Fecha = nuevaFecha,
            ConHora = conHora.Checked,
            Importante = importante.Checked,
            Creada = original.Creada,
            Hecha = original.Hecha
        };
    }
}
