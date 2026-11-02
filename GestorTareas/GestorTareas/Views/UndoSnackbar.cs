namespace TrayTasks.Views;

internal sealed class UndoSnackbar : Panel
{
    private readonly Label mensaje = new();
    private readonly Label deshacer = new();
    private readonly System.Windows.Forms.Timer temporizador = new();

    public event EventHandler? DeshacerSolicitado;

    public UndoSnackbar()
    {
        BackColor = Ui.Tarjeta;
        Visible = false;
        DoubleBuffered = true;

        mensaje.ForeColor = Color.White;
        mensaje.Font = new Font("Segoe UI", 9.5f);
        mensaje.Location = new Point(14, 0);
        mensaje.AutoSize = false;
        mensaje.TextAlign = ContentAlignment.MiddleLeft;

        deshacer.Text = "Deshacer";
        deshacer.ForeColor = Ui.AzulClaro;
        deshacer.BackColor = Ui.Tarjeta;
        deshacer.Font = new Font("Segoe UI", 9.5f);
        deshacer.TextAlign = ContentAlignment.MiddleCenter;
        deshacer.AutoSize = false;
        deshacer.Cursor = Cursors.Hand;
        deshacer.Click += (_, _) =>
        {
            temporizador.Stop();
            Visible = false;
            DeshacerSolicitado?.Invoke(this, EventArgs.Empty);
        };

        Controls.Add(mensaje);
        Controls.Add(deshacer);
        temporizador.Interval = 2000;
        temporizador.Tick += (_, _) =>
        {
            temporizador.Stop();
            Visible = false;
        };
    }

    public void Mostrar(string titulo)
    {
        mensaje.Text = $"Has eliminado: {titulo}";
        MostrarActual();
    }

    public void MostrarProyecto(string nombre)
    {
        mensaje.Text = $"Proyecto eliminado: {nombre}";
        MostrarActual();
    }

    private void MostrarActual()
    {
        Visible = true;
        BringToFront();
        temporizador.Stop();
        temporizador.Start();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        const int anchoBoton = 108;
        const int margenDerecho = 12;
        mensaje.Bounds = new Rectangle(14, 0, Math.Max(0, Width - anchoBoton - margenDerecho - 24), Height);
        deshacer.Bounds = new Rectangle(Math.Max(0, Width - anchoBoton - margenDerecho), 3, anchoBoton, Math.Max(0, Height - 6));
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), Ui.RadioEsquina);
        Region = new Region(path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) temporizador.Dispose();
        base.Dispose(disposing);
    }
}
