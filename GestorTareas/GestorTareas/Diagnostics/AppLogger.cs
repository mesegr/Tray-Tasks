using System.Text;

namespace TareasBandeja.Diagnostics;

/// <summary>Registro local mínimo para diagnosticar fallos sin incluir el contenido de las tareas.</summary>
public static class AppLogger
{
    private static readonly object sincronizacion = new();
    private static readonly string directorioLogs = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrayTasks", "Logs");

    public static string RutaLogs => directorioLogs;

    public static void Advertencia(string contexto, Exception? excepcion = null) =>
        Escribir("WARN", contexto, excepcion);

    public static void Error(string contexto, Exception excepcion) =>
        Escribir("ERROR", contexto, excepcion);

    private static void Escribir(string nivel, string contexto, Exception? excepcion)
    {
        try
        {
            lock (sincronizacion)
            {
                Directory.CreateDirectory(directorioLogs);
                string ruta = Path.Combine(directorioLogs, $"TrayTasks-{DateTime.Today:yyyy-MM-dd}.log");
                var entrada = new StringBuilder()
                    .Append(DateTimeOffset.Now.ToString("O"))
                    .Append(" [").Append(nivel).Append("] ")
                    .AppendLine(contexto);
                if (excepcion != null) entrada.AppendLine(excepcion.ToString());
                entrada.AppendLine();
                File.AppendAllText(ruta, entrada.ToString(), Encoding.UTF8);
                LimpiarLogsAntiguos();
            }
        }
        catch
        {
            // Un fallo del logger nunca debe impedir que la aplicación siga con su gestión de errores.
        }
    }

    private static void LimpiarLogsAntiguos()
    {
        foreach (string archivo in Directory.GetFiles(directorioLogs, "TrayTasks-*.log")
                     .OrderByDescending(File.GetLastWriteTimeUtc)
                     .Skip(14))
        {
            try { File.Delete(archivo); }
            catch { /* La limpieza no es crítica. */ }
        }
    }
}
