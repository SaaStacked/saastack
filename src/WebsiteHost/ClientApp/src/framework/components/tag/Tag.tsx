import React from 'react';
import { createComponentId, toClasses } from '../Components.ts';
import { TailwindColorName } from '../typography/Tailwind.ts';


export interface TagProps {
  className?: string;
  id?: string;
  children?: React.ReactNode;
  label: string;
  color?: TailwindColorName;
  title?: string;
}

const TagColors = {
  primary: 'bg-primary text-white dark:bg-primary dark:text-primary-light',
  secondary: 'bg-secondary text-white dark:bg-secondary dark:text-secondary-light',
  accent: 'bg-accent text-white dark:bg-accent dark:text-accent-light',
  red: 'bg-red-600 text-white dark:bg-red-400 dark:text-gray-600',
  orange: 'bg-orange-600 text-white dark:bg-orange-400 dark:text-gray-600',
  amber: 'bg-amber-600 text-white dark:bg-amber-400 dark:text-gray-600',
  yellow: 'bg-yellow-600 text-white dark:bg-yellow-400 dark:text-gray-600',
  lime: 'bg-lime-600 text-white dark:bg-lime-400 dark:text-gray-600',
  green: 'bg-green-600 text-white dark:bg-green-400 dark:text-gray-600',
  emerald: 'bg-emerald-600 text-white dark:bg-emerald-400 dark:text-gray-600',
  teal: 'bg-teal-600 text-white dark:bg-teal-400 dark:text-gray-600',
  cyan: 'bg-cyan-600 text-white dark:bg-cyan-400 dark:text-gray-600',
  sky: 'bg-sky-600 text-white dark:bg-sky-400 dark:text-gray-600',
  blue: 'bg-blue-600 text-white dark:bg-blue-400 dark:text-gray-600',
  indigo: 'bg-indigo-600 text-white dark:bg-indigo-400 dark:text-gray-600',
  violet: 'bg-violet-600 text-white dark:bg-violet-400 dark:text-gray-600',
  purple: 'bg-purple-600 text-white dark:bg-purple-400 dark:text-gray-600',
  fuchsia: 'bg-fuchsia-600 text-white dark:bg-fuchsia-400 dark:text-gray-600',
  pink: 'bg-pink-600 text-white dark:bg-pink-400 dark:text-gray-600',
  rose: 'bg-rose-600 text-white dark:bg-rose-400 dark:text-gray-600',
  slate: 'bg-slate-600 text-white dark:bg-slate-400 dark:text-gray-600',
  gray: 'bg-gray-600 text-white dark:bg-gray-400 dark:text-gray-600',
  zinc: 'bg-zinc-600 text-white dark:bg-zinc-400 dark:text-gray-600',
  neutral: 'bg-neutral-600 text-white dark:bg-neutral-400 dark:text-gray-600',
  stone: 'bg-stone-600 text-white dark:bg-stone-400 dark:text-gray-600'
} as const;

export default function Tag({ className, id, children, label, color = 'primary', title }: TagProps) {
  const baseClasses = 'inline-flex items-baseline rounded-full px-3 py-1 mr-1 cursor-pointer';
  const colorClasses = TagColors[color as keyof typeof TagColors];
  const classes = toClasses([baseClasses, colorClasses, className]);
  const componentId = createComponentId('tag', id);
  return (
    <span className={classes} data-testid={componentId} title={title}>
      {children || label}
    </span>
  );
}
