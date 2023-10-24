export function initCaptcha({ formElemID }) {
  const formSelector = `#${formElemID}`;
  /**
   * @type {HTMLFormElement | null}
   */
  const formEl = document.querySelector(formSelector);

  if (!formEl) {
    console.error(`No form element found at [${formSelector}]`);

    return;
  }

  formEl.addEventListener("submit", async function (event) {
    event.preventDefault();

    if (!(event.target.CaptchaToken instanceof HTMLInputElement)) {
      throw new Error("Captcha Token element missing from form");
    }

    if (event.target.CaptchaToken.value) {
      event.target.dispatchEvent(new CustomEvent("captchaReady"));
      return;
    }

    const token = await validateCaptcha();

    event.target.CaptchaToken.value = token;

    event.target.dispatchEvent(new CustomEvent("captchaReady"));
  });
}

/**
 * Retrieves the globally accessible "captchaSiteKey" and validates the current page's captcha
 * @returns {Promise<string>} resolved with the validated captcha token and rejected if the captcha was not validated
 */
export function validateCaptcha() {
  /**
   * @type {HTMLMetaElement | null}
   */
  const captchaSiteKeyMetaEl = document.querySelector(
    'meta[name="captchaSiteKey"]'
  );

  if (!captchaSiteKeyMetaEl) {
    throw new Error("Captcha Site Key is missing.");
  }

  const captchaSiteKey = captchaSiteKeyMetaEl.content;

  return new Promise(function (resolve, reject) {
    grecaptcha.ready(function () {
      grecaptcha
        .execute(captchaSiteKey, { action: "submit" })
        .then((token) => {
          if (token && token !== "") {
            resolve(token);
          }

          reject(token);
        })
        .catch(reject);
    });
  });
}
