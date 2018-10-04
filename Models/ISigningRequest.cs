using System.Collections.Generic;

namespace DocumentSignerApi.Models
{
    public interface ISigningRequest
    {
        string FileId { get; set; }
        string SignedHashBase64 { get; set; }
        IDictionary<string, object> Parameters { get; set; }
    }
}
