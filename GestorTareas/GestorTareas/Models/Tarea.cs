namespace TareasBandeja.Models;

/// <summary>
/// Modelo persistido de una tarea.
/// </summary>
public class Tarea
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public bool ConHora { get; set; }
    public bool Importante { get; set; }
    public string Proyecto { get; set; } = "";
    public DateTime Creada { get; set; } = DateTime.Today;

    // Se conserva para poder leer tareas creadas por versiones antiguas.
    public bool Hecha { get; set; }
    public RecurrenciaTarea Recurrencia { get; set; }
    public List<DateTime> FechasCompletadas { get; set; } = new();
}

public enum RecurrenciaTarea
{
    Ninguna,
    Diaria,
    Semanal,
    Mensual
}
