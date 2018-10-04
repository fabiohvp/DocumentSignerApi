using DocumentSignerApi.Exceptions;
using DocumentSignerApi.Helpers;
using DocumentSignerApi.Models;
using DocumentSignerApi.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace DocumentSignerApi.Managers
{
    public abstract class PdfSignerManager : IPdfSignerManager
    {
        private static int? _CacheTimeoutInSeconds = null;

        public static int CacheTimeoutInSeconds
        {
            get
            {
                if (!_CacheTimeoutInSeconds.HasValue)
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains("AssinaturaCacheKeyTimeout"))
                    {
                        _CacheTimeoutInSeconds = int.Parse(ConfigurationManager.AppSettings["AssinaturaCacheKeyTimeout"]);
                    }
                }

                return _CacheTimeoutInSeconds ?? 120;
            }
        }

        public ObjectCache Cache { get; private set; }
        public IPdfSignerService PdfSignerService { get; private set; }

        public PdfSignerManager(ObjectCache cache)
            : this(cache, new PdfSignerService())
        { }

        public PdfSignerManager(ObjectCache cache, IPdfSignerService pdfSignerService)
        {
            Cache = cache;
            PdfSignerService = pdfSignerService;
        }

        public virtual IEnumerable<ISigningResult> GetDocumentAuthenticatedAttributes(
            string fileId,
            string userId,
            string userCertificateBase64,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
       )
        {
            var async = false;

            return GetDocumentsAuthenticatedAttributesInternal
            (
                async,
                new string[] { fileId },
                userId,
                userCertificateBase64,
                location,
                reason,
                hashAlgorithm,
                signatureAlgorithm,
                parameters,
                timeoutInSeconds
            );
        }

        public virtual IEnumerable<ISigningResult> GetDocumentsAuthenticatedAttributes(
            string[] filesIds,
            string userId,
            string userCertificateBase64,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
        )
        {
            var async = false;

            return GetDocumentsAuthenticatedAttributesInternal
            (
                async,
                filesIds,
                userId,
                userCertificateBase64,
                location,
                reason,
                hashAlgorithm,
                signatureAlgorithm,
                parameters,
                timeoutInSeconds
            );
        }

        public virtual IEnumerable<ISigningResult> GetDocumentsAuthenticatedAttributesAsync(
            string[] filesIds,
            string userId,
            string userCertificateBase64,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
        )
        {
            var async = true;

            return GetDocumentsAuthenticatedAttributesInternal
            (
                async,
                filesIds,
                userId,
                userCertificateBase64,
                location,
                reason,
                hashAlgorithm,
                signatureAlgorithm,
                parameters,
                timeoutInSeconds
            );
        }

        public virtual ISigningResult SignDocument(ISigningRequest signRequest, bool removeCache)
        {
            var async = false;

            return SignDocumentsInternal(async, new ISigningRequest[] { signRequest }, removeCache)
                .FirstOrDefault();
        }

        public virtual IEnumerable<ISigningResult> SignDocuments(ISigningRequest[] signRequests, bool removeCaches)
        {
            var async = false;

            return SignDocumentsInternal(async, signRequests, removeCaches);
        }

        public virtual IEnumerable<ISigningResult> SignDocumentsAsync(ISigningRequest[] signRequests, bool removeCaches)
        {
            var async = true;

            return SignDocumentsInternal(async, signRequests, removeCaches);
        }

        public virtual CacheItemPolicy GetCacheItemPolicy(int timeoutInSeconds)
        {
            return new CacheItemPolicy
            {
                AbsoluteExpiration = GetSessionTimeout(timeoutInSeconds),
                RemovedCallback = new CacheEntryRemovedCallback(OnRemoveCache)
            };
        }

        public abstract string CreateTemporaryFilePath(string fileId);

        public abstract void CopyFileToTemporaryFilePath(ISigningDocument document);

        public abstract IEnumerable<TDocumentProjection> GetDocuments<TDocumentProjection>
        (
            string[] filesIds,
            string userId,
            IDictionary<string, object> parameters = default(IDictionary<string, object>)
        )
        where TDocumentProjection : ISigningDocument;

        public virtual void OnCreateCache(IPdfSigningSession pdfPostSigningSession, ISigningDocument document, CacheItemPolicy cacheItemPolicy, out bool removerCache)
        {
            removerCache = true;

            if (!Cache.Add(pdfPostSigningSession.FileId, pdfPostSigningSession, cacheItemPolicy))
            {
                throw new FileBeingSignedException();
            }
        }

        public virtual void OnDocumentError(ISigningDocument document, IDictionary<string, object> parameters, Exception exception)
        {
        }

        public virtual void OnDocumentSigned(IPdfSigningSession sessionData)
        {
        }

        public virtual void OnDocumentsSigned(IDictionary<IPdfSigningSession, ISigningResult> sessionDataAndResults, bool removeCaches)
        {
            if (removeCaches)
            {
                foreach (var sessionDataAndResult in sessionDataAndResults)
                {
                    Cache
                        .Remove(sessionDataAndResult.Key.FileId);
                }
            }
        }

        public virtual void OnRemoveCache(CacheEntryRemovedArguments arguments)
        {
            var sessionData = arguments.CacheItem.Value as IDisposable;

            if (sessionData != null)
            {
                sessionData
                    .Dispose();
            }
        }

        public virtual IEnumerable<ISigningResult> OnValidateUserCertificate(string[] filesIds, X509Certificate2 userCertificate)
        {
            var results = new List<ISigningResult>();

            try
            {
                PdfSignerService
                    .TokenService
                    .ValidateCertificate(userCertificate, DateTime.Now);
            }
            catch (AggregateException ex)
            {
                var messages = ex.InnerExceptions.Select(o => o.Message);

                foreach (var fileId in filesIds)
                {
                    results.Add(new SigningResult
                    {
                        FileId = fileId,
                        Exception = new Exception(string.Join(Environment.NewLine, messages))
                    });
                }
            }

            return results;
        }

        public virtual void UpdateDocument(IPdfSigningSession pdfPostSigningSession, ISigningDocument document)
        { }

        public static DateTimeOffset GetSessionTimeout(int timeoutInSeconds)
        {
            return DateTimeOffset.Now.AddSeconds(timeoutInSeconds);
        }

        protected virtual ISigningResult GetDocumentAuthenticatedAttributesInternal
        (
            ISigningDocument document,
            string ownerIdentification,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            X509Certificate2 userCertificate,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
        )
        {
            var removeCache = false;
            var result = default(ISigningResult);

            try
            {
                var pdfSigningSession = new PdfSigningSession
                {
                    FileId = document.FileId,
                    UserId = document.UserId,
                    FilePath = document.FilePath,
                    HashAlgorithm = hashAlgorithm,
                    OwnerIdentification = ownerIdentification,
                    SignatureAlgorithm = signatureAlgorithm,
                    SignatureVisible = true,
                    UserCertificate = userCertificate,
                    Location = location,
                    Reason = reason,
                    Parameters = parameters
                };

                if (timeoutInSeconds == default(int))
                {
                    timeoutInSeconds = CacheTimeoutInSeconds;
                }

                var cachePolicy = GetCacheItemPolicy(timeoutInSeconds);
                OnCreateCache(pdfSigningSession, pdfSigningSession, cachePolicy, out removeCache);

                //necessário criar o documento somente depois que o cache foi criado 
                //para evitar exceção por utilização simultânea do documento temporário
                CopyFileToTemporaryFilePath(pdfSigningSession);
                UpdateDocument(pdfSigningSession, document);

                pdfSigningSession.AuthenticatedAttributes = PdfSignerService
                    .CreateAuthenticatedAttribute(pdfSigningSession);

                result = new SigningResult
                {
                    FileId = document.FileId,
                    AuthenticatedAttributeBase64 = pdfSigningSession.AuthenticatedAttributes.ValueBase64,
                    Exception = default(Exception)
                };
            }
            catch (Exception ex)
            {
                result = new SigningResult
                {
                    FileId = document.FileId,
                    AuthenticatedAttributeBase64 = default(string),
                    Exception = ex
                };

                if (removeCache)
                {
                    Cache
                        .Remove(document.FileId);
                }

                OnDocumentError(document, parameters, ex);
            }

            return result;
        }

        protected virtual IEnumerable<ISigningResult> GetDocumentsAuthenticatedAttributesInternal(
            bool async,
            string[] filesIds,
            string userId,
            string userCertificateBase64,
            string location,
            string reason,
            string hashAlgorithm,
            string signatureAlgorithm,
            IDictionary<string, object> parameters = default(IDictionary<string, object>),
            int timeoutInSeconds = default(int)
        )
        {
            if (parameters == default(IDictionary<string, object>))
            {
                parameters = new Dictionary<string, object> { };
            }

            var results = new List<ISigningResult>();
            var userCertificate = userCertificateBase64
                .ToX509Certificate2();
            var ownerIdentification = OidHelper
                .OwnerIdentificationWorkflow
                .Execute(userCertificate);

            results
                .AddRange(OnValidateUserCertificate(filesIds, userCertificate));

            if (!results.Any()) //não houve erros de certificado
            {
                var documents = GetDocuments<ISigningDocument>(filesIds, userId, parameters);

                var unauthorizedDocuments = filesIds
                    .Where(o => !documents.Any(p => p.FileId == o));

                foreach (var fileId in unauthorizedDocuments)
                {
                    var result = new SigningResult
                    {
                        FileId = fileId,
                        Exception = new UnauthorizedAccessException("You do not have permission to access this file")
                    };

                    var document = new SigningDocument
                    {
                        FileId = result.FileId,
                        Parameters = parameters,
                        UserId = userId
                    };

                    results
                        .Add(result);
                    OnDocumentError(document, parameters, result.Exception);
                }

                if (async)
                {
                    var list = new ConcurrentBag<ISigningResult>();

                    Parallel
                        .ForEach(documents, document =>
                        {
                            var result = GetDocumentAuthenticatedAttributesInternal
                            (
                                document,
                                ownerIdentification,
                                location,
                                reason,
                                hashAlgorithm,
                                signatureAlgorithm,
                                userCertificate,
                                parameters,
                                timeoutInSeconds
                            );

                            list
                                .Add(result);
                        });

                    results
                        .AddRange(list);
                }
                else
                {
                    foreach (var document in documents)
                    {
                        var result = GetDocumentAuthenticatedAttributesInternal
                        (
                            document,
                            ownerIdentification,
                            location,
                            reason,
                            hashAlgorithm,
                            signatureAlgorithm,
                            userCertificate,
                            parameters,
                            timeoutInSeconds
                        );

                        results
                            .Add(result);
                    }
                }
            }

            return results;
        }

        protected virtual KeyValuePair<IPdfSigningSession, ISigningResult> SignDocumentInternal(ISigningRequest signRequest)
        {
            var result = new SigningResult();
            var sessionData = Cache.Get(signRequest.FileId) as IPdfSigningSession;

            if (sessionData == default(IPdfSigningSession))
            {
                result = new SigningResult
                {
                    FileId = signRequest.FileId,
                    Exception = new TimeoutException()
                };
            }
            else
            {
                try
                {
                    PdfSignerService
                        .SignData(sessionData, signRequest.SignedHashBase64);

                    result = new SigningResult
                    {
                        FileId = signRequest.FileId,
                        Exception = default(Exception)
                    };

                    OnDocumentSigned(sessionData);
                }
                catch (Exception ex)
                {
                    result = new SigningResult
                    {
                        FileId = signRequest.FileId,
                        Exception = ex
                    };

                    OnDocumentError(sessionData, signRequest.Parameters, ex);
                }
            }

            sessionData = Cache.Get(signRequest.FileId) as IPdfSigningSession;
            return new KeyValuePair<IPdfSigningSession, ISigningResult>(sessionData, result);
        }

        protected virtual IEnumerable<ISigningResult> SignDocumentsInternal(bool async, ISigningRequest[] signRequests, bool removeCaches)
        {
            var results = default(IDictionary<IPdfSigningSession, ISigningResult>);

            if (async)
            {
                var dic = new ConcurrentDictionary<IPdfSigningSession, ISigningResult>();

                Parallel
                    .ForEach(signRequests, signRequest =>
                    {
                        var item = SignDocumentInternal(signRequest);
                        dic.TryAdd(item.Key, item.Value);
                    });

                results = dic
                    .ToDictionary(o => o.Key, o => o.Value);
            }
            else
            {
                results = new Dictionary<IPdfSigningSession, ISigningResult>();

                foreach (var signRequest in signRequests)
                {
                    var result = SignDocumentInternal(signRequest);
                    results.Add(result);
                }
            }

            OnDocumentsSigned(results, removeCaches);

            return results
                .Select(o => o.Value);
        }
    }
}