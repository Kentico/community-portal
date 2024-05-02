export type UTMParametersDataType = {
  id: string;
  source: string;
  medium: string;
  campaign: string;
  content: string;
  term: string;
};

export type UTMParameterDataTypeField = Exclude<
  keyof UTMParametersDataType,
  'id'
>;

export function newUTMParameters(): UTMParametersDataType {
  return {
    id: crypto.randomUUID(),
    source: '',
    medium: '',
    campaign: '',
    content: '',
    term: '',
  };
}
