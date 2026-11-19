import "@milkdown/crepe/theme/common/style.css";
import "@milkdown/crepe/theme/frame.css";

import { Crepe } from "@milkdown/crepe";
import { listener, listenerCtx } from "@milkdown/kit/plugin/listener";
import { remarkPreserveEmptyLinePlugin } from "@milkdown/preset-commonmark";
import { getCodeMirrorLanguages } from "./milkdown/codemirror-languages.js";

export async function initQAndA({ editorElemID, formType = "" }) {
  /**
   * @type {Crepe | undefined}
   */
  let crepeEditor = undefined;

  const editorSelector = `#${editorElemID}`;
  const editorEl = document.querySelector(editorSelector);

  if (!(editorEl instanceof HTMLDivElement)) {
    throw new Error(`No editor element found at [${editorSelector}]`);
  }

  const formEl = editorEl.closest("form");

  if (!(formEl instanceof HTMLFormElement)) {
    throw new Error(`No form element found`);
  }

  const textareaEl = formEl.querySelector("textarea");

  if (!(textareaEl instanceof HTMLTextAreaElement)) {
    throw new Error(`No text area element found`);
  }

  const cancelButtonEls = formEl.querySelectorAll("button[cancel-button]");

  const elements = { editorEl, formEl, textareaEl, cancelButtonEls };

  await initialize(elements);

  /**
   * @param {Event} event
   */
  window.toggleEditor = async function (event) {
    if (!(event.target instanceof HTMLInputElement)) {
      throw new Error("Editor toggle requires checkbox");
    }

    this.mode = this.mode === "editor" ? "markdown" : "editor";
    if (this.mode === "editor") {
      await initialize(elements);
    } else {
      cleanupCrepe("toggleEditor", true)(event);
      event.target.innerText = "View editor";
    }

    textareaEl.classList.toggle("d-none");
  };

  /**
   * Initialized the Crepe instance
   * @param {typeof elements} elements
   * @returns
   */
  async function initialize(elements) {
    const { editorEl, formEl, textareaEl, cancelButtonEls } = elements;

    textareaEl.value = getInitialMarkdown(textareaEl.value);
    crepeEditor = new Crepe({
      root: editorEl,
      defaultValue: getInitialMarkdown(textareaEl.value),
      features: {
        [Crepe.Feature.ImageBlock]: false,
      },
      featureConfigs: {
        "code-mirror": {
          languages: getCodeMirrorLanguages(),
          renderLanguage: (language, selected) => {
            const displayName =
              language.toLowerCase() === "csharp" ? "C#" : language;

            return `${displayName} ${selected ? "✅" : ""}`;
          },
        },
        placeholder: {
          text: getPlaceholderText(),
        },
      },
    });

    crepeEditor.editor
      .config((ctx) => {
        const list = ctx.get(listenerCtx);

        const updateField = debounce((ctx, markdown, prevMarkdown) => {
          if (markdown !== prevMarkdown) {
            console.log("markdown updated");
            textareaEl.value = crepeEditor.getMarkdown().trim();
          }
        }, 250);

        list.markdownUpdated(updateField);
      })
      .use(listener);
    crepeEditor.editor.remove(remarkPreserveEmptyLinePlugin);

    await crepeEditor.create();

    formEl.addEventListener("submit", cleanupCrepe("submitReady", false));

    document.body.addEventListener(
      "cleanupEditor",
      cleanupCrepe("submitted", true),
    );

    for (const buttonEl of cancelButtonEls) {
      buttonEl.addEventListener("click", cleanupCrepe("cancelReady", true));
    }

    return crepeEditor;
  }

  /**
   * Cleans up the generated Crepe instance and emits the given custom event when complete
   * @param {'submitReady' | 'cancelReady' | 'toggleEditor' | 'submitted'} customEvent
   * @param {boolean} cleanup
   * @returns {(event: SubmitEvent | MouseEvent | Event ) => void}
   */
  function cleanupCrepe(customEvent, cleanup) {
    return async (event) => {
      event.preventDefault();
      event.stopPropagation();

      if (crepeEditor && cleanup) {
        await crepeEditor.destroy();
        crepeEditor = undefined;
        console.log("editor destroyed");
      }

      if (customEvent === "submitReady" || customEvent === "cancelReady") {
        event.target.dispatchEvent(new CustomEvent(customEvent));
      }
    };
  }

  /**
   * @param {string} initialValue
   * @returns
   */
  function getInitialMarkdown(initialValue) {
    const existingMarkdown = (initialValue ?? "").trim();

    if (existingMarkdown) {
      return existingMarkdown;
    }

    return formType === "Question"
      ? `Describe your question...

---

**Environment**
- Xperience by Kentico version: [30.11.2]
- .NET version: [8|9]
- Execution environment: [SaaS|Private cloud (Azure/AWS/Virtual machine)]
- Link to relevant [Xperience by Kentico documentation](https://docs.kentico.com)`
      : "";
  }

  function getPlaceholderText() {
    return "Type something...";
  }

  /**
   * Debounces a function, ensuring it's only called after the specified delay.
   * @param {(...args: any[]) => void} func - The function to debounce.
   * @param {number} delay - The delay in milliseconds.
   * @returns {(...args: any[]) => void} - The debounced function.
   */
  function debounce(func, delay) {
    let timeoutId;

    return (...args) => {
      clearTimeout(timeoutId);
      timeoutId = setTimeout(() => {
        func.apply(this, args);
      }, delay);
    };
  }
}

