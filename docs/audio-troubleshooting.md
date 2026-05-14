# Diagnostico de fallos de audio

Esta guia ayuda a aislar si un fallo de audio viene de ElevenLabs, de la generacion del archivo `.ogg` o de WhatsApp Web.

## Requisitos ElevenLabs

Para enviar audios, configura ElevenLabs desde el panel **Configuracion**, seccion **ElevenLabs**, o con variables de entorno:

- `ELEVENLABS_API_KEY`
- `ELEVENLABS_VOICE_ID`
- `ELEVENLABS_MODEL_ID` (opcional, por defecto `eleven_multilingual_v2`)
- `ELEVENLABS_OUTPUT_FORMAT` (opcional, por defecto `opus_48000_96`)
- `ELEVENLABS_STABILITY` (opcional, por defecto `0.75`)
- `ELEVENLABS_SIMILARITY_BOOST` (opcional, por defecto `0.75`)

Usa **Probar configuracion** para validar que la API key y el Voice ID existen antes de ejecutar una corrida con audios. Esta prueba valida configuracion minima, guarda la configuracion localmente y no genera audio ni llama a la API.

## Validar `toAudio`

En el Excel, la columna `toAudio` controla si el mensaje se envia como audio.

Valores aceptados:

- `true`
- `false`
- `1`
- `0`

Para diagnostico, usa `true` o `1` solo en la fila que quieras probar. Si `toAudio` esta vacio, invalido o en `false`, el mensaje se enviara como texto.

## Probar con una sola fila

1. Crea o edita una plantilla con una sola fila valida.
2. Usa un telefono de prueba y un mensaje corto, por ejemplo `Prueba de audio`.
3. Marca `toAudio` como `true`.
4. Prepara la linea de WhatsApp hasta que quede lista.
5. Ejecuta **Enviar ahora**.
6. Revisa el log de la app en este orden:
   - `Abriendo chat...`
   - `Generando audio...`
   - `Audio generado...`
   - `Adjuntando audio en WhatsApp Web...`
   - `Audio adjuntado y enviado...`

Si el log se detiene antes de `Audio generado`, el fallo esta en configuracion o respuesta de ElevenLabs. Si llega a `Audio generado` pero falla despues, el problema esta en el archivo local o en WhatsApp Web.

## Errores comunes

### Falta API key

Mensaje esperado:

```text
Falta la variable obligatoria ELEVENLABS_API_KEY.
```

Configura la API key desde la seccion **ElevenLabs** o define `ELEVENLABS_API_KEY` en variables de entorno. Reinicia la app si cambias variables de entorno de usuario.

### Falta Voice ID

Mensaje esperado:

```text
Falta la variable obligatoria ELEVENLABS_VOICE_ID.
```

Configura el Voice ID de la voz que quieres usar. No hay Voice ID por defecto en el codigo.

### Error HTTP de ElevenLabs

Mensaje esperado:

```text
Error HTTP de ElevenLabs: <codigo>
Detalles: <respuesta>
```

Revisa que la API key sea valida, que el Voice ID exista, que el modelo y formato sean compatibles, y que la cuenta tenga saldo o permisos suficientes. Si aparece `401` o `403`, normalmente es credencial o permisos. Si aparece `404`, revisa el Voice ID.

### Archivo `.ogg` vacio

Mensaje esperado:

```text
El archivo de audio generado ... esta vacio
```

La app recibio una ruta, pero el archivo no tiene contenido. Reintenta con un mensaje corto y revisa el detalle previo de ElevenLabs en el log. Tambien valida que la carpeta de salida sea escribible.

### Selector de WhatsApp no encontrado

Mensajes posibles:

```text
WhatsApp Web: no se encontro el boton de adjuntar audio.
WhatsApp Web: no se encontro la opcion Audio en el menu de adjuntos.
WhatsApp Web: no se encontro el input file para adjuntar audio.
WhatsApp Web: no se encontro el boton enviar despues de adjuntar audio.
```

En estos casos ElevenLabs ya genero el archivo, pero WhatsApp Web no expuso el control esperado. Verifica que el chat este abierto, que la sesion este lista, que Chrome no este bloqueado por un modal y que WhatsApp Web no haya cambiado su interfaz.

## Seguridad

No commitees API keys, Voice IDs privados, archivos `.env`, capturas con secretos ni audios generados con datos de clientes. La configuracion local de ElevenLabs vive en `%AppData%/AutoWhatsApp/elevenlabs-settings.json` y no debe versionarse.
