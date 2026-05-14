# Decisiones

## Multiples sesiones Chrome por linea

Se usa un `user-data-dir` independiente por linea de WhatsApp.

Motivos:

- WhatsApp Web guarda autenticacion y estado de sesion en el perfil de Chrome.
- Compartir un perfil entre varias lineas puede pisar sesiones y causar cierres inesperados.
- Separar carpetas permite iniciar sesion con cuentas distintas sin base de datos ni migracion de almacenamiento.
- Selenium mantiene el alcance simple: un `WhatsAppSender` controla una sola linea activa.

Consecuencia operativa:

- El usuario debe autenticar cada linea una vez en su ventana Chrome correspondiente.
- Si una linea necesita QR durante fallback automatico, se marca como no disponible y se prueba otra linea.
- Las carpetas de sesion no se versionan.

## Configuracion en AppData

La configuracion de lineas se guarda en `%AppData%/AutoWhatsApp/whatsapp-lines.json`, no junto al ejecutable.

Motivos:

- En una publicacion o instalacion, el usuario no siempre ve ni puede editar archivos junto al binario.
- AppData es una ruta estable por usuario y sobrevive a recompilaciones o reemplazos del ejecutable.
- La app puede crear la configuracion inicial en primera ejecucion sin requerir pasos manuales.
- La UI puede administrar altas, bajas, activacion y prioridad sin pedir edicion manual de JSON.

Consecuencia operativa:

- Si el archivo no existe, se crea automaticamente con la linea `Principal`.
- Agregar una linea desde la UI solo guarda metadatos: `Id`, nombre visible, ruta de perfil local, `ProfileDirectory`, activacion y prioridad.
- Eliminar una linea solo la quita de la configuracion; no borra carpetas `ChromeUserData`.
- No se guardan cookies, tokens, codigos QR ni credenciales en el JSON.

## Preparacion manual previa al envio

La autenticacion de cada linea se mantiene como accion manual y previa a la campana.

Motivos:

- El QR de WhatsApp Web debe escanearse desde el telefono autorizado del usuario.
- La aplicacion no debe copiar cookies, tokens ni credenciales entre perfiles.
- El envio automatico no puede quedarse bloqueado esperando una intervencion humana.
- El estado `Ready` explicita que una linea fue validada antes de entrar al flujo automatico.

Consecuencia operativa:

- Antes de programar, el usuario debe usar "Preparar linea" o "Preparar todas".
- Si WhatsApp Web requiere QR, la app abre Chrome y muestra una instruccion para escanear y reintentar.
- Si el usuario cancela la preparacion, esa linea queda como `NotAvailable`.
- Durante el envio, una linea que vuelva a pedir QR se descarta para esa corrida; no se espera escaneo.

## Fallback automatico desactivado por defecto

El checkbox de fallback se deja desactivado inicialmente.

Motivos:

- Cambiar de remitente puede tener impacto operativo y de negocio.
- La opcion manual conserva el comportamiento predecible para usuarios que solo tienen una linea.
- Cuando se active, el log deja auditado que linea fallo, que linea tomo el envio y con que linea se envio cada contacto.

## Sin reintentos infinitos

Durante una corrida, las lineas con fallo global se agregan a un conjunto `unhealthy`. El fallback solo considera lineas habilitadas, previamente marcadas como `Ready`, que no esten en ese conjunto.

Esto evita ciclos entre perfiles no autenticados, desconectados o bloqueados. Si no hay una linea saludable, el envio se detiene con alerta y resumen.

## Deteccion del composer de WhatsApp Web

La caja de mensaje se detecta desde `FindMessageBox` con una lista ordenada de selectores CSS/XPath.

Motivos:

- WhatsApp Web cambia atributos internos del composer con frecuencia.
- `data-tab` dejo de ser una senal estable y no debe ser obligatorio para abrir un chat.
- El footer y los atributos publicos `role`, `contenteditable`, `aria-label` y `aria-placeholder` son referencias mas tolerantes para detectar el campo visible.
- Las variantes en espanol e ingles cubren sesiones con idioma distinto al de la aplicacion.

Consecuencia operativa:

- Los envios de texto y audio comparten el mismo punto de deteccion del composer.
- Si el chat no expone una caja visible antes del timeout, el error incluye diagnostico limitado de URL actual, presencia de `header`, presencia de `#pane-side` y una vista previa corta del texto visible del body.

## Adjuntos de audio en WhatsApp Web

El audio se adjunta directamente mediante un `input[type=file]` compatible. La opcion visible `Audio` del menu de adjuntos se conserva solo como senal de diagnostico, junto con el texto visible `Audio` y el icono SVG con `title` `ic-headphones-filled`.

Motivos:

- Hacer click en la opcion visible `Audio` puede abrir el dialogo nativo de Windows "Abrir", que Selenium no puede controlar de forma fiable.
- WhatsApp Web expone inputs file en el DOM al abrir adjuntos; Selenium puede adjuntar el archivo con `input.SendKeys(fullAudioPath)` sin usar el explorador nativo.
- Se considera compatible solo un input cuyo `accept` contenga `audio`, `.ogg`, `.opus`, `.mp3`, `.m4a` o `.wav`.
- El texto visible `Audio` y el SVG con `title` `ic-headphones-filled` siguen siendo utiles para diagnosticar cambios del DOM, pero no se usan como camino feliz de seleccion.
- `SendKeys` solo confirma que Selenium entrego la ruta al input; no confirma que WhatsApp haya creado el adjunto ni que el envio haya salido.
- Si por un cambio de DOM o una ruta accidental se abre el dialogo nativo `Abrir/Open`, la corrida queda en riesgo de continuar con la UI bloqueada.
- No se usan clases CSS obfuscadas de WhatsApp porque cambian con frecuencia entre WhatsApp normal y Business.

Consecuencia operativa:

- Tras abrir adjuntos, la app enumera los `input[type=file]`, registra cuantos hay y sus `accept`, selecciona solo un input compatible con audio y envia el archivo directamente por `SendKeys`.
- Despues de `SendKeys`, la app espera un preview de adjunto con varias senales UI: `role='dialog'`, nombre de archivo cuando esta visible, boton enviar contenido en el panel y controles de preview de adjunto.
- El boton enviar se busca dentro del preview detectado; no se usa un selector global del chat para enviar audios.
- El log de exito solo se emite despues de entregar el path al input, detectar el preview, hacer click en el boton enviar del preview y comprobar que el preview se cerro.
- Si WhatsApp no muestra preview, si no hay boton enviar dentro del preview o si el preview no se cierra despues del click, el contacto falla con `WhatsAppAttachmentFailed` no global.
- Como defensa, despues de abrir adjuntos, despues de entregar el path al input, ante fallos de preview y antes del exito se busca un dialogo nativo de archivo de Chrome con titulo `Abrir` u `Open`; si aparece, se intenta cerrar con Escape y se valida que WhatsApp vuelva a estar interactuable.
- Si el dialogo nativo se cierra y WhatsApp queda `Ready`, solo falla el contacto actual y se registra que el dialogo fue cerrado. Si no se puede cerrar o WhatsApp no vuelve a estar interactuable, el fallo se considera global para detener la corrida.
- Si no hay input compatible, no se hace click en la opcion `Audio`; el contacto falla con `WhatsAppAttachmentFailed` y el log conserva el diagnostico de inputs, texto `Audio` e icono `ic-headphones-filled`.
- Si el input compatible esta oculto y Selenium lo rechaza por visibilidad o interactuabilidad, la app intenta hacerlo visible con JavaScript sin cambiar `accept` ni asignar archivos por script, y reintenta `SendKeys`.
- Si falla la UI de adjuntos pero WhatsApp Web sigue `Ready`, el resultado es `WhatsAppAttachmentFailed` no global: falla solo el contacto actual, aumenta `Procesados` y `Fallidos`, y se conserva la linea activa.
- Si durante el fallo de adjuntos se detecta `LoginRequired`, `PhoneDisconnected`, `BrowserUnavailable` o `SenderAccountBlockedOrRestricted`, se mantiene la clasificacion global y aplica el fallback o la detencion de la corrida.

## Fallo aislado al abrir chat

Si `OpenChat` no encuentra la caja de mensaje pero WhatsApp Web sigue en estado `Ready`, el resultado se clasifica como `ChatOpenFailed` y no como fallo global.

Motivos:

- La linea puede estar autenticada y operativa aunque un chat concreto no exponga el composer dentro del timeout.
- Un selector temporal, una vista intermedia o un contacto problematico no deben marcar la linea como no saludable.
- Los estados realmente globales siguen siendo los que impiden operar la linea: `LoginRequired`, `PhoneDisconnected`, `BrowserUnavailable` y `SenderAccountBlockedOrRestricted`.

Consecuencia operativa:

- El contacto afectado cuenta como procesado y fallido.
- La corrida continua con el siguiente contacto y conserva la linea actual.
- El diagnostico del fallo de chat incluye URL actual, presencia de `header`, presencia de `#pane-side` y una vista previa corta del body para diferenciarlo de `WhatsAppNotReady`.
