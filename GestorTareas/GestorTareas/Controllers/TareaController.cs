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
    private string RutaProyectos => Path.Combine(Path.GetDirectoryName(rutaJson)!, "proyectos.json");
    private string RutaRespaldo => Path.ChangeExtension(rutaJson, ".backup.json");

    private readonly List<Tarea> tareas = new();
    private readonly List<string> proyectos = new();

    public IReadOnlyList<Tarea> Tareas => tareas;
    public IReadOnlyList<string> Proyectos => proyectos;

    public void Cargar()
    {
        List<Tarea>? datos = new();
        if (File.Exists(rutaJson))
        {
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
        }

        foreach (var tarea in datos ?? new List<Tarea>())
        {
            if (tarea.Id == Guid.Empty) tarea.Id = Guid.NewGuid();
            tarea.FechasCompletadas ??= new List<DateTime>();
            tarea.Proyecto ??= "";
            tareas.Add(tarea);
        }

        try
        {
            if (File.Exists(RutaProyectos))
                proyectos.AddRange(JsonSerializer.Deserialize<List<string>>(File.ReadAllText(RutaProyectos)) ?? new());
        }
        catch { }

        bool cambioProyectos = false;
        foreach (string nombre in tareas.Select(t => t.Proyecto).Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (proyectos.Contains(nombre, StringComparer.CurrentCultureIgnoreCase)) continue;
            proyectos.Add(nombre);
            cambioProyectos = true;
        }
        if (cambioProyectos) GuardarProyectos();
    }

    public void Guardar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaJson)!);
        string temporal = rutaJson + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(tareas));
        if (File.Exists(rutaJson)) File.Copy(rutaJson, RutaRespaldo, overwrite: true);
        File.Move(temporal, rutaJson, overwrite: true);
        GuardarProyectos();
    }

    public string CrearProyecto(string nombre)
    {
        nombre = nombre.Trim();
        ValidarNombreProyecto(nombre);

        proyectos.Add(nombre);
        GuardarProyectos();
        return nombre;
    }

    public string RenombrarProyecto(string nombreActual, string nuevoNombre)
    {
        nuevoNombre = nuevoNombre.Trim();
        int indice = proyectos.FindIndex(p => string.Equals(p, nombreActual, StringComparison.CurrentCultureIgnoreCase));
        if (indice < 0) throw new InvalidOperationException("El proyecto ya no existe.");
        ValidarNombreProyecto(nuevoNombre, nombreActual);
        string nombreAnterior = proyectos[indice];
        proyectos[indice] = nuevoNombre;
        foreach (var tarea in tareas.Where(t => string.Equals(t.Proyecto, nombreAnterior, StringComparison.CurrentCultureIgnoreCase)))
            tarea.Proyecto = nuevoNombre;
        Guardar();
        return nuevoNombre;
    }

    public ProyectoEliminado? EliminarProyecto(string nombre)
    {
        int indice = proyectos.FindIndex(p => string.Equals(p, nombre, StringComparison.CurrentCultureIgnoreCase));
        if (indice < 0) return null;
        string nombreProyecto = proyectos[indice];
        var asignaciones = tareas
            .Where(t => string.Equals(t.Proyecto, nombreProyecto, StringComparison.CurrentCultureIgnoreCase))
            .ToDictionary(t => t.Id, t => t.Proyecto);
        proyectos.RemoveAt(indice);
        foreach (var tarea in tareas.Where(t => asignaciones.ContainsKey(t.Id)))
            tarea.Proyecto = "";
        Guardar();
        return new ProyectoEliminado(nombreProyecto, asignaciones);
    }

    public void RestaurarProyecto(ProyectoEliminado eliminado)
    {
        if (!proyectos.Contains(eliminado.Nombre, StringComparer.CurrentCultureIgnoreCase))
            proyectos.Add(eliminado.Nombre);
        foreach (var tarea in tareas)
            if (eliminado.Asignaciones.TryGetValue(tarea.Id, out string? proyecto)) tarea.Proyecto = proyecto;
        Guardar();
    }

    private void ValidarNombreProyecto(string nombre, string? excluir = null)
    {
        if (nombre.Length == 0) throw new ArgumentException("Escribe un nombre para el proyecto.");
        if (nombre.Length > 40) throw new ArgumentException("El nombre del proyecto no puede superar los 40 caracteres.");
        if (nombre.Equals("Sin proyecto", StringComparison.CurrentCultureIgnoreCase) ||
            nombre.Equals("＋ Crear proyecto…", StringComparison.CurrentCultureIgnoreCase) ||
            nombre.Equals("⚙ Gestionar proyectos…", StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("Ese nombre está reservado por la aplicación.");
        if (proyectos.Any(p => !string.Equals(p, excluir, StringComparison.CurrentCultureIgnoreCase) &&
            string.Equals(p, nombre, StringComparison.CurrentCultureIgnoreCase)))
            throw new InvalidOperationException("Ya existe un proyecto con ese nombre.");
    }

    public string ExportarCopia()
    {
        var copia = new CopiaTareas
        {
            Tareas = tareas,
            Proyectos = proyectos
        };
        return JsonSerializer.Serialize(copia, new JsonSerializerOptions { WriteIndented = true });
    }

    public void ReemplazarDesdeArchivo(string ruta)
    {
        using var documento = JsonDocument.Parse(File.ReadAllText(ruta));
        List<Tarea>? datos;
        List<string> proyectosCopia;
        if (documento.RootElement.ValueKind == JsonValueKind.Array)
        {
            datos = JsonSerializer.Deserialize<List<Tarea>>(documento.RootElement.GetRawText());
            proyectosCopia = datos?.Select(t => t.Proyecto).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList() ?? new();
        }
        else if (documento.RootElement.ValueKind == JsonValueKind.Object)
        {
            var copia = JsonSerializer.Deserialize<CopiaTareas>(documento.RootElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            datos = copia?.Tareas;
            proyectosCopia = copia?.Proyectos ?? new();
        }
        else throw new InvalidDataException("El archivo no contiene una copia de tareas válida.");

        if (datos == null) throw new InvalidDataException("El archivo no contiene tareas válidas.");
        tareas.Clear();
        foreach (var tarea in datos)
        {
            if (tarea.Id == Guid.Empty) tarea.Id = Guid.NewGuid();
            tarea.FechasCompletadas ??= new List<DateTime>();
            tarea.Proyecto ??= "";
            tareas.Add(tarea);
        }
        proyectos.Clear();
        proyectos.AddRange(proyectosCopia.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.CurrentCultureIgnoreCase));
        foreach (string nombre in tareas.Select(t => t.Proyecto).Where(p => !string.IsNullOrWhiteSpace(p)))
            if (!proyectos.Contains(nombre, StringComparer.CurrentCultureIgnoreCase)) proyectos.Add(nombre);
        Guardar();
    }

    private void GuardarProyectos()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaProyectos)!);
        string temporal = RutaProyectos + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(proyectos));
        File.Move(temporal, RutaProyectos, overwrite: true);
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
            // Las tareas importantes se arrastran desde su creación hasta la fecha límite,
            // pero no deben marcar como ocupados los días posteriores a esa fecha.
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

        var copia = new Tarea
        {
            Id = tarea.Id,
            Titulo = tarea.Titulo,
            Fecha = dia.Add(tarea.Fecha.TimeOfDay),
            ConHora = tarea.ConHora,
            Importante = tarea.Importante,
            Proyecto = tarea.Proyecto,
            Creada = tarea.Creada,
            Hecha = tarea.FechasCompletadas.Any(f => f.Date == dia),
            Recurrencia = tarea.Recurrencia,
            FechasCompletadas = tarea.FechasCompletadas
        };
        yield return copia;
    }

    public static bool EsDestacada(Tarea tarea, DateTime dia) =>
        tarea.Importante && dia.Date >= tarea.Creada.Date && dia.Date <= tarea.Fecha.Date;

    private sealed class CopiaTareas
    {
        public CopiaTareas() { }
        public List<Tarea> Tareas { get; set; } = new();
        public List<string> Proyectos { get; set; } = new();
    }
}

public sealed record ProyectoEliminado(string Nombre, Dictionary<Guid, string> Asignaciones);
