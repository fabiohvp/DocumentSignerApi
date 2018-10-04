using System;

namespace DocumentSignerApi.Models
{
    public interface ISigningResult
    {
        string AuthenticatedAttributeBase64 { get; set; }
        Exception Exception { get; set; }
        string FileId { get; set; }
    }
}
