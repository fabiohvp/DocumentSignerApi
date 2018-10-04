using Enflow.Base;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DocumentSignerApi.Rules
{
    public class IsCertificateValidRule : StateRule<X509Certificate2>
    {
        private static IEnumerable<string> TrustedRootsThumbprints;

        private List<string> _Messages;
        private bool MessagesAdded;

        private X509Chain Chain;
        private DateTime SignatureDate;

        public List<string> Messages
        {
            get
            {
                if (!MessagesAdded)
                {
                    var ignoreCRLOffline = Chain
                        .ChainStatus
                        .Any(o => o.Status == X509ChainStatusFlags.OfflineRevocation);

                    foreach (var certificateStatus in Chain.ChainStatus)
                    {
                        switch (certificateStatus.Status)
                        {
                            //case X509ChainStatusFlags.NotTimeValid:
                            //    verificação customizada após o foreach
                            //    break;
                            case X509ChainStatusFlags.NotTimeNested:
                                _Messages.Add("Período de validade não aninhado.");
                                break;
                            case X509ChainStatusFlags.Revoked:
                                _Messages.Add("Certificado revogado.");
                                break;
                            case X509ChainStatusFlags.NotSignatureValid:
                                _Messages.Add("Lista de certificados confiáveis (CTL) contém uma assinatura inválida.");
                                break;
                            case X509ChainStatusFlags.NotValidForUsage:
                                _Messages.Add("Lista de certificados confiáveis (CTL) não é válida para uso.");
                                break;
                            case X509ChainStatusFlags.UntrustedRoot:
                                _Messages.Add("Certificado raiz não confiável.");
                                break;
                            case X509ChainStatusFlags.RevocationStatusUnknown:
                                if (!ignoreCRLOffline)
                                {
                                    _Messages.Add("Não foi possível verificar se o certificado está revogado.");
                                }
                                break;
                            case X509ChainStatusFlags.Cyclic:
                                _Messages.Add("Cadeia do certificado não pode ser construída.");
                                break;
                            case X509ChainStatusFlags.InvalidExtension:
                                _Messages.Add("Extensão inválida.");
                                break;
                            case X509ChainStatusFlags.InvalidPolicyConstraints:
                                _Messages.Add("Restrições de políticas inválidas.");
                                break;
                            case X509ChainStatusFlags.InvalidBasicConstraints:
                                _Messages.Add("Restrições básicas inválidas.");
                                break;
                            case X509ChainStatusFlags.InvalidNameConstraints:
                                _Messages.Add("Restrições de nome inválidas.");
                                break;
                            case X509ChainStatusFlags.HasNotSupportedNameConstraint:
                                _Messages.Add("Restrição de nome não suportada.");
                                break;
                            case X509ChainStatusFlags.HasNotDefinedNameConstraint:
                                _Messages.Add("Restrições básicas inválidas.");
                                break;
                            case X509ChainStatusFlags.HasNotPermittedNameConstraint:
                                _Messages.Add("Restrição de nome não permitida.");
                                break;
                            case X509ChainStatusFlags.HasExcludedNameConstraint:
                                _Messages.Add("Restrição de nome excluída.");
                                break;
                            case X509ChainStatusFlags.PartialChain:
                                if (!ignoreCRLOffline)
                                {
                                    _Messages.Add("Cadeia não pode ser construída até o certificado raiz.");
                                }
                                break;
                            case X509ChainStatusFlags.CtlNotTimeValid:
                                _Messages.Add("Lista de certificados confiáveis (CTL) inválida devido a um valor de tempo inválido.");
                                break;
                            case X509ChainStatusFlags.CtlNotSignatureValid:
                                _Messages.Add("Lista de certificados confiáveis (CTL) contém uma assinatura inválida.");
                                break;
                            case X509ChainStatusFlags.CtlNotValidForUsage:
                                _Messages.Add("Lista de certificados confiáveis (CTL) não é válida para uso.");
                                break;
                            case X509ChainStatusFlags.OfflineRevocation:
                                if (!ignoreCRLOffline)
                                {
                                    _Messages.Add("Lista de certificados revogados (CRL) está offline.");
                                }
                                break;
                            case X509ChainStatusFlags.NoIssuanceChainPolicy:
                                _Messages.Add("Certificado não possui extensão de política que é exigida.");
                                break;
                                //case X509ChainStatusFlags.NoError:
                                //    se não possui erro, não precisa de mensagem;
                                //    break;
                        }
                    }

                    var certificate = Chain.ChainElements[0].Certificate;

                    if (SignatureDate < certificate.NotBefore)
                    {
                        _Messages.Add("Certificado ainda não está ativado.");
                    }
                    else if (SignatureDate > certificate.NotAfter)
                    {
                        _Messages.Add("Certificado expirado.");
                    }

                    MessagesAdded = true;
                }

                return _Messages;
            }
        }

        public IsCertificateValidRule(DateTime signatureDate)
            : this(signatureDate, X509RevocationMode.Online, X509RevocationFlag.EntireChain, X509VerificationFlags.NoFlag)
        {
        }

        public IsCertificateValidRule(DateTime signatureDate, X509RevocationMode revocationMode, X509RevocationFlag revocationFlag, X509VerificationFlags verificationFlags)
        {
            Description = "Certificado foi validado pela unidade autenticadora";
            Chain = new X509Chain();
            SignatureDate = signatureDate;
            _Messages = new List<string>();

            var validarCadeia = true;

            if (ConfigurationManager.AppSettings["ASSINATURA_DIGITAL_VALIDAR_REVOGACAO"] != default(string))
            {
                bool.TryParse(ConfigurationManager.AppSettings["ASSINATURA_DIGITAL_VALIDAR_REVOGACAO"], out validarCadeia);

                if (!validarCadeia)
                {
                    revocationMode = X509RevocationMode.NoCheck;
                }
            }

            Chain.ChainPolicy.RevocationMode = revocationMode;
            Chain.ChainPolicy.RevocationFlag = revocationFlag;
            Chain.ChainPolicy.VerificationFlags = verificationFlags;

            if (TrustedRootsThumbprints == null)
            {
                TrustedRootsThumbprints = ConfigurationManager
                    .AppSettings
                    .AllKeys
                    .Where(o => o.StartsWith("trusted_root_"))
                    .Select(o => ConfigurationManager.AppSettings[o].ToLower());
            }
        }

        public override System.Linq.Expressions.Expression<Func<X509Certificate2, bool>> Predicate
        {
            get
            {
                return certificate =>
                    (
                        Chain.Build(certificate)
                        || !Messages.Any()
                    )
                    && IsICPBrasil();
            }
        }

        private bool IsICPBrasil()
        {
            if (TrustedRootsThumbprints.Contains(Chain.ChainElements[Chain.ChainElements.Count - 1].Certificate.Thumbprint.ToLower()))
            {
                return true;
            }

            _Messages.Add("Certificado não pertence a cadeia ICP Brasil");
            return false;
        }
    }
}
