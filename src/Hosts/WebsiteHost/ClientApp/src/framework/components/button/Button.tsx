import React from 'react';
import { useNavigate } from 'react-router-dom';
import { createComponentId, toClasses } from '../Components.ts';

export interface ButtonProps {
  className?: string;
  id?: string;
  children?: React.ReactNode;
  type?: 'button' | 'submit' | 'reset';
  variant?: 'brand-primary' | 'brand-secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  busy?: boolean;
  fullWidth?: boolean;
  label?: string;
  onClick?: () => void;
  navigateTo?: string;
  title?: string;
}

const Button: React.FC<ButtonProps> = ({
  className = '',
  children,
  id,
  type = 'button',
  variant = 'brand-primary',
  size = 'md',
  disabled = false,
  busy = false,
  fullWidth = false,
  label,
  onClick,
  navigateTo,
  title
}) => {
  const baseClasses =
    'inline-flex items-center justify-center font-medium rounded-full transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed shadow-sm';
  const variantClasses = {
    'brand-primary': 'bg-brand-primary text-white hover:bg-brand-primary/90 focus:ring-brand-primary',
    'brand-secondary': 'bg-brand-secondary text-white hover:bg-brand-secondary/90 focus:ring-brand-secondary',
    outline:
      'border border-neutral-300 dark:border-neutral-600 bg-white dark:bg-neutral-800 text-neutral-700 dark:text-neutral-200 hover:bg-neutral-50 dark:hover:bg-neutral-700 focus:ring-brand-primary-500',
    ghost:
      'text-neutral-700 dark:text-neutral-200 hover:bg-neutral-50 dark:hover:bg-neutral-800 focus:ring-brand-primary-500 shadow-none',
    danger: 'bg-red-700 text-white hover:bg-red-800 focus:ring-red-500 border border-transparent'
  };
  const sizeClasses = {
    sm: 'px-3 py-1.5 text-xs',
    md: 'px-4 py-2 text-sm',
    lg: 'px-6 py-3 text-base'
  };
  const widthClass = fullWidth ? 'w-full' : 'w-fit';
  const classes = toClasses([baseClasses, variantClasses[variant], sizeClasses[size], widthClass, className]);
  let onClickTarget = onClick;
  let navigate = useNavigate();
  if (navigateTo) {
    onClickTarget = () => navigate(navigateTo);
  }
  const componentId = createComponentId('button', id);
  return (
    <button
      className={classes}
      data-testid={componentId}
      type={type}
      disabled={disabled || busy}
      onClick={onClickTarget}
      title={title}
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
