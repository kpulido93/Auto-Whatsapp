# Seguridad

## Secretos

No guardar secretos en el repositorio. En particular:

- API keys de ElevenLabs.
- Certificados `*.pfx`, `*.p12`, `*.pem`, `*.key`.
- Archivos `.env`.
- Sesiones de Chrome o datos de WhatsApp Web.
- Audios generados con contenido de clientes.

La aplicacion lee estas variables en tiempo de ejecucion:

- `ELEVENLABS_API_KEY`
- `ELEVENLABS_VOICE_ID`

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
