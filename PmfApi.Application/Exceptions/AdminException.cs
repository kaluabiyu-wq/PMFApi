namespace PmfApi.Application.Exceptions;

public class AdminRuleException(string message) : Exception(message) { }

public class RoleNotFoundException : Exception { }


public class AccountSuspendedException(string? reason) : Exception(reason) { }