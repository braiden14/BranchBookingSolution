namespace BranchBooking.Core.Exceptions;

public sealed class AuthenticationException(string message) : DomainException(message);
