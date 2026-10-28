-- Applied by Flyway (docker compose run --rm flyway). Never edit a script once applied:
-- add the next change as V2__<description>.sql.
CREATE TABLE dbo.Technologies
(
    Id      INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Technologies PRIMARY KEY,
    Name    NVARCHAR(100)       NOT NULL,
    Version NVARCHAR(50)        NOT NULL
);

CREATE UNIQUE INDEX IX_Technologies_Name ON dbo.Technologies (Name);
