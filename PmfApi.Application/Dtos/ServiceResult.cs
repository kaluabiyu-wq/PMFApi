namespace PmfApi.Application.Dtos;

public enum ServiceOutCome
{
    Success,
    NotFound,
    Invalid,
    Conflict,
}

public record ServiceResult<T>(ServiceOutCome OutCome, T? Value = default,
 string? Error = null)
{
    public bool IsSuccess => OutCome == ServiceOutCome.Success;

    public static ServiceResult<T> Ok(T value) => 
    new(ServiceOutCome.Success, value);
    public static ServiceResult<T> NotFound(string error) =>
     new(ServiceOutCome.NotFound, default, error);
    public static ServiceResult<T> Invalid(string error) =>
     new(ServiceOutCome.Invalid, default, error);
    public static ServiceResult<T> Conflict(string error) =>
    new (ServiceOutCome.Conflict, default, error);


}