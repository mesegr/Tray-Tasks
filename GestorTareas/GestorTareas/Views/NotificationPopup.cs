using System.Drawing.Drawing2D;

namespace TareasBandeja.Views;

/// <summary>
/// Notificación visual independiente del sistema de notificaciones de Windows.
/// </summary>
internal sealed class NotificationPopup : Form
{
    private readonly System.Windows.Forms.Timer cierre = new();

    public NotificationPopup(string titulo, string mensaje)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Ui.Tarjeta;
        Size = new Size(360, 120);
        DoubleBuffered = true;
        Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, Width, Height), 14));

        var indicador = new Panel
        {
            BackColor = Ui.Azul,
            Location = new Point(0, 16),
            Size = new Size(4, 88)
        };

        var lblTitulo = new Label
        {
            AutoSize = false,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Text = titulo,
            Location = new Point(22, 13),
            Size = new Size(320, 25)
        };

        var lblMensaje = new Label
        {
            AutoSize = false,
            ForeColor = Ui.Gris,
            Font = new Font("Segoe UI", 10f),
            Text = mensaje,
            Location = new Point(22, 42),
            Size = new Size(320, 64),
            AutoEllipsis = false
        };

        Controls.Add(indicador);
        Controls.Add(lblTitulo);
        Controls.Add(lblMensaje);

        cierre.Interval = 6000;
        cierre.Tick += (_, _) => Close();
        Shown += (_, _) =>
        {
            var area = Screen.PrimaryScreen!.WorkingArea;
            Location = new Point(area.Right - Width - 16, area.Bottom - Height - 16);
            cierre.Start();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 14);
        using var fondo = new SolidBrush(Ui.Tarjeta);
        using var borde = new Pen(Ui.Separador, 1f);
        e.Graphics.FillPath(fondo, path);
        e.Graphics.DrawPath(borde, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) cierre.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// Mantiene las notificaciones en cola para que varias no se oculten entre sí.
/// </summary>
internal static class Notificador
{
    private static readonly Queue<(string Titulo, string Mensaje)> cola = new();
    private static NotificationPopup? actual;

    public static void Mostrar(string titulo, string mensaje)
    {
        cola.Enqueue((titulo, mensaje));
        MostrarSiguiente();
    }

    private static void MostrarSiguiente()
    {
        if (actual != null || cola.Count == 0) return;

        var aviso = cola.Dequeue();
        actual = new NotificationPopup(aviso.Titulo, aviso.Mensaje);
        actual.FormClosed += (_, _) =>
        {
            actual?.Dispose();
            actual = null;
            MostrarSiguiente();
        };
        actual.Show();
        actual.BringToFront();
    }
}
