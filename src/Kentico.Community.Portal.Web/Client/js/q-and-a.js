export function initQAndA({ editorElemID, formElemID = null, formType = "" }) {
  const editorSelector = `#${editorElemID}`;
  const content = document.querySelector(editorSelector);

  if (!(content instanceof HTMLTextAreaElement)) {
    throw new Error(`No editor element found at [${editorSelector}]`);
  }

  const defaultContent =
    formType === "Question"
      ? `[describe your problem or question]

---

**Environment**
- Xperience by Kentico version: [27.0.1]
- .NET version: [6]
- Deployment environment: [SaaS|Azure|VM]
- Link to relevant [Xperience by Kentico documentation](https://docs.xperience.io/xp)`
      : "[detail your answer]";

  const excludedToolbarItems = [
    "h1",
    "h2",
    "h3",
    "h4",
    "goto-line",
    "reference-link",
    "emoji",
    "datetime",
    "html-entities",
    "watch",
    "preview",
    "help",
    "info",
  ];

  const _editor = editormd(`editor_${editorElemID}`, {
    height: 580,
    autoFocus: true, // Disable to prevent viewport scroll to newly created editor UI
    toolbarIcons: function () {
      return editormd.toolbarModes.full.filter(
        (m) => !excludedToolbarItems.includes(m)
      );
    },
    markdown: content.value || defaultContent,
    path: "/vendor/js/editor/libs/",
    onfullscreen: function () {
      this.editor.css("border-radius", 0).css("z-index", 40);
    },
    onfullscreenExit: function () {
      this.editor.css({
        zIndex: 10,
        border: "none",
        "border-radius": "5px",
      });

      this.resize();
    },
  });
}
