import React, { AllHTMLAttributes } from 'react';
import { createComponentId } from '../Components';


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
    const componentId = createComponentId('input', id);
    const labelText = label || name || componentId;
    return (
      <div
        className={`grid grid-cols-1 sm:grid-cols-[auto_1fr_auto] sm:grid-rows-[auto_auto] gap-1 sm:gap-2 items-start ${fullWidth ? 'w-full' : ''}`}
        data-testid={`${componentId}_wrapper`}
      >
        <div className="sm:order-1">
          {labelText && (
            <label
              className={`block text-sm font-medium text-gray-700 sm:min-w-0 sm:flex-shrink-0 ${size === 'lg' ? 'pt-3' : 'pt-2'}`}
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
        </div>

        <div className="sm:order-2 flex flex-col">
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

          {/* Always reserve space for error/hint messages */}
          <div className="mt-1 min-h-[1.25rem]">
            <p
              className={`text-sm transition-opacity duration-200 ${
                errorMessage ? 'text-red-600 opacity-100' : 'opacity-0'
              }`}
              data-testid={`${componentId}_error`}
              aria-live="polite"
            >
              {errorMessage || '\u00A0'}
            </p>
            <p
              className={`text-sm text-gray-500 transition-opacity duration-200 ${
                hintText && !errorMessage ? 'opacity-100' : 'opacity-0'
              }`}
              data-testid={`${componentId}_hint`}
              aria-live="polite"
            >
              {hintText || '\u00A0'}
            </p>
          </div>
        </div>
      </div>
    );
  }
);

export default Input;
