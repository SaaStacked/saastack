import { useContext } from 'react';
import { createComponentId, toClasses } from '../../Components.ts';
import Input from '../../input/Input';
import { FormActionRequiredFieldsContext } from '../FormActionContexts.tsx';
import { useFormValidation } from '../FormValidation.ts';


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
  const requiredFormFields = useContext(FormActionRequiredFieldsContext);
  const isRequired = requiredFormFields.includes(name);
  const baseClasses = '';
  const classes = toClasses([baseClasses, className]);
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

export default FormInput;
