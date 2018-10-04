using DocumentSignerApi.Workflows;
using Org.BouncyCastle.Security;
using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Helpers
{
    /// <summary>
    /// good place to find Oids
    /// http://www.oid-info.com/
    /// brazilian Oids
    /// http://icp-brasil.validcertificadora.com.br/ac-validbrasil/pcA3-ac-validbrasil.pdf
    /// http://www.iti.gov.br/images/twiki/URL/pub/Certificacao/DocIcp/docs13082012/DOC-ICP-04.01_-_versao_2.3.pdf
    /// </summary>
    public abstract class OidHelper
    {
        internal static string Personal = "2.16.76.1.3.1";
        internal static string Company = "2.16.76.1.3.4";

        public static GetCertificatePropertyWorkflow<string> OwnerIdentificationWorkflow = new GetOwnerIdentificationWorkflow();

        private static string FormatOidValue(string oidValue)
        {
            if (oidValue != null)
            {
                var indexCloseBracket = oidValue.IndexOf(']') + 1;
                oidValue = oidValue.Substring(indexCloseBracket, oidValue.Length - indexCloseBracket - 1);

                if (oidValue.StartsWith("#"))
                {
                    oidValue = oidValue.Substring(1)
                        .FromHexadecimal();
                }
            }

            return oidValue;
        }

        public static string GetOidValue(Org.BouncyCastle.X509.X509Certificate certificate, string oidKey)
        {
            var oidValue = default(string);

            if (certificate.GetSubjectAlternativeNames() != null)
            {
                var alternativeNames = certificate
                    .GetSubjectAlternativeNames()
                    .Cast<ArrayList>()
                    .Where(o => o.Count == 2 && o[0].GetType() == typeof(int));

                var oidKeys = alternativeNames
                    .Where(o => o[1] != null)
                    .Select(o => o[1].ToString());

                var oidValues = oidKeys
                    .Where(o => o.Contains('['))
                    .Where(o => o.Substring(1, o.IndexOf(',') - 1) == oidKey);

                oidValue = oidValues
                    .FirstOrDefault();
            }

            return FormatOidValue(oidValue);
        }

        public static string GetOidValue(X509Certificate2 certificate, string oidKey)
        {
            var bcCertificate = DotNetUtilities.FromX509Certificate(certificate);
            return GetOidValue(bcCertificate, oidKey);
        }

        public static string GetSubjectByOid(X509Certificate2 certificate, string oidKey)
        {
            return GetSubjectByOid(DotNetUtilities.FromX509Certificate(certificate), oidKey);
        }

        public static string GetSubjectByOid(Org.BouncyCastle.X509.X509Certificate certificate, string oidKey)
        {
            var values = certificate.SubjectDN.GetValues(new Org.BouncyCastle.Asn1.DerObjectIdentifier(oidKey));

            if (values.Count == 0)
            {
                return null;
            }

            return values[0].ToString();
        }
    }
}
