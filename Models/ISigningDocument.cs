using System.Collections.Generic;

namespace DocumentSignerApi.Models
{
    public interface ISigningDocument
    {
        string FileId { get; set; }
        string UserId { get; set; }
        string FilePath { get; set; }
        IDictionary<string, object> Parameters { get; set; }
    }
}
