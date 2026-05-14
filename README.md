# Auto-Whatsapp

Aplicacion Windows Forms para programar envios por WhatsApp Web desde un archivo Excel. La aplicacion lee mensajes con ClosedXML, automatiza WhatsApp Web con Selenium y puede convertir texto a audio con ElevenLabs antes de enviarlo.

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

## Plantilla Excel

Desde la aplicacion, usa el boton **Descargar plantilla** en la seccion **1. Archivo Excel**. La app pedira donde guardar `plantilla_auto_whatsapp.xlsx` y generara el archivo localmente.

La plantilla incluye:

- Hoja `Mensajes`, vacia salvo los encabezados obligatorios.
- Hoja `Instrucciones`, con ejemplos seguros y notas de llenado.
- Validacion en `toAudio` para los valores `true`, `false`, `1` o `0`.

## Formato del Excel

La primera fila se usa como encabezado. Desde la fila 2:

| Columna | Campo | Descripcion |
| --- | --- | --- |
| A | Código país | Ejemplo: `57`, sin `+`. |
| B | Teléfono | Numero sin espacios. |
| C | Mensaje | Texto a enviar o convertir a audio. |
| D | toAudio | `true`, `false`, `1` o `0`. |

Al cargar una plantilla vacia, la vista previa mostrara 0 filas y no se habilitara el envio. Completa los datos desde la fila 2 antes de programar o enviar.

## Documentacion

- [Arquitectura](docs/architecture.md)
- [Despliegue](docs/deployment.md)
- [Seguridad](docs/security.md)
- [Diagnostico de audio](docs/audio-troubleshooting.md)
- [Decisiones](docs/decisions.md)
