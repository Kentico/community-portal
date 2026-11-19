export default function setup() {
  stickyHeader();
  initializeNavbar();
  initializeNotifications();

  window.onresize = () => {
    stickyHeader();
  };
  window.onscroll = () => {
    stickyHeader();
  };
}

const headerEl = document.querySelector("#header");
const navbarNavDropdownEl = document.querySelector("#navbarNavDropdown");

function initializeNavbar() {
  const openClass = "mobile-nav-open";
  navbarNavDropdownEl.addEventListener("hidden.bs.collapse", function () {
    headerEl.classList.remove(openClass);
  });
  navbarNavDropdownEl.addEventListener("hide.bs.collapse", function () {
    headerEl.classList.remove(openClass);
  });
  navbarNavDropdownEl.addEventListener("shown.bs.collapse", function () {
    headerEl.classList.add(openClass);
  });
  navbarNavDropdownEl.addEventListener("show.bs.collapse", function () {
    headerEl.classList.add(openClass);
  });
}

const stickyHeader = () => {
  if (window.scrollY > 20) {
    headerEl.classList.add("scrolled");
  } else {
    headerEl.classList.remove("scrolled");
  }
};

function initializeNotifications() {
  const notificationBellEls = [
    document.querySelector("#notificationBell"),
    document.querySelector("#notificationBellMobile"),
  ];
  const notificationTrayEl = document.querySelector("#notificationTray");
  const closeTrayEl = document.querySelector("#notificationsClose");
  /**
   * @type { HTMLSpanElement }
   */
  const notificationCountEls = document.querySelectorAll(
    "[notification-count]",
  );

  for (const bellEl of notificationBellEls) {
    bellEl.addEventListener("click", () => {
      notificationTrayEl.classList.toggle("open");
      for (const countEl of notificationCountEls) {
        countEl.classList.add("d-none");
        countEl.innerHTML = "";
        bellEl.classList.remove("btn-outline-secondary");
      }
    });
  }

  closeTrayEl.addEventListener("click", () => {
    notificationTrayEl.classList.remove("open");
  });

  const notificationCount = parseInt(
    notificationTrayEl.getAttribute("notification-total"),
    0,
  );

  if (notificationCount > 0) {
    for (const countEl of notificationCountEls) {
      countEl.innerHTML = notificationCount;
      countEl.classList.remove("d-none");
      for (const bellEl of notificationBellEls) {
        bellEl.classList.add("btn-outline-secondary");
      }
    }
  }
}
