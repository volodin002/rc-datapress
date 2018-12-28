

CREATE TABLE [dbo].[Task] (
    [Id]    INT NOT NULL IDENTITY,
    
    [Name]  NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(100) NULL,
    [Description]  NVARCHAR(250) NULL,
    
    [IsActive] BIT not NULL CONSTRAINT DF_Task_IsActive DEFAULT 0,
    
	[MinThreads] INT     NOT NULL CONSTRAINT DF_Task_MinThreads DEFAULT 1,
    [MaxThreads] INT     NOT NULL CONSTRAINT DF_Task_MaxThreads DEFAULT 1,

	[ParametersJson] NVARCHAR(MAX) NULL,

	IsFixedRate BIT NOT NULL,
	Rate        INT NOT NULL,
	LogLevel    TINYINT NOT NULL,
    [Version]   INT NOT NULL CONSTRAINT DF_Task_Version DEFAULT 0,

    CONSTRAINT PK_Task PRIMARY KEY (Id)
)

CREATE TABLE [dbo].[TaskAction] (
    [Id]     INT NOT NULL IDENTITY,
    [TaskId] INT NOT NULL, 

    [Name]  NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(100) NULL,
    [Description]  NVARCHAR(250) NULL,
    
    [Module] NVARCHAR(100) NOT NULL,
    [Type]  NVARCHAR(100) NOT NULL,

	[ParametersJson] NVARCHAR(MAX) NULL,

    [Order] INT NOT NULL,

    CONSTRAINT PK_Action PRIMARY KEY (Id),
    CONSTRAINT FK_Action_TaskId FOREIGN KEY (TaskId) REFERENCES [dbo].[Task](Id)
)

/*
CREATE TABLE [dbo].[ActionParam] (
    [Id]     INT NOT NULL IDENTITY,
    [ActionId] INT NOT NULL, 

    [Name]  NVARCHAR(50) NOT NULL,

    -- 0: string, 1: int, 2: bool, 3: datetime, 4: float
    ValType TINYINT NOT NULL,
    IsArray BIT     NOT NULL CONSTRAINT DF_ActionParam_IsArray DEFAULT 0,
    StrVal  NVARCHAR(1000) NULL,
    IntVal  INT NULL,
    BoolVal BIT NULL,
    DtVal   DATETIME NULL,
    FlVal   FLOAT NULL,

    CONSTRAINT PK_ActionParam PRIMARY KEY (Id),
    CONSTRAINT FK_ActionParam_ActionId FOREIGN KEY (ActionId) REFERENCES [dbo].[Action](Id)
)

CREATE TABLE [dbo].[TaskParam] (
    [Id]     INT NOT NULL IDENTITY,
    [TaskId] INT NOT NULL, 

    [Name]  NVARCHAR(50) NOT NULL,

    -- 0: string, 1: int, 2: bool, 3: datetime, 4: float
    ValType TINYINT NOT NULL,
    IsArray BIT     NOT NULL CONSTRAINT DF_TaskParam_IsArray DEFAULT 0,
    StrVal  NVARCHAR(1000) NULL,
    IntVal  INT NULL,
    BoolVal BIT NULL,
    DtVal   DATETIME NULL,
    FlVal   FLOAT NULL,

    CONSTRAINT PK_TaskParam PRIMARY KEY (Id),
    CONSTRAINT FK_TaskParam_TaskId FOREIGN KEY (TaskId) REFERENCES [dbo].[Task](Id)
)
*/

CREATE TABLE [dbo].[TaskLog] (
    [Id]       BIGINT NOT NULL IDENTITY,
    [TaskId]   INT NOT NULL,
    [ActionId] INT NULL,

    [Level]    TINYINT NOT NULL,
    [Thread]   INT NULL,
    [Created]  DATETIME NOT NULL,
    [Message]  NVARCHAR(MAX),
	[Error]    NVARCHAR(MAX),

    CONSTRAINT PK_TaskLog PRIMARY KEY (Id),
)

CREATE TABLE [dbo].[SvcParam] (
    [Id]     INT NOT NULL IDENTITY,
    
    [Name]  NVARCHAR(50) NOT NULL,
    [Description]  NVARCHAR(250) NULL,

    -- 0: string, 1: int, 2: bool, 3: datetime, 4: float
    ValType TINYINT NOT NULL,
    IsArray BIT     NOT NULL CONSTRAINT DF_SvcParam_IsArray DEFAULT 0,
    StrVal  NVARCHAR(1000) NULL,
    IntVal  INT NULL,
    BoolVal BIT NULL,
    DtVal   DATETIME NULL,
    FlVal   FLOAT NULL,

    CONSTRAINT PK_SvcParam PRIMARY KEY (Id),
)

CREATE TABLE [dbo].TaskTag (
	[Id]   INT          NOT NULL IDENTITY,
	[Name] VARCHAR(100) NOT NULL,
	
	[Description]  NVARCHAR(250) NULL,

	CONSTRAINT PK_TaskTag PRIMARY KEY (Id),
)

CREATE TABLE [dbo].TaskToTag (
	[TaskId] INT NOT NULL,
	[TagId]  INT NOT NULL

	CONSTRAINT FK_TaskToTag_TaskId FOREIGN KEY (TaskId) REFERENCES [dbo].[Task],
	CONSTRAINT FK_TaskToTag_TagId FOREIGN KEY (TagId) REFERENCES [dbo].[TaskTag],
)

---------------- // Initial Data //------------------
/*
INSERT INTO [dbo].[SvcParam] ([Name],[Description],ValType,IntVal)
VALUES
(
    'pool_threads', 
    
    'Size of Thread Pool.\
    If not specified then it will default to the number \
    of processors on the machine, multiplied by 5 \
    If lower or equal to 0 Thread Pool will not be created',

    1, 8
)


INSERT INTO [dbo].[SvcParam] ([Name],[Description],ValType,IntVal)
VALUES
(
    'pool_processes', 
    
    'Size of Process Pool. \
    If not specified then it will default to the number \
    of processors on the machine \
    If lower or equal to 0 Process Pool will not be created',

    1, 8
)
*/