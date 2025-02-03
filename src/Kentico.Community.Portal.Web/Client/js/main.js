import "htmx.org";
import Alpine from "alpinejs";
import setupBootstrap from "./features/bootstrap-customization";
import setupNavigation from "./features/navigation";
import { registerAnswerListComponent } from "../../Features/QAndA/answerListComponent";

window.asyncReady(() => {
  setupBootstrap();
  setupNavigation();
  registerAnswerListComponent(Alpine);

  window.Alpine = Alpine;
  Alpine.start();
});
