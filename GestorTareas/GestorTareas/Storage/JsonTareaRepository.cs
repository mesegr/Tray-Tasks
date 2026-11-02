using System.Text.Json;
using TareasBandeja.Diagnostics;
using TareasBandeja.Models;

namespace TareasBandeja.Storage;

/// <summary>Guarda los datos de la aplicación como JSON en el perfil del usuario.</summary>
public sealed class JsonTareaRepository : ITareaRepository
{
    private const int VersionCopiaActual = 1;
    private static readonly JsonSerializerOptions OpcionesCopia = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string rutaTareas;
    private readonly string rutaProyectos;
    private readonly string rutaRespaldo;
    private readonly string rutaRespaldoProyectos;

    public JsonTareaRepository(string? directorioDatos = null)
    {
        directorioDatos ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TareasBandeja");
        rutaTareas = Path.Combine(directorioDatos, "tareas.json");
        rutaProyectos = Path.Combine(directorioDatos, "proyectos.json");
        rutaRespaldo = Path.ChangeExtension(rutaTareas, ".backup.json");
        rutaRespaldoProyectos = Path.ChangeExtension(rutaProyectos, ".backup.json");
    }

    public TareaSnapshot Cargar()
    {
        var advertencias = new List<string>();
        List<Tarea> tareas = CargarListaConRespaldo<Tarea>(rutaTareas, rutaRespaldo, "tareas", advertencias);
        List<string> proyectos = CargarListaConRespaldo<string>(rutaProyectos, rutaRespaldoProyectos, "proyectos", advertencias);
        return new TareaSnapshot(tareas, proyectos, advertencias);
    }

    public void Guardar(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos)
    {
        GuardarDatosAtomico(tareas, proyectos);
    }

    public void GuardarProyectos(IReadOnlyCollection<string> proyectos)
    {
        GuardarJsonAtomico(rutaProyectos, proyectos, rutaRespaldoProyectos);
    }

    public string ExportarCopia(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos)
    {
        var copia = new CopiaTareas
        {
            SchemaVersion = VersionCopiaActual,
            Tareas = tareas.ToList(),
            Proyectos = proyectos.ToList()
        };
        return JsonSerializer.Serialize(copia, OpcionesCopia);
    }

    public TareaSnapshot LeerCopia(string ruta)
    {
        try
        {
            return LeerCopiaInterna(ruta);
        }
        catch (Exception ex)
        {
            AppLogger.Error("No se pudo leer o validar el archivo de copia seleccionado.", ex);
            throw;
        }
    }

    private TareaSnapshot LeerCopiaInterna(string ruta)
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
            if (copia?.SchemaVersion > VersionCopiaActual)
                throw new InvalidDataException($"La copia usa una versión de formato no compatible ({copia.SchemaVersion}).");
            tareas = copia?.Tareas;
            proyectos = copia?.Proyectos ?? new();
        }
        else
        {
            throw new InvalidDataException("El archivo no contiene una copia de tareas válida.");
        }

        if (tareas == null) throw new InvalidDataException("El archivo no contiene tareas válidas.");
        if (tareas.Any(tarea => tarea == null))
            throw new InvalidDataException("La copia contiene una tarea vacía o incompleta.");
        if (proyectos.Any(nombre => nombre == null))
            throw new InvalidDataException("La copia contiene un proyecto vacío o incompleto.");
        return new TareaSnapshot(tareas, proyectos);
    }

    private List<T> CargarListaConRespaldo<T>(string ruta, string rutaBackup, string tipo,
        ICollection<string> advertencias)
    {
        if (!File.Exists(ruta)) return new List<T>();

        try
        {
            return LeerLista<T>(ruta, tipo);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            AppLogger.Advertencia($"El archivo de {tipo} está dañado; se intentará recuperar el respaldo.", ex);
            if (!File.Exists(rutaBackup))
            {
                AppLogger.Error($"No existe un respaldo válido para recuperar {tipo}.", ex);
                throw new InvalidDataException(
                    $"El archivo de {tipo} está dañado y no hay un respaldo disponible. Se ha conservado el archivo original. Consulta los registros para más información.", ex);
            }

            List<T> recuperados;
            try
            {
                recuperados = LeerLista<T>(rutaBackup, $"respaldo de {tipo}");
            }
            catch (Exception errorRespaldo)
            {
                AppLogger.Error($"Tampoco se pudo leer el respaldo de {tipo}; se conservaron ambos archivos.", errorRespaldo);
                throw new InvalidDataException(
                    $"Los datos de {tipo} y su respaldo están dañados. No se ha sobrescrito ninguno de los archivos.",
                    new AggregateException(ex, errorRespaldo));
            }

            RestaurarDesdeRespaldo(ruta, recuperados, tipo);
            advertencias.Add($"Los datos de {tipo} se recuperaron desde su respaldo. Se conservó el archivo dañado original.");
            return recuperados;
        }
    }

    private static List<T> LeerLista<T>(string ruta, string tipo)
    {
        List<T>? elementos = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(ruta));
        if (elementos == null || elementos.Any(elemento => elemento == null))
            throw new InvalidDataException($"El archivo de {tipo} no contiene una lista válida.");
        return elementos;
    }

    private static void RestaurarDesdeRespaldo<T>(string ruta, List<T> elementos, string tipo)
    {
        string archivoDaniado = $"{ruta}.corrupt-{DateTime.Now:yyyyMMdd-HHmmssfff}";
        File.Move(ruta, archivoDaniado);
        try
        {
            GuardarJsonAtomico(ruta, elementos, rutaRespaldo: null);
            AppLogger.Advertencia($"Se recuperaron {tipo} desde el respaldo. El original dañado se conservó en '{archivoDaniado}'.");
        }
        catch
        {
            if (!File.Exists(ruta) && File.Exists(archivoDaniado)) File.Move(archivoDaniado, ruta);
            throw;
        }
    }

    private static void GuardarJsonAtomico<T>(string ruta, T datos, string? rutaRespaldo)
    {
        string directorio = Path.GetDirectoryName(ruta)!;
        Directory.CreateDirectory(directorio);
        string temporal = $"{ruta}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporal, JsonSerializer.Serialize(datos));
            if (rutaRespaldo != null && File.Exists(ruta))
                File.Copy(ruta, rutaRespaldo, overwrite: true);
            File.Move(temporal, ruta, overwrite: true);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"No se pudo guardar el archivo '{Path.GetFileName(ruta)}'.", ex);
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(temporal)) File.Delete(temporal);
            }
            catch (Exception ex)
            {
                AppLogger.Advertencia($"No se pudo limpiar un archivo temporal de guardado para '{Path.GetFileName(ruta)}'.", ex);
            }
        }
    }

    private void GuardarDatosAtomico(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos)
    {
        string temporalTareas = $"{rutaTareas}.{Guid.NewGuid():N}.tmp";
        string temporalProyectos = $"{rutaProyectos}.{Guid.NewGuid():N}.tmp";
        bool habiaTareas = File.Exists(rutaTareas);
        bool habiaProyectos = File.Exists(rutaProyectos);
        bool tareasActualizadas = false;
        bool proyectosActualizados = false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(rutaTareas)!);
            File.WriteAllText(temporalTareas, JsonSerializer.Serialize(tareas));
            File.WriteAllText(temporalProyectos, JsonSerializer.Serialize(proyectos));

            if (habiaTareas) File.Copy(rutaTareas, rutaRespaldo, overwrite: true);
            if (habiaProyectos) File.Copy(rutaProyectos, rutaRespaldoProyectos, overwrite: true);

            File.Move(temporalTareas, rutaTareas, overwrite: true);
            tareasActualizadas = true;
            File.Move(temporalProyectos, rutaProyectos, overwrite: true);
            proyectosActualizados = true;
        }
        catch (Exception ex)
        {
            var erroresReversion = new List<Exception>();
            if (tareasActualizadas) Revertir(rutaTareas, rutaRespaldo, habiaTareas, erroresReversion);
            if (proyectosActualizados) Revertir(rutaProyectos, rutaRespaldoProyectos, habiaProyectos, erroresReversion);

            AppLogger.Error("No se pudo guardar de forma consistente el conjunto de tareas y proyectos.", ex);
            if (erroresReversion.Count > 0)
            {
                erroresReversion.Insert(0, ex);
                throw new AggregateException("Falló el guardado y también la reversión de los archivos de datos.", erroresReversion);
            }
            throw;
        }
        finally
        {
            LimpiarTemporal(temporalTareas);
            LimpiarTemporal(temporalProyectos);
        }
    }

    private static void Revertir(string ruta, string rutaRespaldo, bool existia,
        ICollection<Exception> errores)
    {
        try
        {
            if (existia)
                File.Copy(rutaRespaldo, ruta, overwrite: true);
            else if (File.Exists(ruta))
                File.Delete(ruta);
        }
        catch (Exception ex)
        {
            errores.Add(ex);
            AppLogger.Error($"No se pudo revertir el archivo '{Path.GetFileName(ruta)}'.", ex);
        }
    }

    private static void LimpiarTemporal(string ruta)
    {
        try
        {
            if (File.Exists(ruta)) File.Delete(ruta);
        }
        catch (Exception ex)
        {
            AppLogger.Advertencia($"No se pudo eliminar el temporal '{Path.GetFileName(ruta)}'.", ex);
        }
    }

    private sealed class CopiaTareas
    {
        public int SchemaVersion { get; set; } = VersionCopiaActual;
        public List<Tarea> Tareas { get; set; } = new();
        public List<string> Proyectos { get; set; } = new();
    }
}
