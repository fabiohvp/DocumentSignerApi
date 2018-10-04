using DocumentSignerApi.Helpers;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Security;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Models
{
    public class Signature
    {
        public readonly string SignatureFieldName;
        public readonly FileFormat FileFormat;
        public readonly X509Certificate2 Certificate;
        public readonly Org.BouncyCastle.X509.X509Certificate CertificateBouncyCastle;
        public X509Chain Chain;

        public DateTime SignDate { get; private set; }
        public string Identification { get { return OidHelper.OwnerIdentificationWorkflow.Execute(Certificate); } }
        public string Name
        {
            get
            {
                var name = OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.Name.Id) ?? CommonName;

                if (name == null)
                {
                    return Identification;
                }

                return name.Split(new char[] { ':' })
                    .FirstOrDefault();
            }
        }
        public string Email { get { return OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.EmailAddress.Id); } }
        public string Country { get { return OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.C.Id); } }
        public string State { get { return OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.ST.Id); } }
        public string Locality { get { return OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.L.Id); } }
        public string Organization { get { return OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.O.Id); } }
        public string OrganizationUnit { get { return OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.OU.Id); } }
        public string CommonName { get { return OidHelper.GetSubjectByOid(CertificateBouncyCastle, X509Name.CN.Id); } }

        public Signature(string signatureFieldName, FileFormat fileFormat, X509Certificate2 certificate, DateTime signDate)
            : this(signatureFieldName, fileFormat, DotNetUtilities.FromX509Certificate(certificate), signDate)
        {
            Certificate = certificate;
        }

        public Signature(string signatureFieldName, FileFormat fileFormat, Org.BouncyCastle.X509.X509Certificate certificateBC, DateTime signDate)
        {
            SignatureFieldName = signatureFieldName;
            FileFormat = fileFormat;
            Certificate = new X509Certificate2(certificateBC.GetEncoded());
            CertificateBouncyCastle = certificateBC;
            SignDate = signDate;

            Chain = new X509Chain();
            Chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 30);
            Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;
            Chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            Chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        }

        /// <summary>
        /// After verification you can access Chain.ChainStatus for more details
        /// </summary>
        /// <returns>If certificate is valid</returns>
        public bool Validate()
        {
            Chain.Build(Certificate);
            return Certificate.Verify();
        }
    }
}
