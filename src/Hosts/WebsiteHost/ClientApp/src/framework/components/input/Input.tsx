import React, { AllHTMLAttributes } from 'react';
import { createComponentId, toClasses } from '../Components';


type HTMLInputProps = AllHTMLAttributes<HTMLInputElement>;

export interface InputProps {
  className?: string;
  id?: string;
  name?: HTMLInputProps['name'];
  type?: HTMLInputProps['type'];
  size?: 'sm' | 'md' | 'lg';
  label?: string;
  placeholder?: HTMLInputProps['placeholder'];
  value?: HTMLInputProps['value'];
  disabled?: boolean;
  required?: boolean;
  errorMessage?: string;
  hintText?: string;
  fullWidth?: boolean;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur?: HTMLInputProps['onBlur'];
  onFocus?: HTMLInputProps['onFocus'];
  autoComplete?: HTMLInputProps['autoComplete'];
}

// Creates an input field with the specified type, and size
// Layout is critical:
// - The label occupies the first half of the width of the parent
// - The input occupies the second half of the width of the parent, for alignment with other form controls
// - We stack the input and label on top the errorMessage in mobile and desktop
// - We reserve space for the errorMessage and hintText below the input
const Input = React.forwardRef<HTMLInputElement, InputProps>(
  (
    {
      className,
      id,
      name,
      type = 'text',
      size = 'md',
      label,
      placeholder,
      value,
      disabled = false,
      required = false,
      errorMessage,
      hintText,
      fullWidth = false,
      onChange,
      onBlur,
      onFocus,
      autoComplete,
      ...props
    },
    ref
  ) => {
    const baseClasses =
      'w-full border-0 rounded-sm bg-gray-100 dark:bg-gray-800 outline-none text-sm text-gray-900 dark:text-gray-100 disabled:opacity-50 disabled:cursor-not-allowed placeholder-gray-500 dark:placeholder-gray-400';
    const sizeClasses = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'p-0 text-sm',
      lg: 'px-4 py-3 text-base'
    };
    const stateClasses = errorMessage
      ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
      : 'border-gray-300 focus:border-primary focus:ring-primary';
    const widthClass = fullWidth ? 'w-full' : '';
    const classes = toClasses([baseClasses, sizeClasses[size], stateClasses, widthClass, className]);
    const componentId = createComponentId('input', id);
    const labelText = label || name || componentId;
    return (
      <div className={`flex flex-col gap-1`} data-testid={`${componentId}_wrapper`}>
        <div className="flex flex-col">
          <div
            className={`border rounded-sm p-3 transition-colors ${errorMessage ? 'border-red-300 focus-within:border-red-500' : 'border-gray-300 focus-within:border-primary'}`}
          >
            {labelText && (
              <label
                className={`block text-xs font-medium text-gray-700 dark:text-gray-400 mb-1`}
                data-testid={`${componentId}_label`}
                htmlFor={componentId}
                aria-labelledby={componentId}
              >
                {`${labelText}:`}
                {required && (
                  <span className="text-red-500 ml-1" data-testid={`${componentId}_required`}>
                    *
                  </span>
                )}
              </label>
            )}
            <input
              className={classes}
              data-testid={componentId}
              id={componentId}
              name={name}
              type={type}
              placeholder={placeholder}
              value={value}
              disabled={disabled}
              required={required}
              onChange={onChange}
              onBlur={onBlur}
              onFocus={onFocus}
              autoComplete={autoComplete}
              ref={ref}
              {...props}
            />
          </div>
          <div className="mt-1 h-6 flex items-start">
            {errorMessage && (
              <p className="text-xs text-red-600 break-words" data-testid={`${componentId}_error`}>
                {errorMessage}
              </p>
            )}
            {hintText && !errorMessage && (
              <p className="text-xs text-gray-500 break-words" data-testid={`${componentId}_hint`}>
                {hintText}
              </p>
            )}
          </div>
        </div>
      </div>
    );
  }
);

export default Input;
