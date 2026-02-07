import React, { AllHTMLAttributes } from 'react';
import { createComponentId, toClasses } from '../Components';
import Icon from '../icon/Icon.tsx';


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
      'w-full border-0 rounded-sm bg-white dark:bg-neutral-900 outline-none text-sm text-neutral-900 dark:text-neutral-100 disabled:opacity-60 disabled:cursor-not-allowed placeholder-neutral-400 dark:placeholder-neutral-500';
    const sizeClasses = {
      sm: 'px-3 py-1.5 text-xs',
      md: 'p-0 text-sm',
      lg: 'px-4 py-3 text-base'
    };
    const stateClasses = errorMessage
      ? 'border-error focus:border-error focus:ring-error'
      : 'border-neutral-300 focus:border-brand-primary focus:ring-brand-primary';
    const widthClass = fullWidth ? 'w-full' : '';
    const classes = toClasses([baseClasses, sizeClasses[size], stateClasses, widthClass, className]);
    const componentId = createComponentId('input', id);
    const labelText = label || name || componentId;
    return (
      <div className={`flex flex-col gap-1`} data-testid={`${componentId}_wrapper`}>
        <div className="flex flex-col">
          <div
            className={`border rounded-md p-3 transition-all duration-150 ${errorMessage ? 'border-error focus-within:border-error-700' : 'border-white dark:border-neutral-900 hover:border-neutral-400 dark:hover:border-neutral-700 focus-within:border-brand-primary dark:focus-within:border-brand-primary'}`}
          >
            {labelText && (
              <label
                className={`block text-xs font-medium text-neutral-700 dark:text-neutral-300 mb-1`}
                data-testid={`${componentId}_label`}
                htmlFor={componentId}
                aria-labelledby={componentId}
              >
                {`${labelText}:`}
                {required && (
                  <span className="text-error ml-1" data-testid={`${componentId}_required`}>
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
              <div className="text-xs text-error break-words" data-testid={`${componentId}_error`}>
                <Icon className="pr-1" size={12} color="error" symbol="exclamation-circle" />
                {errorMessage}
              </div>
            )}
            {hintText && !errorMessage && (
              <p
                className="text-xs text-neutral-500 dark:text-neutral-400 break-words"
                data-testid={`${componentId}_hint`}
              >
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
