using System.Drawing.Drawing2D;
using System.Globalization;
using TareasBandeja.Controllers;
using TareasBandeja.Diagnostics;
using TareasBandeja.Models;

namespace TareasBandeja.Views;

public partial class MainForm
{
    // ---------- Textos de fechas ----------
    static string FechaTxt(DateTime f)
    {
        var hoy = DateTime.Today;
        if (f.Date == hoy) return "hoy";
        if (f.Date == hoy.AddDays(1)) return "mañana";
        if (f.Date == hoy.AddDays(-1)) return "ayer";
        return f.ToString("d MMM yyyy", es).Replace(".", "");
    }

    void ActualizarChipFecha()
    {
        chipFecha.Text = fechaNueva.ToString("d MMM yyyy", es).Replace(".", "");
    }

    void AlternarCalendario(bool navegarDia = false)
    {
        if (calendario.Visible)
        {
            calendario.Visible = false;
            return;
        }
        CerrarPanelesSuperpuestos();
        calendarioParaNavegar = navegarDia;
        calendario.Seleccionado = navegarDia ? dia : fechaNueva;
        calendario.Visible = true;
        calendario.BringToFront();
    }

    // ---------- Dibujo de cada fila de la lista ----------
    bool EsDestacada(Tarea t) => TareaController.EsDestacada(t, dia);

    // Texto pequeño bajo el título (hora o entrega) y su color
    (string? texto, Color color) Subtitulo(Tarea t, bool destacada)
    {
        string hora = t.ConHora ? " · " + t.Fecha.ToString("HH:mm") : "";
        string? proyecto = string.IsNullOrWhiteSpace(t.Proyecto) ? null : $"Proyecto: {t.Proyecto}";
        string? Combinar(string? detalle) => string.Join(" · ", new[] { detalle, proyecto }
            .Where(parte => !string.IsNullOrWhiteSpace(parte)));

        if (destacada)
        {
            bool vencida = t.ConHora ? t.Fecha < DateTime.Now : t.Fecha.Date < DateTime.Today;
            return vencida
                ? (Combinar($"Vencida · {FechaTxt(t.Fecha)}{hora}"), Ui.Rojo)
                : (Combinar($"Entrega {FechaTxt(t.Fecha)}{hora}"), Ui.Naranja);
        }
        if (t.Importante) return (Combinar($"Entrega {FechaTxt(t.Fecha)}{hora}"), Ui.Gris);
        if (t.ConHora) return (Combinar(t.Fecha.ToString("HH:mm")), Ui.Gris);
        return (proyecto == null ? null : Combinar(null), Ui.Gris);
    }

    void DibujarItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= visibles.Count) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = visibles[e.Index];
        var r = e.Bounds;
        bool destacada = EsDestacada(t);

        // Fondo (con un tono más claro al pasar el ratón)
        using (var fondo = new SolidBrush(e.Index == hover ? Ui.Hover : Ui.Tarjeta))
            g.FillRectangle(fondo, r);

        // Línea separadora
        if (e.Index < visibles.Count - 1)
        {
            using var sep = new Pen(Ui.Separador, 1f);
            g.DrawLine(sep, r.Left + S(40), r.Bottom - 1, r.Right - S(12), r.Bottom - 1);
        }

        // Indicador circular a juego con las casillas de Ajustes.
        int lado = S(19);
        var p = new Rectangle(r.Left + S(12), r.Top + (r.Height - lado) / 2, lado, lado);
        if (t.Hecha)
        {
            using var fondoPunto = new SolidBrush(Ui.Azul);
            g.FillEllipse(fondoPunto, p);
            using var tick = new Pen(Color.White, Sf(1.8f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(tick, new[]
            {
                new PointF(p.Left + lado * 0.24f, p.Top + lado * 0.53f),
                new PointF(p.Left + lado * 0.43f, p.Top + lado * 0.70f),
                new PointF(p.Left + lado * 0.76f, p.Top + lado * 0.32f)
            });
        }
        else
        {
            using var fondoPunto = new SolidBrush(Ui.Hover);
            using var bordePunto = new Pen(t.Importante ? Ui.Naranja : Ui.Separador, Sf(1.4f));
            g.FillEllipse(fondoPunto, p);
            g.DrawEllipse(bordePunto, p);
        }

        // Título y, si hay, el texto pequeño de debajo
        int xTexto = r.Left + S(40);
        int anchoTexto = r.Width - S(40) - S(40);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        var (sub, colorSub) = Subtitulo(t, destacada);
        int opacidad = opacidadesFilas.GetValueOrDefault(t.Id, 255);
        Color colorTitulo = Mezclar(Ui.Tarjeta, t.Hecha ? Ui.Gris : Ui.Texto, opacidad);
        bool mostrandoAyuda = e.Index == hover;
        if (sub == null && !mostrandoAyuda)
        {
            TextRenderer.DrawText(g, t.Titulo, fNormal, new Rectangle(xTexto, r.Top, anchoTexto, r.Height), colorTitulo, flags);
        }
        else
        {
            TextRenderer.DrawText(g, t.Titulo, fNormal, new Rectangle(xTexto, r.Top + S(6), anchoTexto, S(24)), colorTitulo, flags);
            var lineaSecundaria = new Rectangle(xTexto, r.Top + S(28), anchoTexto, S(18));
            if (mostrandoAyuda)
            {
                const string ayuda = "Doble clic para editar";
                int anchoAyuda = Math.Min(S(124), lineaSecundaria.Width);
                var areaAyuda = new Rectangle(lineaSecundaria.Left, lineaSecundaria.Top,
                    anchoAyuda, lineaSecundaria.Height);
                TextRenderer.DrawText(g, ayuda, fSub, areaAyuda, Ui.Gris,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                if (sub != null)
                {
                    lineaSecundaria.X += anchoAyuda + S(8);
                    lineaSecundaria.Width = Math.Max(0, lineaSecundaria.Width - anchoAyuda - S(8));
                    TextRenderer.DrawText(g, sub, fSub, lineaSecundaria, colorSub, flags);
                }
            }
            else if (sub != null)
                TextRenderer.DrawText(g, sub, fSub, lineaSecundaria, colorSub, flags);
        }
        if (t.Hecha)
        {
            var tituloTamano = TextRenderer.MeasureText(t.Titulo, fNormal, new Size(anchoTexto, S(24)), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            float yTachado = sub == null && !mostrandoAyuda
                ? r.Top + r.Height / 2f
                : r.Top + S(18);
            using var tachado = new Pen(Ui.Gris, Math.Max(1f, Sf(1f)));
            g.DrawLine(tachado, xTexto, yTachado, xTexto + Math.Min(anchoTexto, tituloTamano.Width), yTachado);
        }

        // Aspa para borrar: siempre visible en gris y roja al pasar el ratón por la fila
        {
            float cx = r.Right - S(24), cy = r.Top + r.Height / 2f, medio = Sf(5f);
            using var pen = new Pen(e.Index == hover ? Ui.Rojo : Color.FromArgb(99, 99, 102), Sf(1.8f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLine(pen, cx - medio, cy - medio, cx + medio, cy + medio);
            g.DrawLine(pen, cx - medio, cy + medio, cx + medio, cy - medio);
        }
    }

    void Lista_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        int i = lista.IndexFromPoint(e.Location);
        if (i < 0 || i >= visibles.Count) return;

        if (e.X <= S(34))
        {
            var tarea = visibles[i];
            controller.MarcarHecha(tarea, dia, !tarea.Hecha);
            Refrescar();
            AnimarTarea(tarea);
            return;
        }

        // El aspa sigue siendo el acceso rápido para borrar.
        if (e.X <= lista.ClientSize.Width - S(44)) return;

        var eliminada = visibles[i];
        controller.Eliminar(eliminada);
        borradas.Push(eliminada);
        if (itemDeshacerBorrado != null) itemDeshacerBorrado.Enabled = true;
        Refrescar();
        snackbar.Mostrar(eliminada.Titulo);
    }

    void Lista_DoubleClick(object? sender, EventArgs e)
    {
        int i = lista.IndexFromPoint(lista.PointToClient(Cursor.Position));
        if (i < 0 || i >= visibles.Count) return;

        var punto = lista.PointToClient(Cursor.Position);
        if (punto.X <= S(34) || punto.X > lista.ClientSize.Width - S(44)) return;

        AbrirEdicion(visibles[i]);
    }

    void AbrirEdicion(Tarea tarea)
    {
        CerrarPanelesSuperpuestos();
        tareaEnEdicion = tarea;
        panelEdicion.CargarProyectos(controller.Proyectos);
        panelEdicion.Editar(tareaEnEdicion);
        panelEdicion.Visible = true;
        panelEdicion.BringToFront();
    }

    void ConfigurarMenuTareas()
    {
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Renderer = new ToolStripProfessionalRenderer(new ColoresOscuros()) { RoundedEdges = false },
            BackColor = Ui.Tarjeta,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f)
        };
        menu.Items.Add("Marcar como hecha / pendiente", null, (_, _) =>
        {
            if (tareaContextual == null) return;
            controller.MarcarHecha(tareaContextual, dia, !tareaContextual.Hecha);
            Refrescar();
            AnimarTarea(tareaContextual);
        });
        menu.Items.Add("Editar tarea", null, (_, _) => { if (tareaContextual != null) AbrirEdicion(tareaContextual); });
        menu.Items.Add("Eliminar tarea", null, (_, _) =>
        {
            if (tareaContextual == null) return;
            controller.Eliminar(tareaContextual);
            borradas.Push(tareaContextual);
            if (itemDeshacerBorrado != null) itemDeshacerBorrado.Enabled = true;
            snackbar.Mostrar(tareaContextual.Titulo);
            Refrescar();
        });
        menu.Opening += (_, e) => e.Cancel = tareaContextual == null;
        lista.ContextMenuStrip = menu;
        lista.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            int i = lista.IndexFromPoint(e.Location);
            tareaContextual = i >= 0 && i < visibles.Count ? visibles[i] : null;
            if (tareaContextual == null) menu.Close();
        };
    }

    void DeshacerBorrado()
    {
        if (borradas.Count == 0) return;

        var borrado = borradas.Pop();
        if (borrado is Tarea tarea)
            controller.Anadir(tarea);
        else if (borrado is ProyectoEliminado proyecto)
        {
            controller.RestaurarProyecto(proyecto);
            proyectoParaNueva = proyecto.Nombre;
            ActualizarSelectorProyectos(proyecto.Nombre);
            panelGestionProyectos.CargarProyectos(controller.Proyectos, proyecto.Nombre);
            panelEdicion.CargarProyectos(controller.Proyectos);
        }
        if (itemDeshacerBorrado != null)
            itemDeshacerBorrado.Enabled = borradas.Count > 0;
        Refrescar();
    }

    void ExportarCopia()
    {
        using var dialogo = new SaveFileDialog
        {
            Title = "Guardar copia de seguridad",
            Filter = "Copia de tareas (*.json)|*.json",
            FileName = $"tareas-{DateTime.Now:yyyy-MM-dd}.json",
            DefaultExt = "json",
            AddExtension = true
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialogo.FileName, controller.ExportarCopia());
    }

    void RestaurarCopia()
    {
        using var dialogo = new OpenFileDialog
        {
            Title = "Restaurar copia de seguridad",
            Filter = "Copia de tareas (*.json)|*.json|Todos los archivos (*.*)|*.*"
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(this, "La restauración reemplazará la lista actual de tareas. ¿Continuar?", "Restaurar copia",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            controller.ReemplazarDesdeArchivo(dialogo.FileName);
            Refrescar();
        }
        catch (Exception ex)
        {
            AppLogger.Error("No se pudo restaurar la copia de seguridad seleccionada.", ex);
            MessageBox.Show(this, "No se pudo restaurar la copia: " + ex.Message, "Error al restaurar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---------- Lógica de tareas ----------

    // Una tarea importante sale TODOS los días (desde que se creó) hasta que se elimine
    // Lista de un día: primero las importantes (por fecha de entrega) y después las del día
    List<Tarea> TareasDelDia(DateTime d)
        => controller.ObtenerParaDia(d);

    int PendientesHoy() => controller.PendientesHoy();

    void IrADia(DateTime d)
    {
        dia = d.Date;
        fechaNueva = dia;
        ActualizarChipFecha();
        Refrescar();
    }

    void Anadir()
    {
        string titulo = txtNueva.Text.Trim();
        if (titulo == "") return;

        // Fecha elegida + hora (si se ha escrito)
        var fecha = fechaNueva.Date;
        bool conHora = false;

        if (txtHora.Text.Any(char.IsDigit))
        {
            if (txtHora.MaskCompleted &&
                TimeOnly.TryParseExact(txtHora.Text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            {
                fecha = fecha.Add(hora.ToTimeSpan());
                conHora = true;
            }
            else
            {
                txtHora.ForeColor = Ui.Rojo; // hora incompleta o inválida
                txtHora.Focus();
                return;
            }
        }

        var nueva = new Tarea
        {
            Titulo = titulo,
            Fecha = fecha,
            ConHora = conHora,
            Importante = chipImportante.Checked,
            Proyecto = proyectoParaNueva,
            Creada = DateTime.Today,
            Recurrencia = (RecurrenciaTarea)Math.Max(0, repeticionNueva.SelectedIndex)
        };
        controller.Anadir(nueva);

        txtNueva.Clear();
        txtHora.Clear();
        chipImportante.Checked = false;
        repeticionNueva.SelectedIndex = 0;
        // Si la tarea no se ve en el día actual, saltamos al día de la tarea para que se vea dónde ha caído
        if (!TareasDelDia(dia).Contains(nueva))
            IrADia(fecha.Date);
        else
            Refrescar();
        AnimarTarea(nueva);
    }

    void ActualizarSelectorProyectos(string? seleccionar = null)
    {
        if (seleccionar == null) seleccionar = proyectoParaNueva;
        actualizandoSelectorProyectos = true;
        proyectoNueva.Items.Clear();
        proyectoNueva.Items.Add(OpcionSinProyecto);
        proyectoNueva.Items.Add(OpcionCrearProyecto);
        proyectoNueva.Items.Add(OpcionGestionarProyectos);
        foreach (string proyecto in controller.Proyectos)
            proyectoNueva.Items.Add(proyecto);

        int indice = proyectoNueva.Items.FindIndex(item =>
            string.Equals(item?.ToString(), string.IsNullOrWhiteSpace(seleccionar) ? OpcionSinProyecto : seleccionar,
                StringComparison.CurrentCultureIgnoreCase));
        proyectoNueva.SelectedIndex = indice >= 0 ? indice : 0;
        string valorSeleccionado = proyectoNueva.SelectedItem?.ToString() ?? OpcionSinProyecto;
        proyectoParaNueva = valorSeleccionado == OpcionSinProyecto || valorSeleccionado == OpcionCrearProyecto ||
            valorSeleccionado == OpcionGestionarProyectos ? "" : valorSeleccionado;
        actualizandoSelectorProyectos = false;
    }

    void CambiarProyectoNueva()
    {
        if (actualizandoSelectorProyectos) return;
        string seleccionado = proyectoNueva.SelectedItem?.ToString() ?? OpcionSinProyecto;
        if (seleccionado == OpcionCrearProyecto)
        {
            BeginInvoke(new Action(CrearProyectoDesdeSelector));
            return;
        }
        if (seleccionado == OpcionGestionarProyectos)
        {
            BeginInvoke(new Action(AbrirGestionProyectos));
            return;
        }

        proyectoParaNueva = seleccionado == OpcionSinProyecto ? "" : seleccionado;
    }

    void CrearProyectoDesdeSelector()
    {
        CerrarPanelesSuperpuestos();
        panelCrearProyecto.ReconfigurarProyectos(controller.Proyectos);
        panelCrearProyecto.Visible = true;
        panelCrearProyecto.BringToFront();
        panelCrearProyecto.EnfocarNombre();
    }

    void AbrirGestionProyectos()
    {
        CerrarPanelesSuperpuestos();
        panelGestionProyectos.CargarProyectos(controller.Proyectos);
        panelGestionProyectos.Visible = true;
        panelGestionProyectos.BringToFront();
    }

    void Refrescar()
    {
        hover = -1;
        lista.Items.Clear();
        IEnumerable<Tarea> resultado = TareasDelDia(dia);
        string consulta = txtBuscar.Text.Trim();
        if (consulta.Length > 0)
            resultado = resultado.Where(t => t.Titulo.Contains(consulta, StringComparison.CurrentCultureIgnoreCase) ||
                (t.Proyecto?.Contains(consulta, StringComparison.CurrentCultureIgnoreCase) ?? false));
        resultado = (filtroTareas.SelectedIndex) switch
        {
            1 => resultado.Where(t => !t.Hecha),
            2 => resultado.Where(t => t.Hecha),
            3 => resultado.Where(t => t.Importante),
            4 => resultado.Where(t => !t.Hecha && (t.ConHora ? t.Fecha < DateTime.Now : t.Fecha.Date < dia.Date)),
            _ => resultado
        };
        visibles = resultado.ToList();
        foreach (var t in visibles)
            lista.Items.Add(t.Titulo);
        barraLista.Actualizar();

        lblVacio.Text = txtBuscar.TextLength > 0 || filtroTareas.SelectedIndex > 0
            ? "No hay resultados"
            : "No hay tareas";
        lblVacio.Visible = visibles.Count == 0;
        if (lblVacio.Visible) lblVacio.BringToFront();

        ActualizarCabecera();
        ActualizarIcono();
    }

    void AnimarTarea(Tarea tarea)
    {
        if (!visibles.Any(t => t.Id == tarea.Id)) return;
        opacidadesFilas[tarea.Id] = 0;
        if (!animacionFilas.Enabled) animacionFilas.Start();
    }

    static Color Mezclar(Color desde, Color hasta, int opacidad)
    {
        float p = Math.Clamp(opacidad / 255f, 0f, 1f);
        return Color.FromArgb(
            (int)(desde.R + (hasta.R - desde.R) * p),
            (int)(desde.G + (hasta.G - desde.G) * p),
            (int)(desde.B + (hasta.B - desde.B) * p));
    }

    void ActualizarCabecera()
    {
        var hoy = DateTime.Today;
        lblTitulo.Text =
            dia.Date == hoy ? "Hoy" :
            dia.Date == hoy.AddDays(1) ? "Mañana" :
            dia.Date == hoy.AddDays(-1) ? "Ayer" :
            dia.ToString("d MMM", es).Replace(".", "");

        string fechaTxt = dia.ToString("dddd d 'de' MMMM", es);
        fechaTxt = char.ToUpper(fechaTxt[0]) + fechaTxt[1..];

        int pendientes = visibles.Count(t => !t.Hecha);
        int total = visibles.Count;
        lblSub.Text = total == 0 ? fechaTxt : pendientes == total
            ? $"{fechaTxt} · {total} tarea{(total == 1 ? "" : "s")}"
            : $"{fechaTxt} · {pendientes} pendientes · {total} tareas";
    }
}
