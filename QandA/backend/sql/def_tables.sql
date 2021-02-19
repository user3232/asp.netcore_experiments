-- https://github.com/PacktPublishing/ASP.NET-Core-3-and-React/blob/master/Chapter07/backend/SQLScripts/01-Tables.sql


-- script initializing tables in QandA database


/*
  DOC:

  # Some common acronyms
    - online transaction processing (OLTP)
    - Decision Support System (DSS)
    - data warehousing (OLAP)

  # All about indexes

    https://docs.microsoft.com/en-us/sql/relational-databases/sql-server-index-design-guide?view=sql-server-ver15
*/

-- switch to QandA context
USE QandA
GO


CREATE TABLE dbo.Question (
  -- Only one identity column can be created per table.
  -- column marked as identity will generate values
  -- IDENTITY[(seed,increment)]
  -- identity does not guarantee uniqness (e.g. concurent transactions)
  QuestionId INT IDENTITY(1,1) NOT NULL,
  -- nvarchar are strings with varying length
  -- nvarchar [ ( n | max ) ] , (n is maximum number of bytes stored)
  -- When n is not specified -> n = 1
  -- When string must be truncated error is generated
  Title NVARCHAR(100) NOT NULL,
  Content NVARCHAR(max) NOT NULL,
  UserId NVARCHAR(150) NOT NULL,
  UserName NVARCHAR(150) NOT NULL,
  /*
    Defines a date that is combined with a time of day 
    that is based on 24-hour clock. 
    
    datetime2 can be considered as an extension of the existing
    datetime type that:
    - has a larger date range, 
    - a larger default fractional precision, 
    - and optional user-specified precision.

    Syntax: 	datetime2 [ (fractional seconds precision) ]
    Usage: 	  DECLARE @MyDatetime2 datetime2(7)

    Format:   
      Default:  YYYY-MM-DD hh:mm:ss[.fractional seconds]
      ISO 8601: YYYY-MM-DDThh:mm:ss[.nnnnnnn]
      ODBC:     { ts 'yyyy-mm-dd hh:mm:ss[.fractional seconds]' }

    Date range: [0001-01-01] to [9999-12-31]
                [January 1,1 CE] to [December 31, 9999 CE]
    Time range: [00:00:00] to [23:59:59.9999999]
    Default value: 	1900-01-01 00:00:00
    Calendar: 	Gregorian
    No time zones
  */
  Created DATETIME2(7) NOT NULL,
  /*
    PRIMARY KEY = UNIQUE and NOT NULL

    - only one primary
    - cannot exceed 16 columns and a total key length of 900 bytes
    - index is generated 
    - All columns defined within a primary key constraint must be defined as not null.
    - If a primary key is defined on a CLR user-defined type column, 
      the implementation of the type must support binary ordering.
  
    Indexex:

    - CREATE CLUSTERED INDEX : index have data
    - CREATE INDEX : index have pointers to data
    - https://docs.microsoft.com/en-us/sql/relational-databases/indexes/indexes?view=sql-server-ver15
  */
  CONSTRAINT PK_Question PRIMARY KEY CLUSTERED -- clustered means 
  (
    QuestionId ASC -- if specified query optimizer sometimes may use this
  )
)
GO

CREATE TABLE dbo.Answer (
  AnswerId INT IDENTITY(1,1) NOT NULL,
  QuestionId INT NOT NULL,
  Content NVARCHAR(max) NOT NULL,
  UserId NVARCHAR(150) NOT NULL,
  UserName NVARCHAR(150) NOT NULL,
  Created DATETIME2(7) NOT NULL,
  CONSTRAINT PK_Answer PRIMARY KEY CLUSTERED
  (
    AnswerId ASC
  )
)
GO


ALTER TABLE dbo.Answer 
  WITH CHECK -- validated table rows against newly added constaint
             -- NOCHECK assumes data is valid
  ADD 
    CONSTRAINT FK_Answer_Question FOREIGN KEY -- referencing PK or unique column (set)
                                              -- of another table
      (QuestionId) -- fk column in this table
      REFERENCES dbo.Question (QuestionId) -- pk column in referenced table
      ON UPDATE CASCADE -- Corresponding rows are updated in the referencing table 
                        -- when that row is updated in the parent table.
      ON DELETE CASCADE -- Corresponding rows are deleted from the referencing 
                        -- table if that row is deleted from the parent table.
                        -- (dbo.Answer is referencing table)
GO

/*
  For shoure one could do:
    ALTER TABLE dbo.Answer 
      WITH CHECK
        CHECK CONSTRAINT FK_Answer_Question
    GO
  */
ALTER TABLE dbo.Answer 
  CHECK CONSTRAINT FK_Answer_Question -- !!! this enables constraint
GO

