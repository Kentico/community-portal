export default function setup() {
  stickyHeader();
  initializeNavbar();
  initializeNotifications();
  initialiseCookiesBar();
  initialiseCookiePreferences();

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

function initialiseCookiesBar() {
  const COOKIE_SELECTION_LEVEL_NAME =
    "kenticocommunityportal.cookielevelselection";
  const COOKIE_CONSENT_LEVEL_NAME = "kenticocommunityportal.cookieconsentlevel";
  const cookieBar = document.querySelector("[xp-cookie-banner]");

  const cookieValue = getCookie(COOKIE_SELECTION_LEVEL_NAME);
  const cookieLevelValue = getCookie(COOKIE_CONSENT_LEVEL_NAME);

  if (!cookieBar || cookieValue === "true" || cookieLevelValue === "4") {
    return;
  }

  const cookieBarCtas = document.querySelectorAll("[cookie-cta]");

  cookieBarCtas.forEach(function (elem) {
    elem.addEventListener("click", () => {
      cookieBar.classList.remove("is-active");
    });
  });
}

function getCookie(cname) {
  let name = cname + "=";
  let decodedCookie = decodeURIComponent(document.cookie);
  let ca = decodedCookie.split(";");
  for (let i = 0; i < ca.length; i++) {
    let c = ca[i];
    while (c.charAt(0) == " ") {
      c = c.substring(1);
    }
    if (c.indexOf(name) == 0) {
      return c.substring(name.length, c.length);
    }
  }
  return "";
}

function initialiseCookiePreferences() {
  const options = document.querySelectorAll(".cookie-preferences__option");
  const rangeInput = document.querySelector("#CookieLevelSelected");

  // Setup options
  for (let i = 0; i < options.length; i++) {
    const option = options[i];
    option.addEventListener(
      "click",
      function () {
        selectOption(option);
      },
      false,
    );
    option.style.top =
      (300 / (options.length - 1)) * i - options.length * (i * 2) + "px";
  }

  if (rangeInput) {
    // Setup range input
    rangeInput.addEventListener("input", changeRange, false);

    // Init range fill
    changeRange();
  }
}

// Select range option based on option click
function selectOption(option) {
  const rangeInput = document.querySelector("#CookieLevelSelected");
  const optionValue = option.getAttribute("data-value");
  rangeInput.value = optionValue;
  changeRange();
}

// Fill proper space in range input
function changeRange() {
  const rangeInput = document.querySelector("#CookieLevelSelected");
  const rangeFill = document.querySelector("#range-fill");
  let percent = (parseInt(rangeInput.value) - 1) * 33;
  if (percent < 5) percent += 5;

  rangeFill.style.width = percent + "%";
}

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
