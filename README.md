# Sistema de Control - Tanque Reactor
**Exportadora Romex S.A. - Planta de Cacao Chincha**

Sistema de escritorio profesional para el registro y control del proceso de Alcalinizado en Tanque Reactor.

## Características

- Registro de cargas por turno (Día / Noche / Domingo Extra)
- Control de insumos por carga (Torta Trozada + Merma)
- Registro de temperaturas, presiones y tiempos
- Checklist de máquinas y limpieza
- Historial completo con filtros
- Diseño moderno, limpio y profesional (Material Design)
- Conexión a SQL Server

## Requisitos

- Windows 10/11
- .NET 8 SDK
- SQL Server (Express, Developer o Full) + SQL Server Management Studio
- Visual Studio 2022 (recomendado) o VS Code

## Instalación rápida

1. Clona el repositorio:
```bash
git clone https://github.com/rframosyataco8-ux/Sistema-Tanque-Reactor-Romex.git
cd Sistema-Tanque-Reactor-Romex
```

2. Abre la solución en Visual Studio 2022:
```
SistemaTanqueReactor.sln
```

3. Restaura los paquetes NuGet (se hace automáticamente).

4. Crea la base de datos en SQL Server Management Studio ejecutando el script:
```
Database/CreateDatabase.sql
```

5. Configura la cadena de conexión en:
```
SistemaTanqueReactor/appsettings.json
```

6. Ejecuta el proyecto (F5).

## Estructura del Proyecto

```
SistemaTanqueReactor/
├── Models/              # Entidades de la base de datos
├── Data/                # DbContext y configuración EF Core
├── ViewModels/          # Lógica de presentación (MVVM)
├── Views/               # Pantallas XAML
├── Services/            # Servicios de negocio
├── Helpers/             # Utilidades
├── Styles/              # Temas y estilos modernos
└── Resources/           # Iconos e imágenes
```

## Autor
Desarrollado para Exportadora Romex S.A. - Planta Chincha
