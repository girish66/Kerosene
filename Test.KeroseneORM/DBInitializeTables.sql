--- =============================================
--- PRINT 'DELETING CONTENTS...'
--- =============================================
USE KeroseneDB
GO

PRINT 'Deleting contents from Employees...'
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type in (N'U'))
DELETE FROM [dbo].[Employees]
GO

PRINT 'Deleting contents from Countries...'
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Countries]') AND type in (N'U'))
DELETE FROM [dbo].[Countries]
GO

PRINT 'Deleting contents from Regions...'
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Regions]') AND type in (N'U'))
DELETE FROM [dbo].[Regions]
GO

--- =============================================
--- PRINT 'DROPPING TABLES...'
--- =============================================
USE KeroseneDB
GO

--- EMPLOYEES
PRINT 'Dropping table Employees...'

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Employees_Countries]')
	AND parent_object_id = OBJECT_ID(N'[dbo].[Employees]'))
ALTER TABLE [dbo].[Employees] DROP CONSTRAINT [FK_Employees_Countries]
GO

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Employees_Employees]')
	AND parent_object_id = OBJECT_ID(N'[dbo].[Employees]'))
ALTER TABLE [dbo].[Employees] DROP CONSTRAINT [FK_Employees_Employees]
GO

IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_Employees_Active]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Employees] DROP CONSTRAINT [DF_Employees_Active]
END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type in (N'U'))
DROP TABLE [dbo].[Employees]
GO

--- COUNTRIES
PRINT 'Dropping table Countries...'

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Countries_Regions]')
	AND parent_object_id = OBJECT_ID(N'[dbo].[Countries]'))
ALTER TABLE [dbo].[Countries] DROP CONSTRAINT [FK_Countries_Regions]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Countries]') AND type in (N'U'))
DROP TABLE [dbo].[Countries]
GO

--- REGIONS
PRINT 'Dropping table Regions...'

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Regions_Regions]')
	AND parent_object_id = OBJECT_ID(N'[dbo].[Regions]'))
ALTER TABLE [dbo].[Regions] DROP CONSTRAINT [FK_Regions_Regions]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Regions]') AND type in (N'U'))
DROP TABLE [dbo].[Regions]
GO

--- =============================================
--- PRINT 'CREATING TABLES...'
--- =============================================
USE KeroseneDB
GO

--- REGIONS
PRINT 'Creating table Regions...'
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Regions](
	[Id] [nvarchar](3) NOT NULL,
	[Name] [nvarchar](50) NULL DEFAULT NULL,
	[ParentId] [nvarchar](3) NULL DEFAULT NULL,
 CONSTRAINT [PK_Regions] PRIMARY KEY CLUSTERED ( [Id] ASC )
 WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
 ON [PRIMARY]
)
ON [PRIMARY]
GO

ALTER TABLE [dbo].[Regions]  WITH CHECK ADD  CONSTRAINT [FK_Regions_Regions] FOREIGN KEY([ParentId])
REFERENCES [dbo].[Regions] ([Id])
GO
ALTER TABLE [dbo].[Regions] CHECK CONSTRAINT [FK_Regions_Regions]
GO

--- COUNTRIES
PRINT 'Creating table Countries...'
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Countries](
	[Id] [nvarchar](3) NOT NULL,
	[Name] [nvarchar](50) NULL DEFAULT NULL,
	[RegionId] [nvarchar](3) NOT NULL,
 CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED ( [Id] ASC )
 WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
 ON [PRIMARY]
)
ON [PRIMARY]
GO

ALTER TABLE [dbo].[Countries]  WITH CHECK ADD  CONSTRAINT [FK_Countries_Regions] FOREIGN KEY([RegionId])
REFERENCES [dbo].[Regions] ([Id])
GO
ALTER TABLE [dbo].[Countries] CHECK CONSTRAINT [FK_Countries_Regions]
GO

--- EMPLOYEES
PRINT 'Creating table Employees...'
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Employees](
	[Id] [nvarchar](6) NOT NULL,
	[FirstName] [nvarchar](50) NULL DEFAULT NULL,
	[LastName] [nvarchar](50) NULL DEFAULT NULL,
	[BirthDate] [date] NULL DEFAULT NULL,
	[Active] [bit] NULL DEFAULT NULL,
	[ManagerId] [nvarchar](6) NULL DEFAULT NULL,
	[CountryId] [nvarchar](3) NOT NULL,
	[JoinDate] [date] NULL DEFAULT NULL,
	[StartTime] [time] NULL DEFAULT NULL,
	[Photo] [varbinary](max) NULL DEFAULT NULL,
	[FullName] AS (([FirstName] + ' ') + [LastName]),

 CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED ( [Id] ASC )
 WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
 ON [PRIMARY]
)
ON [PRIMARY]
GO

ALTER TABLE [dbo].[Employees]  WITH CHECK ADD  CONSTRAINT [FK_Employees_Countries] FOREIGN KEY([CountryId])
REFERENCES [dbo].[Countries] ([Id])
GO
ALTER TABLE [dbo].[Employees] CHECK CONSTRAINT [FK_Employees_Countries]
GO

ALTER TABLE [dbo].[Employees]  WITH CHECK ADD CONSTRAINT [FK_Employees_Employees] FOREIGN KEY([ManagerId])
REFERENCES [dbo].[Employees] ([Id])
GO
ALTER TABLE [dbo].[Employees] CHECK CONSTRAINT [FK_Employees_Employees]
GO

--- ALTER TABLE [dbo].[Employees] ADD CONSTRAINT [DF_Employees_Active] DEFAULT ((NULL)) FOR [Active]
--- GO

--- =============================================
--- PRINT 'INSERTING INITIAL CONTENTS...'
--- =============================================
USE KeroseneDB
GO

--- REGIONS
PRINT 'Inserting contents in Regions...'

INSERT INTO [dbo].[Regions] ( Id, Name, ParentId )
VALUES
	( '000', 'World', null ),

	( '100', 'Americas', '000'  )
		,( '110', 'North America', '100' )
		,( '120', 'Central America', '100' )
	
	,( '200', 'Europe, Middle East & Africa', '000' )
		,( '210', 'EMEA North', '200' )
		,( '220', 'EMEA South', '200' )
			,( '221', 'West Mediterranean', '230' )
			,( '223', 'Middle East', '230' )
			,( '225', 'Africa', '230' )
		,( '230', 'EMEA Central', '200' )
		,( '240', 'EMEA East', '200' )
	
	,( '300', 'Asia and Pacific', '000' )
		,( '310', 'Japan', '300' )
GO

--- COUNTRIES
PRINT 'Inserting contents in Countries...'

INSERT INTO [dbo].[Countries] ( Id, Name, RegionId )
VALUES
	( 'us', 'United States of America', '110' )
	,( 'mx', 'Mexico', '120' )
	
	,( 'uk', 'United Kingdom', '210' )
	
	,( 'es', 'España', '221' )	
	,( 'pt', 'Portugal', '221' )
	,( 'it', 'Italy', '221' )
	,( 'ae', 'United Arab Emirates', '223' )
	,( 'za', 'Republic of South Africa', '225' )
	
	,( 'jp', 'Japan', '310' )
GO 

--- EMPLOYEES
PRINT 'Inserting contents in Employees...'

INSERT INTO [dbo].[Employees]
	( Id, Active, ManagerId, CountryId, FirstName, LastName, BirthDate, JoinDate, StartTime, Photo )
VALUES
	( '1001', 1, null, 'us', 'Tom', 'Thomsom', '1969-9-12', '2002-1-24', '10:12:45', null )
		,('1002', 1, '1001', 'us', 'Dave', 'Alistair', '1959-11-23', '2001-11-9', '18:00:45', null )
		,('2001', 1, '1001', 'uk', 'Mohammed', 'Al Yashira', null, null, null, null )
			,('2002', 1, '2001', 'uk', 'Andrew', 'Mc Quanty', '1970-11-23', '2001-11-9', '18:00:45', null )
			,('2003', 1, '2001', 'es', 'David', 'Perez de Manto', null, null, null, null )
				,('2005', 1, '2004', 'es', 'Juan', 'Perez Gomez', null, null, null, null )
					,('2008', 1, '2005', 'es', 'Fernando', 'Quesero Villaverde', null, null, null, null )
					,('2009', 1, '2005', 'es', 'Antonio', 'Martinez Alamo', null, null, null, null )
				,('2006', 1, '2004', 'za', 'Richard', 'Mc Donnel', null, null, null, null )
					,('2010', 1, '2006', 'za', 'Paul', 'Biggeron', null, null, null, null )
					,('2011', 1, '2006', 'za', 'Nicole', 'Weather', null, null, null, null )
			,('2004', 1, '2001', 'ae', 'Hassan', 'El Auly', '1969-1-15', '2001-11-9', '18:00:45', null )
				,('2007', 1, '2005', 'ae', 'John', 'Burrogough', null, null, null, null )
		,('3001', 1, '1001', 'jp', 'Asira', 'Yamamoto', '1967-2-20', '2003-8-23', '8:00:00', null )
GO

--- =============================================
--- PRINT 'VALIDATING DB INITIALIZATION...'
--- =============================================
--- SELECT * FROM [dbo].[Employees]
--- SELECT * FROM [dbo].[Countries]
--- SELECT * FROM [dbo].[Regions]
--- GO
