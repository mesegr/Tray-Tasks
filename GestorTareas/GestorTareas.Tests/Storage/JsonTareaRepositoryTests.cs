using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TareasBandeja.Controllers;
using TareasBandeja.Models;
using TareasBandeja.Services;
using TareasBandeja.Storage;

namespace TareasBandeja.Tests.Storage;

[TestClass]
public sealed class JsonTareaRepositoryTests
{
    private string directorio = null!;
    private JsonTareaRepository repositorio = null!;

    [TestInitialize]
    public void Inicializar()
    {
        directorio = Path.Combine(Path.GetTempPath(), "TrayTasks.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directorio);
        repositorio = new JsonTareaRepository(directorio);
    }

    [TestCleanup]
    public void Limpiar()
    {
        if (Directory.Exists(directorio)) Directory.Delete(directorio, recursive: true);
    }

    [TestMethod]
    public void DatosInexistentesDevuelvenListasVacias()
    {
        var datos = repositorio.Cargar();

        Assert.AreEqual(0, datos.Tareas.Count);
        Assert.AreEqual(0, datos.Proyectos.Count);
    }

    [TestMethod]
    public void JsonDaniadoSeRecuperaYSeConservaElOriginal()
    {
        var anteriores = new[] { NuevaTarea("Una"), NuevaTarea("Dos") };
        repositorio.Guardar(anteriores, new[] { "Trabajo" });
        repositorio.Guardar(new[] { NuevaTarea("Actual") }, new[] { "Trabajo" });
        string rutaPrincipal = Path.Combine(directorio, "tareas.json");
        File.WriteAllText(rutaPrincipal, "{ JSON incompleto");

        var recuperados = repositorio.Cargar();

        CollectionAssert.AreEquivalent(anteriores.Select(tarea => tarea.Titulo).ToArray(),
            recuperados.Tareas.Select(tarea => tarea.Titulo).ToArray());
        Assert.AreEqual(1, Directory.GetFiles(directorio, "tareas.json.corrupt-*").Length);
        CollectionAssert.AreEquivalent(anteriores.Select(tarea => tarea.Titulo).ToArray(),
            JsonSerializer.Deserialize<List<Tarea>>(File.ReadAllText(rutaPrincipal))!
                .Select(tarea => tarea.Titulo).ToArray());
    }

    [TestMethod]
    public void JsonDaniadoSinRespaldoFallaSinSobrescribirlo()
    {
        string ruta = Path.Combine(directorio, "tareas.json");
        const string contenidoDaniado = "{ no borrar";
        File.WriteAllText(ruta, contenidoDaniado);

        Assert.ThrowsExactly<InvalidDataException>(() => repositorio.Cargar());

        Assert.AreEqual(contenidoDaniado, File.ReadAllText(ruta));
    }

    [TestMethod]
    public void ElRespaldoDeProyectosTambienPermiteRecuperacion()
    {
        repositorio.Guardar(Array.Empty<Tarea>(), new[] { "Anterior" });
        repositorio.Guardar(Array.Empty<Tarea>(), new[] { "Actual" });
        File.WriteAllText(Path.Combine(directorio, "proyectos.json"), "[");

        var datos = repositorio.Cargar();

        CollectionAssert.AreEqual(new[] { "Anterior" }, datos.Proyectos);
        Assert.AreEqual(1, Directory.GetFiles(directorio, "proyectos.json.corrupt-*").Length);
        Assert.AreEqual(1, datos.AdvertenciasRecuperacion!.Count);
    }

    [TestMethod]
    public void CopiaExportadaIncluyeVersionYSePuedeLeer()
    {
        string json = repositorio.ExportarCopia(new[] { NuevaTarea("Exportada") }, new[] { "Personal" });
        string ruta = Path.Combine(directorio, "copia.json");
        File.WriteAllText(ruta, json);

        using var documento = JsonDocument.Parse(json);
        Assert.AreEqual(1, documento.RootElement.GetProperty("schemaVersion").GetInt32());
        var copia = repositorio.LeerCopia(ruta);
        Assert.AreEqual("Exportada", copia.Tareas.Single().Titulo);
        CollectionAssert.AreEqual(new[] { "Personal" }, copia.Proyectos);
    }

    [TestMethod]
    public void SigueAceptandoCopiaAntiguaEnFormatoDeLista()
    {
        string ruta = Path.Combine(directorio, "antigua.json");
        File.WriteAllText(ruta, JsonSerializer.Serialize(new[] { NuevaTarea("Antigua", "Personal") }));

        var copia = repositorio.LeerCopia(ruta);

        Assert.AreEqual("Antigua", copia.Tareas.Single().Titulo);
        CollectionAssert.AreEqual(new[] { "Personal" }, copia.Proyectos);
    }

    [TestMethod]
    public void RechazaVersionFuturaSinMutarEstadoActivo()
    {
        repositorio.Guardar(new[] { NuevaTarea("Conservar") }, Array.Empty<string>());
        var controller = new TareaController(repositorio);
        controller.Cargar();
        string rutaFutura = Path.Combine(directorio, "futura.json");
        File.WriteAllText(rutaFutura, "{\"schemaVersion\":99,\"tasks\":[],\"projects\":[]}");

        Assert.ThrowsExactly<InvalidDataException>(() => controller.ReemplazarDesdeArchivo(rutaFutura));

        Assert.AreEqual("Conservar", controller.Tareas.Single().Titulo);
        Assert.AreEqual("Conservar", repositorio.Cargar().Tareas.Single().Titulo);
    }

    [TestMethod]
    public void TareaImportanteNoSeProyectaTrasSuFechaLimite()
    {
        var importante = NuevaTarea("Límite");
        importante.Importante = true;

        Assert.AreEqual(1, TareaService.ObtenerParaDia(new[] { importante }, new DateTime(2026, 11, 16)).Count);
        Assert.AreEqual(1, TareaService.ObtenerParaDia(new[] { importante }, new DateTime(2026, 11, 17)).Count);
        Assert.AreEqual(0, TareaService.ObtenerParaDia(new[] { importante }, new DateTime(2026, 11, 18)).Count);
    }

    [TestMethod]
    public void ErrorAlGuardarRevierteElCambioEnMemoria()
    {
        var tarea = NuevaTarea("Pendiente");
        var controller = new TareaController(new RepositorioConError(tarea));
        controller.Cargar();
        var referenciaOriginal = controller.Tareas.Single();

        Assert.ThrowsExactly<IOException>(() => controller.MarcarHecha(referenciaOriginal, tarea.Fecha, true));

        Assert.AreSame(referenciaOriginal, controller.Tareas.Single());
        Assert.IsFalse(referenciaOriginal.Hecha);
    }

    [TestMethod]
    public void ErrorAlGuardarElSegundoArchivoNoDejaElPrimeroAmedias()
    {
        Directory.CreateDirectory(Path.Combine(directorio, "proyectos.json"));

        Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
            repositorio.Guardar(new[] { NuevaTarea("No debe persistirse") }, new[] { "Trabajo" }));

        Assert.IsFalse(File.Exists(Path.Combine(directorio, "tareas.json")));
        Assert.AreEqual(0, Directory.GetFiles(directorio, "*.tmp").Length);
    }

    private static Tarea NuevaTarea(string titulo, string proyecto = "") => new()
    {
        Id = Guid.NewGuid(),
        Titulo = titulo,
        Proyecto = proyecto,
        Fecha = new DateTime(2026, 11, 17),
        Creada = new DateTime(2026, 11, 1)
    };

    private sealed class RepositorioConError(Tarea tarea) : ITareaRepository
    {
        public TareaSnapshot Cargar() => new(new List<Tarea> { tarea }, new List<string>());
        public void Guardar(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos) =>
            throw new IOException("Fallo simulado de persistencia.");
        public void GuardarProyectos(IReadOnlyCollection<string> proyectos) { }
        public string ExportarCopia(IReadOnlyCollection<Tarea> tareas, IReadOnlyCollection<string> proyectos) => "{}";
        public TareaSnapshot LeerCopia(string ruta) => new(new List<Tarea>(), new List<string>());
    }
}
