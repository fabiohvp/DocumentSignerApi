using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Models
{
    public class PdfSigningSession : IPdfSigningSession
    {
        public virtual IAuthenticatedAttributes AuthenticatedAttributes { get; set; }
        public virtual string FileId { get; set; }
        public virtual string UserId { get; set; }
        public virtual string FilePath { get; set; }
        public virtual string HashAlgorithm { get; set; }
        public virtual string Location { get; set; }
        public virtual string OwnerIdentification { get; set; }
        public virtual string Reason { get; set; }
        public virtual string SignatureAlgorithm { get; set; }
        public virtual bool SignatureVisible { get; set; }
        public virtual X509Certificate2 UserCertificate { get; set; }
        public virtual IDictionary<string, object> Parameters { get; set; }

        public PdfSigningSession()
        {
            Parameters = new Dictionary<string, object>();
        }

        public virtual void Dispose()
        {
            UserCertificate = null;
            Location = null;
            Reason = null;
            Parameters = null;

            try
            {
                AuthenticatedAttributes
                    .PdfSignatureAppearance
                    .Stamper
                    .Close();

                AuthenticatedAttributes
                    .PdfSignatureAppearance
                    .Stamper
                    .Dispose();
            }
            finally
            {
                AuthenticatedAttributes = null;
            }
        }
    }
}
