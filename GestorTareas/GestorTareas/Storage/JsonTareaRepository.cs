using System.Text.Json;
using TareasBandeja.Models;

namespace TareasBandeja.Storage;

/// <summary>Guarda los datos de la aplicación como JSON en el perfil del usuario.</summary>
public sealed class JsonTareaRepository : ITareaRepository
{
    private readonly string rutaTareas;
    private readonly string rutaProyectos;
    private readonly string rutaRespaldo;

    public JsonTareaRepository(string? directorioDatos = null)
    {
        directorioDatos ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TareasBandeja");
        rutaTareas = Path.Combine(directorioDatos, "tareas.json");
        rutaProyectos = Path.Combine(directorioDatos, "proyectos.json");
        rutaRespaldo = Path.ChangeExtension(rutaTareas, ".backup.json");
    }

    public TareaSnapshot Cargar()
    {
        List<Tarea>? tareas = new();
        if (File.Exists(rutaTareas))
        {
            try
            {
                tareas = JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(rutaTareas));
            }
            catch (JsonException) when (File.Exists(rutaRespaldo))
            {
                tareas = JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(rutaRespaldo));
                if (tareas != null)
                    File.WriteAllText(rutaTareas, JsonSerializer.Serialize(tareas));
            }
        }

        List<string> proyectos = new();
        try
        {
            if (File.Exists(rutaProyectos))
                proyectos = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(rutaProyectos)) ?? new();
        }
        catch
        {
            // Un archivo de proyectos dañado no debe impedir abrir la lista de tareas.
        }

        return new TareaSnapshot(tareas ?? new(), proyectos);
    }

    public void Guardar(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaTareas)!);
        string temporal = rutaTareas + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(tareas));
        if (File.Exists(rutaTareas)) File.Copy(rutaTareas, rutaRespaldo, overwrite: true);
        File.Move(temporal, rutaTareas, overwrite: true);
        GuardarProyectos(proyectos);
    }

    public void GuardarProyectos(IReadOnlyCollection<string> proyectos)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaProyectos)!);
        string temporal = rutaProyectos + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(proyectos));
        File.Move(temporal, rutaProyectos, overwrite: true);
    }

    public string ExportarCopia(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos)
    {
        var copia = new CopiaTareas { Tareas = tareas.ToList(), Proyectos = proyectos.ToList() };
        return JsonSerializer.Serialize(copia, new JsonSerializerOptions { WriteIndented = true });
    }

    public TareaSnapshot LeerCopia(string ruta)
    {
        using var documento = JsonDocument.Parse(File.ReadAllText(ruta));
        List<Tarea>? tareas;
        List<string> proyectos;

        if (documento.RootElement.ValueKind == JsonValueKind.Array)
        {
            tareas = JsonSerializer.Deserialize<List<Tarea>>(documento.RootElement.GetRawText());
            proyectos = tareas?
                .Select(tarea => tarea.Proyecto)
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList() ?? new();
        }
        else if (documento.RootElement.ValueKind == JsonValueKind.Object)
        {
            var copia = JsonSerializer.Deserialize<CopiaTareas>(documento.RootElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            tareas = copia?.Tareas;
            proyectos = copia?.Proyectos ?? new();
        }
        else
        {
            throw new InvalidDataException("El archivo no contiene una copia de tareas válida.");
        }

        if (tareas == null) throw new InvalidDataException("El archivo no contiene tareas válidas.");
        return new TareaSnapshot(tareas, proyectos);
    }

    private sealed class CopiaTareas
    {
        public List<Tarea> Tareas { get; set; } = new();
        public List<string> Proyectos { get; set; } = new();
    }
}
