namespace BespokeStudio.Application.Validation;

public sealed class UploadValidationException(string message) : Exception(message);
