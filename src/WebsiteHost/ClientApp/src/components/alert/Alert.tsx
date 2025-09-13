import { ReactNode } from 'react';
import Icon, { IconSymbol } from '../icon/Icon';

interface AlertProps {
  id?: string;
  children?: ReactNode;
  type?: AlertType;
  title?: string;
  message?: string | null;
}

type AlertType = 'info' | 'error' | 'warning' | 'success';

const alertColors = {
  error: 'border-red-600 bg-red-50',
  success: 'border-green-600 bg-green-50',
  warning: 'border-yellow-600 bg-yellow-50',
  info: 'border-blue-600 bg-blue-50'
} as const;

const alertIcons = {
  error: 'exclamation-triangle-fill',
  warning: 'exclamation-triangle-fill',
  success: 'check-circle-fill',
  info: 'info-circle-fill'
} as const;

const IconColors = {
  error: 'text-red-600',
  success: 'text-green-600',
  warning: 'text-yellow-600',
  info: 'text-blue-600'
} as const;

// Creates an inline alert to display a title and a message
export default function Alert({ id, children, type = 'info', title, message }: AlertProps) {
  if (!message && !children) {
    return null;
  }

  const iconType: IconSymbol = alertIcons[type];
  const componentId = `${id}_alert`;
  return (
    <div data-testid={componentId} className={`px-4 py-[18px] rounded border ${alertColors[type]}`}>
      <div className="flex">
        <div className="flex-shrink-0 h-6 mr-3 flex items-center">
          <div className={`w-5 h-5 ${IconColors[type]}`}>
            <Icon size={20} symbol={iconType} />
          </div>
        </div>
        <div className="flex-1">
          {title && (
            <h3 className="text-lg font-semibold mb-2" data-testid={`${componentId}_title`}>
              {title}
            </h3>
          )}
          <div className="text-sm">
            {children ? (
              <span data-testid={`${componentId}_children`}>{children}</span>
            ) : (
              <span data-testid={`${componentId}_message`}>{message}</span>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
