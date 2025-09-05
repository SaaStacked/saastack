import React from 'react';

export interface InputProps {
  className?: string;
  label?: string;
  placeholder?: string;
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url';
  value?: string;
  defaultValue?: string;
  disabled?: boolean;
  required?: boolean;
  error?: boolean;
  errorMessage?: string;
  helpText?: string;
  size?: 'sm' | 'md' | 'lg';
  fullWidth?: boolean;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur?: (event: React.FocusEvent<HTMLInputElement>) => void;
  onFocus?: (event: React.FocusEvent<HTMLInputElement>) => void;
  name?: string;
  id?: string;
  autoComplete?: string;
}

const Input: React.FC<InputProps> = ({
  className = '',
  label,
  placeholder,
  type = 'text',
  value,
  defaultValue,
  disabled = false,
  required = false,
  error = false,
  errorMessage,
  helpText,
  size = 'md',
  fullWidth = false,
  onChange,
  onBlur,
  onFocus,
  name,
  id,
  autoComplete
}) => {
  const baseClasses =
    'border rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed';

  const sizeClasses = {
    sm: 'px-3 py-1.5 text-sm',
    md: 'px-3 py-2 text-sm',
    lg: 'px-4 py-3 text-base'
  };

  const stateClasses = error
    ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
    : 'border-gray-300 focus:border-primary-500 focus:ring-primary-500';

  const widthClass = fullWidth ? 'w-full' : '';

  const inputClasses = [baseClasses, sizeClasses[size], stateClasses, widthClass, className].filter(Boolean).join(' ');

  const randomId = `input-${Math.random().toString(36).substring(7)}`;
  const inputId = id || randomId;
  const labelText = label || inputId;

  return (
    <div className={fullWidth ? 'w-full' : ''}>
      {labelText && (
        <label htmlFor={inputId} aria-labelledby={inputId} className="block text-sm font-medium text-gray-700 mb-1">
          {labelText}:{required && <span className="text-red-500 ml-1">*</span>}
        </label>
      )}
      <input
        id={inputId}
        name={name}
        type={type}
        placeholder={placeholder}
        value={value}
        defaultValue={defaultValue}
        disabled={disabled}
        required={required}
        onChange={onChange}
        onBlur={onBlur}
        onFocus={onFocus}
        autoComplete={autoComplete}
        className={inputClasses}
      />
      {errorMessage && error && <p className="mt-1 text-sm text-red-600">{errorMessage}</p>}
      {helpText && !error && <p className="mt-1 text-sm text-gray-500">{helpText}</p>}
    </div>
  );
};

export default Input;
