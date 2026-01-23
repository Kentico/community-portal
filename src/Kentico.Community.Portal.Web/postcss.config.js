import postcssPresentEnv from "postcss-preset-env";
import { purgeCSSPlugin } from "@fullhuman/postcss-purgecss";

/** @type { import('postcss-load-config').ConfigFn} */
const config = ({ env }) => ({
  plugins: [
    // https://purgecss.com/configuration.html
    env === "production" &&
      purgeCSSPlugin({
        content: ["./**/*.cshtml", "./**/TagHelpers/*.cs"],

        safelist: {
          standard: [
            // .note comes from markdown :::note syntax
            "note",
            // Syntax highlighting for inline code blocks
            "code",
            // Responsive embeds for youtube videos
            "embed-container",
            // Used in blog posts and Q&A
            "blockquote",
            "figure",
            // For custom 404 content
            "error-hero",
            // HTMX dynamic classes
            "htmx-request",
            // bootstrap form validation
            "is-valid",
            "is-invalid",
            // bootstrap tooltip
            "tooltip",
            "tooltip-inner",
            "tooltip-arrow",
            "bs-tooltip-auto",
            "fade",
            "show",
          ],
          greedy: [
            // Bootstrap forms
            /form-/,
            // Milkdown editor
            /milkdown/,
            // Syntax highlighting in Milkdown editor
            /ProseMirror/,
          ],
        },
      }),
    // https://www.npmjs.com/package/postcss-preset-env
    env === "production" && postcssPresentEnv(),
  ],
});

export default config;
