using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrayTasks.Models;
using TrayTasks.Services;

namespace TrayTasks.Tests.Services;

[TestClass]
public sealed class RecordatorioSchedulerTests
{
    [TestMethod]
    public void ComprobarEmiteAvisoUnaSolaVezDentroDeLaVentana()
    {
        var scheduler = new RecordatorioScheduler();
        DateTime ahora = new(2026, 11, 2, 10, 0, 30);
        Tarea tarea = CrearTarea(ahora.AddMinutes(30));

        var primero = scheduler.Comprobar(new[] { tarea }, _ => new[] { tarea }, new[] { 30 }, ahora);
        var segundo = scheduler.Comprobar(new[] { tarea }, _ => new[] { tarea }, new[] { 30 }, ahora.AddSeconds(30));

        Assert.AreEqual(1, primero.Count);
        Assert.AreEqual(30, primero[0].MinutosAntes);
        Assert.AreEqual(0, segundo.Count);
    }

    [TestMethod]
    public void AlIniciarEligeElAvisoConfiguradoMasCercano()
    {
        var scheduler = new RecordatorioScheduler();
        DateTime ahora = new(2026, 11, 2, 10, 0, 0);
        Tarea tarea = CrearTarea(ahora.AddMinutes(90));

        var avisos = scheduler.Comprobar(new[] { tarea }, _ => new[] { tarea }, new[] { 60, 120, 30 }, ahora, alIniciar: true);

        Assert.AreEqual(1, avisos.Count);
        Assert.AreEqual(120, avisos[0].MinutosAntes);
    }

    [TestMethod]
    public void PosponerEmiteElAvisoTrasElIntervaloYNoSiLaTareaEstaHecha()
    {
        var scheduler = new RecordatorioScheduler();
        DateTime ahora = new(2026, 11, 2, 10, 0, 0);
        Tarea tarea = CrearTarea(ahora.AddHours(3));
        scheduler.Posponer(tarea, 15, ahora);

        var antes = scheduler.Comprobar(Array.Empty<Tarea>(), _ => new[] { tarea }, new[] { 30 }, ahora.AddMinutes(14));
        var aviso = scheduler.Comprobar(Array.Empty<Tarea>(), _ => new[] { tarea }, new[] { 30 }, ahora.AddMinutes(15));
        scheduler.Posponer(tarea, 15, ahora.AddMinutes(15));
        tarea.Hecha = true;
        var cancelado = scheduler.Comprobar(Array.Empty<Tarea>(), _ => new[] { tarea }, new[] { 30 }, ahora.AddMinutes(30));

        Assert.AreEqual(0, antes.Count);
        Assert.AreEqual(1, aviso.Count);
        Assert.AreEqual(15, aviso[0].MinutosAntes);
        Assert.AreEqual(0, cancelado.Count);
    }

    private static Tarea CrearTarea(DateTime fecha) => new()
    {
        Titulo = "Prueba",
        Fecha = fecha,
        ConHora = true
    };
}

