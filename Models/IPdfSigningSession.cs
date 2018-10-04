namespace DocumentSignerApi.Models
{
    public interface IPdfSigningSession : IPdfPreSigningSession
    {
        IAuthenticatedAttributes AuthenticatedAttributes { get; set; }
    }
}
