﻿namespace DocumentSignerApi.Models
{
    public enum ProviderType
    {
        PROV_RSA_FULL = 1,
        PROV_RSA_SIG = 2,
        PROV_DSS = 3,
        PROV_FORTEZZA = 4,
        PROV_MS_EXCHANGE = 5,
        PROV_SSL = 6,
        PROV_RSA_SCHANNEL = 12,
        PROV_DSS_DH = 13,
        PROV_EC_ECDSA_SIG = 14,
        PROV_EC_ECNRA_SIG = 15,
        PROV_EC_ECDSA_FULL = 16,
        PROV_EC_ECNRA_FULL = 17,
        PROV_DH_SCHANNEL = 18,
        PROV_SPYRUS_LYNKS = 20,
        PROV_RNG = 21,
        PROV_INTEL_SEC = 22,
        PROV_REPLACE_OWF = 23,
        PROV_RSA_AES = 24
    }
}
