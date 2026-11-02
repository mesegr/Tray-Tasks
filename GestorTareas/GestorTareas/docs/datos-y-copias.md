# Datos locales y copias de seguridad

TrayTasks guarda sus datos localmente; no los envía a un servicio en la nube.

## Archivos de la aplicación

En `%AppData%\TareasBandeja` se guardan:

- `tareas.json`: lista de tareas.
- `tareas.backup.json`: versión anterior del archivo de tareas, actualizada al guardar.
- `proyectos.json`: lista de nombres de proyecto.
- `proyectos.backup.json`: versión anterior de la lista de proyectos.

Los reemplazos se escriben primero en un archivo temporal y después se mueven a su destino. Si un JSON principal está dañado y su respaldo es válido, TrayTasks recupera el respaldo, conserva el original como `*.corrupt-<fecha-hora>` y muestra un aviso al iniciar. Si ambos están dañados, la aplicación no continúa con datos vacíos ni sobrescribe los archivos: hay que conservarlos y revisar el registro antes de intentar una recuperación manual.

## Copias exportadas

La opción **Exportar copia…** genera un objeto JSON con `schemaVersion` y dos listas: `tasks` y `projects`. La versión actual del formato es `1`. Las fechas usan el formato ISO de `System.Text.Json`; los valores de recurrencia se guardan como números del enum.

Ejemplo reducido:

```json
{
  "schemaVersion": 1,
  "tasks": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "titulo": "Preparar la presentación",
      "fecha": "2026-11-10T09:00:00",
      "conHora": true,
      "importante": false,
      "proyecto": "Trabajo",
      "creada": "2026-11-02T00:00:00",
      "hecha": false,
      "recurrencia": 0,
      "fechasCompletadas": []
    }
  ],
  "projects": ["Trabajo"]
}
```

El importador también acepta copias antiguas sin versión y el formato histórico que era directamente una lista JSON de tareas. Una versión futura desconocida se rechaza para evitar importar datos de forma incompleta.

## Registros de diagnóstico

Los errores se guardan en `%LocalAppData%\TrayTasks\Logs`. Se mantienen los 14 archivos de registro diarios más recientes. El registro incluye contexto técnico y excepciones, pero la aplicación no escribe deliberadamente los títulos o el contenido de las tareas.
