using Microsoft.Win32;
using TrayTasks.Diagnostics;

namespace TrayTasks.Views;

public partial class MainForm
{
    // ---------- Inicio automático con Windows ----------
    // Cada vez que se abre la app se registra la ruta de ese .exe para el próximo arranque
    static void EstablecerInicioAutomatico(bool habilitado)
    {
        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(ClaveInicio);
            if (habilitado)
                clave?.SetValue(NombreApp, $"\"{Application.ExecutablePath}\" --inicio-windows");
            else
                clave?.DeleteValue(NombreApp, false);
        }
        catch (Exception ex)
        {
            AppLogger.Advertencia("No se pudo actualizar el inicio automático de Windows.", ex);
        }
    }

    void CargarPreferencias()
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey(@"Software\TareasBandeja");
            if (clave?.GetValue("AvisosPrevios") is string avisosGuardados)
            {
                avisosPreviosMinutos = avisosGuardados
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(valor => int.TryParse(valor, out int minutos) ? minutos : 0)
                    .Where(IntervalosAvisoDisponibles.Contains)
                    .Distinct()
                    .OrderByDescending(minutos => minutos)
                    .ToArray();
            }
            else
            {
                int legado = Convert.ToInt32(clave?.GetValue("AvisoPrevio") ?? 60) <= 30 ? 30 : 60;
                avisosPreviosMinutos = new[] { legado, 15 };
            }
            int duracion = Convert.ToInt32(clave?.GetValue("DuracionNotificacion") ?? 2000);
            duracionNotificacion = duracion <= 2000 ? 2000 : duracion <= 4000 ? 4000 : 6000;
            sonidoHabilitado = Convert.ToInt32(clave?.GetValue("SonidoHabilitado") ?? 1) != 0;
            inicioAutomaticoHabilitado = Convert.ToInt32(clave?.GetValue("InicioAutomatico") ?? 1) != 0;
            temaClaroHabilitado = Convert.ToInt32(clave?.GetValue("TemaClaro") ?? 0) != 0;
            ubicacionNotificaciones = Math.Clamp(Convert.ToInt32(clave?.GetValue("UbicacionNotificaciones") ?? 0), 0, 3);
            mostrarVentanaAlIniciar = Convert.ToInt32(clave?.GetValue("MostrarAlIniciar") ?? 0) != 0;
        }
        catch (Exception ex) { AppLogger.Advertencia("No se pudieron cargar las preferencias; se usarán los valores predeterminados.", ex); }
        Notificador.DuracionAviso = duracionNotificacion;
        Notificador.Posicion = ubicacionNotificaciones;
        panelAjustes.CargarPreferencias(avisosPreviosMinutos, duracionNotificacion, sonidoHabilitado,
            inicioAutomaticoHabilitado, temaClaroHabilitado, ubicacionNotificaciones, mostrarVentanaAlIniciar);
    }

    void GuardarPreferencias()
    {
        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(@"Software\TareasBandeja");
            clave?.SetValue("AvisosPrevios", string.Join(",", avisosPreviosMinutos), RegistryValueKind.String);
            clave?.SetValue("AvisoPrevio", avisosPreviosMinutos.DefaultIfEmpty(30).Max(), RegistryValueKind.DWord);
            clave?.SetValue("DuracionNotificacion", duracionNotificacion, RegistryValueKind.DWord);
            clave?.SetValue("SonidoHabilitado", sonidoHabilitado ? 1 : 0, RegistryValueKind.DWord);
            clave?.SetValue("InicioAutomatico", inicioAutomaticoHabilitado ? 1 : 0, RegistryValueKind.DWord);
            clave?.SetValue("TemaClaro", temaClaroHabilitado ? 1 : 0, RegistryValueKind.DWord);
            clave?.SetValue("UbicacionNotificaciones", ubicacionNotificaciones, RegistryValueKind.DWord);
            clave?.SetValue("MostrarAlIniciar", mostrarVentanaAlIniciar ? 1 : 0, RegistryValueKind.DWord);
        }
        catch (Exception ex) { AppLogger.Advertencia("No se pudieron guardar las preferencias.", ex); }
    }

    void AplicarTema(bool claro)
    {
        var fondoAnterior = Ui.Fondo;
        var tarjetaAnterior = Ui.Tarjeta;
        var hoverAnterior = Ui.Hover;
        var separadorAnterior = Ui.Separador;
        var grisAnterior = Ui.Gris;
        var textoAnterior = Ui.Texto;
        Ui.EstablecerTema(claro);
        BackColor = Ui.Fondo;
        AplicarPaleta(this, fondoAnterior, tarjetaAnterior, hoverAnterior, separadorAnterior, grisAnterior, textoAnterior);
        Invalidate(true);
    }

    static void AplicarPaleta(Control control, Color fondoViejo, Color tarjetaVieja, Color hoverViejo,
        Color separadorViejo, Color grisViejo, Color textoViejo)
    {
        Color fondoOriginal = control.BackColor;
        bool superficieNeutra = fondoOriginal == fondoViejo || fondoOriginal == tarjetaVieja || fondoOriginal == hoverViejo;
        if (fondoOriginal == fondoViejo) control.BackColor = Ui.Fondo;
        else if (fondoOriginal == tarjetaVieja) control.BackColor = Ui.Tarjeta;
        else if (fondoOriginal == hoverViejo) control.BackColor = Ui.Hover;
        else if (fondoOriginal == separadorViejo) control.BackColor = Ui.Separador;
        if (control.ForeColor == grisViejo) control.ForeColor = Ui.Gris;
        else if (control.ForeColor == textoViejo &&
            (superficieNeutra || control is Label or TextBox or MaskedTextBox or ComboBox or CheckBox or Chip))
            control.ForeColor = Ui.Texto;
        if (control is RoundedPanel panel && panel.Fill == tarjetaVieja) panel.Fill = Ui.Tarjeta;
        if (control is Chip chip && chip.ColorInactivo == tarjetaVieja) chip.ColorInactivo = Ui.Tarjeta;
        if (control is RoundedPanel rounded) rounded.Invalidate();
        foreach (Control hijo in control.Controls) AplicarPaleta(hijo, fondoViejo, tarjetaVieja, hoverViejo, separadorViejo, grisViejo, textoViejo);
    }

    static ConfiguracionAtajo CargarConfiguracionAtajo()
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey(@"Software\TareasBandeja");
            uint modificadores = Convert.ToUInt32(clave?.GetValue("HotkeyModifiers") ?? (MOD_CONTROL | MOD_ALT));
            var tecla = (Keys)Convert.ToInt32(clave?.GetValue("HotkeyKey") ?? (int)Keys.T);
            if (tecla == Keys.None || modificadores == 0) throw new InvalidDataException();
            return new ConfiguracionAtajo(modificadores, tecla);
        }
        catch (Exception ex)
        {
            AppLogger.Advertencia("No se pudo leer el atajo de teclado guardado; se usará el atajo predeterminado.", ex);
            return new ConfiguracionAtajo(MOD_CONTROL | MOD_ALT, Keys.T);
        }
    }

    static void GuardarConfiguracionAtajo(ConfiguracionAtajo config)
    {
        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(@"Software\TareasBandeja");
            clave?.SetValue("HotkeyModifiers", config.Modificadores, RegistryValueKind.DWord);
            clave?.SetValue("HotkeyKey", (int)config.Tecla, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            AppLogger.Advertencia("No se pudo guardar el atajo de teclado.", ex);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tray.Dispose();
            iconoActual?.Dispose();
            reloj.Dispose();
            animacionFilas.Dispose();
            fNormal.Dispose();
            fSub.Dispose();
        }
        base.Dispose(disposing);
    }
}
