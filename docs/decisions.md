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

El audio se adjunta mediante un `input[type=file]` compatible. Si ese input no existe al abrir el menu de adjuntos, la app activa la opcion visible `Audio` y vuelve a buscar el input compatible.

Motivos:

- Algunas versiones de WhatsApp Web exponen el input de audio solo despues de activar la opcion visible `Audio`.
- Selenium adjunta el archivo con `input.SendKeys(fullAudioPath)` cuando existe input compatible; si WhatsApp abre el dialogo nativo tras activar `Audio`, se usa como fallback controlado.
- Se considera compatible solo un input cuyo `accept` contenga `audio`, `.ogg`, `.opus`, `.mp3`, `.m4a` o `.wav`.
- La opcion `Audio` se localiza como una lista ordenada de candidatos: primero elementos `role='button'` o `role='menuitem'`, luego `button`, `li` o `tabindex='0'`, despues ancestros comunes que combinen texto `Audio` e icono `ic-headphones-filled`, filas del menu de adjuntos y finalmente el selector legacy basado en `li`.
- Los `div` no se aceptan por si solos: deben tener senales de item de menu, tamano razonable, texto exacto `Audio` o icono de audifonos, estar dentro o asociados al menu de adjuntos, y no pertenecer al footer del chat ni ser un contenedor interno de tipografia.
- `SendKeys` solo confirma que Selenium entrego la ruta al input; no confirma que WhatsApp haya creado el adjunto ni que el envio haya salido.
- Si WhatsApp abre el dialogo nativo `Abrir/Open`, la app debe cargar la ruta del audio o cerrar el dialogo antes de continuar.
- La deteccion del dialogo nativo se separa en estricta y amplia. La estricta exige clase `#32770`, titulo `Abrir/Open` y proceso compatible con Chrome; solo esa ruta permite pegar la ruta del archivo.
- La deteccion amplia se usa despues del click en `Audio` y en diagnosticos: considera titulos localizados como `Seleccionar`, `Choose`, `File Upload` o `Cargar`, y ventanas `#32770` visibles que aparecen durante la ventana post-click. Esas ventanas no se manipulan ni reciben rutas si no se validan antes.
- Cuando se usa el dialogo nativo validado, la subida devuelve un resultado con estado: ruta entregada y dialogo cerrado, ruta entregada pero dialogo aun abierto, o error. Un dialogo aun abierto no es fallo inmediato si WhatsApp llega a mostrar preview.
- No se usan clases CSS obfuscadas de WhatsApp porque cambian con frecuencia entre WhatsApp normal y Business.

Consecuencia operativa:

- Tras abrir adjuntos, la app enumera los `input[type=file]`, registra cuantos hay y sus `accept`, y usa un input compatible si ya existe.
- Si solo hay inputs no compatibles, como `image/*`, pero la opcion `Audio` esta visible, la app diagnostica los candidatos de Audio y los prueba en orden hasta que un click produzca un efecto real: input de audio, dialogo nativo `Abrir/Open`, preview de adjunto o cambio en los inputs file.
- Cada candidato registra indice, origen del selector, tag, `role`, `tabindex`, `aria-label`, texto visible corto, tamano, `pointer-events`, cursor, presencia del icono `ic-headphones-filled` y si esta dentro de `[role='menu']`.
- Si un candidato cierra el menu sin producir efecto, la app reabre adjuntos una sola vez, recompone la lista de candidatos y reintenta. Si se agotan los candidatos, el fallo conserva el diagnostico de los elementos probados.
- Despues de cada click candidato, la app espera hasta 5 segundos por input de audio, preview, dialogo estricto o dialogo posible. El log del resultado incluye el efecto detectado y el tiempo esperado; si no hubo efecto, conserva inputs actuales y diagnostico de dialogos.
- Si todos los candidatos fallan, el error final incluye cantidad de candidatos por pasada, resumen de candidatos intentados, inputs antes y despues del click, dialogo nativo detectado y health actual.
- Despues de `SendKeys`, la app espera un preview de adjunto con varias senales UI: `role='dialog'`, nombre de archivo cuando esta visible, boton enviar contenido en el panel y controles de preview de adjunto.
- El boton enviar se busca dentro del preview detectado; no se usa un selector global del chat para enviar audios.
- El log de exito solo se emite despues de entregar el path al input, detectar el preview, hacer click en el boton enviar del preview y comprobar que el preview se cerro.
- El flujo registra etapas `AudioAttach.OpenMenu`, `AudioAttach.FindInputBeforeClick`, `AudioAttach.ClickAudioOption`, `AudioAttach.FindInputAfterClick`, `AudioAttach.NativeDialogUpload`, `AudioAttach.WaitPreview`, `AudioAttach.ClickPreviewSend`, `AudioAttach.WaitPreviewClose` y `AudioAttach.Done`.
- Los fallos de audio incluyen en una linea la etapa, inputs file detectados, accepts detectados, presencia de texto `Audio`, presencia del icono `ic-headphones-filled`, dialogo nativo detectado y health actual de WhatsApp.
- Si WhatsApp no muestra preview, si no hay boton enviar dentro del preview o si el preview no se cierra despues del click, el contacto falla con `WhatsAppAttachmentFailed` no global.
- Si despues del click en `Audio` se abre el dialogo nativo `Abrir/Open`, la app lo trae al frente, pega `fullAudioPath` desde el portapapeles y presiona Enter; sigue si el dialogo se cierra o si WhatsApp muestra preview.
- Despues de cargar por dialogo nativo, la app no cierra inmediatamente `Abrir/Open` antes del preview. Primero espera que el dialogo se cierre solo, que WhatsApp muestre preview o que expire el timeout.
- Si el preview aparece aunque el dialogo nativo siga abierto, se considera que WhatsApp recibio el archivo, se envia desde el preview y la limpieza visual del dialogo queda para despues del envio confirmado.
- Si no aparece preview y el dialogo nativo sigue abierto, entonces se intenta cerrar y el contacto falla sin marcar el audio como enviado.
- Como defensa, despues de abrir adjuntos, despues de entregar el path al input o dialogo, ante fallos de preview y antes del exito se busca un dialogo nativo de archivo de Chrome con titulo `Abrir` u `Open`; si aparece fuera del fallback esperado, se intenta cerrar con Escape y se valida que WhatsApp vuelva a estar interactuable.
- Despues de confirmar que el preview se cerro, el envio ya se considera confirmado; si queda abierto un dialogo nativo `Abrir/Open`, la app intenta limpiarlo con Escape y, si sigue abierto, con `WM_CLOSE` sobre el handle del dialogo.
- Si esa limpieza post-envio falla, se registra advertencia pero no se marca fallido ni se reintenta el audio para evitar duplicados.
- Si el dialogo nativo no puede cargarse ni cerrarse, el fallo se considera global para detener la corrida. Si se cierra y WhatsApp queda `Ready`, solo falla el contacto actual.
- Si despues del click en `Audio` no aparece input compatible, dialogo nativo ni preview, el contacto falla con `WhatsAppAttachmentFailed` y el log conserva diagnostico de inputs antes y despues del click, texto `Audio`, icono `ic-headphones-filled` y sospecha de dialogo nativo.
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
