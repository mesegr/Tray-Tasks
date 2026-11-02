using System.Drawing.Drawing2D;

namespace TrayTasks.Views;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "Acerca de TrayTasks";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Ui.Tarjeta;
        ForeColor = Ui.Texto;
        ClientSize = new Size(560, 570);
        var areaTrabajo = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(areaTrabajo.Left + (areaTrabajo.Width - Width) / 2,
            areaTrabajo.Top + (areaTrabajo.Height - Height) / 2);
        MinimizeBox = false;
        MaximizeBox = false;
        DoubleBuffered = true;
        Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina));

        var titulo = new Label
        {
            Text = "Acerca de TrayTasks",
            ForeColor = Ui.Texto,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Location = new Point(24, 20),
            Size = new Size(450, 30)
        };

        var cerrar = new CloseButton
        {
            Bounds = new Rectangle(512, 16, 32, 32),
            BackColor = Ui.Tarjeta
        };
        cerrar.Click += (_, _) => Close();

        var marca = new Label
        {
            Text = "T",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 21f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Ui.Azul,
            Bounds = new Rectangle(24, 68, 50, 50)
        };
        using (var path = Ui.RoundedRect(new Rectangle(0, 0, marca.Width, marca.Height), 14))
            marca.Region = new Region(path);

        var nombre = new Label
        {
            Text = "TrayTasks",
            ForeColor = Ui.Texto,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(92, 63),
            Size = new Size(420, 36)
        };
        var lema = new Label
        {
            Text = "Tus tareas y recordatorios, siempre a mano.",
            ForeColor = Ui.Gris,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(93, 105),
            Size = new Size(420, 24)
        };

        Controls.AddRange(new Control[] { titulo, cerrar, marca, nombre, lema });
        AgregarBloque(148, "Planifica cada día",
            "Crea tareas con fecha, hora e importancia. Usa el calendario y los filtros para ver qué toca.");
        AgregarBloque(238, "Avisos a tu ritmo",
            "Configura varios avisos previos. Posponer programa otro aviso con el siguiente intervalo menor.");
        AgregarBloque(328, "Repite sin volver a crear",
            "Programa tareas diarias, semanales o mensuales y completa cada ocurrencia por separado.");
        AgregarBloque(418, "Siempre a mano y bajo tu control",
            "Abre la app desde la bandeja o con tu atajo. Exporta una copia de tus tareas y restáurala cuando quieras.");

        string versionApp = Application.ProductVersion;
        if (string.IsNullOrWhiteSpace(versionApp))
            versionApp = typeof(AboutForm).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        var version = new Label
        {
            Text = $"Versión {versionApp}",
            ForeColor = Ui.Gris,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(24, 526),
            Size = new Size(300, 30)
        };
        var listo = new AboutButton("Listo")
        {
            Location = new Point(434, 520),
            Size = new Size(102, 36)
        };
        listo.Click += (_, _) => Close();
        Controls.AddRange(new Control[] { version, listo });

        AcceptButton = listo;
        CancelButton = listo;
    }

    private void AgregarBloque(int top, string titulo, string descripcion)
    {
        var bloque = new RoundedPanel
        {
            Bounds = new Rectangle(24, top, 512, 78),
            BackColor = Ui.Hover,
            Fill = Ui.Hover,
            Radius = Ui.RadioEsquina
        };
        var encabezado = new Label
        {
            Text = titulo,
            ForeColor = Ui.Texto,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoSize = false,
            Location = new Point(18, 10),
            Size = new Size(476, 22)
        };
        var detalle = new Label
        {
            Text = descripcion,
            ForeColor = Ui.Gris,
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = false,
            Location = new Point(18, 35),
            Size = new Size(476, 36)
        };
        bloque.Controls.AddRange(new Control[] { encabezado, detalle });
        Controls.Add(bloque);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Width < 2 || Height < 2) return;
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina);
        var regionAnterior = Region;
        Region = new Region(path);
        regionAnterior?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Ui.RadioEsquina);
        using var borde = new Pen(Ui.Separador, 1f);
        e.Graphics.DrawPath(borde, path);
    }
}

internal sealed class AboutButton : Button
{
    private bool hover;
    private bool pulsado;

    public AboutButton(string texto)
    {
        Text = texto;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; pulsado = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); pulsado = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pulsado = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2);
        using var fondo = new SolidBrush(hover || pulsado ? ControlPaint.Light(Ui.Azul, 0.12f) : Ui.Azul);
        g.FillPath(fondo, path);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
