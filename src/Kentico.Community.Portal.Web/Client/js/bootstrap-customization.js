/**
 * We use the /dist/ import because the root Bootstrap module doesn't treeshake correctly
 * See: https://stackoverflow.com/questions/75661065/bootstrap-javascript-in-webpack-5-only-working-as-esm
 * To ensure the generated production bundle is optimized, execute `npm run bundle-stats`
 */
import Tooltip from "bootstrap/js/dist/tooltip";
import Collapse from "bootstrap/js/dist/collapse";
import Dropdown from "bootstrap/js/dist/dropdown";
import Toast from "bootstrap/js/dist/toast";
import Alert from "bootstrap/js/dist/alert";
import Tab from "bootstrap/js/dist/tab";

export default function setup() {
  bindBootstrapComponents();
  bindToastComponent();
  bindFAQWidgets();
}

function bindBootstrapComponents() {
  document.querySelectorAll("[data-bs-toggle='tooltip']").forEach((el) => {
    const tooltipTitle =
      el.getAttribute("data-bs-title") ?? el.getAttribute("title") ?? "";

    if (tooltipTitle.trim().length === 0) {
      return;
    }

    new Tooltip(el);
  });

  document
    .querySelectorAll("[data-bs-toggle-collapse]")
    .forEach((el) => new Collapse(el, { toggle: false }));

  document
    .querySelectorAll("[data-bs-toggle='dropdown']")
    .forEach((el) => new Dropdown(el));

  document
    .querySelectorAll("[data-bs-toggle='tab']")
    .forEach((el) => new Tab(el));

  document.querySelectorAll(".alert").forEach((el) => new Alert(el));
}

function bindToastComponent() {
  document
    .querySelectorAll(".toast")
    .forEach((toastEl) => new Toast(toastEl, { autohide: true, delay: 1000 }));

  document.body.addEventListener("showToast", function (evt) {
    if (!(evt.target instanceof HTMLElement)) {
      return;
    }

    const toastContainerEl = document.querySelector("#toastContainer");
    if (!(toastContainerEl instanceof HTMLElement)) {
      return;
    }

    const toastEl = toastContainerEl.querySelector("[toast]");
    if (!(toastEl instanceof HTMLElement)) {
      return;
    }

    const toastCloneEl = toastEl.cloneNode(true);
    if (!(toastCloneEl instanceof HTMLElement)) {
      return;
    }

    const messageEl = toastCloneEl.querySelector("[toast-message]");
    if (!(messageEl instanceof HTMLElement)) {
      return;
    }

    messageEl.innerText = evt.detail.message;
    toastCloneEl.classList.add(
      evt.detail.status === "failure" ? "text-bg-danger" : "text-bg-success",
    );
    const toast = new Toast(toastCloneEl, {});
    toast.show();
    toastContainerEl.insertAdjacentElement("beforeend", toastCloneEl);
    toastCloneEl.addEventListener("hidden.bs.toast", (e) => {
      toastCloneEl.remove();
    });
  });

  document.addEventListener("htmx:responseError", function handleError(e) {
    let message = "There was an error processing your request";
    let status = "failure";

    // Check if the server provided custom error details
    if (e.detail && e.detail.xhr && e.detail.xhr.responseText) {
      try {
        const responseData = JSON.parse(e.detail.xhr.responseText);
        if (responseData.detail && responseData.detail.message) {
          message = responseData.detail.message || message;
          status = responseData.detail.status || status;
        }
      } catch (parseError) {
        // If JSON parsing fails, use the default message
        console.warn("Could not parse error response:", parseError);
      }
    }

    document.body.dispatchEvent(
      new CustomEvent("showToast", {
        detail: {
          message: message,
          status: status,
        },
      }),
    );
  });
}

function bindFAQWidgets() {
  // Execute all registered FAQ widget initialization callbacks
  if (window.faqInitCallbacks && Array.isArray(window.faqInitCallbacks)) {
    window.faqInitCallbacks.forEach((callback) => {
      try {
        callback(Collapse);
      } catch (error) {
        console.error("Error initializing FAQ widget:", error);
      }
    });
    // Clear callbacks after execution
    window.faqInitCallbacks = [];
  }
}
