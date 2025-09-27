import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { ReactNode } from 'react';
import { faAdd, faArrowRightFromBracket, faBars, faBuilding, faCalendar, faCamera, faCarAlt, faCheck, faCheckCircle, faChevronDown, faChevronLeft, faChevronRight, faChevronUp, faCircleArrowLeft, faEdit, faExclamationTriangle, faEye, faEyeSlash, faInfoCircle, faMoon, faPencil, faPeopleRoof, faRepeat, faShuffle, faTrash, faUser, faUserCircle, faXmark } from '@fortawesome/free-solid-svg-icons';
import { createComponentId, toClasses } from '../Components.ts';


interface IconProps {
  className?: string;
  id?: string;
  symbol: IconSymbol;
  color?: TailwindColor;
  size?: number;
}

export type TailwindColor =
  | 'primary'
  | 'secondary'
  | 'accent'
  | 'red-50'
  | 'red-100'
  | 'red-200'
  | 'red-300'
  | 'red-400'
  | 'red-500'
  | 'red-600'
  | 'red-700'
  | 'red-800'
  | 'red-900'
  | 'red-950'
  | 'orange-50'
  | 'orange-100'
  | 'orange-200'
  | 'orange-300'
  | 'orange-400'
  | 'orange-500'
  | 'orange-600'
  | 'orange-700'
  | 'orange-800'
  | 'orange-900'
  | 'orange-950'
  | 'amber-50'
  | 'amber-100'
  | 'amber-200'
  | 'amber-300'
  | 'amber-400'
  | 'amber-500'
  | 'amber-600'
  | 'amber-700'
  | 'amber-800'
  | 'amber-900'
  | 'amber-950'
  | 'yellow-50'
  | 'yellow-100'
  | 'yellow-200'
  | 'yellow-300'
  | 'yellow-400'
  | 'yellow-500'
  | 'yellow-600'
  | 'yellow-700'
  | 'yellow-800'
  | 'yellow-900'
  | 'yellow-950'
  | 'lime-50'
  | 'lime-100'
  | 'lime-200'
  | 'lime-300'
  | 'lime-400'
  | 'lime-500'
  | 'lime-600'
  | 'lime-700'
  | 'lime-800'
  | 'lime-900'
  | 'lime-950'
  | 'green-50'
  | 'green-100'
  | 'green-200'
  | 'green-300'
  | 'green-400'
  | 'green-500'
  | 'green-600'
  | 'green-700'
  | 'green-800'
  | 'green-900'
  | 'green-950'
  | 'emerald-50'
  | 'emerald-100'
  | 'emerald-200'
  | 'emerald-300'
  | 'emerald-400'
  | 'emerald-500'
  | 'emerald-600'
  | 'emerald-700'
  | 'emerald-800'
  | 'emerald-900'
  | 'emerald-950'
  | 'teal-50'
  | 'teal-100'
  | 'teal-200'
  | 'teal-300'
  | 'teal-400'
  | 'teal-500'
  | 'teal-600'
  | 'teal-700'
  | 'teal-800'
  | 'teal-900'
  | 'teal-950'
  | 'cyan-50'
  | 'cyan-100'
  | 'cyan-200'
  | 'cyan-300'
  | 'cyan-400'
  | 'cyan-500'
  | 'cyan-600'
  | 'cyan-700'
  | 'cyan-800'
  | 'cyan-900'
  | 'cyan-950'
  | 'sky-50'
  | 'sky-100'
  | 'sky-200'
  | 'sky-300'
  | 'sky-400'
  | 'sky-500'
  | 'sky-600'
  | 'sky-700'
  | 'sky-800'
  | 'sky-900'
  | 'sky-950'
  | 'blue-50'
  | 'blue-100'
  | 'blue-200'
  | 'blue-300'
  | 'blue-400'
  | 'blue-500'
  | 'blue-600'
  | 'blue-700'
  | 'blue-800'
  | 'blue-900'
  | 'blue-950'
  | 'indigo-50'
  | 'indigo-100'
  | 'indigo-200'
  | 'indigo-300'
  | 'indigo-400'
  | 'indigo-500'
  | 'indigo-600'
  | 'indigo-700'
  | 'indigo-800'
  | 'indigo-900'
  | 'indigo-950'
  | 'violet-50'
  | 'violet-100'
  | 'violet-200'
  | 'violet-300'
  | 'violet-400'
  | 'violet-500'
  | 'violet-600'
  | 'violet-700'
  | 'violet-800'
  | 'violet-900'
  | 'violet-950'
  | 'purple-50'
  | 'purple-100'
  | 'purple-200'
  | 'purple-300'
  | 'purple-400'
  | 'purple-500'
  | 'purple-600'
  | 'purple-700'
  | 'purple-800'
  | 'purple-900'
  | 'purple-950'
  | 'fuchsia-50'
  | 'fuchsia-100'
  | 'fuchsia-200'
  | 'fuchsia-300'
  | 'fuchsia-400'
  | 'fuchsia-500'
  | 'fuchsia-600'
  | 'fuchsia-700'
  | 'fuchsia-800'
  | 'fuchsia-900'
  | 'fuchsia-950'
  | 'pink-50'
  | 'pink-100'
  | 'pink-200'
  | 'pink-300'
  | 'pink-400'
  | 'pink-500'
  | 'pink-600'
  | 'pink-700'
  | 'pink-800'
  | 'pink-900'
  | 'pink-950'
  | 'rose-50'
  | 'rose-100'
  | 'rose-200'
  | 'rose-300'
  | 'rose-400'
  | 'rose-500'
  | 'rose-600'
  | 'rose-700'
  | 'rose-800'
  | 'rose-900'
  | 'rose-950'
  | 'slate-50'
  | 'slate-100'
  | 'slate-200'
  | 'slate-300'
  | 'slate-400'
  | 'slate-500'
  | 'slate-600'
  | 'slate-700'
  | 'slate-800'
  | 'slate-900'
  | 'slate-950'
  | 'gray-50'
  | 'gray-100'
  | 'gray-200'
  | 'gray-300'
  | 'gray-400'
  | 'gray-500'
  | 'gray-600'
  | 'gray-700'
  | 'gray-800'
  | 'gray-900'
  | 'gray-950'
  | 'zinc-50'
  | 'zinc-100'
  | 'zinc-200'
  | 'zinc-300'
  | 'zinc-400'
  | 'zinc-500'
  | 'zinc-600'
  | 'zinc-700'
  | 'zinc-800'
  | 'zinc-900'
  | 'zinc-950'
  | 'neutral-50'
  | 'neutral-100'
  | 'neutral-200'
  | 'neutral-300'
  | 'neutral-400'
  | 'neutral-500'
  | 'neutral-600'
  | 'neutral-700'
  | 'neutral-800'
  | 'neutral-900'
  | 'neutral-950'
  | 'stone-50'
  | 'stone-100'
  | 'stone-200'
  | 'stone-300'
  | 'stone-400'
  | 'stone-500'
  | 'stone-600'
  | 'stone-700'
  | 'stone-800'
  | 'stone-900'
  | 'black'
  | 'white';

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