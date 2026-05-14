# Seguridad

## Secretos

No guardar secretos en el repositorio. En particular:

- API keys de ElevenLabs.
- Certificados `*.pfx`, `*.p12`, `*.pem`, `*.key`.
- Archivos `.env`.
- Sesiones de Chrome o datos de WhatsApp Web.
- Audios generados con contenido de clientes.

La aplicacion lee estas variables en tiempo de ejecucion:

- `ELEVENLABS_API_KEY` (obligatoria para audios)
- `ELEVENLABS_VOICE_ID` (obligatoria para audios)
- `ELEVENLABS_MODEL_ID` (opcional)
- `ELEVENLABS_OUTPUT_FORMAT` (opcional)
- `ELEVENLABS_STABILITY` (opcional)
- `ELEVENLABS_SIMILARITY_BOOST` (opcional)

No hay API key ni voice ID por defecto en el codigo. Si falta una variable obligatoria, la aplicacion no intenta llamar a ElevenLabs y registra la variable faltante.

Desde el panel de configuracion se puede guardar ElevenLabs en `%AppData%/AutoWhatsApp/elevenlabs-settings.json`. La API key se cifra con proteccion de datos de Windows para el usuario actual; no debe copiarse ni versionarse ese archivo.

## Datos sensibles

Los archivos Excel pueden contener numeros telefonicos y mensajes. No deben commitearse salvo que sean fixtures anonimizados para pruebas.

## Rotacion

Si una clave fue commiteada o compartida por error:

1. Revocarla en el proveedor.
2. Generar una nueva.
3. Revisar el historial Git antes de publicar.
4. Documentar el incidente y la accion tomada.

## Git

El `.gitignore` excluye artefactos comunes, certificados, variables locales, sesiones de navegador y audios generados. Aun asi, revisar `git status` y `git diff --cached` antes de cada commit.
