import React from 'react';
import { useNavigate } from 'react-router-dom';
import { createComponentId } from '../Components.ts';


export interface ButtonProps {
  className?: string;
  id?: string;
  children?: React.ReactNode;
  type?: 'button' | 'submit' | 'reset';
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  busy?: boolean;
  fullWidth?: boolean;
  label?: string;
  onClick?: () => void;
  navigateTo?: string;
}

const Button: React.FC<ButtonProps> = ({
  className = '',
  children,
  id,
  type = 'button',
  variant = 'primary',
  size = 'md',
  disabled = false,
  busy = false,
  fullWidth = false,
  label,
  onClick,
  navigateTo
}) => {
  const baseClasses =
    'inline-flex items-center justify-center font-medium rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';
  const variantClasses = {
    primary: `${type === 'submit' ? 'bg-green-600' : 'bg-primary'} text-white hover:bg-primary focus:ring-primary`,
    secondary: 'bg-secondary text-white hover:bg-secondary focus:ring-secondary',
    outline: 'border border-gray-300 bg-white text-gray-700 hover:bg-gray-50 focus:ring-primary',
    ghost: 'text-gray-700 hover:bg-gray-100 focus:ring-primary',
    danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500'
  };
  const sizeClasses = {
    sm: 'px-3 py-1.5 text-sm',
    md: 'px-4 py-2 text-sm',
    lg: 'px-6 py-3 text-base'
  };
  const widthClass = fullWidth ? 'w-full' : '';
  const classes = [baseClasses, variantClasses[variant], sizeClasses[size], widthClass, className]
    .filter(Boolean)
    .join(' ');
  let onClickTarget = onClick;
  let navigate = useNavigate();
  if (navigateTo) {
    onClickTarget = () => navigate(navigateTo);
  }
  const componentId = createComponentId('button', id);
  return (
    <button
      data-testid={componentId}
      type={type}
      className={classes}
      disabled={disabled || busy}
      onClick={onClickTarget}
    >
      {busy && (
        <svg
          className="animate-spin -ml-1 mr-2 h-4 w-4"
          data-testid={`${componentId}_busy`}
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
        >
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
      )}
      {label || children}
    </button>
  );
};

export default Button;
