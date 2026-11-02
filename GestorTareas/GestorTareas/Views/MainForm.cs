using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;
using TrayTasks.Controllers;
using TrayTasks.Diagnostics;
using TrayTasks.Models;
using TrayTasks.Services;

namespace TrayTasks.Views;

public partial class MainForm : Form
{
    const int IdAtajoVentana = 7001;
    const int WM_HOTKEY = 0x0312;
    const uint MOD_CONTROL = 0x0002;
    const uint MOD_ALT = 0x0001;
    const uint MOD_SHIFT = 0x0004;
    const uint MOD_WIN = 0x0008;
    static readonly int[] IntervalosAvisoDisponibles = { 1440, 720, 480, 240, 120, 60, 45, 30, 15 };

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    const string ClaveInicio = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string NombreApp = "TareasBandeja";
    static readonly CultureInfo es = new("es-ES");

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool DestroyIcon(IntPtr handle);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hWnd, string appName, string? idList);

    readonly TareaController controller = new();
    readonly RecordatorioScheduler recordatorioScheduler = new();
    readonly Stack<object> borradas = new();
    List<Tarea> visibles = new();     // lo que se muestra en la lista del día actual
    DateTime dia = DateTime.Today;    // día que se está viendo
    DateTime fechaNueva = DateTime.Today; // fecha elegida para la tarea que se va a crear

    readonly NotifyIcon tray = new();
    readonly Label lblTitulo = new();
    readonly Label lblSub = new();
    readonly Label lblVacio = new();
    readonly ChevronButton btnPrev = new();
    readonly ChevronButton btnNext = new();
    readonly SettingsButton btnAjustes = new();
    readonly CloseButton btnCerrar = new();
    readonly CalendarButton btnCalendario = new();
    readonly RoundedPanel tarjeta = new();
    readonly RoundedPanel barraFiltros = new();
    readonly RoundedPanel entrada = new();
    readonly RoundedPanel pildoraHora = new();
    readonly BufferedListBox lista = new();
    readonly AppTextBox txtBuscar = new();
    readonly SelectorRedondeado filtroTareas = new();
    readonly TextBox txtNueva = new();
    readonly AppMaskedTextBox txtHora = new();
    readonly PlusButton btnAnadir = new();
    readonly Chip chipFecha = new();
    readonly Chip chipImportante = new();
    readonly SelectorRedondeado repeticionNueva = new();
    readonly SelectorRedondeado proyectoNueva = new();
    readonly TaskScrollBar barraLista = new();
    readonly MiniCalendar calendario = new();
    readonly TareaEditPanel panelEdicion = new();
    readonly ProjectCreatePanel panelCrearProyecto = new(Array.Empty<string>());
    readonly ProjectManagerPanel panelGestionProyectos = new();
    readonly AjustesPanel panelAjustes = new();
    readonly UndoSnackbar snackbar = new();
    readonly System.Windows.Forms.Timer reloj = new();
    readonly System.Windows.Forms.Timer animacionFilas = new();
    readonly Dictionary<Guid, int> opacidadesFilas = new();

    readonly Font fNormal = new("Segoe UI", 11f);
    readonly Font fSub = new("Segoe UI", 8.5f);

    readonly float k;   // factor de escala según el zoom de Windows (100% = 1, 150% = 1.5...)
    int S(int v) => (int)Math.Round(v * k);
    float Sf(float v) => v * k;

    Icon? iconoActual;
    DateTime ultimoDia = DateTime.Today;
    int hover = -1;
    int[] avisosPreviosMinutos = { 60, 15 };
    int duracionNotificacion = 2000;
    int alturaVentanaNormal;
    bool sonidoHabilitado = true;
    bool inicioAutomaticoHabilitado = true;
    bool temaClaroHabilitado;
    bool mostrarVentanaAlIniciar;
    int ubicacionNotificaciones;
    readonly bool iniciadoPorWindows;
    bool salir;
    bool calendarioParaNavegar;
    bool primeraVez = true;
    bool actualizandoSelectorProyectos;
    string proyectoParaNueva = "";
    const string OpcionSinProyecto = "Sin proyecto";
    const string OpcionCrearProyecto = "＋ Crear proyecto…";
    const string OpcionGestionarProyectos = "⚙ Gestionar proyectos…";
    ToolStripMenuItem? itemDeshacerBorrado;
    Tarea? tareaEnEdicion;
    Tarea? tareaContextual;
    ConfiguracionAtajo configuracionAtajo;

    public MainForm(bool iniciadoPorWindows = false)
    {
        this.iniciadoPorWindows = iniciadoPorWindows;
        k = DeviceDpi / 96f;
        Ui.EstablecerRadioEsquina(S(16));
        configuracionAtajo = CargarConfiguracionAtajo();

        // --- Ventana: sin bordes, oscura, tipo "popover" ---
        AutoScaleMode = AutoScaleMode.None; // la escala la hacemos nosotros con S()
        Text = "Tareas";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Ui.Fondo;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        DoubleBuffered = true;

        int ancho = S(340);
        int margen = S(16);
        int hueco = S(10);
        int boton = S(32);

        // --- Cabecera ---
        lblTitulo.AutoSize = false;
        lblTitulo.Font = new Font("Segoe UI", 24f, FontStyle.Bold);
        lblTitulo.ForeColor = Color.White;
        lblTitulo.Cursor = Cursors.Hand;
        lblTitulo.Text = "Hoy";
        lblTitulo.Bounds = new Rectangle(S(20), S(8), S(220), lblTitulo.PreferredHeight);

        lblSub.AutoSize = false;
        lblSub.Font = new Font("Segoe UI", 10f);
        lblSub.ForeColor = Ui.Gris;
        lblSub.Text = "Fecha";
        lblSub.Bounds = new Rectangle(S(22), lblTitulo.Bottom - S(2), S(300), lblSub.PreferredHeight);

        btnNext.Derecha = true;
        btnAjustes.Text = "⚙";
        btnAjustes.Font = new Font("Segoe UI Symbol", 13f);
        btnAjustes.ForeColor = Ui.Gris;
        btnAjustes.BackColor = Ui.Fondo;
        btnAjustes.FlatStyle = FlatStyle.Flat;
        btnAjustes.FlatAppearance.BorderSize = 0;
        btnAjustes.Cursor = Cursors.Hand;
        btnAjustes.TabStop = false;
        int ladoBotonCabecera = S(28);
        int separacionBotonesCabecera = S(2);
        int yBotonesCabecera = S(16);
        btnCerrar.Bounds = new Rectangle(ancho - S(12) - ladoBotonCabecera, yBotonesCabecera, ladoBotonCabecera, ladoBotonCabecera);
        btnAjustes.Bounds = new Rectangle(btnCerrar.Left - separacionBotonesCabecera - ladoBotonCabecera, yBotonesCabecera, ladoBotonCabecera, ladoBotonCabecera);
        btnNext.Bounds = new Rectangle(btnAjustes.Left - separacionBotonesCabecera - ladoBotonCabecera, yBotonesCabecera, ladoBotonCabecera, ladoBotonCabecera);
        btnPrev.Bounds = new Rectangle(btnNext.Left - separacionBotonesCabecera - ladoBotonCabecera, yBotonesCabecera, ladoBotonCabecera, ladoBotonCabecera);
        btnCalendario.Bounds = new Rectangle(btnPrev.Left - separacionBotonesCabecera - ladoBotonCabecera, yBotonesCabecera, ladoBotonCabecera, ladoBotonCabecera);
        lblTitulo.Width = btnCalendario.Left - S(28);

        int y = lblSub.Bottom + S(8);

        barraFiltros.Bounds = new Rectangle(margen, y, ancho - 2 * margen, S(38));
        barraFiltros.Radius = Ui.RadioEsquina;
        barraFiltros.Fill = Ui.Tarjeta;
        barraFiltros.BackColor = Ui.Fondo;
        txtBuscar.Bounds = new Rectangle(S(12), 0, S(132), S(38));
        txtBuscar.BorderStyle = BorderStyle.None;
        txtBuscar.BackColor = Ui.Tarjeta;
        txtBuscar.ForeColor = Color.White;
        txtBuscar.Font = new Font("Segoe UI", 9.5f);
        txtBuscar.PlaceholderText = "Buscar tareas";
        txtBuscar.Location = new Point(S(12), (barraFiltros.Height - txtBuscar.PreferredHeight) / 2);
        filtroTareas.Bounds = new Rectangle(S(150), 0, Math.Max(S(120), barraFiltros.Width - S(162)), S(38));
        filtroTareas.BackColor = Ui.Tarjeta;
        filtroTareas.ForeColor = Color.White;
        filtroTareas.Font = new Font("Segoe UI", 11f);
        filtroTareas.Items.AddRange(new object[] { "Todas", "Pendientes", "Hechas", "Importantes", "Vencidas" });
        filtroTareas.SelectedIndex = 0;
        barraFiltros.Controls.AddRange(new Control[] { txtBuscar, filtroTareas });

        y = barraFiltros.Bottom + S(8);

        // --- Tarjeta con la lista ---
        tarjeta.Bounds = new Rectangle(margen, y, ancho - 2 * margen, S(242));
        tarjeta.Padding = new Padding(0, S(10), 0, S(10));
        tarjeta.Radius = Ui.RadioEsquina;
        tarjeta.BackColor = Ui.Fondo;

        lista.Dock = DockStyle.Fill;
        lista.BorderStyle = BorderStyle.None;
        lista.BackColor = Ui.Tarjeta;
        lista.DrawMode = DrawMode.OwnerDrawFixed;
        lista.ItemHeight = S(52);
        lista.SelectionMode = SelectionMode.None;
        lista.IntegralHeight = false;
        // Cubre completamente la barra nativa del ListBox; el fondo de la tarjeta queda continuo.
        barraLista.Bounds = new Rectangle(tarjeta.Width - S(24), S(8), S(24), tarjeta.Height - S(16));
        barraLista.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        barraLista.Conectar(lista);

        lblVacio.Dock = DockStyle.Fill;
        lblVacio.Text = "No hay tareas";
        lblVacio.Font = new Font("Segoe UI", 10.5f);
        lblVacio.ForeColor = Ui.Gris;
        lblVacio.BackColor = Ui.Tarjeta;
        lblVacio.TextAlign = ContentAlignment.MiddleCenter;
        lblVacio.Visible = false;

        tarjeta.Controls.Add(lista);
        tarjeta.Controls.Add(barraLista);
        tarjeta.Controls.Add(lblVacio);
        barraLista.BringToFront();

        y = tarjeta.Bottom + S(12);

        // --- Campo de texto en forma de píldora + botón "+" ---
        int anchoRepeticion = S(116);
        int altoCampos = S(32);
        proyectoNueva.Bounds = new Rectangle(margen, y, ancho - 2 * margen, altoCampos);
        proyectoNueva.BackColor = Ui.Tarjeta;
        proyectoNueva.ForeColor = Color.White;
        proyectoNueva.Font = new Font("Segoe UI", 9.5f);
        proyectoNueva.Items.Add(OpcionSinProyecto);
        proyectoNueva.Items.Add(OpcionCrearProyecto);
        proyectoNueva.SelectedIndex = 0;

        int yEntrada = proyectoNueva.Bottom + S(8);
        int yCampos = yEntrada + (boton - altoCampos) / 2;
        entrada.Bounds = new Rectangle(margen, yEntrada,
            ancho - 2 * margen - boton - anchoRepeticion - 2 * hueco, altoCampos);
        entrada.Radius = Ui.RadioEsquina;
        entrada.BackColor = Ui.Fondo;

        txtNueva.BorderStyle = BorderStyle.None;
        txtNueva.BackColor = Ui.Tarjeta;
        txtNueva.ForeColor = Color.White;
        txtNueva.Font = new Font("Segoe UI", 9.5f);
        txtNueva.PlaceholderText = "Nueva tarea";
        txtNueva.MaxLength = 100;
        txtNueva.Width = entrada.Width - S(36);
        txtNueva.Location = new Point(S(18), (entrada.Height - txtNueva.Height) / 2);
        entrada.Controls.Add(txtNueva);

        repeticionNueva.Bounds = new Rectangle(entrada.Right + hueco, yCampos, anchoRepeticion, altoCampos);
        repeticionNueva.BackColor = Ui.Tarjeta;
        repeticionNueva.ForeColor = Color.White;
        repeticionNueva.Font = new Font("Segoe UI", 9.5f);
        repeticionNueva.Items.AddRange(new object[] { "No se repite", "Cada día", "Cada semana", "Cada mes" });
        repeticionNueva.SelectedIndex = 0;

        btnAnadir.Bounds = new Rectangle(repeticionNueva.Right + hueco, yEntrada, boton, boton);
        y = entrada.Bottom + S(8);

        // --- Fila de opciones: fecha, hora e importante ---
        int altoChip = S(32);
        int separacion = S(8);

        chipFecha.Font = new Font("Segoe UI", 9.5f);
        chipFecha.Bounds = new Rectangle(margen, y, S(108), altoChip);

        pildoraHora.Bounds = new Rectangle(chipFecha.Right + separacion, y, S(70), altoChip);
        pildoraHora.Radius = Ui.RadioEsquina;
        pildoraHora.BackColor = Ui.Fondo;

        txtHora.Mask = "00:00";
        txtHora.PromptChar = '-';
        txtHora.HidePromptOnLeave = false;
        txtHora.BorderStyle = BorderStyle.None;
        txtHora.BackColor = Ui.Tarjeta;
        txtHora.ForeColor = Color.White;
        txtHora.Font = new Font("Segoe UI", 10f);
        txtHora.TextAlign = HorizontalAlignment.Center;
        txtHora.Width = S(54);
        txtHora.Height = S(26);
        txtHora.Location = new Point((pildoraHora.Width - txtHora.Width) / 2, (pildoraHora.Height - txtHora.Height) / 2);
        pildoraHora.Controls.Add(txtHora);

        chipImportante.Font = new Font("Segoe UI", 9.5f);
        chipImportante.Text = "Importante";
        chipImportante.ConCasilla = true;
        chipImportante.ColorActivo = Ui.Naranja;
        int xImp = pildoraHora.Right + separacion;
        chipImportante.Bounds = new Rectangle(xImp, y, ancho - margen - xImp, altoChip);

        ClientSize = new Size(ancho, chipFecha.Bottom + margen);
        alturaVentanaNormal = ClientSize.Height;

        // --- Calendario (aparece encima de la lista al pulsar la fecha) ---
        calendario.Bounds = tarjeta.Bounds;
        calendario.Radius = Ui.RadioEsquina;
        calendario.Font = new Font("Segoe UI", 10f);
        calendario.CantidadTareas = fecha => controller.ObtenerParaDia(fecha).Count(t => !t.Hecha);
        calendario.Visible = false;

        panelEdicion.Bounds = tarjeta.Bounds;
        panelEdicion.Radius = Ui.RadioEsquina;
        panelEdicion.Visible = false;
        panelEdicion.Guardado += (_, tareaEditada) =>
        {
            if (tareaEnEdicion == null) return;
            controller.Actualizar(tareaEnEdicion, tareaEditada);
            tareaEnEdicion = null;
            panelEdicion.Visible = false;
            Refrescar();
            AnimarTarea(tareaEditada);
        };
        panelEdicion.Cancelado += (_, _) =>
        {
            tareaEnEdicion = null;
            panelEdicion.Visible = false;
        };

        panelCrearProyecto.Bounds = tarjeta.Bounds;
        panelCrearProyecto.Radius = Ui.RadioEsquina;
        panelCrearProyecto.Visible = false;
        panelCrearProyecto.Guardado += nombreProyecto =>
        {
            string proyecto = controller.CrearProyecto(nombreProyecto);
            panelCrearProyecto.Visible = false;
            ActualizarSelectorProyectos(proyecto);
            txtNueva.Focus();
        };
        panelCrearProyecto.Cancelado += (_, _) =>
        {
            panelCrearProyecto.Visible = false;
            ActualizarSelectorProyectos(proyectoParaNueva);
            txtNueva.Focus();
        };

        panelGestionProyectos.Bounds = tarjeta.Bounds;
        panelGestionProyectos.Radius = Ui.RadioEsquina;
        panelGestionProyectos.Visible = false;
        panelGestionProyectos.CrearSolicitado += nombre =>
        {
            try
            {
                string creado = controller.CrearProyecto(nombre);
                panelGestionProyectos.CargarProyectos(controller.Proyectos, creado);
                ActualizarSelectorProyectos(creado);
                panelEdicion.CargarProyectos(controller.Proyectos);
            }
            catch (Exception ex) { panelGestionProyectos.MostrarError(ex.Message); }
        };
        panelGestionProyectos.RenombrarSolicitado += (anterior, nuevo) =>
        {
            try
            {
                string renombrado = controller.RenombrarProyecto(anterior, nuevo);
                if (string.Equals(proyectoParaNueva, anterior, StringComparison.CurrentCultureIgnoreCase))
                    proyectoParaNueva = renombrado;
                panelGestionProyectos.CargarProyectos(controller.Proyectos, renombrado);
                ActualizarSelectorProyectos(proyectoParaNueva);
                panelEdicion.CargarProyectos(controller.Proyectos);
            }
            catch (Exception ex) { panelGestionProyectos.MostrarError(ex.Message); }
        };
        panelGestionProyectos.EliminarSolicitado += nombre =>
        {
            var eliminado = controller.EliminarProyecto(nombre);
            if (eliminado == null) return;
            borradas.Push(eliminado);
            if (itemDeshacerBorrado != null) itemDeshacerBorrado.Enabled = true;
            if (string.Equals(proyectoParaNueva, nombre, StringComparison.CurrentCultureIgnoreCase))
                proyectoParaNueva = "";
            panelGestionProyectos.CargarProyectos(controller.Proyectos);
            ActualizarSelectorProyectos(proyectoParaNueva);
            panelEdicion.CargarProyectos(controller.Proyectos);
            Refrescar();
            snackbar.MostrarProyecto(nombre);
        };
        panelGestionProyectos.Cancelado += (_, _) =>
        {
            panelGestionProyectos.Visible = false;
            ActualizarSelectorProyectos(proyectoParaNueva);
            txtNueva.Focus();
        };

        panelAjustes.Bounds = new Rectangle(tarjeta.Left, tarjeta.Top, tarjeta.Width,
            Math.Max(S(320), alturaVentanaNormal - tarjeta.Top - margen));
        panelAjustes.Radius = Ui.RadioEsquina;
        panelAjustes.RecolocarBotones();
        panelAjustes.Visible = false;
        panelAjustes.Editar(configuracionAtajo);
        CargarPreferencias();
        panelAjustes.TemaCambiado += (_, _) =>
        {
            temaClaroHabilitado = panelAjustes.TemaClaroHabilitado;
            AplicarTema(temaClaroHabilitado);
            GuardarPreferencias();
        };
        panelAjustes.Guardado += (_, nuevoAtajo) =>
        {
            if (!AplicarAtajo(nuevoAtajo))
            {
                MessageBox.Show(this, "Esa combinación ya está siendo utilizada por otra aplicación.",
                    "Atajo no disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            configuracionAtajo = nuevoAtajo;
            GuardarConfiguracionAtajo(nuevoAtajo);
            avisosPreviosMinutos = panelAjustes.AvisosPrevios;
            duracionNotificacion = panelAjustes.DuracionNotificacion;
            sonidoHabilitado = panelAjustes.SonidoHabilitado;
            inicioAutomaticoHabilitado = panelAjustes.InicioAutomaticoHabilitado;
            temaClaroHabilitado = panelAjustes.TemaClaroHabilitado;
            mostrarVentanaAlIniciar = panelAjustes.MostrarAlIniciarHabilitado;
            ubicacionNotificaciones = panelAjustes.UbicacionNotificacion;
            Notificador.DuracionAviso = duracionNotificacion;
            Notificador.Posicion = ubicacionNotificaciones;
            GuardarPreferencias();
            EstablecerInicioAutomatico(inicioAutomaticoHabilitado);
            AplicarTema(temaClaroHabilitado);
            MostrarAjustes(false);
        };
        panelAjustes.Cancelado += (_, _) => MostrarAjustes(false);

        snackbar.Bounds = new Rectangle(S(12), ClientSize.Height - S(52), ClientSize.Width - S(24), S(42));
        snackbar.DeshacerSolicitado += (_, _) => DeshacerBorrado();

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblSub, btnPrev, btnNext, btnAjustes, btnCerrar, barraFiltros, tarjeta, entrada, btnAnadir,
            proyectoNueva, chipFecha, btnCalendario, pildoraHora, chipImportante, repeticionNueva, calendario, panelEdicion, panelCrearProyecto, panelGestionProyectos, panelAjustes, snackbar
        });
        btnPrev.BringToFront();
        btnNext.BringToFront();
        btnCalendario.BringToFront();
        btnAjustes.BringToFront();
        btnCerrar.BringToFront();
        AplicarTema(temaClaroHabilitado);
        AcceptButton = btnAnadir; // Enter = añadir
        Shown += (_, _) =>
        {
            if (!this.iniciadoPorWindows || !mostrarVentanaAlIniciar) return;
            var area = Screen.PrimaryScreen!.WorkingArea;
            Location = new Point(area.Right - Width - S(12), area.Bottom - Height - S(12));
        };

        // --- Icono de bandeja y menú oscuro ---
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Renderer = new ToolStripProfessionalRenderer(new ColoresOscuros()) { RoundedEdges = false },
            BackColor = Ui.Tarjeta,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f)
        };
        menu.Items.Add("Abrir", null, (s, e) => BeginInvoke(new Action(Mostrar)));
        menu.Items.Add("Exportar copia…", null, (s, e) => ExportarCopia());
        menu.Items.Add("Restaurar copia…", null, (s, e) => RestaurarCopia());
        menu.Items.Add("Acerca de TrayTasks", null, (s, e) =>
        {
            using var acercaDe = new AboutForm();
            acercaDe.ShowDialog(this);
        });
        menu.Items.Add(new ToolStripSeparator());
        itemDeshacerBorrado = new ToolStripMenuItem("Deshacer borrado");
        menu.Items.Add(itemDeshacerBorrado);
        itemDeshacerBorrado.Enabled = false;
        itemDeshacerBorrado.Click += (s, e) => DeshacerBorrado();
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (s, e) => { salir = true; tray.Visible = false; Application.Exit(); });
        tray.ContextMenuStrip = menu;
        // Clic en el icono: abre el panel; otro clic en el icono: lo cierra
        tray.MouseClick += (s, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (Visible) Hide(); else Mostrar();
        };

        // --- Datos ---
        try
        {
            controller.Cargar();
            if (controller.AdvertenciasRecuperacion.Count > 0)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, controller.AdvertenciasRecuperacion) +
                    $"{Environment.NewLine}{Environment.NewLine}Puedes revisar los detalles en: {AppLogger.RutaLogs}",
                    "TrayTasks recuperó datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"No se pudieron cargar los datos. Para evitar sobrescribirlos, TrayTasks se cerrará. Registro: {AppLogger.RutaLogs}", ex);
        }
        ActualizarSelectorProyectos();
        ActualizarChipFecha();
        Refrescar();
        tray.Visible = true;

        // Arrancar automáticamente con Windows si la opción sigue activada.
        EstablecerInicioAutomatico(inicioAutomaticoHabilitado);

        // Aviso al arrancar si tienes tareas pendientes hoy
        int pendientes = PendientesHoy();
        if (pendientes > 0)
            Notificador.Mostrar("Tareas de hoy", $"Tienes {pendientes} tarea(s) pendiente(s)", sonidoHabilitado, duracionNotificacion);

        // --- Eventos ---
        btnPrev.Click += (s, e) => IrADia(dia.AddDays(-1));
        btnNext.Click += (s, e) => IrADia(dia.AddDays(1));
        btnAjustes.Click += (s, e) =>
        {
            if (panelAjustes.Visible)
            {
                MostrarAjustes(false);
                return;
            }

            calendario.Visible = false;
            panelEdicion.Visible = false;
            panelAjustes.Editar(configuracionAtajo);
            panelAjustes.CargarPreferencias(avisosPreviosMinutos, duracionNotificacion, sonidoHabilitado,
                inicioAutomaticoHabilitado, temaClaroHabilitado, ubicacionNotificaciones, mostrarVentanaAlIniciar);
            MostrarAjustes(true);
        };
        btnCerrar.Click += (s, e) => Hide();
        lblTitulo.Click += (s, e) => IrADia(DateTime.Today);
        btnAnadir.Click += (s, e) => Anadir();
        proyectoNueva.SelectedIndexChanged += (_, _) => CambiarProyectoNueva();
        chipFecha.Click += (s, e) => AlternarCalendario();
        btnCalendario.Click += (s, e) => AlternarCalendario(navegarDia: true);
        calendario.DiaElegido += (s, d) =>
        {
            calendario.Visible = false;
            if (calendarioParaNavegar)
                IrADia(d);
            else
            {
                fechaNueva = d.Date;
                ActualizarChipFecha();
            }
            txtNueva.Focus();
        };
        txtHora.TextChanged += (s, e) => txtHora.ForeColor = Color.White;

        lista.DrawItem += DibujarItem;
        txtBuscar.TextChanged += (_, _) => Refrescar();
        filtroTareas.SelectedIndexChanged += (_, _) => Refrescar();
        lista.MouseMove += (s, e) =>
        {
            int i = lista.IndexFromPoint(e.Location);
            if (i != hover) { hover = i; lista.Invalidate(); }
        };
        lista.MouseLeave += (s, e) =>
        {
            hover = -1;
            lista.Invalidate();
        };
        lista.MouseDown += Lista_MouseDown;
        lista.DoubleClick += Lista_DoubleClick;
        ConfigurarMenuTareas();
        animacionFilas.Interval = 16;
        animacionFilas.Tick += (_, _) =>
        {
            foreach (var id in opacidadesFilas.Keys.ToList())
            {
                opacidadesFilas[id] = Math.Min(255, opacidadesFilas[id] + 34);
                if (opacidadesFilas[id] >= 255) opacidadesFilas.Remove(id);
            }
            lista.Invalidate();
            if (opacidadesFilas.Count == 0) animacionFilas.Stop();
        };

        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (calendario.Visible) calendario.Visible = false;
                else if (panelAjustes.Visible) MostrarAjustes(false);
                else if (panelEdicion.Visible) panelEdicion.Visible = false;
                else if (panelCrearProyecto.Visible) panelCrearProyecto.Cancelar();
                else if (panelGestionProyectos.Visible) panelGestionProyectos.Cancelar();
                else if (Visible) Hide();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.F) { txtBuscar.Focus(); txtBuscar.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.N) { txtNueva.Focus(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Z) { DeshacerBorrado(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Left) { IrADia(dia.AddDays(-1)); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Right) { IrADia(dia.AddDays(1)); e.Handled = true; }
            else if (e.KeyCode == Keys.F2 && hover >= 0 && hover < visibles.Count) { AbrirEdicion(visibles[hover]); e.Handled = true; }
            else if (e.KeyCode == Keys.Space && lista.Focused && hover >= 0 && hover < visibles.Count)
            {
                var tarea = visibles[hover];
                controller.MarcarHecha(tarea, dia, !tarea.Hecha);
                Refrescar();
                AnimarTarea(tarea);
                e.Handled = true;
            }
        };

        // Cada minuto comprueba si ha cambiado el día (medianoche)
        reloj.Interval = 60_000;
        reloj.Tick += (s, e) =>
        {
            if (DateTime.Today != ultimoDia)
            {
                ultimoDia = DateTime.Today;
                IrADia(DateTime.Today);
            }

            ComprobarRecordatorios();
        };
        ComprobarRecordatorios(alIniciar: true);
        reloj.Start();
    }

    // ---------- Comportamiento de la ventana ----------

    // La app arranca oculta (solo el icono de bandeja)
    protected override void SetVisibleCore(bool value)
    {
        if (primeraVez)
        {
            primeraVez = false;
            value = value && iniciadoPorWindows && mostrarVentanaAlIniciar;
            if (!IsHandleCreated) CreateHandle();
        }
        base.SetVisibleCore(value);
    }

    // Sombra para la ventana sin bordes
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
            return cp;
        }
    }

    // Esquinas redondeadas (Windows 11 nativo; en Windows 10 con una región)
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterHotKey(Handle, IdAtajoVentana, configuracionAtajo.Modificadores, (uint)configuracionAtajo.Tecla);
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            int redondeo = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(Handle, 33, ref redondeo, sizeof(int));
        }
        else
        {
            using var path = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina);
            Region = new Region(path);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (IsHandleCreated && Environment.OSVersion.Version.Build < 22000)
        {
            using var path = Ui.RoundedRect(new Rectangle(0, 0, Width, Height), Ui.RadioEsquina);
            Region = new Region(path);
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (IsHandleCreated)
            UnregisterHotKey(Handle, IdAtajoVentana);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == IdAtajoVentana)
        {
            if (Visible) Hide();
            else Mostrar();
            return;
        }

        base.WndProc(ref m);
    }

    bool AplicarAtajo(ConfiguracionAtajo nuevo)
    {
        UnregisterHotKey(Handle, IdAtajoVentana);
        if (RegisterHotKey(Handle, IdAtajoVentana, nuevo.Modificadores, (uint)nuevo.Tecla))
            return true;

        RegisterHotKey(Handle, IdAtajoVentana, configuracionAtajo.Modificadores, (uint)configuracionAtajo.Tecla);
        return false;
    }

    // Al cerrar no se cierra la app: se oculta
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!salir && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    void Mostrar()
    {
        calendario.Visible = false;
        IrADia(DateTime.Today);
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - S(12), area.Bottom - Height - S(12));
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        SetForegroundWindow(Handle);
        txtNueva.Focus();
    }

    void MostrarAjustes(bool mostrar)
    {
        if (mostrar) CerrarPanelesSuperpuestos();
        panelAjustes.Visible = mostrar;
        snackbar.Bounds = new Rectangle(S(12), ClientSize.Height - S(52), ClientSize.Width - S(24), S(42));
        if (Visible)
        {
            var area = Screen.FromControl(this).WorkingArea;
            Location = new Point(Math.Min(Location.X, area.Right - Width - S(12)), area.Bottom - Height - S(12));
        }
        if (mostrar) panelAjustes.BringToFront();
    }

    void CerrarPanelesSuperpuestos()
    {
        calendario.Visible = false;
        panelEdicion.Visible = false;
        panelCrearProyecto.Visible = false;
        panelGestionProyectos.Visible = false;
        panelAjustes.Visible = false;
    }
}
