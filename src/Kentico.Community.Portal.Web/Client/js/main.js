import setupBootstrap from "./features/bootstrap-customization";
import setupSearch from "./features/search";
import setupNavigation from "./features/navigation";

document.addEventListener("DOMContentLoaded", function () {
  setupBootstrap();
  setupSearch();
  setupNavigation();
});
