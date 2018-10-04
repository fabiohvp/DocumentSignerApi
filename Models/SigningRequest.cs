using System.Collections.Generic;

namespace DocumentSignerApi.Models
{
    public class SigningRequest : ISigningRequest
    {
        public virtual string FileId { get; set; }

        public IDictionary<string, object> Parameters { get; set; }

        public virtual string SignedHashBase64 { get; set; }

        public SigningRequest()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
}
