import { FormComponentProps } from '@kentico/xperience-admin-base';
import { Input } from '@kentico/xperience-admin-components';
import React, { ChangeEvent, useState } from 'react';
import { LinkDataType } from './LinkDataType';

interface LinkDataTypeFormComponentProps extends FormComponentProps {
  newLink: LinkDataType;
  value: LinkDataType;
}

export const LinkDataTypeFormComponent = (
  props: LinkDataTypeFormComponentProps,
) => {
  const [link, setLink] = useState(props.value ?? { ...props.newLink });

  const handleFieldChange = (event: ChangeEvent<HTMLInputElement>) => {
    if (props.onChange) {
      const field = event.target.name as keyof LinkDataType;
      const updatedLink = {
        ...link,
        [field]: event.target.value,
      };
      setLink(updatedLink);
      props.onChange(updatedLink);
    }
  };

  const fieldStyle = { marginTop: '.5rem' };

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
        <div style={fieldStyle}>
          <Input
            label="Label"
            name={'label'}
            value={link.label}
            onChange={handleFieldChange}
          />
        </div>
        <div style={fieldStyle}>
          <Input
            label="URL"
            name={'url'}
            value={link.url}
            onChange={handleFieldChange}
          />
        </div>
      </div>
    </div>
  );
};
