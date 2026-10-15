namespace TareasBandeja.Views;

internal sealed class UndoSnackbar : Panel
{
    private readonly Label mensaje = new();
    private readonly Button deshacer = new();
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
        deshacer.FlatStyle = FlatStyle.Flat;
        deshacer.FlatAppearance.BorderSize = 0;
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
        Visible = true;
        BringToFront();
        temporizador.Stop();
        temporizador.Start();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        mensaje.Bounds = new Rectangle(14, 0, Math.Max(0, Width - 112), Height);
        deshacer.Bounds = new Rectangle(Width - 92, 4, 82, Math.Max(0, Height - 8));
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), Height / 2);
        Region = new Region(path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) temporizador.Dispose();
        base.Dispose(disposing);
    }
}
