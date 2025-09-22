import React, { AllHTMLAttributes } from 'react';
import { createComponentId } from '../Components';


type HTMLInputProps = AllHTMLAttributes<HTMLInputElement>;

export interface CheckboxProps {
  className?: string;
  id?: string;
  children?: React.ReactNode;
  name?: HTMLInputProps['name'];
  size?: 'sm' | 'md' | 'lg';
  label?: string;
  value?: HTMLInputProps['checked'];
  disabled?: boolean;
  errorMessage?: string;
  fullWidth?: boolean;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur?: HTMLInputProps['onBlur'];
  onFocus?: HTMLInputProps['onFocus'];
}

// Creates a checkbox field with the specified size
// Layout is critical:
// - We distance ourselves from the form field above in both mobile and desktop, to maintain vertical spacing
// - We occupy the second half of the width of the parent, for alignment with other form controls
// - We have a placeholder in the left cell that is hidden on mobile, but visible on desktop
// - We stack the input and label on top the errorMessage in mobile and desktop
const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  (
    {
      className,
      id,
      children,
      name,
      size = 'md',
      label,
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
      sm: 'w-4 h-4 text-sm',
      md: 'w-5 h-5 text-sm',
      lg: 'w-6 h-6 text-base'
    };
    const stateClasses = errorMessage
      ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
      : 'border-gray-300 focus:border-primary focus:ring-primary';
    const widthClass = fullWidth ? 'w-full' : '';
    const classes = [baseClasses, sizeClasses[size], stateClasses, widthClass, className].filter(Boolean).join(' ');
    const componentId = createComponentId('checkbox', id);
    const labelText = children || label || name || componentId;
    return (
      <div
        className={`grid grid-cols-1 sm:grid-cols-2 gap-1 sm:gap-2 items-start mt-2`}
        data-testid={`${componentId}_wrapper`}
      >
        <div className="flex flex-col sm:col-span-2">
          <div className="flex items-center">
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
              className="ml-2 text-sm font-medium text-gray-700 flex-1 w-full"
              data-testid={`${componentId}_label`}
              htmlFor={componentId}
              aria-labelledby={componentId}
            >
              {labelText}
            </label>
          </div>
          <div className="mt-1 h-12 flex items-start w-full overflow-hidden">
            {errorMessage && (
              <p className="mt-1 text-sm text-red-600 break-words" data-testid={`${componentId}_error`}>
                {errorMessage}
              </p>
            )}
          </div>
        </div>
      </div>
    );
  }
);

export default Checkbox;
