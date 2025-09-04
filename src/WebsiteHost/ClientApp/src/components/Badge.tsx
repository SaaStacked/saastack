import React from 'react';

export interface BadgeProps {
  /** Badge content */
  children: React.ReactNode;
  /** Badge variant */
  variant?: 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info';
  /** Badge size */
  size?: 'sm' | 'md' | 'lg';
  /** Badge style */
  style?: 'filled' | 'outlined' | 'soft';
  /** Removable badge */
  removable?: boolean;
  /** Remove handler */
  onRemove?: () => void;
  /** Additional CSS classes */
  className?: string;
}

const Badge: React.FC<BadgeProps> = ({
  children,
  variant = 'default',
  size = 'md',
  style = 'filled',
  removable = false,
  onRemove,
  className = ''
}) => {
  const baseClasses = 'inline-flex items-center font-medium rounded-full';

  const sizeClasses = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-0.5 text-sm',
    lg: 'px-3 py-1 text-sm'
  };

  const variantClasses = {
    filled: {
      default: 'bg-gray-100 text-gray-800',
      primary: 'bg-primary-600 text-white',
      secondary: 'bg-secondary-600 text-white',
      success: 'bg-green-600 text-white',
      warning: 'bg-yellow-600 text-white',
      danger: 'bg-red-600 text-white',
      info: 'bg-blue-600 text-white'
    },
    outlined: {
      default: 'border border-gray-300 text-gray-700 bg-white',
      primary: 'border border-primary-600 text-primary-600 bg-white',
      secondary: 'border border-secondary-600 text-secondary-600 bg-white',
      success: 'border border-green-600 text-green-600 bg-white',
      warning: 'border border-yellow-600 text-yellow-600 bg-white',
      danger: 'border border-red-600 text-red-600 bg-white',
      info: 'border border-blue-600 text-blue-600 bg-white'
    },
    soft: {
      default: 'bg-gray-100 text-gray-800',
      primary: 'bg-primary-100 text-primary-800',
      secondary: 'bg-secondary-100 text-secondary-800',
      success: 'bg-green-100 text-green-800',
      warning: 'bg-yellow-100 text-yellow-800',
      danger: 'bg-red-100 text-red-800',
      info: 'bg-blue-100 text-blue-800'
    }
  };

  const classes = [baseClasses, sizeClasses[size], variantClasses[style][variant], className].filter(Boolean).join(' ');

  return (
    <span className={classes}>
      {children}
      {removable && (
        <button
          type="button"
          className="ml-1 inline-flex items-center justify-center w-4 h-4 rounded-full hover:bg-black hover:bg-opacity-10 focus:outline-none focus:bg-black focus:bg-opacity-10"
          onClick={onRemove}
        >
          <svg className="w-2 h-2" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
            <path
              fillRule="evenodd"
              d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
              clipRule="evenodd"
            />
          </svg>
        </button>
      )}
    </span>
  );
};

export default Badge;
