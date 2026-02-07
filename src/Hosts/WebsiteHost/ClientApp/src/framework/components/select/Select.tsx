import React, { AllHTMLAttributes } from 'react';
import { createComponentId, toClasses } from '../Components';
import Icon from '../icon/Icon.tsx';


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
      'w-full border-0 rounded-sm bg-neutral-100 dark:bg-neutral-800 outline-none text-sm text-neutral-900 dark:text-neutral-100 disabled:opacity-50 disabled:cursor-not-allowed placeholder-neutral-500 dark:placeholder-neutral-400';
    const sizeClasses = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'p-0 text-sm',
      lg: 'px-4 py-3 text-base'
    };
    const stateClasses = errorMessage
      ? 'border-error focus:border-error focus:ring-error'
      : 'border-neutral-300 focus:border-brand-primary focus:ring-brand-primary';
    const widthClass = fullWidth ? 'w-full' : '';
    const classes = toClasses([baseClasses, sizeClasses[size], stateClasses, widthClass, className]);
    const componentId = createComponentId('select', id);
    const showPlaceholder = (value === undefined || value === null || value === '') && placeholder !== undefined;
    const labelText = label || name || componentId;
    return (
      <div className={`flex flex-col gap-1`} data-testid={`${componentId}_wrapper`}>
        <div className="flex flex-col">
          <div
            className={`border rounded-sm p-3 transition-colors ${errorMessage ? 'border-error focus-within:border-error-700' : 'border-white dark:border-neutral-900 hover:border-neutral-400 dark:hover:border-neutral-700 focus-within:border-brand-primary dark:focus-within:border-brand-primary'}`}
          >
            {labelText && (
              <label
                className={`block text-xs font-medium text-neutral-700 dark:text-neutral-400 mb-1`}
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
              <div className="text-xs text-error break-words" data-testid={`${componentId}_error`}>
                <Icon className="pr-1" size={12} color="error" symbol="exclamation-circle" />
                {errorMessage}
              </div>
            )}
            {hintText && !errorMessage && (
              <p className="text-xs text-neutral-500 break-words" data-testid={`${componentId}_hint`}>
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
