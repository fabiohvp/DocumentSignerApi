using System;

namespace DocumentSignerApi.Models
{
    public class CertificateDetail
    {
        public virtual string Name { get; set; }
        public virtual DateTime ExpireDate { get; set; }
        public virtual string Thumbprint { get; set; }
        public virtual string UserCertificateBase64 { get; set; }
        public virtual string HashAlgorithm { get; set; }
        public virtual string SignatureAlgorithm { get; set; }
        public virtual string OwnerName { get; set; }
        public virtual string OwnerIdentification { get; set; }
    }
}
