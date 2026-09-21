-- =====================================================
-- Migración: Producción ≠ Despacho + flag Despachado
-- Ejecutar en SSMS sobre TanqueReactorDB
-- =====================================================
USE TanqueReactorDB;
GO

-- Renombrar columna si aún se llama DespachoBolsas
IF COL_LENGTH('dbo.LotesTorta', 'ProduccionBolsas') IS NULL
   AND COL_LENGTH('dbo.LotesTorta', 'DespachoBolsas') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.LotesTorta.DespachoBolsas', 'ProduccionBolsas', 'COLUMN';
END
GO

-- Si no existe ninguna, crear ProduccionBolsas
IF COL_LENGTH('dbo.LotesTorta', 'ProduccionBolsas') IS NULL
BEGIN
    ALTER TABLE dbo.LotesTorta ADD ProduccionBolsas INT NOT NULL DEFAULT 0;
END
GO

-- Flag despachado (check)
IF COL_LENGTH('dbo.LotesTorta', 'Despachado') IS NULL
BEGIN
    ALTER TABLE dbo.LotesTorta ADD Despachado BIT NOT NULL DEFAULT 0;
END
GO

PRINT 'Migración OK: ProduccionBolsas + Despachado';
GO
