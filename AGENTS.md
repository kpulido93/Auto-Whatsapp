# AGENTS.md

## Alcance

Este repositorio mantiene la linea principal `AutoWhatsApp`. Los proyectos legacy del workspace original no forman parte de este repo.

## Reglas de trabajo

- No commitear secretos, certificados, perfiles de usuario, sesiones de Chrome ni artefactos generados.
- Mantener las claves de servicios externos en variables de entorno o en el gestor de secretos del entorno local.
- Antes de cerrar cambios de codigo, ejecutar:

```powershell
dotnet build .\AutoWhatsApp.sln
```

- Evitar cambios grandes en archivos `*.Designer.cs` salvo que provengan del diseñador de WinForms o sean estrictamente necesarios.
- La automatizacion de WhatsApp Web depende de selectores fragiles; documentar cualquier cambio de selector en `docs/decisions.md`.
