IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Launches')
BEGIN
    CREATE TABLE [Launches] (
        [Id]              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        [FlightNumber]    INT             NOT NULL,
        [Name]            NVARCHAR(200)   NOT NULL,
        [DateUtc]         DATETIME2       NOT NULL,
        [Success]         BIT             NULL,
        [Details]         NVARCHAR(MAX)   NULL,
        [RocketId]        NVARCHAR(50)    NULL,
        [PatchSmall]      NVARCHAR(500)   NULL,
        [PatchLarge]      NVARCHAR(500)   NULL,
        [Webcast]         NVARCHAR(500)   NULL,
        [Article]         NVARCHAR(500)   NULL,
        [Wikipedia]       NVARCHAR(500)   NULL
    );
    
    CREATE INDEX IX_Launches_FlightNumber ON [Launches]([FlightNumber]);
    CREATE INDEX IX_Launches_DateUtc ON [Launches]([DateUtc]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LaunchFailures')
BEGIN
    CREATE TABLE [LaunchFailures] (
        [Id]              INT             NOT NULL PRIMARY KEY IDENTITY(1,1),
        [LaunchId]        NVARCHAR(50)    NOT NULL,
        [TimeSeconds]     INT             NULL,
        [AltitudeKm]      INT             NULL,
        [Reason]          NVARCHAR(500)   NOT NULL,
    
        CONSTRAINT FK_LaunchFailures_Launches
            FOREIGN KEY ([LaunchId]) REFERENCES [Launches]([Id])
            ON DELETE CASCADE
    );
    
    CREATE INDEX IX_LaunchFailures_LaunchId ON [LaunchFailures]([LaunchId]);
END