using System;

namespace DocumentSignerApi.Exceptions
{
    public class FileBeingSignedException : Exception
    {
        public FileBeingSignedException()
            : base("O documento já está sendo assinado por outro usuário, tente novamente")
        { }

        public FileBeingSignedException(string message)
            : base(message)
        { }
    }
}
