using TareasBandeja.Views;
using TareasBandeja.Diagnostics;

namespace TareasBandeja;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Evita abrir dos copias (si no, saldrían dos iconos)
        using var mutex = new Mutex(true, "TareasBandejaUnica", out bool esLaPrimera);
        if (!esLaPrimera) return;

        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                AppLogger.Error("Excepción no controlada en el hilo de interfaz.", e.Exception);
                MessageBox.Show(
                    $"Ha ocurrido un error inesperado. La aplicación intentará continuar.\n\nRegistro: {AppLogger.RutaLogs}",
                    "TrayTasks", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception exception)
                    AppLogger.Error("Excepción no controlada en la aplicación.", exception);
            };

            Application.Run(new MainForm(args.Contains("--inicio-windows", StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception ex)
        {
            AppLogger.Error("No se pudo iniciar TrayTasks.", ex);
            MessageBox.Show(
                $"No se pudo iniciar TrayTasks. No se han sobrescrito los datos.\n\n{ex.Message}\n\nRegistro: {AppLogger.RutaLogs}",
                "Error al iniciar TrayTasks", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
