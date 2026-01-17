import { ReactNode } from 'react';
import { createComponentId, toClasses } from '../Components.ts';
import Icon, { IconSymbol } from '../icon/Icon';
import { TailwindColor } from '../typography/Tailwind.ts';


interface AlertProps {
  id?: string;
  children?: ReactNode;
  type?: AlertType;
  title?: string;
  message?: string | null;
}

type AlertType = 'info' | 'error' | 'warning' | 'success';

const BorderColors = {
  error: 'border-error-600 dark:border-error-400',
  success: 'border-success-600 dark:border-success-400',
  warning: 'border-warning-600 dark:border-warning-400',
  info: 'border-info-600 dark:border-info-400'
} as const;

const Icons = {
  error: 'exclamation-triangle-fill',
  warning: 'exclamation-triangle-fill',
  success: 'check-circle-fill',
  info: 'info-circle-fill'
} as const;

const BackgroundColors = {
  error: 'bg-error-100 dark:bg-error-900/20',
  success: 'bg-success-100 dark:bg-success-900/20',
  warning: 'bg-warning-100 dark:bg-warning-900/20',
  info: 'bg-info-100 dark:bg-info-900/20'
} as const;

const IconColors = {
  error: 'error-600',
  success: 'success-600',
  warning: 'warning-600',
  info: 'info-600'
} as const;

const TitleColors = {
  error: 'text-error-600 dark:text-error-200',
  success: 'text-success-600 dark:text-success-200',
  warning: 'text-warning-600 dark:text-warning-200',
  info: 'text-info-600 dark:text-info-200'
} as const;

const TextColors = {
  error: 'text-error-800 dark:text-error-200',
  success: 'text-success-800 dark:text-success-200',
  warning: 'text-warning-800 dark:text-warning-200',
  info: 'text-info-800 dark:text-info-200'
} as const;

// Creates an inline alert to display a title and a message
export default function Alert({ id, children, type = 'info', title, message }: AlertProps) {
  if (!message && !children) {
    return null;
  }

  const iconType: IconSymbol = Icons[type];
  const iconColor = IconColors[type] as TailwindColor;
  const textColor = TextColors[type] as TailwindColor;
  const titleColor = TitleColors[type] as TailwindColor;
  const backgroundColor = BackgroundColors[type] as TailwindColor;
  const borderColor = BorderColors[type] as TailwindColor;
  const baseClasses = 'px-2 py-[8px] rounded border';
  const classes = toClasses([baseClasses, backgroundColor, borderColor]);
  const componentId = createComponentId('alert', id);
  return (
    <div className={classes} data-testid={componentId}>
      <div className="flex">
        <div className="flex-shrink-0 h-6 mr-3 flex items-center">
          <div className={`w-8 h-5`}>
            <Icon size={30} color={iconColor} symbol={iconType} />
          </div>
        </div>
        <div className="flex-1 mb-3">
          {title ? (
            <h1 className={`mt-1 mb-2 text-base font-semibold ${titleColor}`} data-testid={`${componentId}_title`}>
              {title}
            </h1>
          ) : (
            <div className="h-1"></div>
          )}
          <div className="text-xs">
            {message && (
              <span data-testid={`${componentId}_message`} className={textColor}>
                {message}
              </span>
            )}
          </div>
          <div className="text-xs">{children && <span data-testid={`${componentId}_children`}>{children}</span>}</div>
        </div>
      </div>
    </div>
  );
}
