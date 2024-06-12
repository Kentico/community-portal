namespace System.ComponentModel.DataAnnotations;

public class AllowedExtensionsAttribute(string[] extensions) : ValidationAttribute
{
    private readonly string[] extensions = extensions;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not IFormFile file)
        {
            return new ValidationResult(GetErrorMessage());
        }

        string? extension = Path.GetExtension(file?.FileName);

        if (extension is null)
        {
            return new ValidationResult(GetErrorMessage());
        }

        if (!extensions.Contains(extension.ToLower()))
        {
            return new ValidationResult(GetErrorMessage());
        }

        return ValidationResult.Success;
    }

    public string GetErrorMessage() => $"Your image's filetype is not valid. Please use a file with one of these extensions: {string.Join(",", extensions)}";
}
