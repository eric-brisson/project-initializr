-- Appliqué par Flyway (docker compose run --rm flyway). Ne modifiez jamais un script déjà appliqué :
-- ajoutez le changement suivant dans V2__<description>.sql.
CREATE TABLE dbo.Technologies
(
    Id   INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Technologies PRIMARY KEY,
    Name NVARCHAR(100)       NOT NULL
);

-- Correspond à HasIndex(...).IsUnique() dans Technology.cs : un nom en double reçoit un 409 Conflict.
CREATE UNIQUE INDEX IX_Technologies_Name ON dbo.Technologies (Name);
