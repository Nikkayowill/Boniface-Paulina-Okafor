import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Bundles the React landing page (client/landing) into a single committed
// wwwroot/js/landing.js, the same "compile source -> commit output" pattern
// already used for Tailwind (wwwroot/css/tailwind.input.css -> tailwind.css).
// There is no dev server here on purpose: Views/Home/Index.cshtml loads the
// built file directly, same as every other <script> on the site, so the app
// stays a single non-headless ASP.NET Core deployment. Run `npm run watch:landing`
// while editing.
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "wwwroot/js",
    emptyOutDir: false,
    sourcemap: false,
    rollupOptions: {
      input: fileURLToPath(new URL("./client/landing/main.jsx", import.meta.url)),
      output: {
        entryFileNames: "landing.js",
        chunkFileNames: "landing-[name].js",
        assetFileNames: "landing-[name][extname]",
      },
    },
  },
});
