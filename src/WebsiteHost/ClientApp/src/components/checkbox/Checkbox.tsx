import React, { AllHTMLAttributes } from 'react';
import { createComponentId } from '../Components';

type HTMLInputProps = AllHTMLAttributes<HTMLInputElement>;

export interface CheckboxProps {
  className?: string;
  id?: string;
  name?: HTMLInputProps['name'];
  size?: 'sm' | 'md' | 'lg';
  label: string;
  placeholder?: HTMLInputProps['placeholder'];
  value?: HTMLInputProps['checked'];
  disabled?: boolean;
  errorMessage?: string;
  fullWidth?: boolean;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur?: HTMLInputProps['onBlur'];
  onFocus?: HTMLInputProps['onFocus'];
}

// Creates an input field with the specified type, and size
const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  (
    {
      className,
      id,
      name,
      size = 'md',
      label,
      placeholder,
      value,
      disabled = false,
      errorMessage,
      fullWidth = false,
      onChange,
      onBlur,
      onFocus,
      ...props
    },
    ref
  ) => {
    const baseClasses =
      'border rounded-sm transition-colors focus:outline-none focus:ring-1 focus:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed';
    const sizeClasses = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'px-3 py-2 text-sm',
      lg: 'px-4 py-3 text-base'
    };
    const stateClasses = errorMessage
      ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
      : 'border-gray-300 focus:border-primary focus:ring-primary';
    const widthClass = fullWidth ? 'w-full' : '';
    const classes = [baseClasses, sizeClasses[size], stateClasses, widthClass, className].filter(Boolean).join(' ');
    const componentId = createComponentId('checkbox', id);
    const labelText = label || name || componentId;
    return (
      <div className={`grid grid-cols-1 gap-1 sm:gap-2`} data-testid={`${componentId}_wrapper`}>
        <div className={`sm:order-1 flex items-center`}>
          <input
            className={classes}
            data-testid={componentId}
            id={componentId}
            name={name}
            type="checkbox"
            checked={value}
            disabled={disabled}
            onChange={onChange}
            onBlur={onBlur}
            onFocus={onFocus}
            ref={ref}
            {...props}
          />
          <label
            className={`ml-2 text-sm font-medium text-gray-700`}
            data-testid={`${componentId}_label`}
            htmlFor={componentId}
            aria-labelledby={componentId}
          >
            {labelText}
          </label>
        </div>
        <div className="sm:order-2">
          {errorMessage && (
            <p className="mt-1 text-sm text-red-600" data-testid={`${componentId}_error`}>
              {errorMessage}
            </p>
          )}
        </div>
      </div>
    );
  }
);

export default Checkbox;
