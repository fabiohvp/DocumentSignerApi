using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Models
{
    public interface ITokenService
    {
        bool DetectOnlyValidCertificates { get; set; }

        X509Certificate2 GetCertificate(string thumbprint);
        string GetPublicKeyXML(X509Certificate2 certificate);
        ICollection<object> ReadCertificates(object findValue);
        byte[] SignHash(string thumbprint, string hashAlgorithm, byte[] originalHash);
        void ValidateSignature(X509Certificate2 certificate, string hashAlgorithm, byte[] originalHash, byte[] signedHash);
        void ValidateCertificate(X509Certificate2 certificate, DateTime signatureDate);
    }
}