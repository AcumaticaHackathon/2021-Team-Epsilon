
/*pseudocode for webhook
10) Webhook receives a request with two mandatory query params 1-DeviceId and 2-PayloadType as well as a JSON body.

20) The webhook takes the DeviceId and determines if the record exists, and if none exists, one is auto-created.
             * for auto create records, a human user will initialize a Description manually sometime after a create

30) The payload data is stored in the payload table using the extracted DeviceId and Payload type. Any extra 
             query params will store in the query params field. This will allow us to send any query param we please
             that may be picked up in the processing

40) The new payload data record is sent in for processing
       50) The payload.PayloadType is sent into a Case/Switch statement or handled via Polymorphic dispatch. 
                    That will, in turn, handle specific logic for that payload type.
       60) if the payload.PayloadType is not yet programmed, then exit with a return. This will allow us to investigate the 
                    JSON payload and allow someone to start building a new processing routine for that type.
40) End processing
*/

DROP TABLE IF EXISTS [dbo].[IoTDevice]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[IoTDevice](
       [CompanyID] [int] NOT NULL,
       [DeviceID] [int] IDENTITY(1,1) NOT NULL,
       [DeviceCD] NVARCHAR (50) NOT NULL,
       [DeviceName] [nvarchar](60) NOT NULL,
       --[UserID] [uniqueidentifier] NOT NULL,
       --[CollectedDate] [datetime] NULL,
       --[ExpirationDate] [datetime] NULL,
       
       --these fields will allow us to map the device to an acumatica entity
       [AcumaticaEntityType][nvarchar](30) NULL,
       [RefNoteID] [uniqueidentifier] NULL,
       
       --the devices location.
       [Latitude] DECIMAL(11,8) null,
       [Longitude] DECIMAL(11,8) null,
       --this will be a value indications a code of where the 
       --     device is. as a device traverses from room to room
       --     regarding presence detection this will be a string
       --     with the rooms name.
       [Zone][nvarchar](60) null,
       -- closest Business address. if we can determine the 
       --     the device is close enough to a business address in the system
       --     we can assume the device is at that address, and this "may"
       --     help us automatically determine the business entity the 
       --     the device is at. 
       [AddressID][int] null, --todo: confirm this is indeed an int type
       --we can have this property set an automatic purge and cleanup of 
       --data history no longer needed. Some devices will have a large data stream
       --and we should have a mechanism that self cleans. Default it to an off value of 0, -1 or any negative or null
       --timespan can be hours, or mins, or whatever consistent known time span.
       [AutoPurgeDataTimeSpan] INT NULL,
       [PayLineCtr] INT null,
       [LocLineCtr] INT, 
       
       [NoteID] [uniqueidentifier] NOT NULL,
       [CreatedByID] [uniqueidentifier] NOT NULL,
       [CreatedByScreenID] [char](8) NOT NULL,
       [CreatedDateTime] [datetime] NOT NULL,
       [LastModifiedByID] [uniqueidentifier] NOT NULL,
       [LastModifiedByScreenID] [char](8) NOT NULL,
       [LastModifiedDateTime] [datetime] NOT NULL,
       [tstamp] [timestamp] NOT NULL,
CONSTRAINT [IoTDevice_PK] PRIMARY KEY CLUSTERED 
(
       [CompanyID] ASC,
       [DeviceID] ASC
)WITH (
PAD_INDEX = OFF, 
STATISTICS_NORECOMPUTE = OFF, 
IGNORE_DUP_KEY = OFF, 
ALLOW_ROW_LOCKS = ON, 
ALLOW_PAGE_LOCKS = ON, 
OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[IoTDevice] ADD  DEFAULT ((0)) FOR [CompanyID]
GO



