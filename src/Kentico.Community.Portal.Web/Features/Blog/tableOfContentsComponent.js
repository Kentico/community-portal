/**
 * Alpine.js component that builds a client-side Table of Contents
 * by scanning [data-toc-heading] elements inside [role="main"].
 * Rendered as a fixed floating panel so it never causes layout shift.
 *
 * @param {import("alpinejs").Alpine} Alpine
 */
export function registerTableOfContentsComponent(Alpine) {
  Alpine.data("tableOfContents", () => ({
    /** @type {{ id: string, text: string, level: string }[]} */
    headings: [],
    isOpen: false,
    activeId: "",

    init() {
      const articleBody = document.querySelector('[role="main"]');
      if (!articleBody) {
        return;
      }

      const headingEls = articleBody.querySelectorAll("[data-toc-heading]");
      this.headings = Array.from(headingEls).map((el) => ({
        id: el.id,
        text: el.textContent.trim(),
        level: el.tagName.toLowerCase(),
      }));

      if (this.headings.length === 0) {
        return;
      }

      this._setupScrollObserver(headingEls);
    },

    get hasHeadings() {
      return this.headings.length > 0;
    },

    toggle() {
      this.isOpen = !this.isOpen;
    },

    close() {
      this.isOpen = false;
    },

    /**
     * Highlights the TOC entry whose section is currently in view.
     * @param {NodeList} headingEls
     */
    _setupScrollObserver(headingEls) {
      const observer = new IntersectionObserver(
        (entries) => {
          for (const entry of entries) {
            if (entry.isIntersecting) {
              this.activeId = entry.target.id;
              break;
            }
          }
        },
        { rootMargin: "0px 0px -70% 0px", threshold: 0 },
      );

      headingEls.forEach((el) => observer.observe(el));
    },
  }));
}
