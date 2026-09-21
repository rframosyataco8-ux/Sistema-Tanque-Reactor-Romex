# Guía de Instalación - Sistema Tanque Reactor

## 1. Requisitos previos

- Windows 10 o Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (Express, Developer o Full) + SQL Server Management Studio (SSMS)
- Visual Studio 2022 (Community es suficiente) **o** VS Code + extensión C#

## 2. Clonar el repositorio

```bash
git clone https://github.com/rframosyataco8-ux/Sistema-Tanque-Reactor-Romex.git
cd Sistema-Tanque-Reactor-Romex
```

## 3. Crear la base de datos

1. Abre **SQL Server Management Studio**
2. Conéctate a tu instancia de SQL Server
3. Abre el archivo `Database/CreateDatabase.sql`
4. Ejecuta todo el script (F5)

Esto creará la base de datos `TanqueReactorDB` con todas las tablas y datos iniciales (turnos, insumos, checklist).

## 4. Configurar la conexión

Abre el archivo:

```
SistemaTanqueReactor/appsettings.json
```

Modifica la cadena de conexión según tu SQL Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TanqueReactorDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Ejemplos comunes:**

- SQL Server local (Windows Auth):  
  `Server=localhost;Database=TanqueReactorDB;Trusted_Connection=True;TrustServerCertificate=True;`

- SQL Server Express:  
  `Server=.\SQLEXPRESS;Database=TanqueReactorDB;Trusted_Connection=True;TrustServerCertificate=True;`

- Con usuario y contraseña:  
  `Server=localhost;Database=TanqueReactorDB;User Id=sa;Password=TuPassword;TrustServerCertificate=True;`

## 5. Ejecutar el sistema

### Con Visual Studio 2022:
1. Abre `SistemaTanqueReactor.sln`
2. Restaura los paquetes NuGet (se hace automáticamente)
3. Presiona **F5** o el botón ▶

### Con línea de comandos:
```bash
cd SistemaTanqueReactor
dotnet restore
dotnet run
```

## 6. Primer uso

1. Ve a **Maestros** (próximamente) o inserta operarios directamente en la tabla `Operarios` desde SSMS.
2. Ve a **Nueva Carga**.
3. Selecciona fecha, turno y operario.
4. Usa el botón **"+ 28 Torta + 2 Merma"** para cargar automáticamente la cantidad estándar.
5. Completa tiempos y parámetros.
6. Guarda la carga.

## 7. Estructura de turnos

| Turno          | Horario aproximado | Notas                  |
|----------------|--------------------|------------------------|
| Día            | 07:00 - 19:00      | Turno normal           |
| Noche          | 19:00 - 07:00      | Turno normal           |
| Domingo Extra  | 07:00 - 15:00      | Solo los domingos      |

## Soporte

Si tienes problemas de conexión o errores al ejecutar, revisa:
- Que el servicio de SQL Server esté corriendo
- Que el script de base de datos se haya ejecutado correctamente
- La cadena de conexión en `appsettings.json`
