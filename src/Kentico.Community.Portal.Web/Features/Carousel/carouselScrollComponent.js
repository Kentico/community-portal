/**
 * @param {import("alpinejs").Alpine} Alpine
 */
export function registerCarouselScrollComponent(Alpine) {
  Alpine.data("carouselScrollComponent", () => ({
    activeSlide: 0,
    slides: [],

    get slideCount() {
      return this.slides.length;
    },

    init() {
      this.slides = Array.from(
        this.$el.querySelectorAll(".carousel-widget__slide"),
      );
      this.updateActiveSlide();
    },

    updateActiveSlide() {
      const viewport = this.$refs.viewport;
      if (!viewport) return;

      let closestSlide = 0;
      let closestDistance = Infinity;

      this.slides.forEach((slide, index) => {
        const rect = slide.getBoundingClientRect();
        const viewportRect = viewport.getBoundingClientRect();
        const distance = Math.abs(
          viewportRect.left +
            viewportRect.width / 2 -
            (rect.left + rect.width / 2),
        );

        if (distance < closestDistance) {
          closestDistance = distance;
          closestSlide = index;
        }
      });

      this.activeSlide = closestSlide;
    },

    scrollToSlide(index) {
      const viewport = this.$refs.viewport;

      if (index >= 0 && index < this.slides.length && viewport) {
        const slide = this.slides[index];

        // Calculate how much to scroll to center the slide
        const scrollAmount =
          slide.offsetLeft - viewport.clientWidth / 2 + slide.offsetWidth / 2;

        viewport.scrollTo({
          left: scrollAmount,
          behavior: "smooth",
        });
      }
    },

    scrollNext() {
      const nextIndex = (this.activeSlide + 1) % this.slideCount;
      this.scrollToSlide(nextIndex);
    },

    scrollPrev() {
      const prevIndex =
        (this.activeSlide - 1 + this.slideCount) % this.slideCount;
      this.scrollToSlide(prevIndex);
    },
  }));
}
