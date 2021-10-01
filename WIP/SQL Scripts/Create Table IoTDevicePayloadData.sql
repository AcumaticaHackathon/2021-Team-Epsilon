
DROP TABLE IF EXISTS [dbo].[IoTDevicePayloadData]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
--this table will temporarily store the raw unprocessed payload 
--     data that gets sent from home assistant. As we are developing
--     this will prove helpful as it will allow us to inspect raw JSON
--     so that we can strategize and implement the actual processing 
--     at a later time. 
CREATE TABLE [dbo].[IoTDevicePayloadData](
       [CompanyID] [int] NOT NULL,
       [DeviceID] [int] NOT NULL,
       [LineNbr] INT NOT NULL,

       [DeviceCD] NVARCHAR (50) NOT NULL,

       --this holds the raw JSON payload info sent from home assistant
       --     
       [Payload] [nvarchar](max) NULL,
       --This field will store a string that indicates the type of 
       --     data the payload is. For example, if it is information on a State
       --     change for a Tile device we can code it as "TILE" if it is 
       --     Life360 data, we can code it as "Life360". This will be sent as 
       --     A query parameter expected by the webhook. so that we can let
       --     let Acumatica know how to handle it via a case statement or 
       --     polymorphic dispatch
       [PayloadType][nvarchar](20),
       --Store any other query parameters here. This might prove useful as
       --     it did with our surveys project.
       [QueryParameters] [nvarchar](255) NULL,
       
       [NoteID] [uniqueidentifier] NOT NULL,
       [CreatedByID] [uniqueidentifier] NOT NULL,
       [CreatedByScreenID] [char](8) NOT NULL,
       [CreatedDateTime] [datetime] NOT NULL,
       [LastModifiedByID] [uniqueidentifier] NOT NULL,
       [LastModifiedByScreenID] [char](8) NOT NULL,
       [LastModifiedDateTime] [datetime] NOT NULL,
       [tstamp] [timestamp] NOT NULL,
CONSTRAINT [IoTDevicePayloadData_PK] PRIMARY KEY CLUSTERED 
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
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO