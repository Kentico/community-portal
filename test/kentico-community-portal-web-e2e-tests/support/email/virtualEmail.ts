export type VirtualEmail = {
  id?: number;
  subject?: string;
  htmlBody?: string;
  bodyHtml?: string;
  body?: string;
  textBody?: string;
  recipients?: string;
  recipient?: string;
};

export function getVirtualEmailId(email: unknown): number | undefined {
  if (!email || typeof email !== "object") {
    return undefined;
  }

  const value = email as Record<string, unknown>;
  const idCandidate = value.id ?? value.ID ?? value.emailId ?? value.EmailID;

  return typeof idCandidate === "number" ? idCandidate : undefined;
}
