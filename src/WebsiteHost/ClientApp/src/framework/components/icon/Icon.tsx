import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { ReactNode } from 'react';
import { faAdd, faArrowRightFromBracket, faBars, faBuilding, faCalendar, faCamera, faCarAlt, faCheck, faCheckCircle, faChevronDown, faChevronLeft, faChevronRight, faChevronUp, faCircleArrowLeft, faEdit, faExclamationTriangle, faEye, faEyeSlash, faInfoCircle, faMoon, faPencil, faPeopleRoof, faRepeat, faShuffle, faTrash, faUser, faUserCircle, faXmark } from '@fortawesome/free-solid-svg-icons';
import { createComponentId, toClasses } from '../Components.ts';
import { TailwindColor } from '../typography/Tailwind.ts';

interface IconProps {
  className?: string;
  id?: string;
  symbol: IconSymbol;
  color?: TailwindColor;
  size?: number;
}

export type IconSymbol = keyof typeof fontAwesomeSymbols | keyof typeof nonFontAwesomeSymbols;

const fontAwesomeSymbols = {
  'arrow-left-circle': faCircleArrowLeft,
  avatar: faUserCircle,
  bars: faBars,
  building: faBuilding,
  calendar: faCalendar,
  camera: faCamera,
  car: faCarAlt,
  check: faCheck,
  'check-circle-fill': faCheckCircle,
  'chevron-left': faChevronLeft,
  'chevron-right': faChevronRight,
  'chevron-up': faChevronUp,
  'chevron-down': faChevronDown,
  company: faPeopleRoof,
  cross: faXmark,
  edit: faEdit,
  'exclamation-triangle-fill': faExclamationTriangle,
  'hide-password': faEye,
  'info-circle-fill': faInfoCircle,
  logout: faArrowRightFromBracket,
  moon: faMoon,
  pencil: faPencil,
  user: faUser,
  plus: faAdd,
  repeat: faRepeat,
  'show-password': faEyeSlash,
  shuffle: faShuffle,
  trash: faTrash
} as const;

const nonFontAwesomeSymbols = { placeholder: 'placeholder', bigCross: 'bigCross' } as const;

export const allSymbols = [...Object.keys(fontAwesomeSymbols), ...Object.keys(nonFontAwesomeSymbols)] as const;

// Creates an icon using the specified symbol, size, and color
export default function Icon({ className, id, symbol, size = 20, color = 'primary' }: IconProps) {
  const colors = getComputedStyle(document.documentElement);
  const iconColor = colors.getPropertyValue(`--color-${color}`);
  const componentId = createComponentId('icon', id);

  if (Object.keys(fontAwesomeSymbols).includes(symbol)) {
    return (
      <IconBox size={size}>
        <FontAwesomeIcon
          className={className}
          data-testid={`${componentId}_symbol`}
          style={{ display: 'block', height: `${size}px`, width: `${size}px` }}
          icon={fontAwesomeSymbols[symbol as keyof typeof fontAwesomeSymbols]}
          width={size}
          height={size}
          color={iconColor}
        />
      </IconBox>
    );
  }

  if (symbol === 'placeholder') {
    return (
      <IconBox size={size}>
        <svg
          className={className}
          data-testid={`${componentId}_placeholder`}
          width={size}
          height={size}
          xmlns="http://www.w3.org/2000/svg"
        >
          <rect height={size} width={size} fill={iconColor} />
        </svg>
      </IconBox>
    );
  }

  if (symbol === 'bigCross') {
    return (
      <IconBox size={size}>
        <svg
          className={className}
          data-testid={`${componentId}_bigCross`}
          width={size}
          height={size}
          viewBox="0 0 12 11"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M11.146 9A1.062 1.062 0 1 1 9.644 10.5L6.146 7.004l-3.5 3.497c-.207.209-.479.312-.75.312a1.062 1.062 0 0 1-.751-1.815l3.499-3.5L1.144 2A1.062 1.062 0 1 1 2.647.498l3.499 3.501 3.5-3.5a1.062 1.062 0 1 1 1.502 1.503l-3.5 3.5L11.146 9Z"
            fill={iconColor}
          />
        </svg>
      </IconBox>
    );
  }

  return null;
}

function IconBox({ className, size, children }: { className?: string; size: number; children: ReactNode }) {
  const baseClasses = 'inline-flex items-center justify-center';
  const classes = toClasses([baseClasses, className]);
  return (
    <div className={classes} style={{ width: `${size}px`, height: `${size}px`, boxSizing: 'content-box' }}>
      {children}
    </div>
  );
}
