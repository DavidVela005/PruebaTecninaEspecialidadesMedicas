USE BonusTest;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Bonus', 'U') IS NOT NULL
BEGIN
    PRINT 'dbo.Bonus ya existe; no se realizan cambios.';
    SET NOEXEC ON;
END
GO

CREATE TABLE dbo.Bonus (
    BonusId        INT IDENTITY(1,1) NOT NULL,
    DepartmentCode CHAR(5)       NOT NULL,
    EmployeeNumber INT           NOT NULL,
    Period         DATE          NOT NULL,

    BonusTypeId    INT           NOT NULL,
    BonusValue     DECIMAL(16,2) NOT NULL,
    ReasonCode     CHAR(6)       NOT NULL,

    GrossAmount    DECIMAL(16,2) NOT NULL,
    TaxAmount      DECIMAL(16,2) NOT NULL,
    NetAmount      DECIMAL(16,2) NOT NULL,

    BonusStateCode CHAR(4)       NOT NULL CONSTRAINT DF_Bonus_State DEFAULT '0201',
    IsDeleted      BIT           NOT NULL CONSTRAINT DF_Bonus_IsDeleted DEFAULT 0,
    CreatedBy      VARCHAR(50)   NOT NULL,
    CreatedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_Bonus_CreatedAt DEFAULT SYSUTCDATETIME(),
    ModifiedBy     VARCHAR(50)   NULL,
    ModifiedAt     DATETIME2(0)  NULL,

    CONSTRAINT PK_Bonus PRIMARY KEY CLUSTERED (BonusId),

    CONSTRAINT FK_Bonus_Employee FOREIGN KEY (DepartmentCode, EmployeeNumber)
        REFERENCES dbo.Employee (DepartmentCode, EmployeeNumber),
    CONSTRAINT FK_Bonus_BonusType   FOREIGN KEY (BonusTypeId)    REFERENCES dbo.BonusType (BonusTypeId),
    CONSTRAINT FK_Bonus_BonusReason FOREIGN KEY (ReasonCode)     REFERENCES dbo.BonusReason (ReasonCode),
    CONSTRAINT FK_Bonus_State       FOREIGN KEY (BonusStateCode) REFERENCES dbo.State (StateCode),

    CONSTRAINT CK_Bonus_State   CHECK (BonusStateCode IN ('0201', '0202', '0203')),
    CONSTRAINT CK_Bonus_Period  CHECK (DAY(Period) = 1),
    CONSTRAINT CK_Bonus_Value   CHECK (BonusValue > 0 AND (BonusTypeId <> 2 OR BonusValue <= 100)),
    CONSTRAINT CK_Bonus_TaxRate CHECK (TaxRate BETWEEN 0 AND 100),
    CONSTRAINT CK_Bonus_Amounts CHECK (GrossAmount >= 0 AND TaxAmount >= 0 AND NetAmount = GrossAmount - TaxAmount)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Bonus_Employee_Period_Active
    ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period)
    WHERE IsDeleted = 0 AND BonusStateCode <> '0203';

CREATE NONCLUSTERED INDEX IX_Bonus_Listing
    ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period DESC)
    INCLUDE (BonusStateCode)
    WHERE IsDeleted = 0;

CREATE NONCLUSTERED INDEX IX_Bonus_State_Period
    ON dbo.Bonus (BonusStateCode, Period DESC)
    WHERE IsDeleted = 0;
GO

SET NOEXEC OFF;
GO
