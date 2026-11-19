namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Loads Google reCAPTCHA only on pages that need it, not site-wide.
/// </summary>
/// <remarks>
/// <para>
/// Views with a dedicated captcha form (register) call
/// <see cref="RequireRecaptchaScript"/>. The layout renders the script at the end of
/// <c>&lt;body&gt;</c> (<c>RecaptchaScriptViewComponent</c>), after the page body, so the flag is
/// already set.
/// </para>
/// <para>
/// Form Builder forms with the reCAPTCHA component can sit on any page (the home page has the
/// newsletter form), so they don't set the flag. <c>js/recaptcha.js</c> loads the script when the
/// form scrolls into view, or at the latest on submit.
/// </para>
/// </remarks>
public static class RecaptchaScript
{
    public const string ITEM_KEY = "kcp.recaptcha.scriptRequired";

    /// <summary>
    /// The page renders the reCAPTCHA script. Call it from the view that renders the captcha form.
    /// </summary>
    public static void RequireRecaptchaScript(this HttpContext context) =>
        context.Items[ITEM_KEY] = true;

    public static bool IsRecaptchaScriptRequired(this HttpContext context) =>
        context.Items.ContainsKey(ITEM_KEY);
}
