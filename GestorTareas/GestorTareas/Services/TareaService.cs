using TareasBandeja.Models;

namespace TareasBandeja.Services;

/// <summary>
/// Reglas de dominio para consultar y generar las ocurrencias visibles de una tarea.
/// No depende de WinForms ni del almacenamiento, por lo que puede probarse por separado.
/// </summary>
public static class TareaService
{
    public static int PendientesHoy(IEnumerable<Tarea> tareas) =>
        ObtenerParaDia(tareas, DateTime.Today).Count(tarea => !tarea.Hecha);

    public static List<Tarea> ObtenerParaDia(IEnumerable<Tarea> tareas, DateTime dia)
    {
        dia = dia.Date;

        var ocurrencias = tareas
            .SelectMany(tarea => ObtenerOcurrencias(tarea, dia))
            .ToList();

        var importantes = ocurrencias
            .Where(tarea => EsDestacada(tarea, dia) && !tarea.Hecha)
            .OrderBy(tarea => tarea.Fecha);
        var delDia = ocurrencias
            .Where(tarea => !EsDestacada(tarea, dia) || (tarea.Hecha && tarea.Fecha.Date == dia))
            .OrderBy(tarea => tarea.ConHora ? tarea.Fecha.TimeOfDay : TimeSpan.FromDays(1));

        return importantes.Concat(delDia).ToList();
    }

    public static bool TieneTareas(IEnumerable<Tarea> tareas, DateTime dia) =>
        ObtenerParaDia(tareas, dia).Any(tarea => !tarea.Hecha);

    public static bool EsDestacada(Tarea tarea, DateTime dia) =>
        tarea.Importante && dia.Date >= tarea.Creada.Date && dia.Date <= tarea.Fecha.Date;

    private static IEnumerable<Tarea> ObtenerOcurrencias(Tarea tarea, DateTime dia)
    {
        if (tarea.Recurrencia == RecurrenciaTarea.Ninguna)
        {
            // Las tareas importantes se arrastran desde su creación hasta la fecha límite,
            // pero no deben aparecer en días posteriores.
            if (tarea.Fecha.Date == dia ||
                (tarea.Importante && dia >= tarea.Creada.Date && dia <= tarea.Fecha.Date))
                yield return tarea;
            yield break;
        }

        if (dia < tarea.Fecha.Date) yield break;

        bool coincide = tarea.Recurrencia switch
        {
            RecurrenciaTarea.Diaria => true,
            RecurrenciaTarea.Semanal => (dia - tarea.Fecha.Date).Days % 7 == 0,
            RecurrenciaTarea.Mensual => dia.Day == Math.Min(tarea.Fecha.Day, DateTime.DaysInMonth(dia.Year, dia.Month)),
            _ => false
        };
        if (!coincide) yield break;

        yield return new Tarea
        {
            Id = tarea.Id,
            Titulo = tarea.Titulo,
            Fecha = dia.Add(tarea.Fecha.TimeOfDay),
            ConHora = tarea.ConHora,
            Importante = tarea.Importante,
            Proyecto = tarea.Proyecto,
            Creada = tarea.Creada,
            Hecha = tarea.FechasCompletadas.Any(fecha => fecha.Date == dia),
            Recurrencia = tarea.Recurrencia,
            FechasCompletadas = tarea.FechasCompletadas
        };
    }
}
