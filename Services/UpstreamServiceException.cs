using System;

namespace OpenBreweryApi.Services;

public sealed class UpstreamServiceException : Exception
{
    public UpstreamServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}