//using Enflow.Base;
//using iTextSharp.text.pdf;
//using iTextSharp.text.pdf.security;
//using Org.BouncyCastle.Security;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Cryptography.X509Certificates;

//namespace DocumentSignerApi.Rules
//{
//    public class IsCertificateLtvValidRule : StateRule<X509Certificate2>
//    {
//        public byte[] FileContents;
//        public IEnumerable<string> Messages;

//        public IsCertificateLtvValidRule(byte[] fileContents)
//        {
//            Description = "Documento possui assinatura habilitada para LTV";
//            FileContents = fileContents;
//            Messages = new List<string>();
//        }

//        public override System.Linq.Expressions.Expression<Func<X509Certificate2, bool>> Predicate
//        {
//            get
//            {
//                return certificate => VerifyCertificate(certificate);
//            }
//        }

//        private bool VerifyCertificate(X509Certificate2 certificate)
//        {
//            using (var pdfReader = new PdfReader(FileContents))
//            {
//                var ltvResult = new List<VerificationOK>();

//                new LtvVerifier(pdfReader)
//                {
//                    OnlineCheckingAllowed = true,
//                    CertificateOption = LtvVerification.CertificateOption.WHOLE_CHAIN,
//                    Certificates = GetChain(certificate).ToList(),
//                    VerifyRootCertificate = true
//                }
//                .Verify(ltvResult);

//                Messages = ltvResult.Select(o => o.ToString());

//                return ltvResult.Any();
//            }
//        }

//        private static Org.BouncyCastle.X509.X509Certificate[] GetChain(X509Certificate2 myCert)
//        {
//            var x509Chain = new X509Chain();
//            x509Chain.Build(myCert);

//            var chain = new List<Org.BouncyCastle.X509.X509Certificate>();

//            foreach (var cert in x509Chain.ChainElements)
//            {
//                chain.Add(DotNetUtilities.FromX509Certificate(cert.Certificate));
//            }

//            return chain.ToArray();
//        }
//    }
//}
