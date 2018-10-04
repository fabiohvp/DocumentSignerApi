using System;

namespace DocumentSignerApi.Models
{
    public class SigningResult : ISigningResult
    {
        public virtual string AuthenticatedAttributeBase64 { get; set; }
        public virtual Exception Exception { get; set; }
        public virtual string FileId { get; set; }
    }
}
