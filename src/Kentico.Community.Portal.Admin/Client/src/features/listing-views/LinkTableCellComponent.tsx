import React from 'react';

interface LinkTableCellComponentProps {
  readonly path: string;
  readonly label: string;
}

const LinkTableCellComponent = ({
  label,
  path,
}: LinkTableCellComponentProps): React.JSX.Element => {
  const style = { lineHeight: '16px' };

  return path !== undefined ? (
    <a
      style={style}
      href={path}
      onClick={(e) => {
        e.stopPropagation();
      }}
    >
      {label}
    </a>
  ) : (
    <span style={style}>{label}</span>
  );
};

LinkTableCellComponent.displayName = 'Link';

export { LinkTableCellComponent };
