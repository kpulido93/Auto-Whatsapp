# Arquitectura

## Vista general

`AutoWhatsApp` es una aplicacion WinForms `net8.0-windows` orientada a escritorio. El flujo principal es:

1. El usuario selecciona un archivo Excel.
2. El usuario programa una fecha y hora de envio.
3. La app lee los mensajes con `ExcelReader`.
4. La app abre o reutiliza una sesion de WhatsApp Web con Selenium.
5. Para cada fila valida, envia texto directamente o genera un audio con ElevenLabs y lo adjunta.

## Componentes

| Componente | Archivo | Responsabilidad |
| --- | --- | --- |
| UI principal | `Form1.cs` | Programacion, pausa, cancelacion, progreso y logs. |
| Entrada | `Program.cs` | Inicializa WinForms y abre `Form1`. |
| Lector Excel | `logic/ExcelReader.cs` | Lee codigo de pais, telefono, mensaje y bandera de audio. |
| TTS | `logic/TTSConverter.cs` | Consume ElevenLabs y guarda audio local. |
| WhatsApp | `logic/WhatsAppSender.cs` | Administra ChromeDriver, abre chats, valida numeros y envia texto/audio. |

## Dependencias principales

- `ClosedXML`: lectura de archivos Excel.
- `Selenium.WebDriver`: automatizacion de Chrome.
- `DotNetSeleniumExtras.WaitHelpers`: esperas explicitas para Selenium.

## Datos locales

La sesion de Chrome se guarda en `ChromeUserData/`. Esa carpeta es estado local de usuario y esta excluida de Git.
