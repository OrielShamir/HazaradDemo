using System;

namespace DatwiseSafetyDemo.Models
{
    /// <summary>
    /// Optional extension: store attachments on hazards (DB table: dbo.HazardAttachments).
    /// </summary>
    public sealed class HazardAttachment
    {
        public int AttachmentId { get; set; }
        public int HazardId { get; set; }

        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }

        public int UploadedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}