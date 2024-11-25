import postcssPresentEnv from "postcss-preset-env";
import { purgeCSSPlugin } from "@fullhuman/postcss-purgecss";

/** @type { import('postcss-load-config').ConfigFn} */
const config = ({ env }) => ({
  plugins: [
    // https://purgecss.com/configuration.html
    env === "production" &&
      purgeCSSPlugin({
        content: ["./**/*.cshtml"],

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
            // Member badges tag helper
            "c-tag_badge",
            "rounded-circle",
            "border",
            "align-text-top",
            "border-1",
          ],
          greedy: [/form-/, /milkdown/, /ProseMirror/],
        },
      }),
    // https://www.npmjs.com/package/postcss-preset-env
    env === "production" && postcssPresentEnv(),
  ],
});

export default config;
