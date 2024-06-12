namespace System.ComponentModel.DataAnnotations;

public class MaxFileSizeAttribute(int maxFileSizeBytes) : ValidationAttribute
{
    private readonly int maxFileSizeBytes = maxFileSizeBytes;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file && file.Length > maxFileSizeBytes)
        {
            return new ValidationResult($"Maximum allowed file size is {ConvertBytesToReadableUnit(maxFileSizeBytes)} bytes.");
        }

        return ValidationResult.Success;
    }

    private static string ConvertBytesToReadableUnit(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} Bytes";
        }
        else if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F2} KB";
        }
        else
        {
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }
    }
}
