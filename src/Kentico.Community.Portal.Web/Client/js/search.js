/**
 * @param {{ formElemID: string }} state
 */
export function initSearch({ formElemID }) {
  const formEl = document.querySelector(`#${formElemID}`);
  if (!(formEl instanceof HTMLFormElement)) {
    console.warn(`Cannot find form #${formElemID}`);
    return;
  }

  /**
   * Search forms have duplicate form fields (desktop/mobile). This special submit handling
   * ensures that the "mobile" versions of the fields are always disabled.
   * The copyValueAndDisableField ensures the desktop & mobile field values are always in sync.
   * @param {Event} evt
   */
  function handleSubmit(evt) {
    evt.preventDefault();

    const selectEls = [
      ...formEl.querySelectorAll(`[select-field],[facet-field]`),
    ]
      .filter(
        (el) =>
          el instanceof HTMLSelectElement || el instanceof HTMLInputElement,
      )
      .filter(
        (el) =>
          el.hasAttribute("select-field-mobile") ||
          el.hasAttribute("facet-field-mobile"),
      );

    for (const el of selectEls) {
      el.disabled = true;
    }

    formEl.removeEventListener("submit", handleSubmit);
    evt.target.dispatchEvent(new CustomEvent("submitReady"));
  }
  formEl.addEventListener("submit", handleSubmit);

  /**
   * Disables the counterpart field (mobile/desktop) to prevent duplicate form submissions.
   * Disabled form fields are automatically excluded from form data when the form is submitted.
   *
   * @param {HTMLInputElement | HTMLSelectElement} sourceElement The element that triggered the event
   * @param {string} selector The selector to find the counterpart element
   * @returns {void}
   */
  function copyValueAndDisableField(sourceElement, selector) {
    const counterpart = document.querySelector(selector);

    if (
      !(
        counterpart instanceof HTMLSelectElement ||
        counterpart instanceof HTMLInputElement
      )
    ) {
      console.warn(
        `Failed to copy value to counterpart field: ${error.message}`,
      );
      return;
    }

    if (
      sourceElement instanceof HTMLSelectElement &&
      counterpart instanceof HTMLSelectElement
    ) {
      counterpart.value = sourceElement.value;
    } else if (sourceElement instanceof HTMLInputElement) {
      counterpart.checked = sourceElement.checked;
    }
  }

  /**
   * @type {HTMLInputElement[]}
   */
  const facetEls = [...formEl.querySelectorAll(`[facet-field]`)];
  for (const facetEl of facetEls) {
    facetEl.addEventListener("click", (e) => {
      if (!(e.target instanceof HTMLInputElement)) {
        return;
      }

      const value = e.target.value;
      const isMobile = e.target.hasAttribute("facet-field-mobile");
      const selector = isMobile
        ? `[value="${value}"]:not([facet-field-mobile])`
        : `[value="${value}"][facet-field-mobile]`;

      copyValueAndDisableField(e.target, selector);
    });
  }

  const selectEls = [...formEl.querySelectorAll(`[select-field]`)];
  for (const selectEl of selectEls) {
    selectEl.addEventListener("change", (e) => {
      if (!(e.target instanceof HTMLSelectElement)) {
        return;
      }

      const name = e.currentTarget.name;
      const isMobile = e.target.hasAttribute("select-field-mobile");
      const selector = isMobile
        ? `[name="${name}"]:not([select-field-mobile])`
        : `[name="${name}"][select-field-mobile]`;

      copyValueAndDisableField(e.target, selector);
    });
  }
}
