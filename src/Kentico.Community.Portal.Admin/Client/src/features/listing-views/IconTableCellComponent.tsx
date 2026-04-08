import { Icon, type IconName } from '@kentico/xperience-admin-components';
import React from 'react';

interface IconTableCellComponentProps {
  readonly name: IconName;
}

const IconTableCellComponent = ({
  name,
}: IconTableCellComponentProps): React.JSX.Element => {
  return name ? <Icon name={name} /> : <></>;
};

IconTableCellComponent.displayName = 'Icon';

export { IconTableCellComponent };
