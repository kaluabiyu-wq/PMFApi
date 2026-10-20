namespace PmfApi.Application.Abstractions;


public static class UserCacheKeys
{
    public static string CanUseApi(int userId) => $"user-can-use-api:{userId}";
}