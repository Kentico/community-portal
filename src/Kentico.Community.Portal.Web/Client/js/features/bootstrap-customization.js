/**
 * We use the /dist/ import because the root Bootstrap module doesn't treeshake correctly
 * See: https://stackoverflow.com/questions/75661065/bootstrap-javascript-in-webpack-5-only-working-as-esm
 * To ensure the generated production bundle is optimized, execute `npm run bundle-stats`
 */
import Tooltip from "bootstrap/js/dist/tooltip";
import Collapse from "bootstrap/js/dist/collapse";
import Dropdown from "bootstrap/js/dist/dropdown";
import Toast from "bootstrap/js/dist/toast";

export default function setup() {
  document
    .querySelectorAll("[data-bs-toggle='tooltip']")
    .forEach((el) => new Tooltip(el));

  document
    .querySelectorAll("[data-bs-toggle='collapse']")
    .forEach((el) => new Collapse(el));

  document
    .querySelectorAll("[data-bs-toggle='dropdown']")
    .forEach((el) => new Dropdown(el));

  const toastEl = document.querySelector("[data-error-toast]");

  if (toastEl) {
    const toast = new Toast(toastEl);

    document.addEventListener("htmx:beforeSwap", function (evt) {
      if (evt.detail.xhr.status === 500) {
        evt.detail.shouldSwap = false;
        evt.detail.isError = true;
      }
    });

    document.addEventListener("htmx:responseError", function handleError(e) {
      toast.show();
    });
  }
}
