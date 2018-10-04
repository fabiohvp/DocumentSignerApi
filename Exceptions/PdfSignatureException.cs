using System;

namespace DocumentSignerApi.Exceptions
{
    public class PdfSignatureException : Exception
    {
        public PdfSignatureException()
              : this(null, null)
        { }

        public PdfSignatureException(Exception innerException)
            : this(null, innerException)
        { }

        public PdfSignatureException(string message, Exception innerException)
            : base(message == null ? "Erro ao ler as assinaturas do PDF" : message, innerException)
        { }
    }
}
