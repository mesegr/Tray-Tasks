using System.Drawing.Drawing2D;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;

namespace TareasBandeja.Views;

/// <summary>
/// Notificación visual independiente del sistema de notificaciones de Windows.
/// </summary>
internal sealed class NotificationPopup : Form
{
    private readonly System.Windows.Forms.Timer animacion = new();
    private readonly System.Windows.Forms.Timer cierre = new();
    private bool saliendo;

    public NotificationPopup(string titulo, string mensaje, bool reproducirSonido)
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

        int altoMensaje = TextRenderer.MeasureText(
            mensaje,
            lblMensaje.Font,
            new Size(320, 0),
            TextFormatFlags.WordBreak).Height;
        int altoPopup = Math.Max(92, 58 + altoMensaje);

        Size = new Size(360, altoPopup);
        indicador.Height = altoPopup - 32;
        lblMensaje.Height = altoMensaje;
        Region = new Region(Ui.RoundedRect(new Rectangle(0, 0, Width, Height), 14));

        Controls.Add(indicador);
        Controls.Add(lblTitulo);
        Controls.Add(lblMensaje);

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
        cierre.Interval = milisegundos;
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
        using var path = Ui.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 14);
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
        catch
        {
            // Una notificación nunca debe fallar porque el dispositivo de audio no esté disponible.
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
    private const int DuracionAviso = 2000;

    private static readonly Queue<(string Titulo, string Mensaje)> pendientes = new();
    private static readonly Queue<(string Titulo, string Mensaje, bool Sonido)> colaSecuencial = new();
    private static readonly Queue<(string Titulo, string Mensaje)> colaApilada = new();
    private static readonly List<NotificationPopup> activas = new();
    private static System.Windows.Forms.Timer? temporizadorLote;
    private static System.Windows.Forms.Timer? temporizadorApilado;
    private static NotificationPopup? secuencialActual;

    public static void Mostrar(string titulo, string mensaje)
    {
        pendientes.Enqueue((titulo, mensaje));

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
                colaSecuencial.Enqueue((aviso.Titulo, aviso.Mensaje, true));
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
        secuencialActual = new NotificationPopup(aviso.Titulo, aviso.Mensaje, aviso.Sonido);
        secuencialActual.FormClosed += (_, _) =>
        {
            secuencialActual = null;
            MostrarSiguienteSecuencial();
        };
        secuencialActual.PrepararEntrada();
        secuencialActual.Show();
        secuencialActual.BringToFront();
        secuencialActual.AnimarHasta(PosicionInferior(secuencialActual));
        secuencialActual.ProgramarCierre(DuracionAviso);
    }

    private static void AgregarApilada(bool reproducirSonido)
    {
        if (colaApilada.Count == 0) return;

        var aviso = colaApilada.Dequeue();
        var popup = new NotificationPopup(aviso.Titulo, aviso.Mensaje, reproducirSonido);
        popup.PrepararEntrada();
        activas.Add(popup);
        popup.Show();
        popup.BringToFront();
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

        // La más reciente queda abajo; las anteriores se apilan hacia arriba.
        for (int i = activas.Count - 1; i >= 0; i--)
        {
            var popup = activas[i];
            if (popup.IsDisposed) continue;

            var area = Screen.PrimaryScreen!.WorkingArea;
            var destino = new Point(
                area.Right - popup.Width - 16,
                area.Bottom - popup.Height - 16 - distancia);
            popup.AnimarHasta(destino);
            distancia += popup.Height + Separacion;
        }
    }

    private static Point PosicionInferior(NotificationPopup popup)
    {
        var area = Screen.PrimaryScreen!.WorkingArea;
        return new Point(area.Right - popup.Width - 16, area.Bottom - popup.Height - 16);
    }
}
