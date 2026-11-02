using TrayTasks.Models;

namespace TrayTasks.Services;

public sealed record AvisoProgramado(Tarea Tarea, int MinutosAntes);

/// <summary>
/// Calcula cuándo deben emitirse los recordatorios y gestiona sus posposiciones.
/// No depende de WinForms ni muestra notificaciones directamente.
/// </summary>
public sealed class RecordatorioScheduler
{
    private static readonly int[] IntervalosDisponibles = { 1440, 720, 480, 240, 120, 60, 45, 30, 15 };

    private readonly HashSet<(Guid Id, DateTime Fecha, int MinutosAntes)> mostrados = new();
    private readonly Dictionary<(Guid Id, DateTime Fecha), (DateTime Vence, int Minutos)> pospuestos = new();

    public IReadOnlyList<AvisoProgramado> Comprobar(
        IEnumerable<Tarea> ocurrencias,
        Func<DateTime, IEnumerable<Tarea>> obtenerParaDia,
        IEnumerable<int> intervalosAviso,
        DateTime ahora,
        bool alIniciar = false)
    {
        int[] intervalos = intervalosAviso
            .Where(IntervalosDisponibles.Contains)
            .Distinct()
            .OrderByDescending(minutos => minutos)
            .ToArray();
        var avisos = new List<AvisoProgramado>();

        foreach (var (clave, pospuesto) in pospuestos.ToArray())
        {
            if (ahora < pospuesto.Vence) continue;
            this.pospuestos.Remove(clave);

            var tarea = obtenerParaDia(clave.Fecha.Date)
                .FirstOrDefault(t => t.Id == clave.Id && t.Fecha == clave.Fecha && t.ConHora && !t.Hecha);
            if (tarea == null) continue;

            var claveAviso = (tarea.Id, tarea.Fecha, pospuesto.Minutos);
            if (mostrados.Add(claveAviso)) avisos.Add(new AvisoProgramado(tarea, pospuesto.Minutos));
        }

        foreach (var tarea in ocurrencias
                     .Where(t => t.ConHora && !t.Hecha)
                     .DistinctBy(t => (t.Id, t.Fecha)))
        {
            if (alIniciar)
            {
                // Al iniciar tarde, emitir una vez el intervalo próximo más cercano.
                TimeSpan restante = tarea.Fecha - ahora;
                if (restante <= TimeSpan.Zero) continue;

                int intervalo = intervalos
                    .Where(minutos => restante <= TimeSpan.FromMinutes(minutos))
                    .DefaultIfEmpty(0)
                    .Min();
                if (intervalo > 0) EmitirSiNuevo(tarea, intervalo, avisos);
                continue;
            }

            foreach (int intervalo in intervalos)
            {
                DateTime momentoAviso = tarea.Fecha.AddMinutes(-intervalo);
                bool dentroDeLaVentana = ahora >= momentoAviso && ahora < momentoAviso.AddMinutes(2);
                if (dentroDeLaVentana) EmitirSiNuevo(tarea, intervalo, avisos);
            }
        }

        return avisos;
    }

    public void Posponer(Tarea tarea, int minutos, DateTime ahora) =>
        pospuestos[(tarea.Id, tarea.Fecha)] = (ahora.AddMinutes(minutos), minutos);

    public static int SiguienteIntervalo(int minutos)
    {
        int indice = Array.IndexOf(IntervalosDisponibles, minutos);
        return indice >= 0 && indice < IntervalosDisponibles.Length - 1
            ? IntervalosDisponibles[indice + 1]
            : 5;
    }

    private void EmitirSiNuevo(Tarea tarea, int intervalo, ICollection<AvisoProgramado> avisos)
    {
        if (mostrados.Add((tarea.Id, tarea.Fecha, intervalo)))
            avisos.Add(new AvisoProgramado(tarea, intervalo));
    }
}
