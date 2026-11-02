using System.Drawing.Drawing2D;
using System.Drawing.Text;
using TrayTasks.Models;
using TrayTasks.Services;

namespace TrayTasks.Views;

public partial class MainForm
{
    // ---------- Icono con número ----------
    void ActualizarIcono()
    {
        int n = PendientesHoy();
        Color colorEstado = n == 0 ? Ui.Verde : n <= 3 ? Ui.Naranja : Ui.Rojo;

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using (var fondo = new SolidBrush(colorEstado))
            using (var forma = Ui.RoundedRect(new Rectangle(1, 1, 30, 30), 8))
                g.FillPath(fondo, forma);

            string texto = n > 99 ? "99+" : n.ToString();
            float tamanoFuente = texto.Length == 1 ? 22f : texto.Length == 2 ? 17f : 12f;
            using var fuente = new Font("Segoe UI", tamanoFuente, FontStyle.Bold, GraphicsUnit.Pixel);
            using var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(texto, fuente, Brushes.Black, new RectangleF(1, 1, 30, 30), formato);
        }

        IntPtr handle = bmp.GetHicon();
        var nuevo = (Icon)Icon.FromHandle(handle).Clone();
        DestroyIcon(handle);

        tray.Icon = nuevo;
        iconoActual?.Dispose();
        iconoActual = nuevo;

        tray.Text = n switch
        {
            0 => "0 tareas pendientes hoy",
            1 => "1 tarea pendiente hoy",
            _ => $"{n} tareas pendientes hoy"
        };
    }

    // Las tareas sin hora no tienen un instante concreto para calcular un aviso.
    // Se comprueba cada minuto y cada aviso se muestra como máximo una vez por tarea.
    void ComprobarRecordatorios(bool alIniciar = false)
    {
        DateTime ahora = DateTime.Now;

        var ocurrenciasHoy = controller.ObtenerParaDia(ahora.Date)
            .Concat(controller.ObtenerParaDia(ahora.Date.AddDays(1)))
            .Where(t => t.ConHora && !t.Hecha)
            .DistinctBy(t => (t.Id, t.Fecha));
        IReadOnlyList<AvisoProgramado> avisos = recordatorioScheduler.Comprobar(
            ocurrenciasHoy,
            fecha => controller.ObtenerParaDia(fecha),
            avisosPreviosMinutos,
            ahora,
            alIniciar);

        foreach (AvisoProgramado aviso in avisos)
            MostrarRecordatorio(aviso.Tarea, aviso.MinutosAntes);
    }

    void MostrarRecordatorio(Tarea tarea, int minutosAntes)
    {
        string periodo = minutosAntes switch
        {
            1440 => "24 horas",
            720 => "12 horas",
            480 => "8 horas",
            240 => "4 horas",
            120 => "2 horas",
            60 => "1 hora",
            _ => $"{minutosAntes} minutos"
        };
        string texto = minutosAntes == 60
            ? $"Falta menos de {periodo} para: {tarea.Titulo}"
            : $"Faltan menos de {periodo} para: {tarea.Titulo}";
        int siguienteIntervalo = RecordatorioScheduler.SiguienteIntervalo(minutosAntes);

        Notificador.Mostrar("Recordatorio de tarea", texto, sonidoHabilitado, duracionNotificacion,
            () => BeginInvoke(new Action(() =>
            {
                Mostrar();
                IrADia(tarea.Fecha.Date);
            })), $"Posponer {TextoIntervaloCorto(siguienteIntervalo)}", () =>
            {
                recordatorioScheduler.Posponer(tarea, siguienteIntervalo, DateTime.Now);
            });
    }

    static string TextoIntervaloCorto(int minutos) => minutos switch
    {
        1440 => "24 h",
        720 => "12 h",
        480 => "8 h",
        240 => "4 h",
        120 => "2 h",
        60 => "1 h",
        _ => $"{minutos} min"
    };
}
