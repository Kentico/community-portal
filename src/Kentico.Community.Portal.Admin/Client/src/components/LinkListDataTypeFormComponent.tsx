import { FormComponentProps } from '@kentico/xperience-admin-base';
import {
  Button,
  ButtonColor,
  Input,
} from '@kentico/xperience-admin-components';
import React, { ChangeEvent, useState } from 'react';
import { LinkDataType, newLink } from './LinkDataType';

interface LinkListDataTypeFormComponentProps extends FormComponentProps {
  newLink: LinkDataType;
  value: LinkDataType[];
}

export const LinkListDataTypeFormComponent = (
  props: LinkListDataTypeFormComponentProps,
) => {
  const [links, setLinks] = useState(props.value ?? [{ ...props.newLink }]);

  const handleFieldChange = (
    index: number,
    event: ChangeEvent<HTMLInputElement>,
  ) => {
    if (props.onChange) {
      const field = event.target.name.replace(
        `${index}-`,
        '',
      ) as keyof LinkDataType;
      const updatedLink = {
        ...links[index],
        [field]: event.target.value,
      };
      const updatedLinks = links.map((a, i) => (i === index ? updatedLink : a));
      setLinks(updatedLinks);
      props.onChange(updatedLinks);
    }
  };

  const handleDeleteLink = (index: number) => {
    if (props.onChange) {
      const updatedLinks = links.filter((a, i) => i !== index);
      setLinks(updatedLinks);
      props.onChange(updatedLinks);
    }
  };

  const handleLinkAdd = () => {
    if (props.onChange) {
      const updatedLinks = [...links, { ...newLink() }];
      setLinks(updatedLinks);
      props.onChange(updatedLinks);
    }
  };

  const fieldStyle = { marginTop: '.5rem' };

  return (
    <div>
      <label style={{ color: 'var(--color-text-default-on-light)' }}>
        {props.label}
      </label>
      {links.map((link, index) => (
        <div
          key={link.id}
          style={{
            marginTop: index ? '2rem' : '0',
            color: 'var(--color-text-default-on-light)',
          }}
        >
          <div
            style={{
              ...fieldStyle,
              display: 'flex',
              flexDirection: 'row',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <label>Link {index + 1}</label>
            <Button
              onClick={() => handleDeleteLink(index)}
              label="Remove"
              color={ButtonColor.Quinary}
            />
          </div>
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
                name={`${index}-label`}
                value={link.label}
                onChange={(e) => handleFieldChange(index, e)}
              />
            </div>
            <div style={fieldStyle}>
              <Input
                label="URL"
                name={`${index}-url`}
                value={link.url}
                onChange={(e) => handleFieldChange(index, e)}
              />
            </div>
          </div>
        </div>
      ))}
      <div style={fieldStyle}>
        <Button
          onClick={handleLinkAdd}
          label="Add Link"
          color={ButtonColor.Primary}
        />
      </div>
    </div>
  );
};
