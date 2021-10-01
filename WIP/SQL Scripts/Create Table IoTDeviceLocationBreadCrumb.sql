

DROP TABLE IF EXISTS [dbo].[IoTDeviceLocationBreadCrumb]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

--Location breadcrumb table
--I know there is a way to open google maps preloaded with data points. 
--We could use that to display a breadcrumb trail of where a device is located.

--In context to the idea of Triangulating a Bluetooth signal, that would be 
--more challenging, but we might be able to think of something. I would 
--consider this a bonus item that we table for the end time permitting
CREATE TABLE [dbo].[IoTDeviceLocationBreadCrumb](
       [CompanyID] [int] NOT NULL,
       [DeviceID] [int] NOT NULL,
       [LineNbr] INT NOT NULL,

       [DeviceCD] NVARCHAR (50) NOT NULL,
       --if we are dealing with GPS data these fields are used
       [Latitude] DECIMAL(11,8) null,
       [Longitude] DECIMAL(11,8) null,
       
       --if we are using triagulation of blue tooth signal we
       --can use this. Note we might be able to use the same 
       --feilds of lat long and use a type to qualify either or.
       [ZoneXCoordinate] DECIMAL(11,8) null,
       [ZoneYCooridinate] DECIMAL(11,8) null,
       
       --Time is an important metric to track device within space-time
       --We dont want to depend on the time the record was created as we 
       --may need to deal with a dataset that does not have a continuous 
       --connection to the system and may need to upload many data points 
       --in one push periodically. Hence, the reason we need to manage 
       --this point on our own 
       [Time] DateTime 

       --not certain if these are needed. your discretion to add
       --[NoteID] [uniqueidentifier] NOT NULL,
       --[CreatedByID] [uniqueidentifier] NOT NULL,
       --[CreatedByScreenID] [char](8) NOT NULL,
       --[CreatedDateTime] [datetime] NOT NULL,
       --[LastModifiedByID] [uniqueidentifier] NOT NULL,
       --[LastModifiedByScreenID] [char](8) NOT NULL,
       --[LastModifiedDateTime] [datetime] NOT NULL,
       --[tstamp] [timestamp] NOT NULL,
CONSTRAINT [IoTDeviceLocationBreadCrumb_PK] PRIMARY KEY CLUSTERED 
(
       [CompanyID] ASC,
       [DeviceID] ASC,
       [LineNbr] ASC
)WITH (
PAD_INDEX = OFF, 
STATISTICS_NORECOMPUTE = OFF, 
IGNORE_DUP_KEY = OFF, 
ALLOW_ROW_LOCKS = ON, 
ALLOW_PAGE_LOCKS = ON, 
OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO
