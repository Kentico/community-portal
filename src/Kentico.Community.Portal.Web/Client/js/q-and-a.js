import "@milkdown/crepe/theme/common/style.css";
import "@milkdown/crepe/theme/frame.css";

import {
  LanguageDescription,
  LanguageSupport,
  StreamLanguage,
} from "@codemirror/language";
import { html } from "@milkdown/kit/component";
import { Crepe } from "@milkdown/crepe";
import { listener, listenerCtx } from "@milkdown/kit/plugin/listener";

export async function initQAndA({ editorElemID, formType = "" }) {
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

  await initialize(formEl, cancelButtonEls);

  /**
   * Initialized the Crepe instance
   * @param {HTMLFormElement} formEl
   * @param {HTMLButtonElement[]} cancelButtonEls
   * @returns
   */
  async function initialize(formEl, cancelButtonEls) {
    const c = new Crepe({
      root: editorEl,
      defaultValue: getInitialMarkdown(),
      featureConfigs: {
        "code-mirror": {
          languages: getCodeMirrorLanguages(),
          renderLanguage: (language, selected) => {
            const displayName =
              language.toLowerCase() === "csharp" ? "C#" : language;

            return html`
              <li
                class="language-list-item"
                role="listitem"
                tabindex="0"
                aria-selected="${selected ? "true" : "false"}"
                data-language="${language}"
              >
                ${displayName} ${selected ? "✅" : ""}
              </li>
            `;
          },
        },
      },
    });

    c.editor
      .config((ctx) => {
        const listener = ctx.get(listenerCtx);

        const updateField = debounce((ctx, markdown, prevMarkdown) => {
          if (markdown !== prevMarkdown) {
            console.log("markdown updated");
            textareaEl.value = c.getMarkdown().trim();
          }
        }, 250);

        listener.markdownUpdated(updateField);
      })
      .use(listener);

    await c.create();

    formEl.addEventListener("submit", cleanupCrepe("submitReady"));

    for (const buttonEl of cancelButtonEls) {
      buttonEl.addEventListener("click", cleanupCrepe("cancelReady"));
    }

    return c;

    /**
     * Cleans up the generated Crepe instance and emits the given custom event when complete
     * @param {string} customEvent
     * @returns {(event: SubmitEvent | MouseEvent) => void}
     */
    function cleanupCrepe(customEvent) {
      return (event) => {
        event.preventDefault();
        event.stopPropagation();
        if (
          !(event.target instanceof HTMLButtonElement) &&
          !(event.target instanceof HTMLFormElement)
        ) {
          return;
        }

        /** Code language bug: https://github.com/Milkdown/milkdown/issues/1521 */
        if (event.target.classList.contains("language-button")) {
          return;
        }
        if (
          event instanceof SubmitEvent &&
          event.submitter instanceof HTMLButtonElement &&
          (event.submitter.classList.contains("language-button") ||
            event.submitter.classList.contains("toolbar-item"))
        ) {
          return;
        }

        c.destroy(true);
        console.log("editor destroyed");
        event.target.dispatchEvent(new CustomEvent(customEvent));
      };
    }
  }

  function getInitialMarkdown() {
    const existingMarkdown = (textareaEl.value ?? "").trim();

    if (existingMarkdown) {
      return existingMarkdown;
    }

    return formType === "Question"
      ? `[describe your problem or question]

---

**Environment**
- Xperience by Kentico version: [29.7.0]
- .NET version: [8|9]
- Execution environment: [Local|Azure|SaaS|Other]
- Link to relevant [Xperience by Kentico documentation](https://docs.kentico.com)`
      : "[detail your answer]";
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

function getCodeMirrorLanguages() {
  function legacy(parser) {
    return new LanguageSupport(StreamLanguage.define(parser));
  }
  function sql(dialectName) {
    return import("@codemirror/lang-sql").then((m) =>
      m.sql({ dialect: m[dialectName] }),
    );
  }

  return [
    /*@__PURE__*/ LanguageDescription.of({
      name: "CSharp",
      alias: ["csharp", "cs"],
      extensions: ["cs"],
      load() {
        return import("@codemirror/legacy-modes/mode/clike").then((m) =>
          legacy(m.csharp),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "CSS",
      extensions: ["css"],
      load() {
        return import("@codemirror/lang-css").then((m) => m.css());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "HTML",
      alias: ["xhtml"],
      extensions: ["html", "htm", "handlebars", "hbs"],
      load() {
        return import("@codemirror/lang-html").then((m) => m.html());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "JavaScript",
      alias: ["ecmascript", "js", "node"],
      extensions: ["js", "mjs", "cjs"],
      load() {
        return import("@codemirror/lang-javascript").then((m) =>
          m.javascript(),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "JSON",
      alias: ["json5"],
      extensions: ["json", "map"],
      load() {
        return import("@codemirror/lang-json").then((m) => m.json());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "JSX",
      extensions: ["jsx"],
      load() {
        return import("@codemirror/lang-javascript").then((m) =>
          m.javascript({ jsx: true }),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "Markdown",
      extensions: ["md", "markdown", "mkd"],
      load() {
        return import("@codemirror/lang-markdown").then((m) => m.markdown());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "SCSS",
      extensions: ["scss"],
      load() {
        return import("@codemirror/lang-sass").then((m) => m.sass());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "SQL",
      extensions: ["sql"],
      load() {
        return sql("StandardSQL");
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "TSX",
      extensions: ["tsx"],
      load() {
        return import("@codemirror/lang-javascript").then((m) =>
          m.javascript({ jsx: true, typescript: true }),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "TypeScript",
      alias: ["ts"],
      extensions: ["ts", "mts", "cts"],
      load() {
        return import("@codemirror/lang-javascript").then((m) =>
          m.javascript({ typescript: true }),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "XML",
      alias: ["rss", "wsdl", "xsd"],
      extensions: ["xml", "xsl", "xsd", "svg"],
      load() {
        return import("@codemirror/lang-xml").then((m) => m.xml());
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "YAML",
      alias: ["yml"],
      extensions: ["yaml", "yml"],
      load() {
        return import("@codemirror/lang-yaml").then((m) => m.yaml());
      },
    }),

    /*@__PURE__*/ LanguageDescription.of({
      name: "PowerShell",
      extensions: ["ps1", "psd1", "psm1"],
      load() {
        return import("@codemirror/legacy-modes/mode/powershell").then((m) =>
          legacy(m.powerShell),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "Shell",
      alias: ["bash", "sh", "zsh"],
      extensions: ["sh", "ksh", "bash"],
      filename: /^PKGBUILD$/,
      load() {
        return import("@codemirror/legacy-modes/mode/shell").then((m) =>
          legacy(m.shell),
        );
      },
    }),
    /*@__PURE__*/ LanguageDescription.of({
      name: "Text",
      alias: [],
      extensions: ["txt"],
      load() {
        return import("@codemirror/legacy-modes/mode/properties").then((m) =>
          legacy(m.properties),
        );
      },
    }),
  ];
}
