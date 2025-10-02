using System.Diagnostics.CodeAnalysis;
using Nb.Tolls.Domain.Enums;

namespace Nb.Tolls.Domain.Results;

public class ApplicationResult
{
    public string[] Messages { get; private init; } = Array.Empty<string>();

    public ApplicationResultStatus ApplicationResultStatus { get; private init; }
    protected bool IsSuccessful => ApplicationResultStatus == ApplicationResultStatus.Success;
    
    public static ApplicationResult<TResult> WithError<TResult>(string message) => new()
    {
        Messages = [message],
        ApplicationResultStatus = ApplicationResultStatus.Error
    };
    
    public static ApplicationResult<TResult> WithSuccess<TResult>(TResult data) => new()
    {
        Result = data,
        ApplicationResultStatus = ApplicationResultStatus.Success
    };

    public static ApplicationResult<TResult> NotFound<TResult>(string message) => new()
    {
        Messages = [message], ApplicationResultStatus = ApplicationResultStatus.NotFound
    };
}


public class ApplicationResult<TResult> : ApplicationResult
{
    public TResult? Result { get; init; }

    [MemberNotNullWhen(true, nameof(Result))]
    public new bool IsSuccessful => base.IsSuccessful && Result != null;
}