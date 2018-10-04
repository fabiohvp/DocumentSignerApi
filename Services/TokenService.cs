using DocumentSignerApi.Exceptions;
using DocumentSignerApi.Helpers;
using DocumentSignerApi.Models;
using DocumentSignerApi.Rules;
using DocumentSignerApi.Workflows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Services
{
    public class TokenService : ITokenService
    {
        private readonly string StoreName;
        public bool DetectOnlyValidCertificates { get; set; }

        public TokenService(string storeName = "My")
        {
            DetectOnlyValidCertificates = false;
            StoreName = storeName;
        }

        public virtual X509Certificate2 GetCertificate(string thumbprint)
        {
            var storeCertificates = default(X509Certificate2Collection);
            var store = new X509Store(StoreName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            storeCertificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, DetectOnlyValidCertificates);
            store.Close();

            if (storeCertificates.Count == 0)
            {
                throw new CertificateNotFoundException();
            }

            return storeCertificates[0];
        }

        public virtual string GetPublicKeyXML(X509Certificate2 certificate)
        {
            return certificate.PublicKey.Key.ToXmlString(false);
        }

        public static string GetFriendlyName(X509Certificate2 certificate)
        {
            var friendlyName = certificate.FriendlyName;

            if (string.IsNullOrEmpty(friendlyName))
            {
                friendlyName = certificate.SubjectName.Name;

                if (friendlyName.Contains(","))
                {
                    friendlyName = friendlyName
                        .Split(new char[] { ',' })
                        .FirstOrDefault();
                }
            }

            return friendlyName;
        }

        public virtual ICollection<object> ReadCertificates(object findValue)
        {
            var store = new X509Store(StoreName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var storeCertificates = store.Certificates;

            if (findValue != null)
            {
                storeCertificates = storeCertificates.Find(X509FindType.FindByIssuerName, findValue, DetectOnlyValidCertificates);
            }

            store.Close();

            var today = DateTime.Now;
            var certificates = new List<CertificateDetail>();

            foreach (var certificate in storeCertificates)
            {
#if (!DEBUG)
                if (today < certificate.NotBefore || today > certificate.NotAfter)
                {
                    continue;
                }
#endif

                if (certificate.HasPrivateKey)
                {
                    //O try/catch abaixo é para evitar exibir os certificados que ficam armazenados em cache no Windows
                    try
                    {
                        if (certificate.PrivateKey == null)
                        {
                            continue;
                        }
                    }
                    catch (CryptographicException)
                    {
                        continue;
                    }

                    var friendlyName = GetFriendlyName(certificate);
                    var duplicatedCertificate = certificates
                        .FirstOrDefault(o =>
                            o.Name == friendlyName
                            && o.ExpireDate <= certificate.NotAfter
                        );

                    if (duplicatedCertificate != null)
                    {
                        certificates.Remove(duplicatedCertificate);
                    }

                    var certificateDetails = GetCertificate(certificate.Thumbprint);
                    var hashAlgorithm = Security.HashAlgorithms.SHA1;
                    var signatureAlgorithm = Security.CryptographicAlgorithms.RSA; //por enquanto está fixo

                    using (var rsaCryptoServiceProvider = certificate.PrivateKey as RSACryptoServiceProvider)
                    {
                        if (rsaCryptoServiceProvider.CspKeyContainerInfo.HardwareDevice)
                        {
                            hashAlgorithm = Security.HashAlgorithms.SHA256;
                        }
                    }

                    certificates.Add(new CertificateDetail
                    {
                        ExpireDate = certificate.NotAfter,
                        HashAlgorithm = hashAlgorithm,
                        Name = friendlyName,
                        SignatureAlgorithm = signatureAlgorithm,
                        Thumbprint = certificate.Thumbprint,
                        UserCertificateBase64 = Convert.ToBase64String(certificateDetails.RawData),
                        OwnerName = new GetOwnerNameWorkflow().Execute(certificate),
                        OwnerIdentification = OidHelper.OwnerIdentificationWorkflow.Execute(certificate)
                    });
                }
            }

            return certificates
                .ToArray();
        }

        public virtual byte[] SignHash(string thumbprint, string hashAlgorithm, byte[] originalHash)
        {
            var certificate = GetCertificate(thumbprint);

            using (var rsaCryptoServiceProvider = certificate.PrivateKey as RSACryptoServiceProvider)
            {
                var signature = rsaCryptoServiceProvider.SignData(originalHash, hashAlgorithm);
                ValidateSignature(certificate, hashAlgorithm, originalHash, signature);
                return signature;
            }
        }

        public virtual void ValidateSignature(X509Certificate2 certificate, string hashAlgorithm, byte[] originalHash, byte[] signedHash)
        {
            var publicKeyXML = GetPublicKeyXML(certificate);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKeyXML);

                if (!rsa.VerifyData(originalHash, hashAlgorithm, signedHash))
                {
                    throw new InvalidDataException("Dados incorretos.");
                }
            }
        }

        public virtual void ValidateCertificate(X509Certificate2 certificate, DateTime signatureDate)
        {
            var certificateValidRule = new IsCertificateValidRule(signatureDate);

            if (!certificateValidRule.IsSatisfied(certificate))
            {
                var errors = new List<Exception>();

                foreach (var message in certificateValidRule.Messages)
                {
                    errors.Add(new InvalidDataException(message));
                }

                if (errors.Count > 0)
                {
#if (DEBUG)
                    Debug.WriteLine(string.Join(Environment.NewLine, errors));
#else
                    throw new AggregateException(errors);
#endif
                }
            }
        }
    }
}
