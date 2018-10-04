using System;

namespace DocumentSignerApi.Exceptions
{
    public class PdfProtectedException : Exception
    {
        public PdfProtectedException()
              : this(null, null)
        { }

        public PdfProtectedException(Exception innerException)
            : this(null, innerException)
        { }

        public PdfProtectedException(string message, Exception innerException)
            : base(message == null ? "PDF protegido por senha" : message, innerException)
        { }
    }
}
