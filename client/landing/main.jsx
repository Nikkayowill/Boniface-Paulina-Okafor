import { createRoot } from "react-dom/client";
import App from "./App.jsx";

function readLandingData() {
  const node = document.getElementById("landing-data");

  if (!node || !node.textContent) {
    return null;
  }

  try {
    return JSON.parse(node.textContent);
  } catch {
    return null;
  }
}

const container = document.getElementById("landing-root");
const data = readLandingData();

if (container && data) {
  createRoot(container).render(<App data={data} />);
}
