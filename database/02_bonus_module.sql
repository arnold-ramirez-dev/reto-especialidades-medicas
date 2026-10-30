/* =====================================================================
   Prueba técnica - Bonos de desempeño
   Script 2: módulo de bonos y distribución por proyecto
   Ejecutar después de 01_schema_seed.sql
   ===================================================================== */

USE BonusTest;
GO

CREATE TABLE dbo.Bonus (
    BonusId                INT IDENTITY(1,1) NOT NULL,
    DepartmentCode         CHAR(5)           NOT NULL,
    EmployeeNumber         INT               NOT NULL,
    Period                  DATE              NOT NULL,
    BonusTypeId             INT               NOT NULL,
    FixedAmount             DECIMAL(16,2)     NULL,
    Percentage              DECIMAL(5,2)      NULL,
    ReasonCode              CHAR(6)           NOT NULL,
    MonthlySalarySnapshot   DECIMAL(16,2)     NOT NULL,
    TaxRateSnapshot         DECIMAL(5,2)      NOT NULL,
    GrossAmount             DECIMAL(16,2)     NOT NULL,
    TaxAmount               DECIMAL(16,2)     NOT NULL,
    NetAmount               DECIMAL(16,2)     NOT NULL,
    BonusStateCode          CHAR(4)           NOT NULL
        CONSTRAINT DF_Bonus_BonusStateCode DEFAULT ('0201'),
    IsDeleted               BIT               NOT NULL
        CONSTRAINT DF_Bonus_IsDeleted DEFAULT (0),
    CreatedBy               VARCHAR(100)      NOT NULL,
    CreatedAt               DATETIME2(3)      NOT NULL
        CONSTRAINT DF_Bonus_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ModifiedBy              VARCHAR(100)      NULL,
    ModifiedAt              DATETIME2(3)      NULL,
    RowVersion              ROWVERSION        NOT NULL,

    CONSTRAINT PK_Bonus PRIMARY KEY (BonusId),
    CONSTRAINT FK_Bonus_Employee FOREIGN KEY (DepartmentCode, EmployeeNumber)
        REFERENCES dbo.Employee(DepartmentCode, EmployeeNumber),
    CONSTRAINT FK_Bonus_BonusType FOREIGN KEY (BonusTypeId)
        REFERENCES dbo.BonusType(BonusTypeId),
    CONSTRAINT FK_Bonus_BonusReason FOREIGN KEY (ReasonCode)
        REFERENCES dbo.BonusReason(ReasonCode),
    CONSTRAINT FK_Bonus_State FOREIGN KEY (BonusStateCode)
        REFERENCES dbo.State(StateCode),

    CONSTRAINT CK_Bonus_PeriodFirstDay CHECK (DAY(Period) = 1),
    CONSTRAINT CK_Bonus_EmployeeSnapshot CHECK (
        MonthlySalarySnapshot > 0 AND TaxRateSnapshot BETWEEN 0 AND 100
    ),
    CONSTRAINT CK_Bonus_InputByType CHECK (
        (BonusTypeId = 1 AND FixedAmount IS NOT NULL AND Percentage IS NULL
            AND FixedAmount > 0 AND FixedAmount <= MonthlySalarySnapshot)
        OR
        (BonusTypeId = 2 AND FixedAmount IS NULL AND Percentage IS NOT NULL
            AND Percentage > 0 AND Percentage <= 100)
    ),
    CONSTRAINT CK_Bonus_CalculatedAmounts CHECK (
        GrossAmount > 0 AND TaxAmount >= 0 AND NetAmount >= 0
        AND TaxAmount <= GrossAmount AND NetAmount <= GrossAmount
    ),
    CONSTRAINT CK_Bonus_StateFamily CHECK (LEFT(BonusStateCode, 2) = '02')
);
GO

/* La regla también queda protegida ante solicitudes concurrentes. */
CREATE UNIQUE INDEX UX_Bonus_ActiveEmployeePeriod
    ON dbo.Bonus (DepartmentCode, EmployeeNumber, Period)
    WHERE IsDeleted = 0 AND BonusStateCode <> '0203';
GO

/* Índice que soporta los filtros y el orden del listado principal. */
CREATE INDEX IX_Bonus_List
    ON dbo.Bonus (Period DESC, DepartmentCode, EmployeeNumber, BonusStateCode)
    INCLUDE (BonusTypeId, ReasonCode, FixedAmount, Percentage,
             GrossAmount, TaxAmount, NetAmount, CreatedAt)
    WHERE IsDeleted = 0;
GO

CREATE TABLE dbo.BonusProjectAllocation (
    BonusProjectAllocationId INT IDENTITY(1,1) NOT NULL,
    BonusId                  INT               NOT NULL,
    EmployeeProjectHoursId   INT               NOT NULL,
    ProjectName              VARCHAR(100)      NOT NULL,
    Hours                    DECIMAL(8,2)      NOT NULL,
    AllocatedAmount          DECIMAL(16,2)     NOT NULL,
    CreatedBy                VARCHAR(100)      NOT NULL,
    CreatedAt                DATETIME2(3)      NOT NULL
        CONSTRAINT DF_BonusProjectAllocation_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_BonusProjectAllocation PRIMARY KEY (BonusProjectAllocationId),
    CONSTRAINT FK_BonusProjectAllocation_Bonus FOREIGN KEY (BonusId)
        REFERENCES dbo.Bonus(BonusId),
    CONSTRAINT FK_BonusProjectAllocation_ProjectHours FOREIGN KEY (EmployeeProjectHoursId)
        REFERENCES dbo.EmployeeProjectHours(EmployeeProjectHoursId),
    CONSTRAINT UQ_BonusProjectAllocation_BonusProject
        UNIQUE (BonusId, EmployeeProjectHoursId),
    CONSTRAINT CK_BonusProjectAllocation_Values CHECK (
        Hours > 0 AND AllocatedAmount >= 0
    )
);
GO

CREATE INDEX IX_BonusProjectAllocation_BonusId
    ON dbo.BonusProjectAllocation (BonusId)
    INCLUDE (ProjectName, Hours, AllocatedAmount);
GO
