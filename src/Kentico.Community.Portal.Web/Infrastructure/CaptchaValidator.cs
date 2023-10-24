using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Kentico.Community.Portal.Web.Infrastructure;

public class CaptchaValidator
{
    private readonly ReCaptchaSettings settings;
    private readonly IHttpClientFactory httpClientFactory;

    public CaptchaValidator(IOptions<ReCaptchaSettings> options, IHttpClientFactory httpClientFactory)
    {
        settings = options.Value;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<CaptchaValidationResult> ValidateCaptcha(ICaptchaClientResponse clientResponse)
    {
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

        if (response.Score < settings.ScoreThredhold)
        {
            return new CaptchaValidationResult { IsSuccess = false, ErrorMessage = "Invalid captcha score" };
        }

        if (!response.IsSuccess)
        {
            return new CaptchaValidationResult { IsSuccess = false, ErrorMessage = response.ErrorMessages.FirstOrDefault() ?? "Captcha failed" };
        }

        return new CaptchaValidationResult { IsSuccess = true };
    }

    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool IsSuccess { get; set; }
        [JsonProperty("error-codes")]
        public List<string> ErrorMessages { get; set; }

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

public class ReCaptchaSettings
{
    public string SiteKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public double ScoreThredhold { get; set; }
}
