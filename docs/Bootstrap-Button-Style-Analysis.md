# Bootstrap Button Style Analysis

**Kentico Community Portal - Comprehensive Button Inventory**

## Overview

This document catalogs all Bootstrap button class variations used across the
application, their locations, SCSS customizations, and predicted color rendering
for each state.

---

## 1. Button Class Variations Catalog

### Primary Button Variants

| Class Combination             | Description                  | Usage Count   |
| ----------------------------- | ---------------------------- | ------------- |
| `btn btn-primary`             | Standard primary button      | ~35 instances |
| `btn btn-primary btn-lg`      | Large primary button         | 4 instances   |
| `btn btn-primary btn-sm`      | Small primary button         | 2 instances   |
| `btn btn-primary w-100`       | Full-width primary button    | 8 instances   |
| `btn btn-primary btn-lighter` | Lighter weight primary       | 4 instances   |
| `btn btn-primary-light`       | Custom light primary variant | 1 instance    |

### Outline Primary Variants

| Class Combination                | Description                | Usage Count   |
| -------------------------------- | -------------------------- | ------------- |
| `btn btn-outline-primary`        | Standard outline primary   | ~15 instances |
| `btn btn-outline-primary btn-sm` | Small outline primary      | 4 instances   |
| `btn btn-outline-primary btn-lg` | Large outline primary      | 2 instances   |
| `btn btn-outline-primary w-100`  | Full-width outline primary | 3 instances   |

### Secondary Button Variants

| Class Combination                  | Description                | Usage Count   |
| ---------------------------------- | -------------------------- | ------------- |
| `btn btn-secondary`                | Standard secondary button  | 2 instances   |
| `btn btn-secondary btn-social`     | Social media buttons       | 3 instances   |
| `btn btn-outline-secondary`        | Standard outline secondary | ~12 instances |
| `btn btn-outline-secondary btn-sm` | Small outline secondary    | 2 instances   |

### Danger & Warning Variants

| Class Combination               | Description                | Usage Count |
| ------------------------------- | -------------------------- | ----------- |
| `btn btn-outline-danger`        | Delete/destructive actions | 3 instances |
| `btn btn-sm btn-outline-danger` | Small outline danger       | 3 instances |
| `btn btn-outline-warning`       | Cancel actions             | 4 instances |

### Custom Variants

| Class Combination           | Description                              | Usage Count |
| --------------------------- | ---------------------------------------- | ----------- |
| `btn-invisible-primary`     | Transparent primary (defined but unused) | 0 instances |
| `btn position-relative p-2` | Notification bell button                 | 2 instances |

### Close Button Variants

| Class Combination           | Description           | Usage Count |
| --------------------------- | --------------------- | ----------- |
| `btn-close`                 | Standard close button | Multiple    |
| `btn-close btn-close-white` | White close button    | 1 instance  |
| `btn-close btn-close-dark`  | Dark close button     | 2 instances |

---

## 2. Element-to-File Mapping

### Q&A Feature (14 files)

#### `_FiltersMobile.cshtml`

- Line 3:
  `btn btn-outline-primary align-self-center d-lg-none text-uppercase w-100`
  - Mobile filter toggle button
  - Context: Collapsible filter panel trigger

#### `_Sidebar.cshtml`

- `btn btn-outline-primary w-100 d-inline-block`
  - "Ask New Question" CTA link

#### `Search.cshtml`

- `btn btn-outline-primary w-100 d-inline-block`
  - "Ask New Question" CTA link
- `btn btn-primary text-uppercase`
  - Search form submit button

#### `_QuestionActions.cshtml`

- `btn btn-sm btn-outline-secondary`
  - Edit question button (author only)
- `btn btn-sm btn-outline-danger`
  - Delete question button (author only)

#### `_AnswerActions.cshtml`

- `btn btn-outline-primary btn-sm text-wrap`
  - "Mark as answer" button (question author)
- `btn btn-sm btn-outline-secondary`
  - Edit answer button (answer author)
- `btn btn-sm btn-outline-danger`
  - Delete answer button (answer author)

#### `_SignInPrompt.cshtml`

- `btn btn-primary`
  - "Sign in" link for unauthenticated users
  - Context: Prompts for answer/vote actions

#### `_QuestionForm.cshtml`

- `btn btn-primary btn-lighter px-5 mt-4`
  - Submit question button
  - Used with `xpc-loading-button` tag helper
- `btn btn-outline-warning px-5 mt-4`
  - Cancel button

#### `_AnswerForm.cshtml`

- `btn btn-primary btn-lighter px-5 mt-4`
  - Submit answer button (2 instances: new & edit)
  - Used with `xpc-loading-button` tag helper
- `btn btn-outline-warning px-5 mt-4`
  - Cancel button (2 instances: new & edit)

#### `_AnswerFormPrompt.cshtml`

- `btn btn-primary btn-lighter px-5 mt-4`
  - "Answer this question" button trigger

#### `_TagInput.cshtml`

- `btn-close btn-close-white ms-2`
  - Remove tag button (appears on each selected tag)

### Authentication & Registration (8 files)

#### `Login.cshtml`

- `btn btn-outline-secondary mb-4`
  - "Register" link
- `btn btn-primary w-100`
  - Login submit button
  - Used with `xpc-loading-button` tag helper

#### `Register.cshtml`

- `btn btn-outline-secondary mb-4`
  - "Sign in" link
- `btn btn-primary w-100`
  - Register submit button
  - Used with `xpc-loading-button` tag helper

#### `ConfirmUser.cshtml` & `ConfirmUserChange.cshtml`

- `btn btn-primary mt-3`
  - Email confirmation submit buttons
  - Used with `xpc-loading-button` tag helper

### Password Recovery (4 files)

#### `ForgotPassword.cshtml`

- `btn btn-outline-secondary mb-4`
  - "Sign in" link
- `btn btn-primary w-100`
  - Submit recovery request button
  - Used with `xpc-loading-button` tag helper

#### `ResetPassword.cshtml`

- `btn btn-outline-secondary mb-4`
  - "Sign in" link
- `btn btn-primary w-100`
  - Submit new password button
  - Used with `xpc-loading-button` tag helper

#### `ResetPasswordConfirmation.cshtml`

- `btn btn-primary`
  - "Sign in" link (after successful reset)

#### `ForgotPasswordConfirmation.cshtml`

- `btn btn-primary w-100`
  - "Request recovery email" link
- `btn btn-outline-secondary mb-4`
  - "Sign in" link

### Account Management (4 files)

#### `Profile.cshtml`

- `btn btn-primary mt-3`
  - Update profile button
  - Used with `xpc-loading-button` tag helper

#### `Password.cshtml`

- `btn btn-primary mt-3`
  - Update password button
  - Used with `xpc-loading-button` tag helper

#### `Badges.cshtml`

- `btn btn-primary`
  - Update badges button
  - Used with `xpc-loading-button` tag helper

#### `Avatar.cshtml`

- `btn btn-outline-secondary`
  - "Attach file" label for file input
- `btn btn-primary`
  - Update image button
  - Used with `xpc-loading-button` tag helper

### Blog Feature (4 files)

#### `Search.cshtml` (Blog)

- `btn btn-primary text-uppercase`
  - Search submit button

#### `_FiltersMobile.cshtml` (Blog)

- `btn btn-outline-primary align-self-center d-lg-none text-uppercase w-100`
  - Mobile filter toggle button

#### `_CallToActionSection.cshtml`

- `btn btn-secondary`
  - "Start/Join discussion" link
- `btn btn-secondary btn-social border-0 bg-twitter`
  - Twitter share button
- `btn btn-secondary btn-social border-0 bg-facebook`
  - Facebook share button
- `btn btn-secondary btn-social border-0 bg-linkedin`
  - LinkedIn share button

#### `_BlogPostCard.cshtml`

- `btn btn-outline-primary btn-sm d-inline-flex align-items-center`
  - "Read more" link

### Support (1 file)

#### `Support.cshtml`

- `btn btn-outline-secondary`
  - "Attach file" label for file input
- `btn btn-primary btn-lighter px-5 mt-4 uppercase`
  - Submit support form button
  - Used with `xpc-loading-button` tag helper

### Navigation (2 files)

#### `_Navigation.cshtml`

- `btn position-relative p-2 mx-3`
  - Notification bell buttons (mobile & desktop, 2 instances)
- `btn btn-primary text-uppercase`
  - "Sign in" links (desktop & mobile navbar, 2 instances)
- `btn btn-primary btn-lg text-uppercase`
  - "Sign in" link (mobile navbar expansion)
- `btn dropdown-toggle d-md-none`
  - Dropdown toggle (mobile)
- `btn btn-primary text-uppercase`
  - "My Account" links (2 instances)
- `btn btn-outline-secondary text-uppercase w-100`
  - Logout buttons (2 instances)

#### `_NotificationTray.cshtml`

- `btn-close btn-close-dark`
  - Close tray buttons (2 instances)

### Page Builder Widgets (5 files)

#### `CtaWidget.cshtml`

- `btn btn-primary btn-lg px-4 d-inline`
  - CTA widget button

#### `FileDownloadWidget.cshtml`

- `btn btn-primary btn-lg`
  - File download button (when design = Button)

#### `FormWidget.cshtml`

- `btn btn-primary btn-sm`
  - CTA links in form submission messages

#### `PollWidget.cshtml`

- `btn btn-primary btn-sm`
  - "Sign in" link for unauthenticated poll voters

#### `_CookiePreferences.cshtml`

- `btn btn-primary mt-4 cookie-preferences__button`
  - Save preferences button

### Data Collection (1 file)

#### `_CookieBanner.cshtml`

- `btn btn-primary-light xpcookiebanner__cta text-uppercase`
  - "Accept All" button

### Error Pages (2 files)

#### `500.cshtml`

- `btn btn-primary btn-lg`
  - "Send us some feedback" link

#### `404.cshtml`

- `btn btn-primary btn-lg`
  - "Search our Q&A discussions" link
- `btn btn-outline-primary btn-lg`
  - "Or return to the home page" link

### Layout (1 file)

#### `_Layout.cshtml`

- `btn-close me-2 m-auto`
  - Toast notification close button

---

## 3. SCSS Customizations

### Bootstrap Variable Overrides

**File:** `Client/styles/00_core/_variables.scss` (Lines 312-346)

#### Button Sizing

```scss
// Base button sizing
$input-btn-padding-y: 0.75rem;           // 12px vertical padding
$input-btn-padding-x: 1.5rem;            // 24px horizontal padding
$input-btn-font-size: 0.75rem;           // 12px font size
$input-btn-line-height: 1.333;

// Large button sizing
$input-btn-padding-y-lg: 0.75rem;        // 12px vertical padding
$input-btn-padding-x-lg: 2rem;           // 32px horizontal padding
$input-btn-font-size-lg: 0.875rem;       // 14px font size

// Button-specific overrides
$btn-line-height: 1rem;                  // 16px line height
$btn-padding-y-lg: 0.5rem;               // 8px Y padding override
$btn-padding-x-lg: 1.25rem;              // 20px X padding override
$btn-font-weight: 700;                   // Bold weight
```

#### Button Visual Styling

```scss
// Remove all shadows
$btn-box-shadow: none;
$btn-focus-box-shadow: none;
$btn-active-box-shadow: none;

// Remove focus rings
$input-btn-focus-width: 0;
$input-btn-focus-box-shadow: none;

// Border radius - highly rounded
$btn-border-radius: 1.75rem;             // 28px
$btn-border-radius-sm: 1.5rem;           // 24px
$btn-border-radius-lg: 1.75rem;          // 28px
```

#### Button Colors

```scss
// Link button colors (used for btn-link variant)
$btn-link-color: $white;
$btn-link-hover-color: $white;
```

### Color Variable Definitions

**File:** `Client/styles/00_core/_variables.scss` (Lines 203-235)

```scss
// Primary colors
$orange: #f05a22;                        // Brand orange
$orange-dark: #c64300;                   // Darker orange for hover
$orange-opacity: rgba(240, 90, 34, 0.1); // 10% opacity
$orange-opacity-2: rgba(240, 90, 34, 0.3); // 30% opacity
$orange-gray: #d1c4bf;                   // Disabled state

// Semantic color assignments
$primary: $orange;
$primary-dark: $orange-dark;
$primary-opacity: $orange-opacity;
$primary-opacity-2: $orange-opacity-2;
$primary-focus: $orange;
$secondary: $purple;                      // #7f09b7
$disabled: $orange-gray;

// Theme colors map (generates .btn-* classes)
$theme-colors: (
  "primary": $primary,
  "primary-light": transparentize($primary, 0.86), // rgba(240, 90, 34, 0.14)
  "secondary": $secondary,
  "success": $success,                   // #198754
  "info": $info,                         // #00b3fc
  "warning": $warning,                   // #fdb600
  "danger": $danger,                     // #b72929
  "light": $light,                       // #f3f4f5
  "dark": $dark,                         // #231f20
);
```

### Custom Button Styles

**File:** `Client/styles/00_core/_bootstrap-override.scss` (Lines 70-150)

#### Base Button Styles

```scss
.btn {
  letter-spacing: 0.1em;                 // Spaced letters for uppercase
  white-space: nowrap;                   // Prevent wrapping

  &.link-only {
    padding: 0.25rem 0;                  // Minimal padding for text links
  }
}
```

#### Large Button Override

```scss
.btn-lg {
  line-height: 1.5rem;                   // 24px line height

  @include media-breakpoint-up(lg) {
    padding: 0.75rem 2rem;               // Desktop: 12px 32px
  }
}
```

#### Primary Button Customization

```scss
.btn-primary {
  color: $white;

  &:disabled {
    background-color: $disabled;         // #d1c4bf
    border-color: $disabled;
  }
}
```

#### Primary & Outline Primary States

```scss
.btn-primary,
.btn-outline-primary {
  --bs-btn-disabled-color: #fff;         // White text when disabled

  &:hover {
    border-color: $primary-dark;         // #c64300
    background-color: $primary-dark;
    color: $white;
  }

  &:focus {
    background-color: $primary-focus;    // #f05a22
    border-color: $primary-focus;
    color: $white;
  }
}
```

#### Custom Button Variant: Invisible Primary

```scss
.btn-invisible-primary {
  background: transparent;
  border-color: transparent;
  color: $primary;                       // #f05a22

  &:hover {
    background: $primary-opacity;        // rgba(240, 90, 34, 0.1)
    color: $primary;
  }

  &:focus {
    background: $primary-opacity-2;      // rgba(240, 90, 34, 0.3)
    color: $primary;
  }
}
```

#### Custom Button Variant: Primary Light

```scss
.btn-primary-light {
  background-color: transparentize($primary, 0.86); // rgba(240, 90, 34, 0.14)
  border-color: transparent;
  color: $primary;                       // #f05a22

  &:hover,
  &:focus {
    background-color: transparentize($primary, 0.78); // rgba(240, 90, 34, 0.22)
    border-color: transparent;
    color: $primary;
  }
}
```

#### Outline Secondary Override

```scss
.btn-outline-secondary {
  background: $white;                    // White background (non-standard)

  &:hover {
    border-color: $white;                // White border on hover
  }
}
```

#### Custom Button Modifier: Lighter

```scss
.btn {
  &.btn-lighter {
    font-size: 0.875rem;                 // 14px (larger than base)
    font-weight: 400;                    // Normal weight (not bold)
    letter-spacing: 0;                   // No letter spacing
    line-height: 1rem;                   // 16px
    padding: 0.875rem 2rem;              // 14px 32px
  }
}
```

#### Social Button Variant

```scss
.btn {
  &-social {
    width: 2.25rem;                      // 36px fixed width
    height: 2.25rem;                     // 36px fixed height
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 0.5rem;                     // 8px
    color: $white;

    &:hover,
    &:focus {
      color: $white;                     // Maintain white color
    }
  }
}
```

### Component-Specific Button Styles

#### Icon Integration

**File:** `Client/styles/01_component/_icon.scss`

```scss
/* Icons inside buttons get specific sizing */
:where(.btn .icon) {
  --icon-size: 1.25rem;                  // 20px
  margin-top: -3px;                      // Optical alignment
}

:where(.btn-lg .icon) {
  --icon-size: 1.375rem;                 // 22px
  margin-right: 0.25rem;                 // 4px spacing
  margin-top: 0;
}

:where(.btn-social .icon) {
  --icon-size: 1.5rem;                   // 24px
  margin-top: 0;
}
```

#### Link Behavior

**File:** `Client/styles/01_component/_link.scss`

```scss
a.btn:hover {
  text-decoration: none;                 // Remove underline on button links
}
```

#### Cookie Banner Adjustments

**File:** `Client/styles/01_component/_cookie-banner.scss` (Lines 78-80)

```scss
.xpcookiebanner__ctas {
  .btn {
    @include media-breakpoint-up(md) {
      font-size: 14px;                   // Override to 14px on desktop
      padding-left: 16px;
      padding-right: 16px;
    }
  }
}
```

#### Article Footer Button Layouts

**File:** `Client/styles/02_template/_article.scss` (Lines 38-68)

```scss
.t-article {
  &_footer {
    &_btns {
      .btn {
        margin-top: 0.5rem;              // 8px
        width: 100%;                     // Full width on mobile
      }

      @include media-breakpoint-up(sm) {
        .btn {
          width: calc(50% - 1rem);       // Two columns on tablet+
          margin: 0 0.375rem;            // 6px horizontal margin
        }
      }
    }

    &_share {
      .btn {
        margin: 0 0.375rem;              // 6px horizontal margin
      }

      @include media-breakpoint-up(sm) {
        .btn {
          margin: 0 0 0 1rem;            // 16px left margin on tablet+
        }
      }
    }
  }
}
```

---

## 4. Color Prediction Matrix

### 4.1 Primary Button (`btn btn-primary`)

| State        | Border Color | Background Color | Text Color | Notes                         |
| ------------ | ------------ | ---------------- | ---------- | ----------------------------- |
| **Default**  | `#f05a22`    | `#f05a22`        | `#ffffff`  | Standard primary orange       |
| **Hover**    | `#c64300`    | `#c64300`        | `#ffffff`  | Darker orange                 |
| **Focus**    | `#f05a22`    | `#f05a22`        | `#ffffff`  | Returns to primary            |
| **Active**   | `#c64300`    | `#c64300`        | `#ffffff`  | Same as hover                 |
| **Disabled** | `#d1c4bf`    | `#d1c4bf`        | `#ffffff`  | Orange-gray (custom override) |

### 4.2 Outline Primary (`btn btn-outline-primary`)

| State        | Border Color | Background Color | Text Color | Notes                      |
| ------------ | ------------ | ---------------- | ---------- | -------------------------- |
| **Default**  | `#f05a22`    | `transparent`    | `#f05a22`  | Standard outline           |
| **Hover**    | `#c64300`    | `#c64300`        | `#ffffff`  | Fills with darker orange   |
| **Focus**    | `#f05a22`    | `#f05a22`        | `#ffffff`  | Fills with primary orange  |
| **Active**   | `#c64300`    | `#c64300`        | `#ffffff`  | Same as hover              |
| **Disabled** | `#f05a22`    | `transparent`    | `#ffffff`  | Custom white text override |

### 4.3 Primary Light (`btn btn-primary-light`) - Custom Variant

| State        | Border Color  | Background Color          | Text Color         | Notes                     |
| ------------ | ------------- | ------------------------- | ------------------ | ------------------------- |
| **Default**  | `transparent` | `rgba(240, 90, 34, 0.14)` | `#f05a22`          | 14% opacity orange bg     |
| **Hover**    | `transparent` | `rgba(240, 90, 34, 0.22)` | `#f05a22`          | 22% opacity orange bg     |
| **Focus**    | `transparent` | `rgba(240, 90, 34, 0.22)` | `#f05a22`          | Same as hover             |
| **Active**   | `transparent` | `rgba(240, 90, 34, 0.22)` | `#f05a22`          | Same as hover             |
| **Disabled** | `transparent` | `rgba(240, 90, 34, 0.14)` | `#f05a22` (dimmed) | Bootstrap default opacity |

### 4.4 Invisible Primary (`btn-invisible-primary`) - Custom Variant (UNUSED)

| State        | Border Color  | Background Color         | Text Color         | Notes                     |
| ------------ | ------------- | ------------------------ | ------------------ | ------------------------- |
| **Default**  | `transparent` | `transparent`            | `#f05a22`          | Fully transparent         |
| **Hover**    | `transparent` | `rgba(240, 90, 34, 0.1)` | `#f05a22`          | 10% opacity orange bg     |
| **Focus**    | `transparent` | `rgba(240, 90, 34, 0.3)` | `#f05a22`          | 30% opacity orange bg     |
| **Active**   | `transparent` | `rgba(240, 90, 34, 0.3)` | `#f05a22`          | Same as focus             |
| **Disabled** | `transparent` | `transparent`            | `#f05a22` (dimmed) | Bootstrap default opacity |

### 4.5 Secondary Button (`btn btn-secondary`)

| State        | Border Color | Background Color | Text Color         | Notes                             |
| ------------ | ------------ | ---------------- | ------------------ | --------------------------------- |
| **Default**  | `#7f09b7`    | `#7f09b7`        | `#ffffff`          | Purple                            |
| **Hover**    | `#6a0799`    | `#6a0799`        | `#ffffff`          | Bootstrap calculated (15% darker) |
| **Focus**    | `#6a0799`    | `#6a0799`        | `#ffffff`          | Same as hover                     |
| **Active**   | `#640890`    | `#640890`        | `#ffffff`          | Bootstrap calculated (20% darker) |
| **Disabled** | `#7f09b7`    | `#7f09b7`        | `#ffffff` (dimmed) | Bootstrap default behavior        |

### 4.6 Outline Secondary (`btn btn-outline-secondary`)

| State        | Border Color | Background Color | Text Color         | Notes                                       |
| ------------ | ------------ | ---------------- | ------------------ | ------------------------------------------- |
| **Default**  | `#7f09b7`    | `#ffffff`        | `#7f09b7`          | **Custom: white bg instead of transparent** |
| **Hover**    | `#ffffff`    | `#7f09b7`        | `#ffffff`          | **Custom: white border**                    |
| **Focus**    | `#ffffff`    | `#7f09b7`        | `#ffffff`          | Same as hover                               |
| **Active**   | `#ffffff`    | `#7f09b7`        | `#ffffff`          | Same as hover                               |
| **Disabled** | `#7f09b7`    | `#ffffff`        | `#7f09b7` (dimmed) | Bootstrap default opacity                   |

### 4.7 Outline Warning (`btn btn-outline-warning`)

| State        | Border Color | Background Color | Text Color         | Notes                                      |
| ------------ | ------------ | ---------------- | ------------------ | ------------------------------------------ |
| **Default**  | `#fdb600`    | `transparent`    | `#fdb600`          | Yellow outline                             |
| **Hover**    | `#fdb600`    | `#fdb600`        | `#000000`          | Fills with yellow, black text for contrast |
| **Focus**    | `#fdb600`    | `#fdb600`        | `#000000`          | Same as hover                              |
| **Active**   | `#fdb600`    | `#fdb600`        | `#000000`          | Same as hover                              |
| **Disabled** | `#fdb600`    | `transparent`    | `#fdb600` (dimmed) | Bootstrap default opacity                  |

### 4.8 Outline Danger (`btn btn-outline-danger`)

| State        | Border Color | Background Color | Text Color         | Notes                     |
| ------------ | ------------ | ---------------- | ------------------ | ------------------------- |
| **Default**  | `#b72929`    | `transparent`    | `#b72929`          | Red outline               |
| **Hover**    | `#b72929`    | `#b72929`        | `#ffffff`          | Fills with red            |
| **Focus**    | `#b72929`    | `#b72929`        | `#ffffff`          | Same as hover             |
| **Active**   | `#b72929`    | `#b72929`        | `#ffffff`          | Same as hover             |
| **Disabled** | `#b72929`    | `transparent`    | `#b72929` (dimmed) | Bootstrap default opacity |

### 4.9 Close Button (`btn-close`)

| State        | Border Color | Background Color | Text Color | Notes                                          |
| ------------ | ------------ | ---------------- | ---------- | ---------------------------------------------- |
| **Default**  | `none`       | `transparent`    | N/A        | Uses SVG background image                      |
| **Hover**    | `none`       | `transparent`    | N/A        | Bootstrap default: opacity change (0.5 → 0.75) |
| **Focus**    | `none`       | `transparent`    | N/A        | Bootstrap default: opacity 0.75                |
| **Active**   | `none`       | `transparent`    | N/A        | Bootstrap default: opacity 1                   |
| **Disabled** | `none`       | `transparent`    | N/A        | Opacity reduced                                |

**Variants:**

- `btn-close-white`: White SVG icon for dark backgrounds
- `btn-close-dark`: Dark SVG icon (custom class if exists)

### 4.10 Social Button (`btn btn-secondary btn-social`)

| State        | Border Color          | Background Color      | Text Color         | Notes                                                                            |
| ------------ | --------------------- | --------------------- | ------------------ | -------------------------------------------------------------------------------- |
| **Default**  | `#7f09b7` (or custom) | `#7f09b7` (or custom) | `#ffffff`          | Base uses secondary, overridden by `.bg-twitter`, `.bg-facebook`, `.bg-linkedin` |
| **Hover**    | Same as default       | Same as default       | `#ffffff`          | Custom override maintains white text                                             |
| **Focus**    | Same as default       | Same as default       | `#ffffff`          | Custom override maintains white text                                             |
| **Active**   | Same as default       | Same as default       | `#ffffff`          | Same as focus                                                                    |
| **Disabled** | Same as default       | Same as default       | `#ffffff` (dimmed) | Bootstrap default opacity                                                        |

**Platform-specific backgrounds** (applied via utility classes):

- Twitter: Platform-specific blue (from utility classes)
- Facebook: Platform-specific blue (from utility classes)
- LinkedIn: Platform-specific blue (from utility classes)

---

## 5. Size Modifier Effects

### 5.1 Small Buttons (`btn-sm`)

**Padding:** Bootstrap default (not explicitly overridden)

- Vertical: ~0.25rem (4px)
- Horizontal: ~0.5rem (8px)

**Font Size:** Bootstrap default

- ~0.875rem (14px)

**Border Radius:**

- `$btn-border-radius-sm: 1.5rem` (24px)

**Usage Pattern:**

- Action buttons in constrained spaces (edit, delete, mark as answer)
- CTA links in widgets
- Generally 20-25% of all button instances

### 5.2 Large Buttons (`btn-lg`)

**Padding:**

- Mobile: `0.75rem 1.5rem` (12px 24px) - from base button
- Desktop (lg+): `0.75rem 2rem` (12px 32px) - from `.btn-lg` override
- **Note:** `$btn-padding-y-lg` and `$btn-padding-x-lg` are defined but
  overridden by custom CSS

**Font Size:**

- `$input-btn-font-size-lg: 0.875rem` (14px)

**Line Height:**

- `1.5rem` (24px) - custom override

**Border Radius:**

- `$btn-border-radius-lg: 1.75rem` (28px)

**Icon Sizing:**

- Icons inside large buttons: `1.375rem` (22px)
- Margin-right: `0.25rem` (4px)

**Usage Pattern:**

- Primary CTAs on error pages
- Major action buttons in widgets
- High-importance navigation actions
- Generally 10-15% of all button instances

### 5.3 Custom Modifier: `btn-lighter`

**Applied to base button sizes (standard or large)**

**Overrides:**

- Font size: `0.875rem` (14px) - **larger than base buttons**
- Font weight: `400` (normal) - **not bold**
- Letter spacing: `0` - **removes uppercase spacing**
- Line height: `1rem` (16px)
- Padding: `0.875rem 2rem` (14px 32px)

**Visual Effect:**

- More text-like appearance
- Less commanding presence than standard buttons
- Better for secondary submit actions

**Usage Pattern:**

- Q&A form submissions (questions, answers)
- Support form submission
- 4 instances total (all form submissions)

---

## 6. Notable Patterns & Best Practices

### 6.1 Common Class Combinations

**Form Submissions:**

```html
<button class="btn btn-primary w-100" xpc-loading-button>
```

- Full-width primary button
- Loading state integration
- Used in: Login, Register, Password forms

**Mobile Filter Toggles:**

```html
<button class="btn btn-outline-primary align-self-center d-lg-none text-uppercase w-100">
```

- Outline style for secondary importance
- Hidden on desktop (`d-lg-none`)
- Uppercase text treatment
- Full-width on mobile
- Used in: Blog & Q&A search filters

**Action Buttons (Edit/Delete):**

```html
<button class="btn btn-sm btn-outline-secondary">Edit</button>
<button class="btn btn-sm btn-outline-danger">Delete</button>
```

- Small size for inline actions
- Outline variants for less visual weight
- Semantic color coding (secondary vs danger)

**File Upload Labels:**

```html
<label class="btn btn-outline-secondary" for="fileInput">ATTACH FILE</label>
```

- Button styling on label elements
- Triggers hidden file input
- Used in: Support form, Avatar upload

### 6.2 Responsive Behavior

**Padding Adjustments:**

- Large buttons increase horizontal padding on desktop breakpoints
- Cookie banner buttons reduce padding on mobile

**Layout Changes:**

- Article footer buttons: 100% width mobile → 50% width tablet+
- Navigation buttons: Visibility toggles with display utilities
- Social share buttons: Consistent sizing across breakpoints

**Typography:**

- Text transforms (`text-uppercase`) commonly used with primary actions
- Letter spacing (`letter-spacing: 0.1em`) on all buttons except `.btn-lighter`

### 6.3 Accessibility Considerations

**Focus States:**

- All focus box shadows removed (`$btn-focus-box-shadow: none`)
- Focus states rely on color changes only
- **Potential WCAG concern:** May need focus indicators for keyboard navigation

**Color Contrast:**

- Primary buttons: White on orange (`#ffffff` on `#f05a22`) - WCAG AA compliant
- Outline buttons: Adequate border contrast
- Disabled states: Lower contrast (may not meet WCAG standards)

**Close Buttons:**

- All have `aria-label="Close"` attributes
- SVG background images for consistent rendering
- Opacity changes on interaction states

### 6.4 Unused/Deprecated Patterns

**Defined but Unused:**

- `.btn-invisible-primary` - Fully implemented in SCSS but zero usage in
  codebase
- Potential candidate for removal to reduce CSS bundle size

**Standard Bootstrap Variants Not Used:**

- `.btn-success` - No green button instances found
- `.btn-info` - No blue info button instances found
- `.btn-warning` (solid) - Only outline variant used
- `.btn-light` - No light gray button instances found
- `.btn-dark` - No dark button instances found
- `.btn-link` - No link-styled button instances found

---

## 7. Recommendations

### 7.1 Consistency Improvements

1. **Standardize form button patterns:**

   - Currently mixing `btn-primary` and `btn-primary btn-lighter`
   - Consider establishing clear guidelines for when to use each

2. **Close button variants:**

   - Verify `btn-close-dark` class existence (referenced but may not be defined)
   - Standardize on white vs dark variants based on background colors

3. **Social button backgrounds:**
   - Document which utility classes provide platform-specific colors
   - Consider creating dedicated `.bg-twitter`, `.bg-facebook`, `.bg-linkedin`
     in theme if not present

### 7.2 Accessibility Enhancements

1. **Focus indicators:**

   - Add visible focus indicators beyond color changes
   - Consider outline or box-shadow on `:focus-visible`

2. **Disabled state contrast:**

   - Review disabled button contrast ratios
   - Current disabled color (`#d1c4bf`) may not meet WCAG AA for text

3. **Loading states:**
   - Ensure loading state announcements for screen readers
   - Verify `LoadingButtonTagHelper` includes proper ARIA attributes

### 7.3 Performance Optimizations

1. **Remove unused variant:**

   - Consider removing `.btn-invisible-primary` if truly unused
   - Can save ~8 lines of compiled CSS

2. **Consider CSS custom properties:**
   - Button colors defined in CSS custom properties (`:root`)
   - Could migrate SCSS variables to use these tokens for better runtime
     flexibility

### 7.4 Documentation Needs

1. **When to use each variant:**

   - Primary vs Outline Primary
   - When to add `.btn-lighter` modifier
   - Social button implementation guide

2. **Size selection guidelines:**

   - Standard vs Small vs Large
   - Responsive considerations

3. **Platform-specific colors:**
   - Document social media brand colors
   - Ensure compliance with platform brand guidelines

---

## 8. Summary Statistics

### Button Variant Distribution

- **Primary buttons:** ~45% of all instances
- **Outline Primary:** ~25%
- **Outline Secondary:** ~15%
- **Secondary:** ~5%
- **Outline Warning:** ~5%
- **Outline Danger:** ~3%
- **Close buttons:** ~2%

### Size Distribution

- **Standard size:** ~70%
- **Small (`btn-sm`):** ~20%
- **Large (`btn-lg`):** ~10%

### Custom Modifiers

- **`.btn-lighter`:** 4 instances
- **`.btn-primary-light`:** 1 instance
- **`.btn-invisible-primary`:** 0 instances (unused)
- **`.btn-social`:** 3 instances

### Loading Button Integration

- **Total forms with `xpc-loading-button`:** 11 instances
- **File types:** Login, Register, Password, Profile, Avatar, Badges, Q&A,
  Support

### Color Palette Summary

| Color Name       | Hex Value | Usage               |
| ---------------- | --------- | ------------------- |
| Primary Orange   | `#f05a22` | Main brand color    |
| Primary Dark     | `#c64300` | Hover states        |
| Secondary Purple | `#7f09b7` | Secondary actions   |
| Disabled Gray    | `#d1c4bf` | Disabled states     |
| Warning Yellow   | `#fdb600` | Cancel actions      |
| Danger Red       | `#b72929` | Delete actions      |
| White            | `#ffffff` | Text color          |
| Black            | `#000000` | Warning button text |

---

**Document Version:** 1.0  
**Last Updated:** November 22, 2025  
**Analysis Coverage:** All `.cshtml` files and SCSS customizations in
`Client/styles/`
