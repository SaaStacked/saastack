import { ReactNode } from 'react';
import { createComponentId } from '../Components.ts';
import Icon, { IconSymbol, TailwindColor } from '../icon/Icon';

interface AlertProps {
  id?: string;
  children?: ReactNode;
  type?: AlertType;
  title?: string;
  message?: string | null;
}

type AlertType = 'info' | 'error' | 'warning' | 'success';

const BorderColors = {
  error: 'border-red-600',
  success: 'border-green-600',
  warning: 'border-yellow-600',
  info: 'border-blue-600'
} as const;

const Icons = {
  error: 'exclamation-triangle-fill',
  warning: 'exclamation-triangle-fill',
  success: 'check-circle-fill',
  info: 'info-circle-fill'
} as const;

const IconColors = {
  error: 'red-600',
  success: 'green-600',
  warning: 'yellow-600',
  info: 'blue-600'
} as const;

const BackgroundColors = {
  error: 'bg-red-100',
  success: 'bg-green-100',
  warning: 'bg-yellow-100',
  info: 'bg-blue-100'
} as const;

const TextColors = {
  error: 'red-800',
  success: 'base',
  warning: 'base',
  info: 'base'
} as const;

const TitleColors = {
  error: 'red-600',
  success: 'green-600',
  warning: 'yellow-600',
  info: 'blue-600'
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
  const baseClasses = 'px-4 py-[18px] rounded border';
  const classes = [baseClasses, backgroundColor, borderColor].filter(Boolean).join(' ');
  const componentId = createComponentId('alert', id);
  return (
    <div className={classes} data-testid={componentId}>
      <div className="flex">
        <div className="flex-shrink-0 h-6 mr-3 flex items-center">
          <div className={`w-8 h-5`}>
            <Icon size={30} color={iconColor} symbol={iconType} />
          </div>
        </div>
        <div className="flex-1">
          {title && (
            <h3 className={`text-lg font-semibold mb-2 text-${titleColor}`} data-testid={`${componentId}_title`}>
              {title}
            </h3>
          )}
          <div className="text-sm">
            {message && (
              <span data-testid={`${componentId}_message`} className={`text-${textColor}`}>
                {message}
              </span>
            )}
          </div>
          <div className="text-sm">{children && <span data-testid={`${componentId}_children`}>{children}</span>}</div>
        </div>
      </div>
    </div>
  );
}
