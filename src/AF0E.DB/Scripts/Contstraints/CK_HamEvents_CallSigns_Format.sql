IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints AS cc
    WHERE cc.name = N'CK_HamEvents_CallSigns_Format'
      AND cc.parent_object_id = OBJECT_ID(N'dbo.HamEvents')
)
BEGIN
    ALTER TABLE dbo.HamEvents
    DROP CONSTRAINT CK_HamEvents_CallSigns_Format;
END
GO

ALTER TABLE dbo.HamEvents
WITH CHECK
ADD CONSTRAINT CK_HamEvents_CallSigns_Format
CHECK
(
    NULLIF(LTRIM(RTRIM(CallSigns)), '') IS NULL
    OR
    (
        CallSigns LIKE '|%|'                                 -- starts and ends with pipe
        AND CallSigns NOT LIKE '%||%'                        -- no empty tokens
        AND PATINDEX('%[^|A-Z0-9/]%', UPPER(CallSigns)) = 0  -- only A-Z, 0-9, /, |
    )
);
GO

ALTER TABLE dbo.HamEvents
CHECK CONSTRAINT CK_HamEvents_CallSigns_Format;
GO
