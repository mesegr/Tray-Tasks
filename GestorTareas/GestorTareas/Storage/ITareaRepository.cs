using TareasBandeja.Models;

namespace TareasBandeja.Storage;

/// <summary>Persistencia de tareas y proyectos, desacoplada de las reglas de negocio.</summary>
public interface ITareaRepository
{
    TareaSnapshot Cargar();
    void Guardar(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos);
    void GuardarProyectos(IReadOnlyCollection<string> proyectos);
    string ExportarCopia(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos);
    TareaSnapshot LeerCopia(string ruta);
}

public sealed record TareaSnapshot(
    List<Tarea> Tareas,
    List<string> Proyectos,
    IReadOnlyList<string>? AdvertenciasRecuperacion = null);
