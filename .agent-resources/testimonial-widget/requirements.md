# Testimonial widget

Properties

- Reference 1 TestimonialContent item - required
- Visual customizations options
  - Layout Featured/Simple as enum dropdown field
  - Theme Primary/Secondary as enum dropdown field
  - Show title
  - Show Photo
  - Show Employment (job title and employer)

The widget will have 2 views, each representing one of the two Layouts The
properties will map into view model properties in the view model constructor If
view model values generate different css classes or attributes, express this as
switch expressions in the view files

The website uses a customized bootstrap 5.3 design via SCSS and CSS custom
properties

Create two layouts to match the ideas of "Featured" and "Simple" testimonials
