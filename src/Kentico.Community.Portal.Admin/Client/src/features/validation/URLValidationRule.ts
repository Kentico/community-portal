import {
  ValidationRule,
  ValidationRuleProps,
} from '@kentico/xperience-admin-base';

export interface URLValidationRuleProps extends ValidationRuleProps {
  readonly requireHTTPS: boolean;
}

const urlPattern = /^(https?):\/\/[^\s/$.?#].[^\s]*$/;

export const URLValidationRule: ValidationRule<
  URLValidationRuleProps,
  string
> = (props, value) => {
  const { requireHTTPS, errorMessage } = props;
  const validate = (): { isValid: string; errorMessage: string } => {
    if (!value) {
      return {
        isValid: true,
        errorMessage,
      };
    }

    const isUrl = urlPattern.test(value);

    if (!isUrl) {
      return {
        isValid: false,
        errorMessage: `${errorMessage}: the value is malformed`,
      };
    }

    if (!requireHTTPS) {
      return { isValid: true, errorMessage };
    }

    if (!value.startsWith('https://')) {
      return {
        isValid: false,
        errorMessage: `${errorMessage}: the value must begin with https://`,
      };
    }

    return { isValid: true, errorMessage };
  };

  return validate();
};
