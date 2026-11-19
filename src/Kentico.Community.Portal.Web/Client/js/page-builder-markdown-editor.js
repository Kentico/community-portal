/**
 * Page Builder inline editor for the Markdown widget.
 * Loaded only in Page Builder edit mode (see _Layout.cshtml).
 *
 * Crepe's reset.css is intentionally not imported - it restyles the content
 * (headings, paragraphs, links...) which should use the site styles instead.
 * Only the editor UI (toolbar, slash menu, block handle...) uses Crepe styles.
 */
import "@milkdown/crepe/theme/common/prosemirror.css";
import "@milkdown/crepe/theme/common/block-edit.css";
import "@milkdown/crepe/theme/common/code-mirror.css";
import "@milkdown/crepe/theme/common/cursor.css";
import "@milkdown/crepe/theme/common/link-tooltip.css";
import "@milkdown/crepe/theme/common/list-item.css";
import "@milkdown/crepe/theme/common/placeholder.css";
import "@milkdown/crepe/theme/common/toolbar.css";
import "@milkdown/crepe/theme/common/table.css";
import "@milkdown/crepe/theme/frame.css";
import "../styles/page-builder-markdown-editor.css";

import { Crepe } from "@milkdown/crepe";
import { remarkPreserveEmptyLinePlugin } from "@milkdown/preset-commonmark";
import { getCodeMirrorLanguages } from "./milkdown/codemirror-languages.js";

const UPDATE_DELAY_MS = 300;

/**
 * @typedef {Object} EditorState
 * @property {HTMLElement} editor Inline editor wrapper created by Page Builder
 * @property {string} propertyName
 * @property {string} value Last known markdown value
 * @property {'editor' | 'raw'} mode
 * @property {Crepe | undefined} crepe
 * @property {string | undefined} pending Value waiting to be sent to Page Builder
 * @property {number | undefined} timer
 * @property {boolean} disposed
 * @property {Promise<void>} queue Serializes async Crepe create/destroy
 * @property {AbortController} abort
 * @property {HTMLElement} rootEl
 * @property {HTMLTextAreaElement} rawEl
 * @property {HTMLButtonElement} toggleEl
 */

/**
 * One state per inline editor element - multiple widgets per page are independent
 * @type {WeakMap<HTMLElement, EditorState>}
 */
const instances = new WeakMap();

/**
 * @param {{ editor: HTMLElement, propertyName: string, propertyValue: string }} options
 */
export function init({ editor, propertyName, propertyValue }) {
  destroy({ editor });

  const rootEl = editor.querySelector("[data-md-root]");
  const rawEl = editor.querySelector("[data-md-raw]");
  const toggleEl = editor.querySelector("[data-md-toggle]");

  if (
    !(rootEl instanceof HTMLElement) ||
    !(rawEl instanceof HTMLTextAreaElement) ||
    !(toggleEl instanceof HTMLButtonElement)
  ) {
    throw new Error("Markdown inline editor markup is incomplete");
  }

  /** @type {EditorState} */
  const state = {
    editor,
    propertyName,
    value: propertyValue ?? "",
    mode: "editor",
    crepe: undefined,
    pending: undefined,
    timer: undefined,
    disposed: false,
    queue: Promise.resolve(),
    abort: new AbortController(),
    rootEl,
    rawEl,
    toggleEl,
  };
  instances.set(editor, state);

  const { signal } = state.abort;

  toggleEl.addEventListener(
    "click",
    () => setMode(state, state.mode === "editor" ? "raw" : "editor"),
    { signal },
  );
  rawEl.addEventListener("input", () => push(state, rawEl.value), { signal });
  // Save/Publish right after typing must not lose the debounced value
  editor.addEventListener("focusout", () => flush(state), { signal });

  render(state);
  enqueue(state, () => mountCrepe(state));
}

/**
 * @param {{ editor: HTMLElement }} options
 */
export function destroy({ editor }) {
  const state = instances.get(editor);
  if (!state) {
    return;
  }

  flush(state);
  state.disposed = true;
  state.abort.abort();
  instances.delete(editor);
  enqueue(state, () => unmountCrepe(state));
}

/**
 * @param {{ editor: HTMLElement }} options
 */
export function dragStart({ editor }) {
  const state = instances.get(editor);
  if (state) {
    flush(state);
  }
}

/**
 * The widget DOM is moved on drop, so the editor view is recreated
 * @param {{ editor: HTMLElement }} options
 */
export function drop({ editor }) {
  const state = instances.get(editor);
  if (!state || state.mode !== "editor") {
    return;
  }

  enqueue(state, async () => {
    await unmountCrepe(state);
    await mountCrepe(state);
  });
}

/**
 * @param {EditorState} state
 * @param {() => Promise<void>} task
 */
function enqueue(state, task) {
  state.queue = state.queue.then(task).catch((err) => {
    console.error("Markdown inline editor error", err);
  });
}

/**
 * @param {EditorState} state
 * @param {'editor' | 'raw'} mode
 */
function setMode(state, mode) {
  if (state.mode === mode) {
    return;
  }

  flush(state);
  state.mode = mode;

  enqueue(state, async () => {
    if (state.disposed) {
      return;
    }

    if (mode === "raw") {
      await unmountCrepe(state);
      state.rawEl.value = state.value;
      render(state);
      state.rawEl.focus();
    } else {
      render(state);
      await mountCrepe(state);
    }
  });
}

/**
 * @param {EditorState} state
 */
function render(state) {
  const isRaw = state.mode === "raw";

  state.editor.dataset.mdMode = state.mode;
  state.rootEl.hidden = isRaw;
  state.rawEl.hidden = !isRaw;
  state.toggleEl.setAttribute("aria-pressed", String(isRaw));
  state.toggleEl.textContent = isRaw ? "Rich text" : "Raw markdown";
}

/**
 * @param {EditorState} state
 */
async function mountCrepe(state) {
  if (state.disposed || state.crepe || state.mode !== "editor") {
    return;
  }

  const source = state.value;

  const crepe = new Crepe({
    root: state.rootEl,
    defaultValue: source,
    features: {
      [Crepe.Feature.ImageBlock]: false,
      [Crepe.Feature.Latex]: false,
    },
    featureConfigs: {
      [Crepe.Feature.CodeMirror]: {
        languages: getCodeMirrorLanguages(),
        renderLanguage: (language, selected) => {
          const displayName =
            language.toLowerCase() === "csharp" ? "C#" : language;

          return `${displayName} ${selected ? "✅" : ""}`;
        },
      },
      [Crepe.Feature.Placeholder]: {
        text: "Type markdown content...",
      },
    },
  });

  crepe.editor.remove(remarkPreserveEmptyLinePlugin);

  /**
   * Milkdown re-serializes markdown on load, which can differ from the stored
   * source (ex: Markdig-only syntax). When the document matches what was
   * loaded, the original source is kept so a no-op edit changes nothing.
   * @type {string | undefined}
   */
  let baseline = undefined;

  crepe.on((listener) => {
    listener.markdownUpdated((_ctx, markdown, prevMarkdown) => {
      if (baseline === undefined || markdown === prevMarkdown) {
        return;
      }

      push(state, markdown === baseline ? source : markdown.trim());
    });
  });

  await crepe.create();

  baseline = crepe.getMarkdown();
  state.crepe = crepe;
  state.editor.dataset.mdReady = "true";
}

/**
 * @param {EditorState} state
 */
async function unmountCrepe(state) {
  const crepe = state.crepe;
  if (!crepe) {
    return;
  }

  state.crepe = undefined;
  delete state.editor.dataset.mdReady;
  await crepe.destroy();
  state.rootEl.replaceChildren();
}

/**
 * Queues a value to be sent to Page Builder
 * @param {EditorState} state
 * @param {string} value
 */
function push(state, value) {
  if (state.disposed || value === state.value) {
    return;
  }

  state.value = value;
  state.pending = value;

  clearTimeout(state.timer);
  state.timer = setTimeout(() => flush(state), UPDATE_DELAY_MS);
}

/**
 * Sends the pending value to Page Builder immediately
 * @param {EditorState} state
 */
function flush(state) {
  clearTimeout(state.timer);
  state.timer = undefined;

  if (state.pending === undefined) {
    return;
  }

  const value = state.pending;
  state.pending = undefined;

  state.editor.dispatchEvent(
    new CustomEvent("updateProperty", {
      detail: {
        name: state.propertyName,
        value,
        // A server re-render would destroy the editor while typing
        refreshMarkup: false,
      },
    }),
  );
}
