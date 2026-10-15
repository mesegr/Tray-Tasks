using System.Text.Json;
using TareasBandeja.Models;

namespace TareasBandeja.Controllers;

/// <summary>
/// Coordina las operaciones de negocio y la persistencia de las tareas.
/// La vista no necesita conocer el formato ni la ubicación del archivo JSON.
/// </summary>
public sealed class TareaController
{
    private readonly string rutaJson = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TareasBandeja", "tareas.json");
    private string RutaRespaldo => Path.ChangeExtension(rutaJson, ".backup.json");

    private readonly List<Tarea> tareas = new();

    public IReadOnlyList<Tarea> Tareas => tareas;

    public void Cargar()
    {
        if (!File.Exists(rutaJson)) return;

        List<Tarea>? datos;
        try
        {
            datos = JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(rutaJson));
        }
        catch (JsonException) when (File.Exists(RutaRespaldo))
        {
            datos = JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(RutaRespaldo));
            if (datos != null)
                File.WriteAllText(rutaJson, JsonSerializer.Serialize(datos));
        }
        if (datos == null) return;

        foreach (var tarea in datos)
        {
            if (tarea.Id == Guid.Empty) tarea.Id = Guid.NewGuid();
            tarea.FechasCompletadas ??= new List<DateTime>();
            tareas.Add(tarea);
        }
    }

    public void Guardar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaJson)!);
        string temporal = rutaJson + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(tareas));
        if (File.Exists(rutaJson)) File.Copy(rutaJson, RutaRespaldo, overwrite: true);
        File.Move(temporal, rutaJson, overwrite: true);
    }

    public void ReemplazarDesdeArchivo(string ruta)
    {
        var datos = JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(ruta))
            ?? throw new InvalidDataException("El archivo no contiene una copia de tareas válida.");
        tareas.Clear();
        foreach (var tarea in datos)
        {
            if (tarea.Id == Guid.Empty) tarea.Id = Guid.NewGuid();
            tarea.FechasCompletadas ??= new List<DateTime>();
            tareas.Add(tarea);
        }
        Guardar();
    }

    public void Anadir(Tarea tarea)
    {
        if (tarea.Id == Guid.Empty) tarea.Id = Guid.NewGuid();
        tareas.Add(tarea);
        Guardar();
    }

    public void Eliminar(Tarea tarea)
    {
        var original = tareas.FirstOrDefault(t => t.Id == tarea.Id);
        if (original != null && tareas.Remove(original)) Guardar();
    }

    public void Actualizar(Tarea original, Tarea actualizada)
    {
        int indice = tareas.FindIndex(t => t.Id == original.Id);
        if (indice < 0) return;

        actualizada.Id = original.Id;
        tareas[indice] = actualizada;
        Guardar();
    }

    public void MarcarHecha(Tarea ocurrencia, DateTime dia, bool hecha)
    {
        var original = tareas.FirstOrDefault(t => t.Id == ocurrencia.Id);
        if (original == null) return;

        if (original.Recurrencia == RecurrenciaTarea.Ninguna)
            original.Hecha = hecha;
        else if (hecha)
        {
            if (!original.FechasCompletadas.Any(f => f.Date == dia.Date))
                original.FechasCompletadas.Add(dia.Date);
        }
        else
            original.FechasCompletadas.RemoveAll(f => f.Date == dia.Date);

        Guardar();
    }

    public int PendientesHoy() => ObtenerParaDia(DateTime.Today).Count(t => !t.Hecha);

    public List<Tarea> ObtenerParaDia(DateTime dia)
    {
        dia = dia.Date;

        var ocurrencias = tareas
            .SelectMany(t => ObtenerOcurrencias(t, dia))
            .ToList();

        var importantes = ocurrencias.Where(t => EsDestacada(t, dia) && !t.Hecha).OrderBy(t => t.Fecha);
        var delDia = ocurrencias.Where(t => !EsDestacada(t, dia) || (t.Hecha && t.Fecha.Date == dia))
            .OrderBy(t => t.ConHora ? t.Fecha.TimeOfDay : TimeSpan.FromDays(1));
        return importantes.Concat(delDia).ToList();
    }

    public bool TieneTareas(DateTime dia) => ObtenerParaDia(dia).Any(t => !t.Hecha);

    IEnumerable<Tarea> ObtenerOcurrencias(Tarea tarea, DateTime dia)
    {
        if (tarea.Recurrencia == RecurrenciaTarea.Ninguna)
        {
            if (tarea.Fecha.Date == dia || (tarea.Importante && dia >= tarea.Creada.Date))
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

        var copia = new Tarea
        {
            Id = tarea.Id,
            Titulo = tarea.Titulo,
            Fecha = dia.Add(tarea.Fecha.TimeOfDay),
            ConHora = tarea.ConHora,
            Importante = tarea.Importante,
            Creada = tarea.Creada,
            Hecha = tarea.FechasCompletadas.Any(f => f.Date == dia),
            Recurrencia = tarea.Recurrencia,
            FechasCompletadas = tarea.FechasCompletadas
        };
        yield return copia;
    }

    public static bool EsDestacada(Tarea tarea, DateTime dia) =>
        tarea.Importante && dia.Date >= tarea.Creada.Date;
}
