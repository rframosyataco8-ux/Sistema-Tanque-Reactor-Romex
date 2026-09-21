-- =====================================================
-- SISTEMA TANQUE REACTOR - EXPORTADORA ROMEX S.A.
-- Script de creación de base de datos
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

-- =====================================================
-- TABLAS MAESTRAS
-- =====================================================

CREATE TABLE Turnos (
    IdTurno INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(30) NOT NULL,
    HoraInicio TIME NOT NULL,
    HoraFin TIME NOT NULL,
    EsDomingo BIT NOT NULL DEFAULT 0,
    Activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE Operarios (
    IdOperario INT PRIMARY KEY IDENTITY(1,1),
    Nombres NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NULL,
    Dni NVARCHAR(15) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Insumos (
    IdInsumo INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(120) NOT NULL,
    Unidad NVARCHAR(20) NOT NULL DEFAULT 'Kg',
    EsPrincipal BIT NOT NULL DEFAULT 0,  -- Torta Trozada o Merma
    Activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE Lotes (
    IdLote INT PRIMARY KEY IDENTITY(1,1),
    NumeroLote NVARCHAR(50) NOT NULL UNIQUE,
    IdInsumo INT NULL REFERENCES Insumos(IdInsumo),
    TipoProducto NVARCHAR(80) NULL,
    FechaIngreso DATE NULL,
    CantidadInicialKg DECIMAL(12,2) NULL,
    CantidadDisponibleKg DECIMAL(12,2) NULL,
    Observaciones NVARCHAR(300) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
);

-- =====================================================
-- TABLA PRINCIPAL: CARGAS
-- =====================================================

CREATE TABLE CargasReactor (
    IdCarga INT PRIMARY KEY IDENTITY(1,1),
    Fecha DATE NOT NULL,
    IdTurno INT NOT NULL REFERENCES Turnos(IdTurno),
    IdOperario INT NULL REFERENCES Operarios(IdOperario),
    NumeroTanque INT NOT NULL DEFAULT 1,
    NumeroCargaDelTurno INT NOT NULL DEFAULT 1,
    
    -- Tiempos
    HoraInicioCarga TIME NULL,
    HoraFinCarga TIME NULL,
    HoraInicioSolucion TIME NULL,
    HoraFinSolucion TIME NULL,
    HoraInicioVacio TIME NULL,
    HoraFinVacio TIME NULL,
    TiempoSecadoAlVacio NVARCHAR(30) NULL,
    
    -- Parámetros del producto
    pH DECIMAL(5,2) NULL,
    Humedad DECIMAL(6,2) NULL,
    TempProductoFinal DECIMAL(6,1) NULL,
    CantidadElaboradaKg DECIMAL(12,2) NULL,
    LoteProductoFinal NVARCHAR(50) NULL,
    StockKg DECIMAL(12,2) NULL,
    
    Observaciones NVARCHAR(500) NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'En Proceso', -- En Proceso, Finalizado, Anulado
    
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    UsuarioRegistro NVARCHAR(80) NULL,
    FechaModificacion DATETIME NULL,
    UsuarioModificacion NVARCHAR(80) NULL
);

-- =====================================================
-- DETALLE DE INSUMOS POR CARGA
-- =====================================================

CREATE TABLE CargaInsumos (
    IdCargaInsumo INT PRIMARY KEY IDENTITY(1,1),
    IdCarga INT NOT NULL REFERENCES CargasReactor(IdCarga) ON DELETE CASCADE,
    IdInsumo INT NOT NULL REFERENCES Insumos(IdInsumo),
    IdLote INT NULL REFERENCES Lotes(IdLote),
    CantidadBolsas INT NULL,
    CantidadKg DECIMAL(12,2) NOT NULL,
    EsMerma BIT NOT NULL DEFAULT 0,
    Observacion NVARCHAR(200) NULL
);

-- =====================================================
-- CONTROLES DE TEMPERATURA Y PRESIÓN
-- =====================================================

CREATE TABLE ControlesTemperatura (
    IdControl INT PRIMARY KEY IDENTITY(1,1),
    IdCarga INT NOT NULL REFERENCES CargasReactor(IdCarga) ON DELETE CASCADE,
    Hora TIME NOT NULL,
    TempChaqueta DECIMAL(6,1) NULL,
    TempProducto DECIMAL(6,1) NULL,
    Presion DECIMAL(6,2) NULL,
    Observacion NVARCHAR(150) NULL
);

-- =====================================================
-- CHECKLIST DE MÁQUINAS / LIMPIEZA / DESINFECCIÓN
-- =====================================================

CREATE TABLE ChecklistItems (
    IdItem INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(120) NOT NULL,
    Categoria NVARCHAR(50) NOT NULL, -- Maquina, Limpieza, Desinfeccion, etc.
    Orden INT NOT NULL DEFAULT 0,
    Activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE ChecklistResultados (
    IdResultado INT PRIMARY KEY IDENTITY(1,1),
    IdCarga INT NOT NULL REFERENCES CargasReactor(IdCarga) ON DELETE CASCADE,
    IdItem INT NOT NULL REFERENCES ChecklistItems(IdItem),
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Conforme', -- Conforme, No Conforme, N/A
    Observacion NVARCHAR(250) NULL
);

-- =====================================================
-- DATOS INICIALES
-- =====================================================

INSERT INTO Turnos (Nombre, HoraInicio, HoraFin, EsDomingo) VALUES
('Día', '07:00', '19:00', 0),
('Noche', '19:00', '07:00', 0),
('Domingo Extra', '07:00', '15:00', 1);

INSERT INTO Insumos (Nombre, Unidad, EsPrincipal) VALUES
('Torta Trozada Natural', 'Kg', 1),
('Torta Alcalina', 'Kg', 0),
('Carbonato de Potasio', 'Kg', 0),
('Agua Potable', 'Lt', 0),
('Cocoa Natural en Polvo', 'Kg', 0),
('Merma - Reproceso', 'Kg', 1),
('Cocoa Alcalina en Polvo', 'Kg', 0);

INSERT INTO ChecklistItems (Nombre, Categoria, Orden) VALUES
('Puertas', 'Maquina', 1),
('Parte externa tanque', 'Maquina', 2),
('Tableros de control', 'Maquina', 3),
('Balanza 2', 'Maquina', 4),
('Tolva dosificadora de alcalinizado', 'Maquina', 5),
('Pisos', 'Maquina', 6),
('Elevadores', 'Maquina', 7),
('Malla de techo 2', 'Maquina', 8),
('Malla de techo 3', 'Maquina', 9),
('Malla de techo 4', 'Maquina', 10),
('Tolva de alimentación', 'Maquina', 11),
('Tableros de control (secundario)', 'Maquina', 12),
('Equipo de vacío', 'Maquina', 13),
('Duchas', 'Maquina', 14),
('Máquina de coser', 'Maquina', 15),
('Motores', 'Maquina', 16),
('Balanza 1', 'Maquina', 17),
('Mesa de trabajo', 'Maquina', 18),
('Recogedor', 'Maquina', 19),
('Cuchilla', 'Maquina', 20),
('Ventilador', 'Maquina', 21),
('Limpieza general', 'Limpieza', 30),
('Desinfección', 'Desinfeccion', 40);

PRINT 'Base de datos TanqueReactorDB creada exitosamente.';
GO
