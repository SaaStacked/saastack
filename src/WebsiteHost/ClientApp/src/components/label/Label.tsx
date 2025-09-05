import { ReactNode } from 'react';
import { createComponentId } from '../Components.ts';


interface LabelProps {
  className?: string;
  id?: string;
  children?: ReactNode;
  size?: TailwindFontSize;
  weight?: TailwindFontWeight;
  align?: TextAlign;
}

type TailwindFontSize =
  | 'xs'
  | 'sm'
  | 'base'
  | 'lg'
  | 'xl'
  | '2xl'
  | '3xl'
  | '4xl'
  | '5xl'
  | '6xl'
  | '7xl'
  | '8xl'
  | '9xl';

type TailwindFontWeight =
  | 'thin'
  | 'extralight'
  | 'light'
  | 'normal'
  | 'medium'
  | 'semibold'
  | 'bold'
  | 'extrabold'
  | 'black';

type TextAlign = 'left' | 'center' | 'right' | 'justify' | 'start' | 'end';

// Creates a textual label with the specified size, weight, and alignment
export default function Label({
  className,
  id,
  children,
  size = 'base',
  weight = 'normal',
  align = 'left'
}: LabelProps) {
  const baseClasses = `block w-full text-${size} font-${weight} text-${align} m-0`;
  const classes = [baseClasses, className].filter(Boolean).join(' ');
  const componentId = createComponentId('label', id);
  return (
    <label className={classes} data-testid={componentId}>
      {children}
    </label>
  );
}
