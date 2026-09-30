namespace PmfApi.Application.Exceptions;

public class DuplicateEmailException : Exception { }
public class DuplicateLicenseException : Exception { }
public class LocationNotFoundException : Exception { }
public class InvalidUploadException(string message) : Exception(message) { }