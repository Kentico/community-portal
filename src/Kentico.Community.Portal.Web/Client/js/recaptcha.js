/** How long to wait for the reCAPTCHA script and token before giving up */
const recaptchaTimeoutMs = 15000;

const captchaErrorAttr = "data-captcha-error";

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

  let isPending = false;

  formEl.addEventListener("submit", async function (event) {
    event.preventDefault();

    if (!(event.target[fieldName] instanceof HTMLInputElement)) {
      throw new Error("Captcha Token element missing from form");
    }

    if (event.target[fieldName].value) {
      event.target.dispatchEvent(new CustomEvent("captchaReady"));
      return;
    }

    if (isPending) {
      return;
    }

    isPending = true;
    hideCaptchaError(formEl);
    const restoreSubmit = disableSubmit(formEl);

    let token;
    try {
      token = await validateCaptcha(actionName);
    } catch (error) {
      console.error(error);
      showCaptchaError(formEl);
      return;
    } finally {
      restoreSubmit();
      isPending = false;
    }

    event.target[fieldName].value = token;

    onSubmit(event);

    event.target.dispatchEvent(new CustomEvent("captchaReady"));
  });
}

/**
 * Retrieves the globally accessible "captchaSiteKey" and validates the current page's captcha.
 * Loads the reCAPTCHA script first when the page didn't (see {@link loadRecaptcha}).
 * @returns {Promise<string>} resolved with the validated captcha token and rejected if the captcha was not validated
 */
export async function validateCaptcha(action) {
  const captchaSiteKey = getSiteKey();

  // Also covers grecaptcha.ready never firing (its second script is blocked)
  return withTimeout(
    loadRecaptcha().then(
      (grecaptcha) =>
        new Promise(function (resolve, reject) {
          grecaptcha.ready(function () {
            grecaptcha.execute(captchaSiteKey, { action }).then((token) => {
              if (token && token !== "") {
                resolve(token);
                return;
              }

              reject(new Error("Invalid recaptcha response"));
            }, reject);
          });
        }),
    ),
    "Timed out waiting for recaptcha",
  );
}

const recaptchaScriptSelector =
  'script[src^="https://www.google.com/recaptcha/api.js"]';

/** @type {Promise<any> | undefined} */
let recaptchaLoading;

let failedLoads = 0;

/**
 * Resolves with `window.grecaptcha`, adding the reCAPTCHA script when the page doesn't have it.
 * Google isn't contacted until a captcha is used: pages with a dedicated captcha form render the
 * script (RecaptchaScript.cs); Form Builder forms call this once they scroll into view
 * ({@link loadRecaptchaWhenVisible}).
 * On failure or timeout the script tag is removed, so the next call adds a fresh one.
 * @returns {Promise<any>}
 */
export function loadRecaptcha() {
  if (window.grecaptcha) {
    return Promise.resolve(window.grecaptcha);
  }

  if (recaptchaLoading) {
    return recaptchaLoading;
  }

  /** @type {HTMLScriptElement | undefined} */
  let script;

  const loading = withTimeout(
    new Promise((resolve, reject) => {
      const pageScript = document.querySelector(recaptchaScriptSelector);
      const isPageScript = pageScript instanceof HTMLScriptElement;

      if (isPageScript) {
        script = pageScript;
      } else {
        script = document.createElement("script");
        script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(getSiteKey())}`;
        // A new URL, or the browser reuses a hung request for the same one
        if (failedLoads > 0) {
          script.src += `&retry=${failedLoads}`;
        }
        script.async = true;
        document.head.appendChild(script);
      }

      const settle = () =>
        window.grecaptcha
          ? resolve(window.grecaptcha)
          : reject(new Error("Missing recaptcha library"));

      script.addEventListener("load", settle);
      script.addEventListener("error", settle);

      // The page's tag may have loaded or failed before these listeners were added.
      // Async scripts delay window load, so the tag has settled once the page has loaded.
      if (isPageScript) {
        if (document.readyState === "complete") {
          settle();
        } else {
          window.addEventListener("load", settle, { once: true });
        }
      }
    }),
    "Timed out loading recaptcha",
  );

  recaptchaLoading = loading;

  // Allow a retry with a fresh tag after a failed load
  loading.catch(() => {
    script?.remove();
    failedLoads++;

    if (recaptchaLoading === loading) {
      recaptchaLoading = undefined;
    }
  });

  return loading;
}

/**
 * Loads reCAPTCHA once the element scrolls into view, or right away without IntersectionObserver.
 * Submit still loads it if this didn't ({@link validateCaptcha}).
 * @param {Element} element
 */
export function loadRecaptchaWhenVisible(element) {
  const load = () => loadRecaptcha().catch(() => {});

  if (!("IntersectionObserver" in window)) {
    load();
    return;
  }

  const observer = new IntersectionObserver((entries) => {
    if (!entries.some((entry) => entry.isIntersecting)) {
      return;
    }

    observer.disconnect();
    load();
  });

  observer.observe(element);
}

function getSiteKey() {
  const captchaSiteKeyMetaEl = document.querySelector(
    'meta[name="captchaSiteKey"]',
  );

  if (!(captchaSiteKeyMetaEl instanceof HTMLMetaElement)) {
    throw new Error("Captcha Site Key is missing.");
  }

  return captchaSiteKeyMetaEl.content;
}

/**
 * Rejects when the promise doesn't settle in time
 * @template T
 * @param {Promise<T>} promise
 * @param {string} message
 * @returns {Promise<T>}
 */
function withTimeout(promise, message) {
  let timer;
  const timeout = new Promise((_, reject) => {
    timer = setTimeout(() => reject(new Error(message)), recaptchaTimeoutMs);
  });

  return Promise.race([promise, timeout]).finally(() => clearTimeout(timer));
}

/**
 * Disables the form's submit buttons and shows their loading state (xpc-loading-button)
 * @param {HTMLFormElement} formEl
 * @returns {() => void} re-enables the buttons
 */
function disableSubmit(formEl) {
  const buttons = Array.from(formEl.querySelectorAll('[type="submit"]')).filter(
    (button) => !button.disabled,
  );

  for (const button of buttons) {
    button.disabled = true;
    button.classList.add("htmx-request");
  }

  return () => {
    for (const button of buttons) {
      button.disabled = false;
      button.classList.remove("htmx-request");
    }
  };
}

/** @param {HTMLFormElement} formEl */
function showCaptchaError(formEl) {
  if (formEl.querySelector(`[${captchaErrorAttr}]`)) {
    return;
  }

  const errorEl = document.createElement("div");
  errorEl.className = "alert alert-danger mt-3";
  errorEl.setAttribute("role", "alert");
  errorEl.setAttribute(captchaErrorAttr, "");
  errorEl.setAttribute("test-id", "captchaError");
  errorEl.textContent =
    "The spam check (reCAPTCHA) didn't load. Check your connection or content blocker, then submit again.";

  const submitEl = formEl.querySelector('[type="submit"]');
  if (submitEl) {
    submitEl.after(errorEl);
  } else {
    formEl.append(errorEl);
  }
}

/** @param {HTMLFormElement} formEl */
function hideCaptchaError(formEl) {
  formEl.querySelector(`[${captchaErrorAttr}]`)?.remove();
}
