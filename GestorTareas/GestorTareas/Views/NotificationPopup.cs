using System.Drawing.Drawing2D;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using TrayTasks.Diagnostics;

namespace TrayTasks.Views;

internal sealed class PopupActionButton : Control
{
    private readonly Color fondo;
    private readonly Color texto;
    private bool hover;
    private bool pulsado;

    public PopupActionButton(string etiqueta, Color fondo, Color texto)
    {
        Text = etiqueta;
        this.fondo = fondo;
        this.texto = texto;
        Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Size = new Size(TextRenderer.MeasureText(etiqueta, Font).Width + 24, 30);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
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
        Color color = hover || pulsado ? ControlPaint.Light(fondo, pulsado ? 0.08f : 0.16f) : fondo;
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, texto,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

/// <summary>
/// Notificación visual independiente del sistema de notificaciones de Windows.
/// </summary>
internal sealed class NotificationPopup : Form
{
    private readonly System.Windows.Forms.Timer animacion = new();
    private readonly System.Windows.Forms.Timer cierre = new();
    private bool saliendo;
    private bool tieneAcciones;

    public NotificationPopup(string titulo, string mensaje, bool reproducirSonido, Action? accion = null,
        string? textoPosponer = null, Action? posponer = null)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Ui.Tarjeta;
        Size = new Size(360, 120);
        DoubleBuffered = true;
        Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina));

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

        int altoMensaje = TextRenderer.MeasureText(
            mensaje,
            lblMensaje.Font,
            new Size(320, 0),
            TextFormatFlags.WordBreak).Height;
        tieneAcciones = accion != null || posponer != null;
        int altoPopup = tieneAcciones ? Math.Max(132, 96 + altoMensaje) : Math.Max(92, 58 + altoMensaje);

        Size = new Size(360, altoPopup);
        indicador.Height = altoPopup - 32;
        lblMensaje.Height = altoMensaje;
        Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina));

        Controls.Add(indicador);
        Controls.Add(lblTitulo);
        Controls.Add(lblMensaje);
        const int margenHorizontal = 18;
        const int separacionAcciones = 8;
        int yAcciones = altoPopup - 42;
        int bordeDerecho = Width - margenHorizontal;
        if (accion != null)
        {
            var abrir = new PopupActionButton("Abrir tarea", Ui.Azul, Color.White);
            abrir.Location = new Point(bordeDerecho - abrir.Width, yAcciones);
            abrir.Click += (_, _) => { accion(); CerrarAnimado(); };
            Controls.Add(abrir);
            bordeDerecho = abrir.Left - separacionAcciones;
        }

        if (posponer != null)
        {
            var aplazar = new PopupActionButton(textoPosponer ?? "Posponer", Ui.Hover, Ui.Texto);
            aplazar.Location = new Point(bordeDerecho - aplazar.Width, yAcciones);
            aplazar.Click += (_, _) => { posponer(); CerrarAnimado(); };
            Controls.Add(aplazar);
        }

        animacion.Interval = 15;
        animacion.Tick += Mover;
        cierre.Tick += (_, _) => CerrarAnimado();
        Shown += (_, _) => { if (reproducirSonido) NotificationSound.Play(); };
    }

    public void PrepararEntrada()
    {
        Opacity = 0;
    }

    public void AnimarHasta(Point destino)
    {
        Location = destino;
        if (saliendo || Opacity >= 1) return;

        // La posición se fija directamente; solo se anima la opacidad.
        saliendo = false;
        animacion.Start();
    }

    public void ProgramarCierre(int milisegundos)
    {
        cierre.Interval = tieneAcciones ? Math.Max(milisegundos, 10000) : milisegundos;
        cierre.Start();
    }

    public void CerrarAnimado()
    {
        if (saliendo || IsDisposed) return;
        saliendo = true;
        cierre.Stop();
        animacion.Start();
    }

    private void Mover(object? sender, EventArgs e)
    {
        if (saliendo)
        {
            Opacity = Math.Max(0, Opacity - 0.16);
            if (Opacity <= 0) { animacion.Stop(); Close(); }
            return;
        }

        Opacity = Math.Min(1, Opacity + 0.16);
        if (Opacity >= 1) animacion.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Ui.RadioEsquina);
        using var fondo = new SolidBrush(Ui.Tarjeta);
        using var borde = new Pen(Ui.Separador, 1f);
        e.Graphics.FillPath(fondo, path);
        e.Graphics.DrawPath(borde, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { animacion.Dispose(); cierre.Dispose(); }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Chime corto generado por la aplicación, independiente de los sonidos de Windows.
/// </summary>
internal static class NotificationSound
{
    private const string Alias = "TareasBandejaNotification";
    private static readonly string rutaMp3 = Path.Combine(AppContext.BaseDirectory, "Sounds", "notification.mp3");
    private static readonly MemoryStream audio = new(CrearWav());
    private static readonly SoundPlayer reproductor = CrearReproductor();

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(
        string command,
        StringBuilder? returnValue,
        int returnValueLength,
        IntPtr callback);

    public static void Play()
    {
        try
        {
            if (!File.Exists(rutaMp3)) return;

            // MCI permite reproducir MP3 sin añadir librerías externas al proyecto.
            mciSendString($"close {Alias}", null, 0, IntPtr.Zero);
            mciSendString($"open \"{rutaMp3}\" type mpegvideo alias {Alias}", null, 0, IntPtr.Zero);
            mciSendString($"play {Alias}", null, 0, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            AppLogger.Advertencia("No se pudo reproducir el sonido de una notificación.", ex);
        }
    }

    private static SoundPlayer CrearReproductor()
    {
        var player = new SoundPlayer(audio);
        player.Load();
        return player;
    }

    private static byte[] CrearWav()
    {
        const int sampleRate = 44100;
        const int duracion = 560;
        int muestras = sampleRate * duracion / 1000;
        var datos = new byte[muestras * 2];

        // Arpegio ascendente suave: Do, Mi, Sol y Do agudo.
        (double frecuencia, double inicio, double fin)[] notas =
        {
            (523.25, 0.00, 0.26),
            (659.25, 0.09, 0.35),
            (783.99, 0.18, 0.44),
            (1046.50, 0.28, 0.56)
        };

        using var contenido = new MemoryStream();
        using (var writer = new BinaryWriter(contenido, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + datos.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(datos.Length);

            for (int i = 0; i < muestras; i++)
            {
                double tiempo = (double)i / sampleRate;
                double valor = 0;

                foreach (var nota in notas)
                {
                    if (tiempo < nota.inicio || tiempo >= nota.fin) continue;

                    double transcurrido = tiempo - nota.inicio;
                    double envolvente = Math.Min(1, transcurrido / 0.012) * Math.Exp(-transcurrido * 5.2);
                    valor += Math.Sin(2 * Math.PI * nota.frecuencia * transcurrido) * envolvente * 0.22;
                }

                short muestra = (short)(Math.Clamp(valor, -1, 1) * short.MaxValue);
                writer.Write(muestra);
            }
        }

        return contenido.ToArray();
    }
}

/// <summary>
/// Mantiene las notificaciones en cola para que varias no se oculten entre sí.
/// </summary>
internal static class Notificador
{
    private const int MaximoVisibles = 3;
    private const int Separacion = 8;
    public static int DuracionAviso { get; set; } = 2000;
    public static int Posicion { get; set; }

    private static readonly Queue<(string Titulo, string Mensaje, bool Sonido, int Duracion, Action? Accion, string? TextoPosponer, Action? Posponer)> pendientes = new();
    private static readonly Queue<(string Titulo, string Mensaje, bool Sonido, int Duracion, Action? Accion, string? TextoPosponer, Action? Posponer)> colaSecuencial = new();
    private static readonly Queue<(string Titulo, string Mensaje, bool Sonido, int Duracion, Action? Accion, string? TextoPosponer, Action? Posponer)> colaApilada = new();
    private static readonly List<NotificationPopup> activas = new();
    private static System.Windows.Forms.Timer? temporizadorLote;
    private static System.Windows.Forms.Timer? temporizadorApilado;
    private static NotificationPopup? secuencialActual;

    public static void Mostrar(string titulo, string mensaje, bool reproducirSonido = true, int duracionMs = 2000,
        Action? accion = null, string? textoPosponer = null, Action? posponer = null)
    {
        pendientes.Enqueue((titulo, mensaje, reproducirSonido, Math.Clamp(duracionMs, 1000, 15000), accion, textoPosponer, posponer));

        // Espera unos milisegundos para agrupar avisos generados en el mismo ciclo
        // y decidir si corresponde reproducir uno o varios sonidos.
        if (temporizadorLote != null) return;

        temporizadorLote = new System.Windows.Forms.Timer { Interval = 250 };
        temporizadorLote.Tick += (_, _) =>
        {
            temporizadorLote.Stop();
            temporizadorLote.Dispose();
            temporizadorLote = null;
            MostrarLote();
        };
        temporizadorLote.Start();
    }

    private static void MostrarLote()
    {
        int cantidad = pendientes.Count;

        if (cantidad <= 2)
        {
            while (pendientes.Count > 0)
            {
                var aviso = pendientes.Dequeue();
                colaSecuencial.Enqueue(aviso);
            }

            MostrarSiguienteSecuencial();
            return;
        }

        while (pendientes.Count > 0)
        {
            colaApilada.Enqueue(pendientes.Dequeue());
        }

        if (temporizadorApilado == null)
        {
            for (int i = 0; i < MaximoVisibles && colaApilada.Count > 0; i++)
                AgregarApilada(reproducirSonido: i == 0);

            temporizadorApilado = new System.Windows.Forms.Timer { Interval = DuracionAviso };
            temporizadorApilado.Tick += (_, _) => AvanzarApiladas();
            temporizadorApilado.Start();
        }

        Reordenar();
    }

    private static void MostrarSiguienteSecuencial()
    {
        if (secuencialActual != null || colaSecuencial.Count == 0) return;

        var aviso = colaSecuencial.Dequeue();
        secuencialActual = new NotificationPopup(aviso.Titulo, aviso.Mensaje, aviso.Sonido,
            aviso.Accion, aviso.TextoPosponer, aviso.Posponer);
        secuencialActual.FormClosed += (_, _) =>
        {
            secuencialActual = null;
            MostrarSiguienteSecuencial();
        };
        secuencialActual.PrepararEntrada();
        secuencialActual.Show();
        secuencialActual.BringToFront();
        secuencialActual.AnimarHasta(PosicionInferior(secuencialActual));
        secuencialActual.ProgramarCierre(aviso.Duracion);
    }

    private static void AgregarApilada(bool reproducirSonido)
    {
        if (colaApilada.Count == 0) return;

        var aviso = colaApilada.Dequeue();
        var popup = new NotificationPopup(aviso.Titulo, aviso.Mensaje, reproducirSonido && aviso.Sonido,
            aviso.Accion, aviso.TextoPosponer, aviso.Posponer);
        popup.PrepararEntrada();
        activas.Add(popup);
        popup.Show();
        popup.BringToFront();
        popup.ProgramarCierre(aviso.Duracion);
    }

    private static void AvanzarApiladas()
    {
        if (activas.Count > 0)
        {
            var antigua = activas[0];
            activas.RemoveAt(0);
            antigua.CerrarAnimado();
        }

        if (colaApilada.Count > 0)
            AgregarApilada(reproducirSonido: false);

        Reordenar();

        if (activas.Count == 0 && colaApilada.Count == 0 && temporizadorApilado != null)
        {
            temporizadorApilado.Stop();
            temporizadorApilado.Dispose();
            temporizadorApilado = null;
        }
    }

    private static void Reordenar()
    {
        int distancia = 0;

        bool arriba = Posicion >= 2;
        int inicio = arriba ? 0 : activas.Count - 1;
        int fin = arriba ? activas.Count : -1;
        int paso = arriba ? 1 : -1;
        for (int i = inicio; i != fin; i += paso)
        {
            var popup = activas[i];
            if (popup.IsDisposed) continue;

            var area = Screen.PrimaryScreen!.WorkingArea;
            bool izquierda = Posicion is 1 or 3;
            var destino = new Point(
                izquierda ? area.Left + 16 : area.Right - popup.Width - 16,
                arriba ? area.Top + 16 + distancia : area.Bottom - popup.Height - 16 - distancia);
            popup.AnimarHasta(destino);
            distancia += popup.Height + Separacion;
        }
    }

    private static Point PosicionInferior(NotificationPopup popup)
    {
        var area = Screen.PrimaryScreen!.WorkingArea;
        bool izquierda = Posicion is 1 or 3;
        bool arriba = Posicion >= 2;
        return new Point(izquierda ? area.Left + 16 : area.Right - popup.Width - 16,
            arriba ? area.Top + 16 : area.Bottom - popup.Height - 16);
    }
}
