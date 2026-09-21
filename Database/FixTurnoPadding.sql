-- =====================================================
-- Fix: CHAR(2) rellenaba "I" como "I " y rompía el historial
-- Ejecutar en SSMS sobre TanqueReactorDB
-- =====================================================
USE TanqueReactorDB;
GO

-- Cambiar Turno a NVARCHAR para que no rellene con espacios
ALTER TABLE dbo.RegistrosProduccion ALTER COLUMN Turno NVARCHAR(2) NOT NULL;
GO

-- Limpiar espacios en registros ya guardados
UPDATE dbo.RegistrosProduccion SET Turno = LTRIM(RTRIM(Turno));
GO

PRINT 'Turno corregido (sin espacios). Historial I/II ya separa bien.';
GO
