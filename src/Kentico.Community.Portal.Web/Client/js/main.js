import "htmx.org";
import Alpine from "alpinejs";
import setupBootstrap from "./bootstrap-customization";
import setupNavigation from "./navigation";
import { registerAnswerListComponent } from "../../Features/QAndA/answerListComponent";

window.asyncReady(() => {
  setupBootstrap();
  setupNavigation();
  registerAnswerListComponent(Alpine);

  window.Alpine = Alpine;
  Alpine.start();
});
