/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        void: "#02040a",
        stellar: "#f59e0b",
        pulsar: "#00f0ff",
        horizon: "#8b5cf6",
        plasma: "#ec4899",
      },
    },
  },
  plugins: [],
}
