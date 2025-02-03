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

function initializeNavbar() {
  document.querySelectorAll(".navbar-nav .nav-item.dropdown").forEach((el) => {
    el.addEventListener("mouseenter", (event) => moveHeaderDropdownBg(event));
    el.addEventListener("mouseleave", () => hideHeaderDropdownBg());
  });

  var navbarNavDropdown = document.getElementById("navbarNavDropdown");
  navbarNavDropdown.addEventListener("hidden.bs.collapse", function (event) {
    document.querySelector("#header").classList.remove("mobile-nav-open");
  });
  navbarNavDropdown.addEventListener("hide.bs.collapse", function (event) {
    document.querySelector("#header").classList.remove("mobile-nav-open");
  });
  navbarNavDropdown.addEventListener("shown.bs.collapse", function (event) {
    document.querySelector("#header").classList.add("mobile-nav-open");
  });
  navbarNavDropdown.addEventListener("show.bs.collapse", function (event) {
    document.querySelector("#header").classList.add("mobile-nav-open");
  });
}

const moveHeaderDropdownBg = (e) => {
  let dropdownBg = document.querySelector(".c-navbar-dropdown-bg");
  let currentDropdown = e.target.querySelector(".dropdown-menu");

  if (!dropdownBg || !currentDropdown) {
    return;
  }

  let mainNavOffset = document
    .querySelector(".c-main-nav")
    .getBoundingClientRect().left;
  dropdownBg.style.opacity = 1;
  dropdownBg.style.transform = `translateX(${
    currentDropdown.getBoundingClientRect().left - mainNavOffset
  }px)`;
  dropdownBg.style.width = `${currentDropdown.clientWidth}px`;
  dropdownBg.style.height = `${currentDropdown.clientHeight}px`;
};
const hideHeaderDropdownBg = () => {
  document.querySelector(".c-navbar-dropdown-bg").style.opacity = 0;
};

const stickyHeader = () => {
  if (window.scrollY > 20) {
    document.querySelector("#header").classList.add("scrolled");
  } else {
    document.querySelector("#header").classList.remove("scrolled");
  }
};

function initialiseCookiesBar() {
  const COOKIE_NAME = "kenticocommunityportal.cookielevelselection";
  const COOKIE_LEVEL_NAME = "kenticocommunityportal.cookieconsentlevel";
  const cookieBar = document.querySelector(".js-xpcookiebanner");
  const header = document.querySelector(".js-header");

  const cookieValue = getCookie(COOKIE_NAME);
  const cookieLevelValue = getCookie(COOKIE_LEVEL_NAME);

  if (!cookieBar || cookieValue === "true" || cookieLevelValue === "4") {
    return;
  }

  const cookieBarCtas = document.querySelectorAll(".js-close-cookie");

  cookieBarCtas.forEach(function (elem) {
    elem.addEventListener("click", () => {
      cookieBar.classList.remove("is-active");
      header?.classList.remove("js-header--xpcookiebanner-is-active");
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
    document.getElementById("notificationBell"),
    document.getElementById("notificationBellMobile"),
  ];
  const notificationTrayEl = document.getElementById("notificationTray");
  const closeTrayEl = document.getElementById("notificationsClose");
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
