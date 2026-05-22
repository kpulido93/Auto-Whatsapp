# Uso responsable

## Finalidad

AutoWhatsApp es una herramienta de apoyo operativo para preparar, revisar y ejecutar corridas sobre WhatsApp Web con supervision humana. No sustituye la responsabilidad del operador sobre consentimiento, contenido, alcance ni frecuencia de contacto.

## Reglas minimas de cumplimiento

- No contactes personas sin consentimiento previo y verificable.
- Conserva un `optIn` claro por destinatario antes de incluirlo en el Excel.
- Si una persona solicita no recibir mas mensajes, tratalo como `opt-out` y agregala a la lista local de `no-contactar`.
- No reutilices bases de datos antiguas sin volver a revisar consentimiento, exclusiones y contexto del mensaje.

## Opt-in, opt-out y no-contactar

### Opt-in

`optIn` indica si el destinatario autorizo recibir mensajes. La app solo considera enviables las filas con `optIn` explicito (`true` o `1`), `optInSource` informado y datos minimos validos.

### Opt-out

`opt-out` es la retirada de consentimiento por parte del destinatario. Cuando ocurra:

1. Excluye al contacto de la campaña actual.
2. Registralo en la lista local de `no-contactar`.
3. No vuelvas a incluirlo en campañas futuras salvo nuevo consentimiento verificable y posterior.

### No-contactar

La lista local de `no-contactar` vive en `%AppData%/AutoWhatsApp/do-not-contact.json`. Su proposito es impedir que un contacto excluido vuelva a entrar por error en otra campaña. Esa lista no debe copiarse al repositorio ni compartirse fuera del entorno operativo que la necesita.

## Dry-run obligatorio

Antes de cualquier envio real, ejecuta un `dry-run`. El dry-run:

- No envia mensajes.
- Resume filas enviables, bloqueadas por `no-contactar`, sin `optIn`, invalidas y audios.
- Muestra riesgos operativos que requieren correccion o confirmacion.
- Habilita el envio real solo despues de una aprobacion explicita del operador.

Si cambian el Excel, la linea principal o la seleccion de lineas, repite el dry-run.

## Modo manual asistido, Selenium y alternativas oficiales

### Modo manual asistido

El modo manual asistido es la opcion gratuita de menor riesgo operativo dentro de este repositorio. Consiste en:

- preparar la sesion de WhatsApp Web manualmente;
- revisar el Excel y el dry-run antes de cada corrida;
- confirmar de forma humana que solo saldran filas enviables;
- supervisar la ejecucion y detenerla si aparecen errores o cambios de contexto.

### Selenium sobre WhatsApp Web

La automatizacion actual usa Selenium sobre WhatsApp Web. Esto tiene limites claros:

- depende de selectores e interacciones fragiles;
- puede romperse si cambia la interfaz web;
- no ofrece garantias de estabilidad, disponibilidad o compatibilidad futura;
- requiere autenticacion manual previa y supervision operativa.

Por eso, Selenium debe tratarse como una ayuda operativa frágil, no como una base de mensajeria transaccional o masiva.

### Alternativas oficiales

Si el caso de uso requiere mayor estabilidad, auditoria, trazabilidad o escalado, evalua opciones oficiales como WhatsApp Business Platform o integraciones aprobadas por Meta. Este repositorio no reemplaza esos canales ni replica sus garantias.

## Limites explicitos

La documentacion y el proyecto no incluyen:

- tacticas de evasion;
- rotacion de VPN o proxies;
- fingerprinting;
- calentamiento de lineas;
- bypasses de controles del canal;
- instrucciones para eludir bloqueos, restricciones o terminos de servicio.

## Operacion minima recomendada

Antes de una corrida:

1. Verifica consentimiento y exclusiones.
2. Actualiza `no-contactar` si hubo `opt-out`.
3. Ejecuta `dotnet build .\AutoWhatsApp.sln`.
4. Corre el `dry-run`.
5. Revisa el resumen y confirma solo si las filas enviables coinciden con lo esperado.
