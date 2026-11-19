import { expect, type Locator, type Page, type Response } from "@playwright/test";
import { adminBaseUrl } from "../../config.js";

type Json = unknown;

/** Default sort of the listing, as the server sends it to the listing template. */
export interface ListingSort {
  sortBy: string;
  sortType: string;
}

/**
 * The built-in Email queue listing (EmailQueueListExtender).
 *
 * The admin table renders no sort or action attributes (no aria-sort, hashed class names),
 * so this reads the page configuration the admin API returns instead of rendered text.
 */
export class EmailQueuePage {
  private readonly payloads: Json[] = [];

  constructor(private readonly page: Page) {}

  async open(): Promise<void> {
    const pending: Promise<void>[] = [];
    const collect = (response: Response): void => {
      if (!/\/admin\/api\//i.test(response.url())) {
        return;
      }

      pending.push(
        response
          .json()
          .then((body: Json) => {
            this.payloads.push(body);
          })
          .catch(() => {
            // Not JSON, or the body is gone after a navigation
          }),
      );
    };

    this.page.on("response", collect);

    try {
      await this.page.goto(`${adminBaseUrl}/email-queue/queue`);
      await expect(this.page).toHaveURL(/email-queue\/queue/i);

      await expect
        .poll(
          async () => {
            await Promise.all(pending);
            return this.defaultSort() !== undefined;
          },
          { timeout: 15_000 },
        )
        .toBe(true);
    } finally {
      this.page.off("response", collect);
    }
  }

  /** The listing's default sort, or undefined when none was sent. */
  defaultSort(): ListingSort | undefined {
    const match = this.find(
      (o) => typeof o.sortBy === "string" && o.sortBy !== "",
    );

    return match
      ? { sortBy: String(match.sortBy), sortType: String(match.sortType) }
      : undefined;
  }

  /** The "Delete failed" header button. The caption is owned by EmailQueueListExtender. */
  deleteFailedButton(): Locator {
    return this.page.getByRole("button", { name: "Delete failed", exact: true });
  }

  /** Confirmation dialog opened by a header command. */
  confirmationDialog(): Locator {
    return this.page.getByRole("dialog");
  }

  /** Depth-first search of the captured admin API payloads; independent of the envelope shape. */
  private find(
    predicate: (o: Record<string, Json>) => boolean,
  ): Record<string, Json> | undefined {
    const stack: Json[] = [...this.payloads];

    while (stack.length > 0) {
      const current = stack.pop();

      if (current === null || typeof current !== "object") {
        continue;
      }

      if (Array.isArray(current)) {
        stack.push(...(current as Json[]));
        continue;
      }

      const record = current as Record<string, Json>;

      if (predicate(record)) {
        return record;
      }

      stack.push(...Object.values(record));
    }

    return undefined;
  }
}
