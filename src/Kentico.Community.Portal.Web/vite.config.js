import { defineConfig } from "vite";
import { spawn } from "child_process";
import fs from "fs";
import path from "path";

import appsettings from "./appsettings.json";
import appsettingsDev from "./appsettings.Development.json";

import * as process from "process";

const scssPattern = /\.scss$/;
const imagePattern = /\.(png|jpe?g|gif|svg|webp|avif)$/;
const fontPattern = /\.(woff|woff2|ttf)$/;

const { cert, key } = getDotnetCertPaths();

export default defineConfig(async ({ mode }) => {
  // Skip cert/key creation for prod builds or if the cert/key exist
  // See: https://github.com/dotnet/aspnetcore/issues/58330#issuecomment-2423338006

  // Ensure the certificate and key exist
  if (mode !== "production" && (!fs.existsSync(cert) || !fs.existsSync(key))) {
    // Wait for the certificate to be generated
    console.info("Creating aspnet core dev-certs");
    await new Promise((resolve) => {
      spawn(
        "dotnet",
        [
          "dev-certs",
          "https",
          "--export-path",
          cert,
          "--format",
          "Pem",
          "--no-password",
        ],
        { stdio: "inherit" },
      ).on("exit", (code) => {
        resolve();
        if (code) {
          process.exit(code);
        }
      });
    });
  } else {
    console.info("Skipping dev certificates creation");
  }

  /** @type {import('vite').UserConfig} */
  const config = {
    appType: "custom",
    root: "./Client",
    // See https://github.com/Eptagone/Vite.AspNetCore/wiki#how-to-configure-a-subfolder-as-output-for-my-vite-assets
    base: "/dist/",
    publicDir: "",
    css: {
      preprocessorOptions: {
        scss: {
          /** https://sass-lang.com/documentation/breaking-changes/import/ */
          silenceDeprecations: [
            "mixed-decls",
            "color-functions",
            "import",
            "global-builtin",
            "legacy-js-api",
          ],
        },
      },
    },
    build: {
      manifest: appsettings.Vite.Manifest ?? "manifest.json",
      assetsDir: "",
      emptyOutDir: true,
      outDir: "../wwwroot/dist",
      // CSS sourcemaps are not currently supported by Vite https://github.com/vitejs/vite/issues/2830
      sourcemap: true,
      rollupOptions: {
        input: [
          /**
           * We have explicity entrypoints so we can create small esmodule outputs
           * which can be used with dynamic imports
           * Example: const mod = await import('/path/file.js');
           *
           * Having a separate scss entrypoint also prevents a flash of unstyled content
           * (FOUC) on page navigations. See: https://github.com/Eptagone/Vite.AspNetCore/issues/50
           */
          "Client/js/main.js",
          "Client/js/q-and-a.js",
          "Client/js/recaptcha.js",
          "Client/styles/style.scss",
        ],
        output: {
          preserveModules: false,
        },
        preserveEntrySignatures: "strict",
        external: (url, id) => {
          /**
           * We treat these static assets as external files since they
           * are also used by the ASPNET Core application.
           * We could expand this in the future to also exclude /getmedia/
           * paths if we needed to use those in our SCSS
           */
          return (
            scssPattern.test(id) &&
            (fontPattern.test(url) || imagePattern.test(url))
          );
        },
      },
    },
    server: {
      port: appsettingsDev.Vite.Server.Port,
      strictPort: appsettingsDev.Vite.Server.Https,
      https: appsettingsDev.Vite.Server.Https && {
        cert,
        key,
      },
      hmr: {
        host: "localhost",
        clientPort: appsettingsDev.Vite.Server.Port,
      },
    },
  };

  return config;
});

/**
 * Retrieves the ASP.NET Core dev certificate paths
 */
function getDotnetCertPaths() {
  const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ""
      ? `${process.env.APPDATA}/ASP.NET/https`
      : `${process.env.HOME}/.aspnet/https`;

  const certificateName = process.env.npm_package_name;

  const cert = path.join(baseFolder, `${certificateName}.pem`);
  const key = path.join(baseFolder, `${certificateName}.key`);

  return { cert, key };
}
