using System.Net;

namespace esAPI.Exceptions;

public class ApiClientException(string message, HttpStatusCode statusCode, string? content) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ResponseContent { get; } = content;
}

public class ApiCallFailedException(string message, HttpStatusCode statusCode, string? content) : ApiClientException(message, statusCode, content)
{
}

public class ApiResponseParseException(string message) : Exception(message)
{
}


public class ProcurementStepFailedException(string message, string failureStatus, Exception? innerException = null) : Exception(message, innerException)
{
    public string FailureStatus { get; } = failureStatus;
}