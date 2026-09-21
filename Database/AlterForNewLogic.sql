-- =====================================================
-- Ajustes para la nueva lógica de registro por lote/turno
-- Ejecutar en TanqueReactorDB
-- =====================================================

USE TanqueReactorDB;
GO

-- Tabla de registros diarios por lote y turno
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RegistrosProduccion')
BEGIN
    CREATE TABLE RegistrosProduccion (
        IdRegistro INT PRIMARY KEY IDENTITY(1,1),
        NumeroLote NVARCHAR(50) NOT NULL,
        FechaProduccion DATE NOT NULL,          -- siempre el día anterior al registro
        Turno CHAR(2) NOT NULL,                 -- 'I' o 'II'
        CantidadBolsas INT NOT NULL,            -- cantidad en bolsas (puede venir de suma)
        ExpresionCantidad NVARCHAR(200) NULL,  -- ej: '30+30+30+30'
        FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
        UsuarioRegistro NVARCHAR(80) NULL,
        Observaciones NVARCHAR(300) NULL
    );

    CREATE INDEX IX_Registros_Lote_Fecha ON RegistrosProduccion(NumeroLote, FechaProduccion);
END
GO

-- Tabla de lotes con stock
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LotesTorta')
BEGIN
    CREATE TABLE LotesTorta (
        IdLoteTorta INT PRIMARY KEY IDENTITY(1,1),
        NumeroLote NVARCHAR(50) NOT NULL UNIQUE,
        CantidadBolsasInicial INT NOT NULL DEFAULT 400,  -- siempre 400
        CantidadBolsasDisponible INT NOT NULL DEFAULT 400,
        StockKg AS (CantidadBolsasDisponible * 25) PERSISTED,
        DespachoBolsas INT NOT NULL DEFAULT 0,
        FechaIngreso DATE NULL,
        Activo BIT NOT NULL DEFAULT 1,
        FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

PRINT 'Tablas de nueva lógica creadas correctamente.';
GO
