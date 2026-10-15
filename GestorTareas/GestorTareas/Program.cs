using TareasBandeja.Views;

namespace TareasBandeja;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Evita abrir dos copias (si no, saldrían dos iconos)
        using var mutex = new Mutex(true, "TareasBandejaUnica", out bool esLaPrimera);
        if (!esLaPrimera) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
