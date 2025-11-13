--Create table (Int_Configuration)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_Configuration') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_Configuration(
      [ConfigurationID] INT NOT NULL ,
      [KeyName] NVARCHAR(200) NOT NULL ,
      [KeyValue] NVARCHAR(200) NOT NULL ,

 CONSTRAINT [aaaaaInt_Configuration_PK] PRIMARY KEY CLUSTERED 
(
  KeyName
));

EXEC('INSERT INTO Int_Configuration VALUES (1,''AppVersion'',''000000000000'')')
EXEC('INSERT INTO Int_Configuration VALUES (2,''DBVersion'',''1'')')
EXEC('INSERT INTO Int_Configuration VALUES (3,''ClientVersion'',''0'')')

PRINT 'Table Int_Configuration added successfully.'
END
ELSE
PRINT 'Table Int_Configuration already Exists.'

--Create table (Int_Tasks)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_Tasks') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_Tasks(
      [TaskID] INT IDENTITY(1,1) NOT NULL ,
      [Name] NVARCHAR(200) NOT NULL ,
      [StartDate] DATETIME NOT NULL ,
      [EndDate] DATETIME NOT NULL ,
      [Status] INT NOT NULL,
	  [Priority] INT NOT NULL,
	  [RefTaskID] INT NULL,
	  [CreatedBy] INT NOT NULL
);

PRINT 'Table Int_Tasks added successfully.'
END
ELSE
PRINT 'Table Int_Tasks already Exists.'

--Create table (Int_TaskSchedule)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_TaskSchedule') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_TaskSchedule(
      [TaskID] INT NOT NULL ,
      [ScheduleType] INT NOT NULL ,
      [Time] NVARCHAR(200) NULL ,
	  [EndTime] NVARCHAR(200) NULL ,
      [Period] INT NULL 
 );

PRINT 'Table Int_TaskSchedule added successfully.'
END
ELSE
PRINT 'Table Int_TaskSchedule already Exists.'

--Create table (Int_TaskAction)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_TaskAction') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_TaskAction(
      [TaskID] INT NOT NULL ,
      [ActionID] INT IDENTITY(1,1) NOT NULL ,
      [ActionType] INT NOT NULL ,
	  [OrganizationID] INT NOT NULL ,
      [FieldID] INT NOT NULL,
	  [Sequence] INT NOT NULL
 );
 
PRINT 'Table Int_TaskAction added successfully.'
END
ELSE
PRINT 'Table Int_TaskAction already Exists.'

--Create table (Int_ActionFilter)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_ActionFilter') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_ActionFilter(
      [TaskID] INT NOT NULL ,
      [ActionID] INT NOT NULL ,
      [FilterID] INT NOT NULL ,
      [Value] NVARCHAR(200) NOT NULL
 );

PRINT 'Table Int_ActionFilter added successfully.'
END
ELSE
PRINT 'Table Int_ActionFilter already Exists.'

--Create table (Int_Filter)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_Filter') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_Filter(
      [FilterID] INT IDENTITY(1,1) NOT NULL ,
	  [FilterName] NVARCHAR(200) NOT NULL ,
      [FilterType] INT NOT NULL ,
      [CanSelectAll] BIT NOT NULL ,
      [DefaultValue] NVARCHAR(200) NOT NULL ,
      [QueryString] NVARCHAR(1000) NULL ,
	  [ValueMember] NVARCHAR(200) NULL ,
      [DisplayMember1] NVARCHAR(200) NULL ,
      [DisplayMember2] NVARCHAR(200) NULL ,
      [DisplayDescription1] NVARCHAR(200) NULL ,
      [DisplayDescription2] NVARCHAR(200) NULL 
 );

PRINT 'Table Int_Filter added successfully.'
END
ELSE
PRINT 'Table Int_Filter already Exists.'

--Create table (Int_Session)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_Session') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_Session(
      [SessionID] INT IDENTITY(1,1) NOT NULL ,
      [UserID] INT NOT NULL ,
	  [MachineName] NVARCHAR(200) NOT NULL ,
      [StartTime] DATETIME NOT NULL ,
      [EndTime] DATETIME NULL ,
      [LastActive] DATETIME NOT NULL 
 );

PRINT 'Table Int_Session added successfully.'
END
ELSE
PRINT 'Table Int_Session already Exists.'

--Create table (Int_Privileges)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_Privileges') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_Privileges(
      [PrivilegeID] INT NOT NULL ,
	  [PrivilegeType] INT NOT NULL ,
      [Description] NVARCHAR(200) NULL ,
      [ParentID] INT NOT NULL 
 );

EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (1,1,''Manual Integration'',0)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (2,1,''Schedule Management'',0)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (3,1,''Users Access'',0)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (4,1,''Operations Monitoring'',0)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (5,1,''Employees Importing'',0)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (6,1,''Targets Importing'',0)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (7,1,''Integration Update'',1)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (8,1,''Integration Send'',1)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (1,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (2,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (3,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (4,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (5,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (6,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (7,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (8,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (9,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (10,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (11,2,8)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (12,2,8)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (13,2,8)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (14,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (15,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (16,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (17,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (18,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (19,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (20,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (21,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (22,2,7)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (23,2,8)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (24,2,8)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (25,2,8)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (26,2,8)')
EXEC('INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (27,2,8)')

PRINT 'Table Int_Privileges added successfully.'
END
ELSE
PRINT 'Table Int_Privileges already Exists.'

--Create table (Int_UserPrivileges)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_UserPrivileges') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_UserPrivileges(
      [UserID] INT NOT NULL ,
      [PrivilegeID] INT NOT NULL ,
	  [PrivilegeType] INT NOT NULL ,
      [Value] INT NOT NULL 
 );

PRINT 'Table Int_UserPrivileges added successfully.'
END
ELSE
PRINT 'Table Int_UserPrivileges already Exists.'

--Create table (Int_Field)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_Field') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_Field(
      [FieldID] INT NOT NULL ,
      [ActionType] INT NOT NULL ,
      [FieldName] NVARCHAR(200) NOT NULL 
 );

EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (1,1,''Item'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (2,1,''Customer'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (3,1,''Price'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (4,1,''Discount'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (5,1,''Route'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (6,1,''Invoice'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (7,1,''KPI'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (8,1,''STA'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (9,1,''EDI'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (10,1,''Stock'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (11,2,''Orders'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (12,2,''Reciepts'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (13,2,''ATMDeposit'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (14,1,''STP'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (15,1,''PackGroups'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (16,1,''CNT'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (17,1,''Outstanding'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (18,1,''Main Warehouse Stock'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (19,1,''Vehicles'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (20,1,''Warehouse'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (21,1,''Salesperson'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (22,1,''Geo Locations'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (23,2,''Sales'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (24,2,''Transfers'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (25,2,''Returns'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (26,2,''OrderInvoice'')')
EXEC('INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (27,2,''NewCustomer'')')

PRINT 'Table Int_Field added successfully.'
END
ELSE
PRINT 'Table Int_Field already Exists.'

--Create table (Int_FieldFilters)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_FieldFilters') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_FieldFilters(
      [FieldID] INT NOT NULL ,
      [FilterID] INT NOT NULL 
 );

PRINT 'Table Int_FieldFilters added successfully.'
END
ELSE
PRINT 'Table Int_FieldFilters already Exists.'
