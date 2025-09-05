import { useContext, useEffect, useState } from 'react';
import { useFormContext } from 'react-hook-form';
import { createComponentId } from '../../Components.ts';
import Input from '../../input/Input';
import { ActionFormRequiredFieldsContext, ActionFromValidationContext } from '../Contexts.tsx';

export interface FormInputProps {
  className?: string;
  id: string;
  type?: 'email' | 'text' | 'password' | 'number';
  autoComplete?: 'username' | 'current-password' | 'new-password';
  name: string;
  label?: string;
  placeholder?: string;
  dependencies?: string[];
}

// A hook that begins validation based on the ancestor Form's validatesWhen.
// It keeps track of whether the field has been validated, and whether it has an error.
function useFormValidation(fieldName: string, errorMessage?: string) {
  const {
    formState: { errors, isSubmitted: isFormSubmitted, dirtyFields, touchedFields },
    register
  } = useFormContext();
  const [hasValidationBegun, setHasValidationBegun] = useState(false);
  const hasValidationError = getNestedField(errors, fieldName) !== undefined;
  const isFieldTouched = getNestedField(touchedFields, fieldName);
  const validatesWhen = useContext(ActionFromValidationContext);

  useEffect(() => {
    if (!hasValidationBegun) {
      switch (validatesWhen) {
        case 'onSubmit':
          if (isFormSubmitted) {
            setHasValidationBegun(true);
          }
          break;
        case 'onTouched':
        case 'onBlur':
          if (isFormSubmitted || isFieldTouched) {
            setHasValidationBegun(true);
          }
          break;
        case 'onChange':
        case 'all':
          if (getNestedField(dirtyFields, fieldName)) {
            setHasValidationBegun(true);
          }
          break;
      }
    }
  }, [dirtyFields, isFormSubmitted, fieldName, isFieldTouched, validatesWhen, hasValidationBegun]);

  const validationError = errorMessage ?? getNestedField(errors, fieldName)?.message;
  const isInvalid = hasValidationError && hasValidationBegun;
  const isValid = !hasValidationError && hasValidationBegun;
  return {
    validationError,
    isInvalid,
    isValid,
    isFieldTouched,
    register
  };
}

// Creates a form input element that supports validation
// Accepts all the usual properties for an input, like: type, autoComplete, name, label, placeholder, dependencies
// This input communicates with an ancestor Form via useFormContext() and useContext(), to fetch the validation state.
// This input keeps track of its own validation state, based on the ancestor Form's validation state.
// This input triggers validation when the user interacts with the input, based on the ancestor Form's validatesWhen.
// This input sets its default value based on the ancestor Form's defaultValues.
// This input displays a validation error message, based on its own validation state.
const FormInput = ({
  className,
  id,
  type = 'text',
  autoComplete,
  name,
  label,
  placeholder,
  dependencies = []
}: FormInputProps) => {
  const { validationError, register } = useFormValidation(name);
  const requiredFormFields = useContext(ActionFormRequiredFieldsContext);
  const isRequired = requiredFormFields.includes(name);
  const baseClasses = '';
  const classes = [baseClasses, className].filter(Boolean).join(' ');
  const componentId = createComponentId('form_input', id);
  return (
    <Input
      className={classes}
      id={componentId}
      type={type}
      label={label}
      required={isRequired}
      autoComplete={autoComplete}
      placeholder={placeholder}
      errorMessage={validationError}
      {...register(name, { deps: dependencies })}
    />
  );
};

export function getNestedField(object: any, path: string) {
  if (!object) {
    return undefined;
  }
  if (path === '') {
    return object;
  }

  return path.split('.').reduce((obj, key) => obj?.[key], object);
}

export default FormInput;
