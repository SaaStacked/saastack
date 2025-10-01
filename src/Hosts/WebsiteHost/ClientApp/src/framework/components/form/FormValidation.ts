import { useContext, useEffect, useState } from 'react';
import { useFormContext } from 'react-hook-form';
import { FormActionValidationContext } from './FormActionContexts.tsx';


// A hook that begins validation based on the ancestor Form's validatesWhen.
// It keeps track of whether the field has been validated, and whether it has an error.
// Allows components to communicate with an ancestor Form via useFormContext() and useContext(), to fetch the validation state.
// Enables controls to keep track of its own validation state, based on the ancestor Form's validation state.
// Enables controls to trigger validation when the user interacts with the input, based on the ancestor Form's validatesWhen.
export function useFormValidation(fieldName: string, errorMessage?: string) {
  const {
    formState: { errors, isSubmitted: isFormSubmitted, dirtyFields, touchedFields },
    register,
    control
  } = useFormContext();
  const [hasValidationBegun, setHasValidationBegun] = useState(false);
  const hasValidationError = getNestedField(errors, fieldName) !== undefined;
  const isFieldTouched = getNestedField(touchedFields, fieldName);
  const validatesWhen = useContext(FormActionValidationContext);

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
    register,
    control
  };
}

export function getNestedField(object: any, path: string) {
  if (!object) {
    return undefined;
  }
  if (path === '') {
    return object;
  }

  return path.split('.').reduce((obj, key) => obj?.[key], object);
}
