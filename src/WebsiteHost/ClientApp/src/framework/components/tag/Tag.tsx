import { createComponentId, toClasses } from '../Components.ts';
import { TailwindColor } from '../icon/Icon';

export interface TagProps {
  className?: string;
  id?: string;
  label: string;
  color?: TailwindColor;
  title?: string;
}

export default function Tag({ className, id, label, color = 'primary', title }: TagProps) {
  const baseClasses = 'inline-flex items-center rounded-full px-3 py-1 text-sm font-medium';
  const colorClasses = `bg-${color} text-gray-800 dark:bg-${color} dark:text-${color}`;
  const classes = toClasses([baseClasses, colorClasses, className]);
  const componentId = createComponentId('tag', id);
  return (
    <span className={classes} data-testid={componentId} title={title}>
      {label}
    </span>
  );
}
