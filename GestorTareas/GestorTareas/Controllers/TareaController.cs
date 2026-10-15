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

    private readonly List<Tarea> tareas = new();

    public IReadOnlyList<Tarea> Tareas => tareas;

    public void Cargar()
    {
        if (!File.Exists(rutaJson)) return;

        var datos = JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(rutaJson));
        if (datos == null) return;

        // Las tareas marcadas como hechas en versiones anteriores se descartan.
        tareas.AddRange(datos.Where(t => !t.Hecha));
    }

    public void Guardar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaJson)!);
        File.WriteAllText(rutaJson, JsonSerializer.Serialize(tareas));
    }

    public void Anadir(Tarea tarea)
    {
        tareas.Add(tarea);
        Guardar();
    }

    public void Eliminar(Tarea tarea)
    {
        if (tareas.Remove(tarea)) Guardar();
    }

    public void Actualizar(Tarea original, Tarea actualizada)
    {
        int indice = tareas.IndexOf(original);
        if (indice < 0) return;

        tareas[indice] = actualizada;
        Guardar();
    }

    public int PendientesHoy() => ObtenerParaDia(DateTime.Today).Count;

    public List<Tarea> ObtenerParaDia(DateTime dia)
    {
        dia = dia.Date;

        var importantes = tareas
            .Where(t => EsDestacada(t, dia))
            .OrderBy(t => t.Fecha);

        var delDia = tareas
            .Where(t => !EsDestacada(t, dia) && t.Fecha.Date == dia)
            .OrderBy(t => t.ConHora ? t.Fecha.TimeOfDay : TimeSpan.FromDays(1));

        return importantes.Concat(delDia).ToList();
    }

    public static bool EsDestacada(Tarea tarea, DateTime dia) =>
        tarea.Importante && dia.Date >= tarea.Creada.Date;
}
