/**
 * Integrations hub search and type filter.
 *
 * Entries are server-rendered and stay in the DOM (crawlers, ItemList JSON-LD, #anchors).
 * This only hides non-matching entries, so one component replaces per-entry state.
 *
 * - Search terms are AND-ed; each must appear in an entry's title, type,
 *   author, or description (case and accent insensitive). Order does not matter.
 * - Type filter composes with search.
 * - `?q=` and `?type=` restore state and are kept in sync for sharing.
 * - An `#extension-*` anchor always wins: a hidden target clears the filters.
 *
 * Markup contract: entries are `[data-type]` children of `#integrationsList`
 * with `x-show="matches($el)"`. Text inside `[data-search-skip]` is not searched.
 *
 * @param {import("alpinejs").Alpine} Alpine
 */
export function registerIntegrationsHubComponent(Alpine) {
  Alpine.data("integrationsHub", () => {
    /** Not reactive - built once from the server-rendered markup */
    /** @type {Map<Element, { type: string, text: string }>} */
    const index = new Map();

    return {
      query: "",
      type: "",

      init() {
        for (const el of this.$el.querySelectorAll(
          "#integrationsList > [data-type]",
        )) {
          indexEntry(index, el);
        }

        // A deep link to an entry wins over saved filters, so the entry stays visible
        const params = new URLSearchParams(window.location.search);
        if (targetEntry(index)) {
          this.syncURL();
        } else {
          const type = params.get("type") ?? "";
          this.query = params.get("q") ?? "";
          this.type = [...index.values()].some((e) => e.type === type)
            ? type
            : "";
        }

        this.$watch("query", () => this.syncURL());
        this.$watch("type", () => this.syncURL());
      },

      get total() {
        return index.size;
      },

      get terms() {
        return normalize(this.query).split(/\s+/).filter(Boolean);
      },

      get isFiltered() {
        return this.terms.length > 0 || this.type !== "";
      },

      get visibleCount() {
        let count = 0;
        for (const entry of index.values()) {
          if (this.isMatch(entry)) count++;
        }
        return count;
      },

      get summary() {
        return this.isFiltered
          ? `${this.visibleCount} of ${this.total} extensions`
          : `${this.total} extensions`;
      },

      /** @param {Element} el */
      matches(el) {
        return this.isMatch(index.get(el) ?? indexEntry(index, el));
      },

      /** @param {{ type: string, text: string }} entry */
      isMatch(entry) {
        if (this.type && entry.type !== this.type) {
          return false;
        }
        const terms = this.terms;
        return terms.every((t) => entry.text.includes(t));
      },

      reset() {
        this.query = "";
        this.type = "";
      },

      /** Reveal and scroll to an `#anchor` entry hidden by the filters */
      revealHash() {
        const el = targetEntry(index);
        if (!el || this.matches(el)) {
          return;
        }
        this.reset();
        // x-show re-displays entries after the tick; scroll once laid out
        this.$nextTick(() =>
          requestAnimationFrame(() => el.scrollIntoView()),
        );
      },

      syncURL() {
        const url = new URL(window.location.href);
        setParam(url.searchParams, "q", this.query.trim());
        setParam(url.searchParams, "type", this.type);
        window.history.replaceState(window.history.state, "", url);
      },
    };
  });
}

/**
 * @param {Map<Element, { type: string, text: string }>} index
 * @param {Element} el
 */
function indexEntry(index, el) {
  const article = el.firstElementChild ?? el;
  const text = Array.from(article.children)
    .filter((c) => !c.hasAttribute("data-search-skip"))
    .map((c) => c.textContent)
    .join(" ");
  const entry = {
    type: el.getAttribute("data-type") ?? "",
    text: normalize(text),
  };
  index.set(el, entry);
  return entry;
}

/**
 * The indexed entry the URL hash points at, if any
 * @param {Map<Element, unknown>} index
 */
function targetEntry(index) {
  const id = decodeURIComponent(window.location.hash.slice(1));
  if (!id) {
    return null;
  }
  const el = document.getElementById(id);
  return el && index.has(el) ? el : null;
}

/** Lowercase, accents removed, whitespace collapsed */
function normalize(/** @type {string} */ value) {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/\s+/g, " ")
    .trim();
}

function setParam(
  /** @type {URLSearchParams} */ params,
  /** @type {string} */ name,
  /** @type {string} */ value,
) {
  if (value) {
    params.set(name, value);
  } else {
    params.delete(name);
  }
}
