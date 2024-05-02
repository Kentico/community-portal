export type LinkDataType = {
  id: string;
  label: string;
  url: string;
};

export function newLink(): LinkDataType {
  return {
    id: crypto.randomUUID(),
    label: '',
    url: '',
  };
}
