import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { ReactNode } from 'react';
import {
  faAdd,
  faArrowRightFromBracket,
  faBars,
  faBuilding,
  faCalendar,
  faCamera,
  faCarAlt,
  faCheck,
  faCheckCircle,
  faChevronDown,
  faChevronLeft,
  faChevronRight,
  faChevronUp,
  faCircleArrowLeft,
  faExclamationTriangle,
  faEye,
  faEyeSlash,
  faInfoCircle,
  faMoon,
  faUser,
  faUserCircle,
  faXmark
} from '@fortawesome/free-solid-svg-icons';
import tailwindConfig from '../../../tailwind.config.ts';
import Label from '../label/Label';

interface IconProps {
  symbol: IconSymbol;
  color?: TailwindPrimaryColors;
  size?: number;
}

type TailwindPrimaryColors = 50 | 100 | 200 | 300 | 400 | 500 | 600 | 700 | 800 | 900 | 950;

export type IconSymbol = keyof typeof fontAwesomeSymbols | keyof typeof nonFontAwesomeSymbols;

const fontAwesomeSymbols = {
  camera: faCamera,
  car: faCarAlt,
  calendar: faCalendar,
  'check-circle-fill': faCheckCircle,
  'info-circle-fill': faInfoCircle,
  'exclamation-triangle-fill': faExclamationTriangle,
  logout: faArrowRightFromBracket,
  avatar: faUserCircle,
  bars: faBars,
  'chevron-left': faChevronLeft,
  'chevron-right': faChevronRight,
  'chevron-up': faChevronUp,
  'chevron-down': faChevronDown,
  check: faCheck,
  cross: faXmark,
  'arrow-left-circle': faCircleArrowLeft,
  'show-password': faEyeSlash,
  'hide-password': faEye,
  moon: faMoon,
  user: faUser,
  building: faBuilding,
  plus: faAdd
} as const;

const nonFontAwesomeSymbols = { placeholder: 'placeholder', bigCross: 'bigCross' } as const;

const allSymbols = [...Object.keys(fontAwesomeSymbols), ...Object.keys(nonFontAwesomeSymbols)] as const;

// Used in the storybook
// noinspection JSUnusedGlobalSymbols
export function IconShowcase() {
  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-8 gap-y-16 color-primary-950">
      {allSymbols.map((icon) => (
        <div key={icon}>
          <div className="flex flex-col items-center space-y-2">
            <Icon symbol={icon as IconSymbol} size={50} color={950} />
            <Label id={`${icon}_label`} align="center">
              {icon}
            </Label>
          </div>
        </div>
      ))}
    </div>
  );
}

// Creates an icon using the specified symbol, size, and color
export default function Icon({ symbol, size = 20, color = 950 }: IconProps) {
  const extendedColors = tailwindConfig?.theme?.extend?.colors as { primary?: Record<number, string> };
  const themeColors = extendedColors?.primary ?? ({} as Record<number, string>);
  const iconColor = themeColors[color as number];
  const componentId = `icon`;

  if (Object.keys(fontAwesomeSymbols).includes(symbol)) {
    return (
      <IconBox size={size}>
        <FontAwesomeIcon
          data-testid={`${componentId}_symbol`}
          style={{ display: 'block', height: `${size}px` }}
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
        <svg data-testid={`${componentId}_placeholder`} width={size} height={size} xmlns="http://www.w3.org/2000/svg">
          <rect height={size} width={size} fill={iconColor} />
        </svg>
      </IconBox>
    );
  }

  if (symbol === 'bigCross') {
    return (
      <IconBox size={size}>
        <svg
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

function IconBox({ size, children }: { size: number; children: ReactNode }) {
  return (
    <div
      className="inline-flex items-center justify-center"
      style={{ width: `${size}px`, height: `${size}px`, boxSizing: 'content-box' }}
    >
      {children}
    </div>
  );
}
