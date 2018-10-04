using System;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Models
{
    public interface IPdfPreSigningSession : ISigningDocument, IDisposable
    {
        string HashAlgorithm { get; set; }
        string Location { get; set; }
        string OwnerIdentification { get; set; }
        string Reason { get; set; }
        string SignatureAlgorithm { get; set; }
        bool SignatureVisible { get; set; }
        X509Certificate2 UserCertificate { get; set; }
    }
}
