

-- **************************
-- Lets create login
-- **************************

CREATE LOGIN Michal 
WITH
  PASSWORD = 'MichalPass!1' ;
GO


-- **************************
-- Lets create user
-- **************************

-- switch to database context
USE [QandA];
GO

CREATE USER [Michal] FOR LOGIN [Michal];
GO

-- **************************
-- Lets grant control permission
-- of QandA database to Michal
-- https://docs.microsoft.com/en-us/sql/t-sql/statements/grant-database-permissions-transact-sql?view=sql-server-ver15#examples
-- https://docs.microsoft.com/en-us/sql/t-sql/statements/grant-transact-sql?view=sql-server-ver15
-- **************************


GRANT CONTROL ON DATABASE::QandA TO Michal