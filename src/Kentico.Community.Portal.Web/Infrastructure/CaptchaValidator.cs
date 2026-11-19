using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class CaptchaValidator(
    IOptions<ReCaptchaSettings> options,
    IHttpClientFactory httpClientFactory,
    ILogger<CaptchaValidator> logger)
{
    private readonly ReCaptchaSettings settings = options.Value;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly ILogger<CaptchaValidator> logger = logger;

    /// <param name="clientResponse">The submitted form</param>
    /// <param name="source">The form, for the verification log (e.g. "Registration")</param>
    public async Task<CaptchaValidationResult> ValidateCaptcha(ICaptchaClientResponse clientResponse, string source)
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

        var result = new CaptchaValidationResult { IsSuccess = true };
        if (response.Score < settings.ScoreThreshold)
        {
            result = new() { IsSuccess = false, ErrorMessage = "Invalid captcha score" };
        }
        else if (!response.IsSuccess)
        {
            result = new() { IsSuccess = false, ErrorMessage = response.ErrorMessages.FirstOrDefault() ?? "Captcha failed" };
        }

        logger.LogCaptchaVerification(source, response.Score, response.Action, response.IsSuccess, settings.ScoreThreshold, result.IsSuccess);

        return result;
    }

    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool IsSuccess { get; set; }
        [JsonProperty("error-codes")]
        public List<string> ErrorMessages { get; set; } = [];

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("action")]
        public string? Action { get; set; }
    }
}

public static partial class CaptchaLogging
{
    /// <summary>
    /// One reCAPTCHA verification, for tuning the score threshold. No token, IP or form data.
    /// </summary>
    /// <param name="source">The form or component that verified the token</param>
    /// <param name="success">Google's <c>success</c></param>
    /// <param name="passed">The portal accepted the submission (score and success)</param>
    [LoggerMessage(
        EventId = 1001,
        EventName = "RECAPTCHA_VERIFICATION",
        Level = LogLevel.Information,
        Message = "reCAPTCHA verification: {Source} score {Score} action {Action} success {Success} threshold {Threshold} passed {Passed}")]
    public static partial void LogCaptchaVerification(
        this ILogger logger,
        string source,
        double score,
        string? action,
        bool success,
        double threshold,
        bool passed);
}

public interface ICaptchaClientResponse
{
    public string CaptchaToken { get; set; }
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
    public double ScoreThreshold { get; set; }
}
