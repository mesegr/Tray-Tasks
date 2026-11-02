using TareasBandeja.Models;
using TareasBandeja.Services;
using TareasBandeja.Storage;

namespace TareasBandeja.Controllers;

/// <summary>
/// Fachada de aplicación para tareas y proyectos.
/// Delega las reglas de calendario y la persistencia en sus componentes dedicados.
/// </summary>
public sealed class TareaController
{
    private readonly List<Tarea> tareas = new();
    private readonly List<string> proyectos = new();
    private readonly ITareaRepository repositorio;

    public TareaController(ITareaRepository? repositorio = null)
    {
        this.repositorio = repositorio ?? new JsonTareaRepository();
    }

    public IReadOnlyList<Tarea> Tareas => tareas;
    public IReadOnlyList<string> Proyectos => proyectos;
    public IReadOnlyList<string> AdvertenciasRecuperacion { get; private set; } = Array.Empty<string>();

    public void Cargar()
    {
        TareaSnapshot datos = repositorio.Cargar();
        AdvertenciasRecuperacion = datos.AdvertenciasRecuperacion ?? Array.Empty<string>();
        tareas.Clear();
        proyectos.Clear();

        foreach (var tarea in datos.Tareas)
        {
            Normalizar(tarea);
            tareas.Add(tarea);
        }

        proyectos.AddRange(datos.Proyectos.Where(nombre => !string.IsNullOrWhiteSpace(nombre)));

        bool cambioProyectos = false;
        foreach (string nombre in tareas.Select(t => t.Proyecto).Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (proyectos.Contains(nombre, StringComparer.CurrentCultureIgnoreCase)) continue;
            proyectos.Add(nombre);
            cambioProyectos = true;
        }
        if (cambioProyectos) repositorio.GuardarProyectos(proyectos);
    }

    private static void Normalizar(Tarea tarea)
    {
        if (tarea.Id == Guid.Empty) tarea.Id = Guid.NewGuid();
        tarea.FechasCompletadas ??= new List<DateTime>();
        tarea.Proyecto ??= "";
    }

    public void Guardar() => repositorio.Guardar(tareas, proyectos);

    public string CrearProyecto(string nombre)
    {
        nombre = nombre.Trim();
        ValidarNombreProyecto(nombre);

        return EjecutarCambio(() =>
        {
            proyectos.Add(nombre);
            return nombre;
        });
    }

    public string RenombrarProyecto(string nombreActual, string nuevoNombre)
    {
        nuevoNombre = nuevoNombre.Trim();
        int indice = proyectos.FindIndex(p => string.Equals(p, nombreActual, StringComparison.CurrentCultureIgnoreCase));
        if (indice < 0) throw new InvalidOperationException("El proyecto ya no existe.");
        ValidarNombreProyecto(nuevoNombre, nombreActual);
        return EjecutarCambio(() =>
        {
            string nombreAnterior = proyectos[indice];
            proyectos[indice] = nuevoNombre;
            foreach (var tarea in tareas.Where(t => string.Equals(t.Proyecto, nombreAnterior, StringComparison.CurrentCultureIgnoreCase)))
                tarea.Proyecto = nuevoNombre;
            return nuevoNombre;
        });
    }

    public ProyectoEliminado? EliminarProyecto(string nombre)
    {
        int indice = proyectos.FindIndex(p => string.Equals(p, nombre, StringComparison.CurrentCultureIgnoreCase));
        if (indice < 0) return null;
        return EjecutarCambio(() =>
        {
            string nombreProyecto = proyectos[indice];
            var asignaciones = tareas
                .Where(t => string.Equals(t.Proyecto, nombreProyecto, StringComparison.CurrentCultureIgnoreCase))
                .ToDictionary(t => t.Id, t => t.Proyecto);
            proyectos.RemoveAt(indice);
            foreach (var tarea in tareas.Where(t => asignaciones.ContainsKey(t.Id)))
                tarea.Proyecto = "";
            return new ProyectoEliminado(nombreProyecto, asignaciones);
        });
    }

    public void RestaurarProyecto(ProyectoEliminado eliminado)
    {
        EjecutarCambio(() =>
        {
            if (!proyectos.Contains(eliminado.Nombre, StringComparer.CurrentCultureIgnoreCase))
                proyectos.Add(eliminado.Nombre);
            foreach (var tarea in tareas)
                if (eliminado.Asignaciones.TryGetValue(tarea.Id, out string? proyecto)) tarea.Proyecto = proyecto;
            return true;
        });
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
        => repositorio.ExportarCopia(tareas, proyectos);

    public void ReemplazarDesdeArchivo(string ruta)
    {
        TareaSnapshot copia = repositorio.LeerCopia(ruta);
        var tareasNuevas = copia.Tareas;
        var proyectosNuevos = copia.Proyectos
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        foreach (var tarea in tareasNuevas)
        {
            Normalizar(tarea);
            string? proyecto = tarea.Proyecto;
            if (!string.IsNullOrWhiteSpace(proyecto) &&
                !proyectosNuevos.Contains(proyecto, StringComparer.CurrentCultureIgnoreCase))
                proyectosNuevos.Add(proyecto);
        }

        // Persistir y validar el reemplazo antes de cambiar el estado activo en memoria.
        repositorio.Guardar(tareasNuevas, proyectosNuevos);
        tareas.Clear();
        tareas.AddRange(tareasNuevas);
        proyectos.Clear();
        proyectos.AddRange(proyectosNuevos);
    }

    public void Anadir(Tarea tarea)
    {
        EjecutarCambio(() =>
        {
            if (tarea.Id == Guid.Empty) tarea.Id = Guid.NewGuid();
            tareas.Add(tarea);
            return true;
        });
    }

    public void Eliminar(Tarea tarea)
    {
        if (!tareas.Any(t => t.Id == tarea.Id)) return;
        EjecutarCambio(() =>
        {
            var original = tareas.FirstOrDefault(t => t.Id == tarea.Id);
            if (original != null) tareas.Remove(original);
            return true;
        });
    }

    public void Actualizar(Tarea original, Tarea actualizada)
    {
        int indice = tareas.FindIndex(t => t.Id == original.Id);
        if (indice < 0) return;

        EjecutarCambio(() =>
        {
            actualizada.Id = original.Id;
            tareas[indice] = actualizada;
            return true;
        });
    }

    public void MarcarHecha(Tarea ocurrencia, DateTime dia, bool hecha)
    {
        var original = tareas.FirstOrDefault(t => t.Id == ocurrencia.Id);
        if (original == null) return;

        EjecutarCambio(() =>
        {
            if (original.Recurrencia == RecurrenciaTarea.Ninguna)
                original.Hecha = hecha;
            else if (hecha)
            {
                if (!original.FechasCompletadas.Any(f => f.Date == dia.Date))
                    original.FechasCompletadas.Add(dia.Date);
            }
            else
                original.FechasCompletadas.RemoveAll(f => f.Date == dia.Date);

            return true;
        });
    }

    private TResult EjecutarCambio<TResult>(Func<TResult> cambio)
    {
        var tareasAnteriores = tareas
            .Select(tarea => (Referencia: tarea, Copia: Clonar(tarea)))
            .ToList();
        var proyectosAnteriores = proyectos.ToList();

        try
        {
            TResult resultado = cambio();
            Guardar();
            return resultado;
        }
        catch
        {
            foreach (var (referencia, copia) in tareasAnteriores)
                Copiar(copia, referencia);
            tareas.Clear();
            tareas.AddRange(tareasAnteriores.Select(par => par.Referencia));
            proyectos.Clear();
            proyectos.AddRange(proyectosAnteriores);
            throw;
        }
    }

    private static Tarea Clonar(Tarea tarea) => new()
    {
        Id = tarea.Id,
        Titulo = tarea.Titulo,
        Fecha = tarea.Fecha,
        ConHora = tarea.ConHora,
        Importante = tarea.Importante,
        Proyecto = tarea.Proyecto,
        Creada = tarea.Creada,
        Hecha = tarea.Hecha,
        Recurrencia = tarea.Recurrencia,
        FechasCompletadas = tarea.FechasCompletadas?.ToList() ?? new()
    };

    private static void Copiar(Tarea origen, Tarea destino)
    {
        destino.Titulo = origen.Titulo;
        destino.Fecha = origen.Fecha;
        destino.ConHora = origen.ConHora;
        destino.Importante = origen.Importante;
        destino.Proyecto = origen.Proyecto;
        destino.Creada = origen.Creada;
        destino.Hecha = origen.Hecha;
        destino.Recurrencia = origen.Recurrencia;
        destino.FechasCompletadas = origen.FechasCompletadas.ToList();
    }

    public int PendientesHoy() => TareaService.PendientesHoy(tareas);

    public List<Tarea> ObtenerParaDia(DateTime dia) => TareaService.ObtenerParaDia(tareas, dia);

    public bool TieneTareas(DateTime dia) => TareaService.TieneTareas(tareas, dia);

    public static bool EsDestacada(Tarea tarea, DateTime dia) => TareaService.EsDestacada(tarea, dia);
}

public sealed record ProyectoEliminado(string Nombre, Dictionary<Guid, string> Asignaciones);
