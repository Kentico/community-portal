export default function setup() {
  initializeQAndASearch();
  initializeBlogSearch();
}

function showLoadPanel(loadPanel) {
  setTimeout(() => {
    loadPanel.style.display = "block";
  }, 300);
}

/**
 *
 * @param {HTMLFormElement} formEl
 */
function initializeSortByOnForm(formEl) {
  let sortSelects = document.querySelectorAll("[search-sort-by]");

  for (let i = 0; i < sortSelects.length; i++) {
    sortSelects[i].addEventListener("change", function () {
      if (formEl !== null) {
        const loadPanel = document.getElementById("overlay");
        showLoadPanel(loadPanel);
        formEl.submit();
      }
    });
  }
}

/**
 *
 * @param {HTMLFormElement} formEl
 */
function initializeCheckboxFacetsOnForm(formEl) {
  const submitButton = document.querySelector("#submitSearch");
  if (!(submitButton instanceof HTMLButtonElement)) {
    throw new Error("Missing search submit button");
  }
  submitButton.addEventListener("click", () => {
    showLoadPanel(loadPanel);
  });

  const loadPanel = document.getElementById("overlay");

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

      if (e.target.hasAttribute("facet-mobile")) {
        /**
         * @type {HTMLInputElement}
         */
        const el = document.querySelector(
          `[value="${value}"]:not([facet-mobile])`,
        );
        if (el instanceof HTMLInputElement) {
          /** disable unused field to prevent double field submission */
          el.disabled = true;
        }
      } else {
        /**
         * @type {HTMLInputElement}
         */
        const el = document.querySelector(`[value=${value}][facet-mobile]`);
        if (el instanceof HTMLInputElement) {
          /** disable unused field to prevent double field submission */
          el.disabled = true;
        }
      }
      formEl.submit();
    });
  }
}

function initializeQAndASearch() {
  const form = document.querySelector("#qAndASearchForm");
  if (!(form instanceof HTMLFormElement)) {
    return;
  }

  initializeSortByOnForm(form);
  initializeCheckboxFacetsOnForm(form);
}

function initializeBlogSearch() {
  const form = document.getElementById("blogSearchForm");
  if (!(form instanceof HTMLFormElement)) {
    return;
  }

  initializeSortByOnForm(form);
  initializeCheckboxFacetsOnForm(form);
}
