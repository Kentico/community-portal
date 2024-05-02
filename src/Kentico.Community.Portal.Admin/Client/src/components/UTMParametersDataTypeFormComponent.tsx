import { FormComponentProps } from '@kentico/xperience-admin-base';
import { Input } from '@kentico/xperience-admin-components';
import React, { ChangeEvent, useState } from 'react';
import {
  UTMParameterDataTypeField,
  UTMParametersDataType,
} from './UTMParametersDataType';

interface UTMParametersDataTypeFormComponentProps extends FormComponentProps {
  newUTMParameters: UTMParametersDataType;
  value: UTMParametersDataType;
  visibleFields: { text: string; value: UTMParameterDataTypeField }[];
}

export const UTMParametersDataTypeFormComponent = (
  props: UTMParametersDataTypeFormComponentProps,
) => {
  const [utmParameters, setUTMParameters] = useState({
    ...props.newUTMParameters,
    ...props.value,
  });

  const visibleFields = props.visibleFields ?? [];

  const handleFieldChange = (event: ChangeEvent<HTMLInputElement>) => {
    if (props.onChange) {
      const field = event.target.name as keyof UTMParametersDataType;
      const updatedUTMParameters = {
        ...utmParameters,
        [field]: event.target.value ?? '',
      };
      setUTMParameters(updatedUTMParameters);
      props.onChange(updatedUTMParameters);
    }
  };

  return (
    <div>
      <label style={{ color: 'var(--color-text-default-on-light)' }}>
        {props.label}
      </label>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 1fr',
          gap: '1rem',
        }}
      >
        {visibleFields.map((f) => (
          <div style={{ marginTop: '.5rem' }} key={f.value}>
            <Input
              label={f.text}
              name={f.value}
              value={utmParameters[f.value]}
              onChange={handleFieldChange}
              disabled={props.disabled}
            />
          </div>
        ))}
      </div>
    </div>
  );
};
