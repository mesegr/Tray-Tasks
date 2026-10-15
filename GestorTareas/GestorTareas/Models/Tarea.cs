namespace TareasBandeja.Models;

/// <summary>
/// Modelo persistido de una tarea.
/// </summary>
public class Tarea
{
    public string Titulo { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public bool ConHora { get; set; }
    public bool Importante { get; set; }
    public DateTime Creada { get; set; } = DateTime.Today;

    // Se conserva para poder leer tareas creadas por versiones antiguas.
    public bool Hecha { get; set; }
}
