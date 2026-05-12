# Despliegue

## Compilacion local

```powershell
dotnet restore .\AutoWhatsApp.sln
dotnet build .\AutoWhatsApp.sln -c Release
```

## Publicacion

El perfil actual publica como aplicacion self-contained single-file para Windows x86:

```powershell
dotnet publish .\AutoWhatsApp.csproj -c Release -p:PublishProfile=FolderProfile
```

El perfil `Properties/PublishProfiles/FolderProfile.pubxml` define la carpeta local de salida. Ajustar `PublishDir` segun el equipo o el pipeline.

## Configuracion requerida

Configurar variables de entorno antes de enviar audios:

```powershell
[Environment]::SetEnvironmentVariable("ELEVENLABS_API_KEY", "<api-key>", "User")
[Environment]::SetEnvironmentVariable("ELEVENLABS_VOICE_ID", "<voice-id>", "User")
```

Reiniciar la aplicacion despues de cambiar variables de entorno de usuario.

## Verificacion manual

1. Abrir la aplicacion.
2. Escanear el QR de WhatsApp Web si la sesion no existe.
3. Seleccionar un Excel de prueba.
4. Programar una hora futura cercana.
5. Validar envio de texto.
6. Validar envio de audio con credenciales ElevenLabs configuradas.
