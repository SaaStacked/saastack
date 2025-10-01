import React, { AllHTMLAttributes } from 'react';
import { createComponentId, toClasses } from '../Components';


type HTMLSelectProps = AllHTMLAttributes<HTMLSelectElement>;

export type SelectOption = {
  value: string;
  label: string;
};

export interface SelectProps {
  className?: string;
  id?: string;
  name?: HTMLSelectProps['name'];
  options: SelectOption[];
  size?: 'sm' | 'md' | 'lg';
  label?: string;
  placeholder?: HTMLSelectProps['placeholder'];
  value?: HTMLSelectProps['value'];
  disabled?: boolean;
  required?: boolean;
  errorMessage?: string;
  hintText?: string;
  fullWidth?: boolean;
  onChange?: (event: React.ChangeEvent<HTMLSelectElement>) => void;
  onBlur?: HTMLSelectProps['onBlur'];
  onFocus?: HTMLSelectProps['onFocus'];
}

// Creates a dropbox box with selectable options with the specified size and options
// Layout is critical:
// - The label occupies the first half of the width of the parent
// - The select occupies the second half of the width of the parent, for alignment with other form controls
// - We stack the select and label on top the errorMessage in mobile and desktop
// - We reserve space for the errorMessage and hintText below the select
const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  (
    {
      className,
      id,
      name,
      options,
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
    const componentId = createComponentId('select', id);
    const showPlaceholder = (value === undefined || value === null || value === '') && placeholder !== undefined;
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
            <select
              className={classes}
              data-testid={componentId}
              id={componentId}
              name={name}
              value={value}
              disabled={disabled}
              required={required}
              onChange={onChange}
              onBlur={onBlur}
              onFocus={onFocus}
              ref={ref}
              {...props}
            >
              {showPlaceholder && (
                <option value="" disabled>
                  {placeholder}
                </option>
              )}
              {options.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
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

export default Select;
