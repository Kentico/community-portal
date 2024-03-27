using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class CaptchaValidator(IOptions<ReCaptchaSettings> options, IHttpClientFactory httpClientFactory)
{
    private readonly ReCaptchaSettings settings = options.Value;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    public async Task<CaptchaValidationResult> ValidateCaptcha(ICaptchaClientResponse clientResponse)
    {
        if (settings.IsValidationDisabled)
        {
            return new() { IsSuccess = true };
        }

        string secret = settings.SecretKey;
        var client = httpClientFactory.CreateClient();
        string requestURL = string.Format(
            "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}",
            secret,
            clientResponse.CaptchaToken);

        var responseMessage = await client.GetAsync(requestURL);

        _ = responseMessage.EnsureSuccessStatusCode();

        string data = await responseMessage.Content.ReadAsStringAsync();

        var response = JsonConvert.DeserializeObject<CaptchaResponse>(data);

        if (response is null)
        {
            return new() { IsSuccess = false, ErrorMessage = "Could not validate captcha" };
        }

        if (response.Score < settings.ScoreThredhold)
        {
            return new() { IsSuccess = false, ErrorMessage = "Invalid captcha score" };
        }

        if (!response.IsSuccess)
        {
            return new() { IsSuccess = false, ErrorMessage = response.ErrorMessages.FirstOrDefault() ?? "Captcha failed" };
        }

        return new() { IsSuccess = true };
    }

    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool IsSuccess { get; set; }
        [JsonProperty("error-codes")]
        public List<string> ErrorMessages { get; set; } = [];

        [JsonProperty("score")]
        public double Score { get; set; }
    }
}

public interface ICaptchaClientResponse
{
    string CaptchaToken { get; set; }
}

public class CaptchaValidationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// TODO: Switch to WebsiteCaptchaSettingsInfo
/// </summary>
public class ReCaptchaSettings
{
    /// <summary>
    /// Used to disable server-side captcha validation in specific scenarios (ex: CI)
    /// </summary>
    public bool IsValidationDisabled { get; set; }
    public string SiteKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public double ScoreThredhold { get; set; }
}
