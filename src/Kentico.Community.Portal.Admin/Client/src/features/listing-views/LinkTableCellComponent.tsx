import React from 'react';
import { Link, Tooltip } from '@kentico/xperience-admin-components';

interface LinkTableCellComponentProps {
  readonly path: string;
  readonly label: string;
}

const LinkTableCellComponent = ({
  label,
  path,
}: LinkTableCellComponentProps): React.JSX.Element => {
  const style = { lineHeight: '16px' };

  return !!path ? (
    <Tooltip tooltipText={path}>
      <div style={style}>
        <Link href={path} text={label} />
      </div>
    </Tooltip>
  ) : (
    <Tooltip tooltipText={path}>
      <span style={style}>{label}</span>
    </Tooltip>
  );
};

LinkTableCellComponent.displayName = 'Link';

export { LinkTableCellComponent };
