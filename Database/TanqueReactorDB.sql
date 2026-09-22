-- ============================================================
-- SISTEMA TANQUE REACTOR
-- Archivo ÚNICO de base de datos
-- ============================================================
-- Cómo usar en SQL Server Management Studio (SSMS):
--
--  1) Cierra la aplicación Tanque Reactor.
--  2) Abre este archivo en SSMS.
--  3) Conéctate al servidor (ej: localhost o .\SQLEXPRESS).
--  4) Ejecuta TODO el script (F5).
--
-- Este script:
--  - ELIMINA la base TanqueReactorDB si existe (borra todos los datos)
--  - La vuelve a crear limpia con el esquema actual
-- ============================================================

USE master;
GO

-- Cerrar conexiones activas y borrar la base si existe
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'TanqueReactorDB')
BEGIN
    ALTER DATABASE TanqueReactorDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TanqueReactorDB;
END
GO

CREATE DATABASE TanqueReactorDB;
GO

USE TanqueReactorDB;
GO

-- ------------------------------------------------------------
-- TURNOS (I = día, II = noche)
-- ------------------------------------------------------------
CREATE TABLE dbo.Turnos (
    IdTurno        INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Nombre         NVARCHAR(30)   NOT NULL,
    Codigo         NVARCHAR(2)    NOT NULL,   -- 'I' o 'II' (sin espacios)
    HoraInicio     TIME           NULL,
    HoraFin        TIME           NULL,
    Activo         BIT            NOT NULL CONSTRAINT DF_Turnos_Activo DEFAULT (1)
);

INSERT INTO dbo.Turnos (Nombre, Codigo, HoraInicio, HoraFin) VALUES
(N'Turno I (Día)',   N'I',  '07:00', '19:00'),
(N'Turno II (Noche)', N'II', '19:00', '07:00');

-- ------------------------------------------------------------
-- OPERARIOS
-- ------------------------------------------------------------
CREATE TABLE dbo.Operarios (
    IdOperario     INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Nombres        NVARCHAR(100)  NOT NULL,
    Apellidos      NVARCHAR(100)  NULL,
    Dni            NVARCHAR(15)   NULL,
    Activo         BIT            NOT NULL CONSTRAINT DF_Operarios_Activo DEFAULT (1),
    FechaRegistro  DATETIME       NOT NULL CONSTRAINT DF_Operarios_Fecha DEFAULT (GETDATE())
);

-- ------------------------------------------------------------
-- LOTES DE TORTA (stock en bolsas)
-- ------------------------------------------------------------
CREATE TABLE dbo.LotesTorta (
    IdLoteTorta              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    NumeroLote               NVARCHAR(50)   NOT NULL,
    CantidadBolsasInicial    INT            NOT NULL CONSTRAINT DF_Lotes_Inicial DEFAULT (400),
    CantidadBolsasDisponible INT            NOT NULL CONSTRAINT DF_Lotes_Disponible DEFAULT (400),
    ProduccionBolsas         INT            NOT NULL CONSTRAINT DF_Lotes_Produccion DEFAULT (0),
    Despachado               BIT            NOT NULL CONSTRAINT DF_Lotes_Despachado DEFAULT (0),
    FechaIngreso             DATE           NULL,
    Observaciones            NVARCHAR(300)  NULL,
    Activo                   BIT            NOT NULL CONSTRAINT DF_Lotes_Activo DEFAULT (1),
    FechaRegistro            DATETIME       NOT NULL CONSTRAINT DF_Lotes_Fecha DEFAULT (GETDATE()),
    CONSTRAINT UQ_LotesTorta_Numero UNIQUE (NumeroLote)
);

-- ------------------------------------------------------------
-- REGISTROS DE PRODUCCIÓN (por lote, fecha y turno)
-- ------------------------------------------------------------
CREATE TABLE dbo.RegistrosProduccion (
    IdRegistro         INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    NumeroLote         NVARCHAR(50)   NOT NULL,
    FechaProduccion    DATE           NOT NULL,
    Turno              NVARCHAR(2)    NOT NULL,   -- 'I' o 'II'
    CantidadBolsas     INT            NOT NULL,
    ExpresionCantidad  NVARCHAR(200)  NULL,       -- ej: 30+30+30+30
    IdOperario         INT            NULL,
    FechaRegistro      DATETIME       NOT NULL CONSTRAINT DF_Reg_Fecha DEFAULT (GETDATE()),
    UsuarioRegistro    NVARCHAR(80)   NULL,
    Observaciones      NVARCHAR(300)  NULL,
    CONSTRAINT FK_Registros_Operario FOREIGN KEY (IdOperario)
        REFERENCES dbo.Operarios (IdOperario)
);

CREATE INDEX IX_Registros_Lote_Fecha
    ON dbo.RegistrosProduccion (NumeroLote, FechaProduccion);

CREATE INDEX IX_Registros_Fecha
    ON dbo.RegistrosProduccion (FechaProduccion);

CREATE INDEX IX_Registros_Turno
    ON dbo.RegistrosProduccion (Turno);

PRINT N'OK: Base TanqueReactorDB creada (esquema limpio y listo).';
GO
