/**
 * @param {import("alpinejs").Alpine} Alpine
 */
export function registerAnswerListComponent(Alpine) {
  Alpine.data("answersListComponent", () => ({
    orderDirection: "oldest",
    reorder() {
      const orderDirection = this.orderDirection;
      const list = this.$refs.list;
      /** @@type {HTMLDivElement[]} */
      const answers = [...list.querySelectorAll("[q-and-a-answer]")];
      const sortedAnswers = answers.sort((a, b) => {
        const createdA = a.getAttribute("data-created");
        const createdB = b.getAttribute("data-created");
        const reactionsA = parseInt(
          a.getAttribute("data-reactions") || "0",
          10,
        );
        const reactionsB = parseInt(
          b.getAttribute("data-reactions") || "0",
          10,
        );

        if (orderDirection === "acceptedAnswer") {
          const isAcceptedA = a.getAttribute("data-accepted") === "true";
          const isAcceptedB = b.getAttribute("data-accepted") === "true";

          if (isAcceptedA && !isAcceptedB) return -1;
          if (!isAcceptedA && isAcceptedB) return 1;
          return createdA - createdB;
        }

        if (orderDirection === "mostUpvotes") {
          return reactionsB - reactionsA;
        }

        return orderDirection === "newest"
          ? createdB - createdA
          : createdA - createdB;
      });

      list.innerHTML = "";
      sortedAnswers.forEach((answer) => list.appendChild(answer));
    },
  }));
}
