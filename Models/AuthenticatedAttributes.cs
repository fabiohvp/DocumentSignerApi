using iTextSharp.text.pdf;
using Newtonsoft.Json;
using System;

namespace DocumentSignerApi.Models
{
    public class AuthenticatedAttributes : IAuthenticatedAttributes
    {
        [JsonIgnore]
        public virtual byte[] DigestedData { get; set; }

        [JsonIgnore]
        public virtual PdfSignatureAppearance PdfSignatureAppearance { get; set; }

        [JsonIgnore]
        public virtual byte[] Value { get; set; }

        public virtual string ValueBase64
        {
            get
            {
                return Convert.ToBase64String(Value);
            }
        }
    }
}
