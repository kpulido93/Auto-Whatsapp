# Auto-Whatsapp

Aplicacion Windows Forms para programar envios por WhatsApp Web desde un archivo Excel. La aplicacion lee mensajes con ClosedXML, automatiza WhatsApp Web con Selenium y puede convertir texto a audio con ElevenLabs antes de enviarlo.

## Requisitos

- Windows.
- .NET SDK 8.
- Google Chrome instalado.
- Una cuenta de WhatsApp con acceso a WhatsApp Web.
- Variables de entorno para ElevenLabs si se enviaran audios:
  - `ELEVENLABS_API_KEY`
  - `ELEVENLABS_VOICE_ID`

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

## Formato del Excel

La primera fila se usa como encabezado. Desde la fila 2:

| Columna | Campo | Descripcion |
| --- | --- | --- |
| A | Codigo de pais | Ejemplo: `57`. |
| B | Telefono | Numero sin espacios. |
| C | Mensaje | Texto a enviar o convertir a audio. |
| D | toAudio | `true`, `false`, `1` o `0`. |

## Documentacion

- [Arquitectura](docs/architecture.md)
- [Despliegue](docs/deployment.md)
- [Seguridad](docs/security.md)
- [Decisiones](docs/decisions.md)
