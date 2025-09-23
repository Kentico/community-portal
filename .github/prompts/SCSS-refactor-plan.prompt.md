# SCSS Modernization Plan - Streamlined

## 🎯 Core Philosophy: Bootstrap First + CSS API for Dynamic Needs

### **Decision Framework**

1. **Can Bootstrap utilities handle this?** → Use Bootstrap utilities

- Consult
  [the bootstrap documentation](https://getbootstrap.com/docs/5.3/getting-started/introduction/)
  to always ensure you are using the right SCSS customization points and
  existing Bootstrap classes.

2. **Is this a dynamic color/theme?** → Use CSS API pattern
3. **Is this repeated 10+ times?** → Consider Razor component
4. **Is this a one-off style?** → Use inline style with design tokens

### **The Right Balance**

❌ **Don't replace:** Bootstrap utilities (`g-col-12`, `d-flex`, `bg-white`,
`p-3`, `rounded`)  
✅ **Do replace:** Semantic CSS classes (`c-card`, `c-tag`, `c-group`)  
✅ **Do add:** CSS API for dynamic theming and colors

### **CSS API Pattern**

```scss
// Component with CSS API
:where(.blog-post-card) {
  --card-bg: var(--color-surface-1, #fff);
  --card-bg-hover: var(--color-surface-1-hover, #f8f9fa);

  background: var(--card-bg);
  transition: background-color 0.2s ease;
}

.card--purple {
  --card-bg: var(--color-purple-100);
}
```

### **Template Usage**

```html
<!-- ✅ GOOD: Bootstrap utilities + CSS API -->
<article class="g-col-12 d-flex p-3 rounded blog-post-card card--purple">
  <h2 style="color: var(--bs-gray-600);">@Model.Title</h2>
</article>
```

## 📋 Implementation Phases

### **Phase 1: Foundation** 🟢 Low Risk

- **1.1** Core variables → CSS custom properties
- **1.2** Remove duplicate utilities
- **1.3** Modernize containers

### **Phase 2: Layout** 🟡 Medium Risk

- **2.1** Grid system consolidation
- **2.2** Navigation modernization
- **2.3** Layout templates

### **Phase 3: Components** 🟡 Medium Risk

- **3.1** ✅ Cards
- **3.2** Tags & badges
- **3.3** Forms

### **Phase 4: Advanced** 🔴 Higher Risk

- **4.0** Replace legacy "c-" classes
- **4.1** Interactive elements
- **4.2** Data visualization
- **4.3** Content display

### **Phase 5: Polish** 🟢 Low Risk

- **5.1** Asset optimization
- **5.2** Documentation
- **5.3** Testing & validation

## ✅ Per-Component Process

1. **Audit existing classes** - identify semantic vs. utility
2. **Map to Bootstrap utilities** - grid, flexbox, spacing, colors
3. **Identify dynamic needs** - hover, themes, responsive
4. **Create minimal CSS API** - only for what Bootstrap can't handle
5. **Test responsiveness** - ensure behavior maintained

## 🎨 Design Tokens Reference

**Colors:** `--color-primary`, `--color-purple-100`, `--bs-secondary`,
`--bs-gray-600`  
**Sizing:** `--size-author-avatar`, `--bs-border-radius-lg`  
**Spacing:** `--space-3`, Bootstrap utilities preferred

## 📖 Migration Examples

```html
<!-- ❌ BEFORE -->
<div class="c-card bg-purple-100 c-card_inner">
  <div class="c-card_title">Content</div>
</div>

<!-- ✅ AFTER -->
<div class="bg-white border rounded p-3 card--purple">
  <div class="d-flex justify-content-between">Content</div>
</div>
```

**Success Criteria:** 90% reduction in custom classes, 100% design token usage,
zero Bootstrap duplication

## 🔧 Advanced CSS API Patterns

### **Hover & Interactive States**

```scss
// Use :is() for efficient hover targeting
:where(.blog-post-card:is(:hover, :focus-within)) {
  background: var(--card-bg-hover);
  transform: translateY(-1px);
}

// Multiple states with CSS API
:where(.card) {
  --card-shadow: var(--bs-box-shadow-sm);
  --card-shadow-hover: var(--bs-box-shadow);

  box-shadow: var(--card-shadow);
  transition: box-shadow 0.15s ease-in-out, transform 0.15s ease-in-out;
}
```

### **Theme Variants System**

```scss
// Base component
:where(.notification) {
  --notification-bg: var(--bs-light);
  --notification-border: var(--bs-border-color);
  --notification-text: var(--bs-body-color);
}

// Theme variants
.notification--success {
  --notification-bg: var(--bs-success-bg-subtle);
  --notification-border: var(--bs-success-border-subtle);
  --notification-text: var(--bs-success-text-emphasis);
}
```

## 🚨 Common Pitfalls & Solutions

### **❌ Over-Engineering**

```html
<!-- DON'T: Create CSS API for simple layouts -->
<div class="simple-flex-card">
  <!-- Custom class not needed -->

  <!-- ✅ DO: Use Bootstrap utilities directly -->
  <div class="d-flex align-items-center gap-3 p-3 border rounded"></div>
</div>
```

### **❌ Bootstrap Duplication**

```scss
// DON'T: Recreate Bootstrap functionality
.my-flex {
  display: flex;
  justify-content: space-between;
}

// ✅ DO: Use Bootstrap classes and design tokens
:where(.custom-card) {
  background: var(--bs-gray-100);
  border-radius: var(--bs-border-radius);
  padding: var(--space-3);
}
```

## 📈 Implementation Results

### **Phase 3 Card Modernization** ✅

- **Blog Cards**: 6 custom classes → 1 CSS API + Bootstrap utilities
- **Member Cards**: 4 custom classes → Pure Bootstrap + 1 theme variant
- **Code Reduction**: 89 lines CSS → 23 lines CSS API (74% reduction)
- **Bundle Size**: ~5KB → ~1.8KB per component

### **Next Phase Preview**

```html
<!-- Phase 3.2 Target: Tags & Badges -->
<span class="c-tag c-tag--primary">Technology</span>
<!-- Goal: -->
<span class="badge fs-6" style="background-color: var(--color-primary);"
  >Technology</span
>
```
