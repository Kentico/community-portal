export type VirtualEmail = {
  virtualEmailID: number;
  virtualEmailGUID: string;
  virtualEmailSender: string;
  virtualEmailRecipientsTo: string;
  virtualEmailRecipientsCc: string;
  virtualEmailRecipientsBcc: string;
  virtualEmailSubject: string;
  virtualEmailBodyHTML: string;
  virtualEmailBodyPlainText: string;
  virtualEmailSentUTCDate: string;
  virtualEmailStatus: string;
  virtualEmailErrorMessage: string;
  virtualEmailChannelName: string;
  virtualEmailEmailConfigurationID: number;
};

export function getVirtualEmailId(email: VirtualEmail): number {
  return email.virtualEmailID;
}
