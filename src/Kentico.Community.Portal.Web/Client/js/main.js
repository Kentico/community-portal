import "htmx.org";
import Alpine from "alpinejs";
import setupBootstrap from "./bootstrap-customization";
import setupNavigation from "./navigation";
import { registerAnswerListComponent } from "../../Features/QAndA/answerListComponent";
import { registerTableOfContentsComponent } from "../../Features/Blog/tableOfContentsComponent";

window.asyncReady(() => {
  setupBootstrap();
  setupNavigation();
  registerAnswerListComponent(Alpine);
  registerTableOfContentsComponent(Alpine);

  window.Alpine = Alpine;
  Alpine.start();
});
