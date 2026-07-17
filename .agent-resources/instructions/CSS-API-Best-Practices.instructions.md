# CSS API Best Practices

## 1. Use global design tokens

Define brand-level variables once at the root:

```css
:root {
  --color-surface-1: #fff;
  --color-surface-1-hover: #f4ebff;
  --radius-md: 12px;
  --space-3: 0.75rem;
}

:root.theme-dark {
  --color-surface-1: #171717;
  --color-surface-1-hover: #2a1745;
}
```

## 2. Define component-level variables

Expose a contract for each component with sensible defaults:

```css
:where(.blog-post-card) {
  --card-bg: var(--color-surface-1);
  --card-bg-hover: var(--color-surface-1-hover);
  --card-radius: var(--radius-md);

  border-radius: var(--card-radius);
  background: var(--card-bg);
  transition: background-color 0.2s ease;
}

:where(
    .blog-post-card:is(:hover, :focus-within),
    .blog-post-card[data-state="hover"]
  ) {
  background: var(--card-bg-hover);
}
```

## 3. Let consumers override easily

Scope overrides at page or instance level without `!important`:

```css
.page-variant-landing .blog-post-card {
  --card-bg: #fff7ed;
  --card-bg-hover: #ffedd5;
}

.card--purple {
  --card-bg: #f3e8ff;
  --card-bg-hover: #e9d5ff;
}
```

## 4. Drive states via classes or attributes

- Prefer toggling **classes** or `data-*` attributes over inline styles.
- Example:

  ```html
  <article class="blog-post-card card--purple" data-state="hover"></article>
  ```

## 5. Keep specificity low and predictable

- Use `:where()` or `:is()` to zero out selector weight.
- Organize with cascade layers:

  ```css
  @layer tokens, components, utilities;

  @layer tokens {
    /* root tokens */
  }
  @layer components {
    /* component rules */
  }
  @layer utilities {
    /* one-off overrides */
  }
  ```

## 6. Modern CSS extras

- **`@property`** for animatable custom props.
- **`:has()`** for parent-driven states.
- **`prefers-reduced-motion`** for accessibility.

---

## TL;DR

- Defaults in CSS, not inline.
- Map **component vars → global tokens**.
- States via classes/attributes, not `!important`.
- Use cascade layers to manage overrides.
- Theme per scope (e.g., `.theme-dark`, `.page-variant-*`).
