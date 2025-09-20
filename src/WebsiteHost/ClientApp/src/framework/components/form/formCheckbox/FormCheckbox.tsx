import Checkbox from '../../checkbox/Checkbox.tsx';
import { createComponentId } from '../../Components.ts';
import { useFormValidation } from '../FormValidation.ts';

interface FormCheckboxProps {
  className?: string;
  id: string;
  name: string;
  label: string;
  dependencies?: string[];
}

// Creates a form checkbox element that supports validation
// Accepts all the usual properties for an input, like: name, label
// This input sets its default value based on the ancestor Form's defaultValues.
// This input displays a validation error message, based on its own validation state.
const FormCheckbox = ({ className, id, name, label, dependencies = [] }: FormCheckboxProps) => {
  const { validationError, register } = useFormValidation(name);
  const baseClasses = '';
  const classes = [baseClasses, className].filter(Boolean).join(' ');
  const componentId = createComponentId('form_checkbox', id);
  return (
    <Checkbox
      className={classes}
      id={componentId}
      label={label}
      errorMessage={validationError}
      {...register(name, { deps: dependencies })}
    />
  );
};

export default FormCheckbox;
