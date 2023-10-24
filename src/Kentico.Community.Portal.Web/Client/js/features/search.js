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

function initializeQAndASearch() {
  const form = document.querySelector("#qAndASearchForm");
  if (!(form instanceof HTMLFormElement)) {
    console.info("Q&A search skipped");
    return;
  }

  initializeSortByOnForm(form);
}

function initializeBlogSearch() {
  const form = document.getElementById("blog-search-form");
  if (!(form instanceof HTMLFormElement)) {
    console.info("Blog search skipped");
    return;
  }

  const submitButton = document.querySelector("#blogSearch");
  if (!(submitButton instanceof HTMLButtonElement)) {
    throw new Error("Missing blog search button");
  }

  const facetInput = document.querySelector("[selected-facet-value]");
  if (!(facetInput instanceof HTMLElement)) {
    throw new Error("Missing facet value form input");
  }

  function addFacetsToFacetInput() {
    let tags = document.querySelectorAll("[facet-selected]");

    const selectedValues = [...tags].map((tag) =>
      tag.getAttribute("facet-value")
    );

    facetInput.value = selectedValues.join(";");
  }

  let facets = document.querySelectorAll("[facet-value]");

  initializeSortByOnForm(form);

  const loadPanel = document.getElementById("overlay");

  for (let i = 0; i < facets.length; i++) {
    facets[i].addEventListener("click", (e) => {
      e.preventDefault();
      showLoadPanel(loadPanel);
      if (facets[i].hasAttribute("facet-selected")) {
        facets[i].removeAttribute("facet-selected");
      } else if (!facets[i].hasAttribute("facet-selected")) {
        facets[i].setAttribute("facet-selected", "");
      }
      addFacetsToFacetInput();
      form.submit();
    });
  }

  submitButton.addEventListener("click", () => {
    showLoadPanel(loadPanel);
    addFacetsToFacetInput();
  });
}
