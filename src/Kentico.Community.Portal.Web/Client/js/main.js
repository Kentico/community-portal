import "htmx.org";
import Alpine from "alpinejs";
import setupBootstrap from "./bootstrap-customization";
import setupNavigation from "./navigation";
import { registerAnswerListComponent } from "../../Features/QAndA/answerListComponent";
import { registerTableOfContentsComponent } from "../../Features/Blog/tableOfContentsComponent";
import { registerCarouselScrollComponent } from "../../Features/Carousel/carouselScrollComponent";

window.asyncReady(() => {
  setupBootstrap();
  setupNavigation();
  registerAnswerListComponent(Alpine);
  registerTableOfContentsComponent(Alpine);
  registerCarouselScrollComponent(Alpine);

  window.Alpine = Alpine;
  Alpine.start();
});
