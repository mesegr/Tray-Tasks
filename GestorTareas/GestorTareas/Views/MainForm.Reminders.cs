using System.Drawing.Drawing2D;
using System.Drawing.Text;
using TareasBandeja.Models;

namespace TareasBandeja.Views;

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

        foreach (var (clave, pospuesto) in recordatoriosPospuestos.ToArray())
        {
            if (ahora < pospuesto.Vence) continue;
            recordatoriosPospuestos.Remove(clave);

            var tareaPendiente = controller.ObtenerParaDia(clave.Fecha.Date)
                .FirstOrDefault(t => t.Id == clave.Id && t.Fecha == clave.Fecha && t.ConHora && !t.Hecha);
            if (tareaPendiente == null) continue;

            var claveAviso = (tareaPendiente.Id, tareaPendiente.Fecha, pospuesto.Minutos);
            if (!recordatoriosMostrados.Contains(claveAviso))
                MostrarRecordatorio(tareaPendiente, pospuesto.Minutos);
        }

        foreach (var tarea in ocurrenciasHoy)
        {
            if (alIniciar)
            {
                // Si la aplicación se inicia tarde, no se pierde el aviso:
                // se muestra una sola vez el intervalo seleccionado más cercano.
                TimeSpan tiempoRestante = tarea.Fecha - ahora;
                if (tiempoRestante > TimeSpan.Zero)
                {
                    int intervalo = avisosPreviosMinutos
                        .Where(minutos => tiempoRestante <= TimeSpan.FromMinutes(minutos))
                        .DefaultIfEmpty(0)
                        .Min();
                    if (intervalo > 0) MostrarRecordatorio(tarea, intervalo);
                }

                continue;
            }

            foreach (int intervalo in avisosPreviosMinutos)
                MostrarRecordatorioSiCorresponde(tarea, ahora, intervalo);
        }
    }

    void MostrarRecordatorioSiCorresponde(Tarea tarea, DateTime ahora, int minutosAntes)
    {
        DateTime momentoAviso = tarea.Fecha.AddMinutes(-minutosAntes);
        bool dentroDeLaVentana = ahora >= momentoAviso && ahora < momentoAviso.AddMinutes(2);
        var clave = (tarea.Id, tarea.Fecha, minutosAntes);

        if (!dentroDeLaVentana || !recordatoriosMostrados.Add(clave)) return;

        MostrarRecordatorio(tarea, minutosAntes);
    }

    void MostrarRecordatorio(Tarea tarea, int minutosAntes)
    {
        recordatoriosMostrados.Add((tarea.Id, tarea.Fecha, minutosAntes));

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
        int siguienteIntervalo = SiguienteIntervalo(minutosAntes);

        Notificador.Mostrar("Recordatorio de tarea", texto, sonidoHabilitado, duracionNotificacion,
            () => BeginInvoke(new Action(() =>
            {
                Mostrar();
                IrADia(tarea.Fecha.Date);
            })), $"Posponer {TextoIntervaloCorto(siguienteIntervalo)}", () =>
            {
                recordatoriosPospuestos[(tarea.Id, tarea.Fecha)] =
                    (DateTime.Now.AddMinutes(siguienteIntervalo), siguienteIntervalo);
            });
    }

    static int SiguienteIntervalo(int minutos)
    {
        int[] intervalos = { 1440, 720, 480, 240, 120, 60, 45, 30, 15 };
        int indice = Array.IndexOf(intervalos, minutos);
        return indice >= 0 && indice < intervalos.Length - 1 ? intervalos[indice + 1] : 5;
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
