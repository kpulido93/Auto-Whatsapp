# Arquitectura

## Envio por lineas de WhatsApp

La aplicacion separa la orquestacion WinForms de la automatizacion Selenium:

- `Form1` coordina el archivo Excel, el progreso, la seleccion de linea y la politica de fallback.
- `WhatsAppLineManager` carga `whatsapp-lines.json`, filtra lineas habilitadas y las ordena por `Priority`.
- `WhatsAppSender` encapsula el `ChromeDriver` de una sola linea y usa `WhatsAppSendResult` para reportar exito, errores por contacto y fallos globales.

## Configuracion local de lineas

Las lineas se guardan en una configuracion local estable por usuario:

`%AppData%/AutoWhatsApp/whatsapp-lines.json`

`WhatsAppLineManager` es responsable de:

- Resolver la ruta con `GetConfigPath`.
- Crear la carpeta `AutoWhatsApp` dentro de AppData si no existe.
- Crear una linea inicial `Principal` en primera ejecucion.
- Cargar todas las lineas con `LoadLines`.
- Guardar metadatos con `SaveLines`.
- Exponer solo lineas habilitadas a la pantalla principal con `GetEnabledLines`.

La pantalla `LineSettingsForm` permite administrar esos metadatos desde la UI: agregar, editar nombre visible, activar/desactivar, ajustar prioridad, ordenar y eliminar entradas con confirmacion. Al agregar una linea, genera un `Id` seguro y asigna automaticamente `SessionPath = ChromeUserData/{id}` con `ProfileDirectory = Default`.

La configuracion no guarda credenciales, cookies ni tokens. Las carpetas `ChromeUserData` contienen sesiones de Chrome administradas por el navegador y no se copian, exportan ni eliminan desde la pantalla de configuracion.

## Perfiles Chrome por linea

Cada `WhatsAppLine` tiene:

- `Id`: identificador estable.
- `DisplayName`: nombre visible en la UI.
- `SessionPath`: carpeta `user-data-dir` de Chrome para esa linea.
- `ProfileDirectory`: perfil Chrome dentro del `user-data-dir`.
- `Enabled`: controla si la linea participa en seleccion y fallback.
- `Priority`: orden de preferencia ascendente.
- `OperationalState`: estado operativo en memoria (`Unknown`, `Ready`, `RequiresManualAuth`, `NotAvailable`).

Las sesiones reales quedan fuera de Git mediante `ChromeUserData/` en `.gitignore`. La configuracion local vive fuera del directorio de instalacion, en AppData, para que sobreviva a compilaciones y publicaciones.

## Preparacion manual de lineas

La preparacion de lineas es un flujo asistido por usuario antes de iniciar una campana:

1. La UI permite preparar la linea seleccionada o todas las lineas habilitadas.
2. `WhatsAppSendOrchestrator.PrepareLine` abre Chrome con el `user-data-dir` y `ProfileDirectory` configurados para esa linea.
3. Si habia un `ChromeDriver` activo de otra linea, se cierra antes de abrir el nuevo perfil.
4. `WhatsAppSender.GetOperationalState` navega a WhatsApp Web y clasifica el estado operativo con `WaitForReady`.
5. Si la linea esta lista, queda marcada como `Ready`.
6. Si WhatsApp Web pide QR/login, la linea queda como `RequiresManualAuth` y la UI muestra la instruccion para escanear manualmente el QR desde el telefono.
7. El usuario puede reintentar la verificacion despues de escanear. Si cancela, la linea queda como `NotAvailable`.
8. La preparacion de todas las lineas recorre las habilitadas por `Priority` y muestra un resumen de listas, pendientes y no disponibles.

El proceso no automatiza autenticacion, no copia credenciales entre perfiles y no modifica archivos internos de Chrome. Solo abre el perfil local configurado y espera la accion manual del usuario cuando WhatsApp Web la requiera.

## Programacion visual

La seccion de programacion usa controles WinForms nativos:

- `MonthCalendar` para seleccionar la fecha.
- `NumericUpDown` para hora y minuto en formato 24 horas.
- Un resumen visible con el formato `dd/MM/yyyy HH:mm`.
- Boton "Enviar ahora" para iniciar el envio inmediatamente.
- Boton "Programar envio" para conservar el flujo con `scheduledTime` y `schedulerTimer`.

Al cambiar fecha u hora, `Form1` actualiza el resumen. Al programar, valida que la fecha y hora seleccionadas sean futuras. "Enviar ahora" reutiliza las mismas validaciones de Excel, lineas seleccionadas y linea preparada, pero no usa `schedulerTimer` ni exige una hora futura.

## Fallback automatico

El fallback automatico se controla con el checkbox "Cambiar automaticamente si falla" y esta desactivado por defecto.

## Seleccion de lineas por corrida

Ademas de la configuracion global de lineas habilitadas, `Form1` mantiene una seleccion en memoria para la corrida actual mediante `selectedLineIdsForRun`.

La UI ofrece el boton "Seleccionar lineas a usar", que abre un dialogo con las lineas habilitadas. La linea seleccionada en el ComboBox principal aparece marcada por defecto porque siempre es la linea inicial del envio.

Reglas de la seleccion:

1. No se puede iniciar una corrida si no hay lineas seleccionadas.
2. Si la linea principal no esta dentro de la seleccion, se agrega automaticamente antes de programar.
3. Si el fallback esta activado y solo hay una linea seleccionada, la UI muestra una advertencia no bloqueante.
4. La seleccion se registra en `txtLog` con los nombres visibles de las lineas.
5. `WhatsAppSendOptions` lleva los ids seleccionados al orquestador.
6. El fallback automatico solo considera lineas habilitadas, listas, saludables para la corrida y presentes en `selectedLineIdsForRun`.

La seleccion de corrida no modifica `whatsapp-lines.json`: es una decision operativa temporal para el envio actual.

Durante una corrida:

1. El envio empieza con la linea seleccionada.
2. Si el resultado es exitoso, el contacto no se reintenta.
3. Si el resultado es un error por contacto, como numero invalido, el contacto se marca como procesado y se continua.
4. Si el resultado es un fallo global, la linea actual se marca como no saludable para esa corrida.
5. Si el fallback esta activo, se prueban las siguientes lineas habilitadas por prioridad que ya esten en estado `Ready`, excluyendo las no saludables y las no seleccionadas para la corrida.
6. Cada candidata se abre con su propio perfil Chrome y se valida con `WaitForReady`.
7. Las lineas que requieren QR/login durante el envio, estan desconectadas o estan bloqueadas se descartan para esa corrida. El envio automatico no espera escaneo de QR.
8. Cuando se encuentra una linea lista, se reintenta el mismo contacto con esa linea.
9. Si no queda ninguna linea saludable, el proceso se detiene y la UI muestra una alerta con resumen.

La lista de lineas no saludables vive solo en memoria durante la corrida. Al iniciar un nuevo envio, las lineas se vuelven a evaluar.
