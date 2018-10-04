using DocumentSignerApi.Models;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Services
{
    public class PdfSignerService : IPdfSignerService
    {
        public const string ErroModuloAssinatura = "Erro módulo de assinatura";
        public const int SIGNATURE_ESTIMATED_SIZE = 8192;
        public ITokenService TokenService { get; protected set; }

        public PdfSignerService()
            : this(new TokenService())
        { }

        public PdfSignerService(ITokenService tokenService)
        {
            TokenService = tokenService;
        }

        public virtual IAuthenticatedAttributes CreateAuthenticatedAttribute(IPdfPreSigningSession pdfPreSigningSession)
        {
            var pdfContentBytes = File.ReadAllBytes(pdfPreSigningSession.FilePath);

            using (var PdfReader = new PdfReader(pdfContentBytes))
            {
                using (var pdfStamper = PdfStamper.CreateSignature(PdfReader, null, '\0', pdfPreSigningSession.FilePath, true))
                {
                    var authenticatedAttributes = new AuthenticatedAttributes();
                    var publicKeyManager = GetPublicKeyManager(pdfPreSigningSession);

                    authenticatedAttributes.PdfSignatureAppearance = pdfStamper.SignatureAppearance;

                    UpdatePdfSignatureAppearance(pdfPreSigningSession, PdfReader, authenticatedAttributes.PdfSignatureAppearance);

                    authenticatedAttributes
                        .PdfSignatureAppearance
                        .PreClose(new Dictionary<PdfName, int> { { PdfName.CONTENTS, SIGNATURE_ESTIMATED_SIZE * 2 + 2 } });

                    var pdfSignatureAppearanceData = authenticatedAttributes
                        .PdfSignatureAppearance
                        .GetRangeStream();

                    authenticatedAttributes.DigestedData = DigestAlgorithms
                        .Digest
                        (
                            pdfSignatureAppearanceData,
                            pdfPreSigningSession.HashAlgorithm
                        );

                    authenticatedAttributes.Value = publicKeyManager
                        .getAuthenticatedAttributeBytes
                        (
                            authenticatedAttributes.DigestedData,
                            null,
                            null,
                            CryptoStandard.CMS
                        );

                    return authenticatedAttributes;
                }
            }
        }

        public virtual void SignData(IPdfSigningSession pdfPostSigningSession, string signedHashBase64)
        {
            try
            {
                var signedPdfData = Convert.FromBase64String(signedHashBase64);
                var authenticatedAttribute = Convert.FromBase64String(pdfPostSigningSession.AuthenticatedAttributes.ValueBase64);
                TokenService.ValidateSignature(pdfPostSigningSession.UserCertificate, pdfPostSigningSession.HashAlgorithm, authenticatedAttribute, signedPdfData);

                var publicKeyManager = GetPublicKeyManager(pdfPostSigningSession);
                publicKeyManager.SetExternalDigest(signedPdfData, null, pdfPostSigningSession.SignatureAlgorithm);

                //var tsaServer = new TSAClientBouncyCastle("https://freetsa.org/tsr");

                var encodedSignature = publicKeyManager
                    .GetEncodedPKCS7
                    (
                        pdfPostSigningSession.AuthenticatedAttributes.DigestedData,
                        null,
                        null,
                        null,
                        CryptoStandard.CMS
                    );

                UpdatePdfDictionaryContents(pdfPostSigningSession.AuthenticatedAttributes.PdfSignatureAppearance, encodedSignature);
            }
            catch (Exception ex)
            {
                UpdatePdfDictionaryContents(pdfPostSigningSession.AuthenticatedAttributes.PdfSignatureAppearance, new byte[] { });
                throw ex;
            }
        }

        public virtual void UpdatePdfSignatureAppearance(IPdfPreSigningSession pdfPreSigningSession, PdfReader pdfReader, PdfSignatureAppearance pdfSignatureAppearance)
        {
            pdfSignatureAppearance.Location = pdfPreSigningSession.Location;
            pdfSignatureAppearance.Reason = pdfPreSigningSession.Reason;

            pdfSignatureAppearance.CryptoDictionary = new PdfSignature(PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED)
            {
                Reason = pdfSignatureAppearance.Reason,
                Location = pdfSignatureAppearance.Location,
                Date = new PdfDate(pdfSignatureAppearance.SignDate)
            };
        }

        private PdfPKCS7 GetPublicKeyManager(IPdfPreSigningSession sessionData)
        {
            var chain = new X509Chain();
            var certificates = new List<Org.BouncyCastle.X509.X509Certificate>();

            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
            chain.Build(sessionData.UserCertificate);

            foreach (var chainElement in chain.ChainElements)
            {
                certificates.Add(DotNetUtilities.FromX509Certificate(chainElement.Certificate));
            }

            return new PdfPKCS7(null, certificates, sessionData.HashAlgorithm, false);
        }

        private void UpdatePdfDictionaryContents(PdfSignatureAppearance pdfSignatureAppearance, byte[] encodedSignature)
        {
            var pdfDictionary = new PdfDictionary();
            var paddedSignature = new byte[SIGNATURE_ESTIMATED_SIZE];

            Array.Copy(encodedSignature, 0, paddedSignature, 0, encodedSignature.Length);

            pdfDictionary.Put(PdfName.CONTENTS, new PdfString(paddedSignature).SetHexWriting(true));

            pdfSignatureAppearance.Close(pdfDictionary);
        }
    }
}
