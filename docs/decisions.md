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
