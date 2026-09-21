-- =====================================================
-- SISTEMA TANQUE REACTOR - EXPORTADORA ROMEX S.A.
-- Archivo ÚNICO de base de datos
-- =====================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'TanqueReactorDB')
BEGIN
    CREATE DATABASE TanqueReactorDB;
END
GO

USE TanqueReactorDB;
GO

IF OBJECT_ID('dbo.RegistrosProduccion', 'U') IS NOT NULL DROP TABLE dbo.RegistrosProduccion;
IF OBJECT_ID('dbo.LotesTorta', 'U') IS NOT NULL DROP TABLE dbo.LotesTorta;
IF OBJECT_ID('dbo.Operarios', 'U') IS NOT NULL DROP TABLE dbo.Operarios;
IF OBJECT_ID('dbo.Turnos', 'U') IS NOT NULL DROP TABLE dbo.Turnos;
GO

CREATE TABLE Turnos (
    IdTurno INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(30) NOT NULL,
    Codigo CHAR(2) NOT NULL,
    HoraInicio TIME NULL,
    HoraFin TIME NULL,
    Activo BIT NOT NULL DEFAULT 1
);

INSERT INTO Turnos (Nombre, Codigo, HoraInicio, HoraFin) VALUES
('Turno I (Día)', 'I', '07:00', '19:00'),
('Turno II (Noche)', 'II', '19:00', '07:00');

CREATE TABLE Operarios (
    IdOperario INT PRIMARY KEY IDENTITY(1,1),
    Nombres NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NULL,
    Dni NVARCHAR(15) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
);

-- Lote: inicial / disponible / produccion (reactor) / despachado (check)
CREATE TABLE LotesTorta (
    IdLoteTorta INT PRIMARY KEY IDENTITY(1,1),
    NumeroLote NVARCHAR(50) NOT NULL UNIQUE,
    CantidadBolsasInicial INT NOT NULL DEFAULT 400,
    CantidadBolsasDisponible INT NOT NULL DEFAULT 400,
    ProduccionBolsas INT NOT NULL DEFAULT 0,   -- bolsas usadas en producción (NO despacho)
    Despachado BIT NOT NULL DEFAULT 0,         -- check: lote despachado
    FechaIngreso DATE NULL,
    Observaciones NVARCHAR(300) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE RegistrosProduccion (
    IdRegistro INT PRIMARY KEY IDENTITY(1,1),
    NumeroLote NVARCHAR(50) NOT NULL,
    FechaProduccion DATE NOT NULL,
    Turno CHAR(2) NOT NULL,
    CantidadBolsas INT NOT NULL,
    ExpresionCantidad NVARCHAR(200) NULL,
    IdOperario INT NULL REFERENCES Operarios(IdOperario),
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    UsuarioRegistro NVARCHAR(80) NULL,
    Observaciones NVARCHAR(300) NULL
);

CREATE INDEX IX_Registros_Lote_Fecha ON RegistrosProduccion(NumeroLote, FechaProduccion);
CREATE INDEX IX_Registros_Fecha ON RegistrosProduccion(FechaProduccion);

PRINT 'TanqueReactorDB lista.';
GO
