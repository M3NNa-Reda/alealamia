namespace alealamia.Application.DTOs.Identity
{
    /// <summary>Outcome of validating an email and password pair, including the account lockout state.</summary>
    public enum CredentialCheckResult
    {
        Success = 0,
        InvalidCredentials = 1,
        LockedOut = 2
    }
}
