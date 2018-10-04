using DocumentSignerApi.Models;
using DocumentSignerApi.Services;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Managers
{
    public interface IPdfSignerManager
    {
        ObjectCache Cache { get; }
        IPdfSignerService PdfSignerService { get; }

        IEnumerable<TDocumentProjection> GetDocuments<TDocumentProjection>(
            string[] filesIds,
            string userId,
            IDictionary<string, object> parameters = default(IDictionary<string, object>)
        ) where TDocumentProjection : ISigningDocument;

        IEnumerable<ISigningResult> GetDocumentAuthenticatedAttributes(
            string fileId,
            string userId,
            string userCertificateBase64,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
        );

        IEnumerable<ISigningResult> GetDocumentsAuthenticatedAttributes(
            string[] filesIds,
            string userId,
            string userCertificateBase64,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
        );

        IEnumerable<ISigningResult> GetDocumentsAuthenticatedAttributesAsync(
            string[] filesIds,
            string userId,
            string userCertificateBase64,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
        );

        CacheItemPolicy GetCacheItemPolicy(int timeoutInSeconds);
        void OnCreateCache(IPdfSigningSession pdfPostSigningSession, ISigningDocument document, CacheItemPolicy cacheItemPolicy, out bool removeCache);
        void OnDocumentError(ISigningDocument document, IDictionary<string, object> parameters, Exception exception);
        void OnDocumentSigned(IPdfSigningSession sessionData);
        void OnDocumentsSigned(IDictionary<IPdfSigningSession, ISigningResult> sessionDataAndResults, bool removeCaches);
        void OnRemoveCache(CacheEntryRemovedArguments arguments);
        IEnumerable<ISigningResult> OnValidateUserCertificate(string[] filesIds, X509Certificate2 userCertificate);
        ISigningResult SignDocument(ISigningRequest signRequest, bool removeCache);
        IEnumerable<ISigningResult> SignDocuments(ISigningRequest[] signRequests, bool removeCaches);
        IEnumerable<ISigningResult> SignDocumentsAsync(ISigningRequest[] signRequests, bool removeCaches);
        string CreateTemporaryFilePath(string fileId);
        void CopyFileToTemporaryFilePath(ISigningDocument document);
        void UpdateDocument(IPdfSigningSession pdfPostSigningSession, ISigningDocument document);
    }
}