// import { defineConfig } from "vite";
// import react from "@vitejs/plugin-react";
// import fs from "fs";

// // https://vite.dev/config/
// export default defineConfig(({command}) => ({
//   plugins: [react()],

//   server: 
//     command === "serve"
//     ? {
//     https: {
//       key: fs.readFileSync("../cert.key"),
//       cert: fs.readFileSync("../cert.crt"),
//     },
//     port: 5173,
//   }
//   :undefined,
// }));

import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
  },
});
