using System.Collections.Generic;

namespace DocumentSignerApi.Models
{
    public class SigningDocument : ISigningDocument
    {
        public string FileId { get; set; }
        public string FilePath { get; set; }
        public IDictionary<string, object> Parameters { get; set; }
        public string UserId { get; set; }

        public SigningDocument()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
}
