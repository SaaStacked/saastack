import { useContext } from 'react';
import { createComponentId, toClasses } from '../../Components.ts';
import Select, { SelectOption } from '../../select/Select.tsx';
import { FormActionRequiredFieldsContext } from '../FormActionContexts.tsx';
import { useFormValidation } from '../FormValidation.ts';


export interface FormSelectProps {
  className?: string;
  id: string;
  name: string;
  options: SelectOption[];
  label?: string;
  placeholder?: string;
  dependencies?: string[];
}

// Creates a form select element that supports validation
// Accepts all the usual properties for a select, like: type, name, label, placeholder, dependencies
// This select communicates with an ancestor Form via useFormContext() and useContext(), to fetch the validation state.
// This select keeps track of its own validation state, based on the ancestor Form's validation state.
// This select triggers validation when the user interacts with the select, based on the ancestor Form's validatesWhen.
// This select sets its default value based on the ancestor Form's defaultValues.
// This select displays a validation error message, based on its own validation state.
const FormSelect = ({ className, id, name, options, label, placeholder, dependencies = [] }: FormSelectProps) => {
  const { validationError, register } = useFormValidation(name);
  const requiredFormFields = useContext(FormActionRequiredFieldsContext);
  const isRequired = requiredFormFields.includes(name);
  const baseClasses = '';
  const classes = toClasses([baseClasses, className]);
  const componentId = createComponentId('form_select', id);
  return (
    <Select
      className={classes}
      id={componentId}
      options={options}
      label={label}
      required={isRequired}
      placeholder={placeholder}
      errorMessage={validationError}
      {...register(name, { deps: dependencies })}
    />
  );
};

export default FormSelect;
