import { type FormComponentProps } from '@kentico/xperience-admin-base';
import {
  Colors,
  Headline,
  HeadlineSize,
} from '@kentico/xperience-admin-components';
import React, { useEffect, useState } from 'react';
import { IoCheckmarkSharp } from 'react-icons/io5';
import { MdOutlineCancel } from 'react-icons/md';
import { RxCross1 } from 'react-icons/rx';
import Select, {
  type CSSObjectWithLabel,
  type ClearIndicatorProps,
  type GroupBase,
  type MultiValue,
  type MultiValueRemoveProps,
  type OptionProps,
  type StylesConfig,
  components,
} from 'react-select';
import { Tooltip } from 'react-tooltip';
import { MemberBadgeAssigmentModel } from './MemberBadgeAssignmentModel';

export interface MemberBadgesAssignmentComponentClientProperties
  extends FormComponentProps {
  value: MemberBadgeAssigmentModel[];
}

interface OptionType {
  value: number;
  label: string;
  iconUrl: string | null;
  description: string;
  isAssigned: boolean;
}

const badgeStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  padding: '0.25em 0.65em',

  lineHeight: '1',
  textAlign: 'center',
  whiteSpace: 'nowrap',
  verticalAlign: 'center',
  borderRadius: '50rem',
  backgroundColor: '#287ab5',
  height: '35px',
};

const imageStyle: React.CSSProperties = {
  marginRight: '0.5rem',
  verticalAlign: 'middle',
  width: '1rem',
  height: '1rem',
  overflow: 'hidden',
  alignContent: 'center',
};

export const MemberBadgesAssignmentFormComponent = (
  props: MemberBadgesAssignmentComponentClientProperties,
): JSX.Element => {
  const [isClearIndicatorHover, setIsClearIndicatorHover] = useState(false);
  const [options, setOptions] = useState<OptionType[]>([]);
  const [assignedBadges, setAssignedBadges] = useState<OptionType[]>([]);

  useEffect(() => {
    const initialOptions: OptionType[] = props.value.map((badge) => ({
      value: badge.memberBadgeID,
      label: badge.memberBadgeDisplayName,
      iconUrl: badge.badgeImageRelativePath,
      description: badge.memberBadgeDescription,
      isAssigned: badge.isAssigned,
    }));

    setOptions(initialOptions);
    setAssignedBadges(initialOptions.filter((badge) => badge.isAssigned));
  }, [props.value]);

  const selectBadges = (newValue: MultiValue<OptionType>): void => {
    setAssignedBadges(newValue as OptionType[]);
    props.value.length = 0;

    newValue.forEach((x) => {
      const assignedBadge: MemberBadgeAssigmentModel = {
        memberBadgeID: x.value,
        memberBadgeDescription: x.description,
        memberBadgeDisplayName: x.label,
        isAssigned: true,
        badgeImageRelativePath: x.iconUrl,
      };
      props.value.push(assignedBadge);
    });

    const unAssignedBadges: OptionType[] = options.filter(
      (x) => !newValue.includes(x),
    );

    unAssignedBadges.forEach((x) => {
      const unAssignedBadge: MemberBadgeAssigmentModel = {
        memberBadgeID: x.value,
        memberBadgeDescription: x.description,
        memberBadgeDisplayName: x.label,
        isAssigned: false,
        badgeImageRelativePath: x.iconUrl,
      };
      props.value.push(unAssignedBadge);
    });

    if (props.onChange !== undefined) {
      props.onChange(props.value);
    }
  };

  /* eslint-disable @typescript-eslint/naming-convention */
  const customStyle: StylesConfig<OptionType, true, GroupBase<OptionType>> = {
    control: (styles, { isFocused }) =>
      ({
        ...styles,
        backgroundColor: 'white',
        borderColor: isFocused ? 'black' : 'gray',
        '&:hover': {
          borderColor: 'black',
        },
        borderRadius: 20,
        boxShadow: 'gray',
        padding: 2,
        minHeight: 'fit-content',
      }) as CSSObjectWithLabel,
    option: (styles, { isSelected }) => {
      return {
        ...styles,
        backgroundColor: isSelected ? '#bab4f0' : 'white',
        '&:hover': {
          backgroundColor: isSelected ? '#a097f7' : 'lightgray',
        },
        color: 'white',
        cursor: 'pointer',
      } as CSSObjectWithLabel;
    },
    input: (styles) => ({ ...styles }),
    container: (styles) =>
      ({ ...styles, borderColor: 'gray' }) as CSSObjectWithLabel,
    placeholder: (styles) => ({ ...styles }),
    multiValue: (styles) =>
      ({
        ...styles,
        backgroundColor: '#287ab5',
        borderRadius: 50,
        height: 35,
        alignItems: 'center',
        alignContent: 'center',
      }) as CSSObjectWithLabel,
    multiValueLabel: (styles) =>
      ({
        ...styles,
        color: 'white',
        fontSize: 16,
        alignContent: 'center',
      }) as CSSObjectWithLabel,
    indicatorSeparator: () => ({}),
    dropdownIndicator: (styles, state): CSSObjectWithLabel =>
      ({
        ...styles,
        transform: state.selectProps.menuIsOpen
          ? 'rotate(180deg)'
          : 'rotate(0deg)',
      }) as CSSObjectWithLabel,
    multiValueRemove: (styles) =>
      ({
        ...styles,
        '&:hover': {
          background: '#287ab5',
          borderRadius: 10,
          cursor: 'pointer',
          filter: 'grayscale(40%)',
          height: '100%',
        },
      }) as CSSObjectWithLabel,
  };

  const MultiValueRemove = (
    props: MultiValueRemoveProps<OptionType>,
  ): JSX.Element => {
    return (
      <components.MultiValueRemove {...props}>
        <RxCross1 style={{ color: 'white', height: '20', width: '30' }} />
      </components.MultiValueRemove>
    );
  };

  const handleMouseEnter = (): void => {
    setIsClearIndicatorHover(true);
  };
  const handleMouseLeave = (): void => {
    setIsClearIndicatorHover(false);
  };
  const ClearIndicator = (
    props: ClearIndicatorProps<OptionType>,
  ): JSX.Element => {
    return (
      <components.ClearIndicator {...props}>
        <Tooltip id="clear-content-type-select-tooltip-1" />
        <span
          style={{
            background: isClearIndicatorHover ? 'lightgray' : 'white',
            width: 25,
            height: 25,
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            borderRadius: 5,
            cursor: isClearIndicatorHover ? 'pointer' : 'default',
          }}
        >
          <MdOutlineCancel
            style={{ color: 'black', width: '80%', height: '80%' }}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
            data-tooltip-id="clear-content-type-select-tooltip-1"
            data-tooltip-content="Clear selection"
          />
        </span>
      </components.ClearIndicator>
    );
  };

  const Option = (
    props: OptionProps<OptionType, true, GroupBase<OptionType>>,
  ): JSX.Element => {
    return (
      <components.Option {...props}>
        <Tooltip
          content={props.data.description}
          id={'option-tooltip-' + props.data.value}
        />
        <span
          style={badgeStyle}
          data-tooltip-content={props.data.description}
          data-tooltip-id={'option-tooltip-' + props.data.value}
        >
          {props.isSelected ? (
            <IoCheckmarkSharp
              style={{
                marginRight: '0.5rem',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            />
          ) : (
            <span
              style={{
                marginRight: '0.5rem',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            ></span>
          )}
          {props.data.iconUrl ? (
            <img src={props.data.iconUrl} style={imageStyle} />
          ) : (
            ''
          )}
          {props.label}
        </span>
      </components.Option>
    );
  };

  return (
    <div>
      <h1>
        <Headline size={HeadlineSize.L} labelColor={Colors.TextDefaultOnLight}>
          Manually assigned badges
        </Headline>
      </h1>
      <Select
        isMulti
        closeMenuOnSelect={false}
        value={assignedBadges}
        options={options}
        onChange={selectBadges}
        placeholder="Assign Badges"
        styles={customStyle}
        hideSelectedOptions={false}
        components={{ MultiValueRemove, ClearIndicator, Option }}
        theme={(theme) => ({
          ...theme,
          height: 40,
          borderRadius: 0,
          borderColor: 'gray',
        })}
      />
    </div>
  );
};
