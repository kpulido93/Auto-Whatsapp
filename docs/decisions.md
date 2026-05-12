# Decisiones

## 2026-05-12: usar `AutoWhatsApp` como linea principal

Se crea este repositorio desde el proyecto `AutoWhatsApp` del workspace original. Los proyectos `Automate_Whatsapp` y `Convert-Text-Speech` quedan fuera del repo inicial para reducir duplicacion y mantener una linea de desarrollo clara.

## 2026-05-12: secretos fuera del codigo

La integracion de ElevenLabs usa variables de entorno en lugar de claves hardcodeadas. Esto evita publicar credenciales y facilita rotacion por entorno.

## 2026-05-12: conservar WinForms y Selenium

Se conserva la arquitectura actual para minimizar riesgo funcional. La deuda principal pendiente es robustecer selectores de WhatsApp Web y reemplazar bloqueos de consola por flujo de UI.
