# Auto-Whatsapp

Aplicacion Windows Forms para programar envios por WhatsApp Web desde un archivo Excel. La aplicacion lee mensajes con ClosedXML, automatiza WhatsApp Web con Selenium y puede convertir texto a audio con ElevenLabs antes de enviarlo.

## Uso responsable y limites

- No uses la app para contactar personas sin consentimiento previo.
- Cada campaña debe respetar `optIn`, solicitudes de `opt-out` y la lista local de `no-contactar`.
- El envio real solo debe iniciarse despues de un `dry-run` revisado y confirmado por un operador.
- La automatizacion con Selenium sobre WhatsApp Web es fragil y depende de una interfaz no oficial para este caso de uso.
- Para menor riesgo operativo y sin costo adicional, la opcion recomendada es un modo manual asistido: revision del Excel, autenticacion manual, dry-run y supervisión humana antes de cada corrida.
- Si necesitas un canal mas estable, auditable o escalable, evalua alternativas oficiales como WhatsApp Business Platform o integraciones aprobadas por Meta.

Consulta [Uso responsable](docs/uso-responsable.md) y [Seguridad](docs/security.md) antes de operar la herramienta.

## Requisitos

- Windows.
- .NET SDK 8.
- Google Chrome instalado.
- Una cuenta de WhatsApp con acceso a WhatsApp Web.
- Variables de entorno para ElevenLabs si se enviaran audios:
  - `ELEVENLABS_API_KEY` (obligatoria)
  - `ELEVENLABS_VOICE_ID` (obligatoria)
  - `ELEVENLABS_MODEL_ID` (opcional, por defecto `eleven_multilingual_v2`)
  - `ELEVENLABS_OUTPUT_FORMAT` (opcional, por defecto `opus_48000_96`)
  - `ELEVENLABS_STABILITY` (opcional, por defecto `0.75`)
  - `ELEVENLABS_SIMILARITY_BOOST` (opcional, por defecto `0.75`)

La aplicacion no incluye API keys ni voice IDs por defecto. Tambien puedes configurar ElevenLabs desde el panel **Configuración**, seccion **ElevenLabs**, de la app. La configuracion local se guarda en `%AppData%\AutoWhatsApp\elevenlabs-settings.json`; la API key se guarda cifrada para el usuario actual. Si no existe configuracion local, la app usa las variables de entorno anteriores.

## Compilar

```powershell
dotnet restore .\AutoWhatsApp.sln
dotnet build .\AutoWhatsApp.sln
```

## Ejecutar

```powershell
dotnet run --project .\AutoWhatsApp.csproj
```

La primera ejecucion abre WhatsApp Web. Escanea el QR con el telefono asociado antes de programar envios.

## Configuracion general de envio

Desde **Configuración > Configuración general...** puedes definir la espera entre mensajes en minutos. El valor permitido va de 1 a 120 minutos.

- La espera se aplica entre mensajes: no espera antes del primero ni despues del ultimo.
- Durante la espera, los controles de pausa, reanudacion y cancelacion siguen funcionando.

## Dry-run obligatorio

Antes de programar o enviar una corrida real, la app exige ejecutar un **dry-run** local. Este paso:

- Genera un reporte sin enviar mensajes.
- Resume filas enviables, bloqueadas por no-contactar, sin opt-in, invalidas y audios.
- Señala riesgos operativos, como linea principal no lista o falta de lineas alternativas cuando el cambio automatico esta activo.
- Pide una confirmacion explicita del usuario antes de habilitar el envio real.

Si el Excel o la seleccion operativa cambian, vuelve a ejecutar el dry-run antes de iniciar la corrida.

## Plantilla Excel

Desde la aplicacion, usa el boton **Descargar plantilla** en la seccion **1. Archivo Excel**. La app pedira donde guardar `plantilla_auto_whatsapp.xlsx` y generara el archivo localmente.

La plantilla incluye:

- Hoja `Mensajes`, vacia salvo los encabezados obligatorios.
- Hoja `Instrucciones`, con ejemplos seguros y notas de llenado.
- Validacion en `toAudio` para los valores `true`, `false`, `1` o `0`.
- Validacion en `optIn` para los valores `true`, `false`, `1` o `0`.

## Formato del Excel

La primera fila se usa como encabezado. Desde la fila 2:

| Columna | Campo | Descripcion |
| --- | --- | --- |
| A | Código país | Ejemplo: `57`, sin `+`. |
| B | Teléfono | Numero sin espacios. |
| C | Mensaje | Texto a enviar o convertir a audio. |
| D | toAudio | `true`, `false`, `1` o `0`. |
| E | optIn | `true` o `1` habilitan el envio; `false`, `0` o vacio lo bloquean. |
| F | optInSource | Origen del consentimiento. Obligatorio cuando `optIn` es `true` o `1`. |
| G | optInAt | Fecha opcional del consentimiento. Recomendado en formato `yyyy-MM-dd`. |

La vista previa separa filas enviables, filas sin opt-in e invalidas. Solo las filas con telefono, mensaje y `optIn` explicito se enviaran.

Si un destinatario retira su consentimiento, agregalo a la lista local de `no-contactar` antes de una campaña futura.

Los archivos Excel antiguos siguen cargando para revision, pero la app advertira que faltan columnas de consentimiento y no permitira enviar esas filas hasta completar `optIn`, `optInSource` y, si aplica, `optInAt`.

El envio real siempre usa solo las filas enviables del dry-run mas reciente aprobado.

## Documentacion

- [Arquitectura](docs/architecture.md)
- [Despliegue](docs/deployment.md)
- [Uso responsable](docs/uso-responsable.md)
- [Seguridad](docs/security.md)
- [Diagnostico de audio](docs/audio-troubleshooting.md)
- [Decisiones](docs/decisions.md)
