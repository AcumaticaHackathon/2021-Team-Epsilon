using PX.SM;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.PJ.PhotoLogs.PJ.DAC;
using PX.Common;

namespace PX.Objects.PJ.PhotoLogs.PJ.Graphs
{
    //TODO: Should be removed after implementing AC-190658
    public class FixMobilePhotoExtension<T> : PXGraphExtension<T> where T : PXGraph
    {
        public virtual void _(Events.RowSelecting<Photo> args)
        {
            var photo = args.Row;
            if (photo != null && photo.FileId == null)
            {
                var fileSelect = new SelectFrom<UploadFile>
                    .InnerJoin<NoteDoc>.On<NoteDoc.fileID.IsEqual<UploadFile.fileID>>
                    .Where<NoteDoc.noteID.IsEqual<@P.AsGuid>>.View(Base);

                using (new PXFieldScope(fileSelect.View, typeof(UploadFile.fileID), typeof(UploadFile.createdByID), typeof(UploadFile.name)))
                {
                    UploadFile file = fileSelect.SelectSingle(photo.NoteID);
                    if (file != null)
                    {
                        photo.FileId = file.FileID;
                        photo.UploadedById = file.CreatedByID;
                        photo.Name = file.Name;
                        photo.ImageUrl = string.Concat(PXUrl.SiteUrlWithPath(), Descriptor.Constants.FileUrl, photo.FileId);
                    }
                }
            }
        }
    }

    public class FixMobilePhotoEntryExtension : FixMobilePhotoExtension<PhotoEntry> { }
    public class FixMobilePhotoLogMaintExtension : FixMobilePhotoExtension<PhotoLogMaint> { }
    public class FixMobilePhotoLogEntryExtension : FixMobilePhotoExtension<PhotoLogEntry> { }
}
