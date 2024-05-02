import setupBootstrap from "./features/bootstrap-customization";
import setupSearch from "./features/search";
import setupNavigation from "./features/navigation";

window.asyncReady(() => {
  setupBootstrap();
  setupSearch();
  setupNavigation();
});
