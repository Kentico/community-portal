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
   * @type {HTMLInputElement[]}
   */
  const facetEls = [...formEl.querySelectorAll(`[facet-field]`)];
  for (const facetEl of facetEls) {
    facetEl.addEventListener("click", (e) => {
      if (!(e.target instanceof HTMLInputElement)) {
        return;
      }

      const value = e.target.value;

      if (e.target.hasAttribute("facet-field-mobile")) {
        /**
         * @type {HTMLInputElement}
         */
        const el = document.querySelector(
          `[value="${value}"]:not([facet-field-mobile])`,
        );
        if (el instanceof HTMLInputElement) {
          /** disable unused field to prevent double field submission */
          el.disabled = true;
        }
      } else {
        /**
         * @type {HTMLInputElement}
         */
        const el = document.querySelector(
          `[value=${value}][facet-field-mobile]`,
        );
        if (el instanceof HTMLInputElement) {
          /** disable unused field to prevent double field submission */
          el.disabled = true;
        }
      }
    });
  }

  const selectEls = [...formEl.querySelectorAll(`[select-field]`)];
  for (const selectEl of selectEls) {
    selectEl.addEventListener("change", (e) => {
      if (!(e.target instanceof HTMLSelectElement)) {
        return;
      }

      const name = e.target.name;

      if (e.target.hasAttribute("select-field-mobile")) {
        /**
         * @type {HTMLSelectElement}
         */
        const el = document.querySelector(
          `[name="${name}"]:not([select-field-mobile])`,
        );
        if (el instanceof HTMLSelectElement) {
          /** disable unused field to prevent double field submission */
          el.disabled = true;
        }
      } else {
        /**
         * @type {HTMLSelectElement}
         */
        const el = document.querySelector(
          `[name=${name}][select-field-mobile]`,
        );
        if (el instanceof HTMLSelectElement) {
          /** disable unused field to prevent double field submission */
          el.disabled = true;
        }
      }
    });
  }
}
