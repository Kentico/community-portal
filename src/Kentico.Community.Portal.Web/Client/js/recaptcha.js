export function initCaptcha({
  formElemID,
  fieldElemID,
  fieldName = "CaptchaToken",
  onSubmit = (e) => {},
  actionName = "submit",
}) {
  const formSelector = `#${formElemID}`;
  const fieldSelector = `#${fieldElemID}`;

  const formEl =
    document.querySelector(formSelector) ||
    document.querySelector(fieldSelector)?.closest("form");

  if (!(formEl instanceof HTMLFormElement)) {
    console.error(`No form element found for the given selector`);

    return;
  }

  formEl.addEventListener("submit", async function (event) {
    event.preventDefault();

    if (!(event.target[fieldName] instanceof HTMLInputElement)) {
      throw new Error("Captcha Token element missing from form");
    }

    if (event.target[fieldName].value) {
      event.target.dispatchEvent(new CustomEvent("captchaReady"));
      return;
    }

    const token = await validateCaptcha(actionName);

    event.target[fieldName].value = token;

    onSubmit(event);

    event.target.dispatchEvent(new CustomEvent("captchaReady"));
  });
}

/**
 * Retrieves the globally accessible "captchaSiteKey" and validates the current page's captcha
 * @returns {Promise<string>} resolved with the validated captcha token and rejected if the captcha was not validated
 */
export function validateCaptcha(action) {
  const captchaSiteKeyMetaEl = document.querySelector(
    'meta[name="captchaSiteKey"]'
  );

  if (!(captchaSiteKeyMetaEl instanceof HTMLMetaElement)) {
    throw new Error("Captcha Site Key is missing.");
  }

  const captchaSiteKey = captchaSiteKeyMetaEl.content;

  return new Promise(function (resolve, reject) {
    const grecaptcha = window.grecaptcha;

    if (!grecaptcha) {
      reject("Missing recaptcha library");
      return;
    }

    grecaptcha.ready(function () {
      grecaptcha.execute(captchaSiteKey, { action }).then((token) => {
        if (token && token !== "") {
          resolve(token);
          return;
        }

        reject("Invalid recaptcha response");
      });
    });
  });
}
