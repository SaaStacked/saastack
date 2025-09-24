import React from 'react';
import Checkbox from '../../checkbox/Checkbox.tsx';
import { createComponentId, toClasses } from '../../Components.ts';
import { useFormValidation } from '../FormValidation.ts';


interface FormCheckboxProps {
  className?: string;
  id: string;
  children?: React.ReactNode;
  name: string;
  label?: string;
  dependencies?: string[];
}

// Creates a checkbox field that supports validation
// Accepts all the usual properties for a checkbox, like: name, label, dependencies and children
// This input communicates with an ancestor Form via useFormContext() and useContext(), to fetch the validation state.
// This input keeps track of its own validation state, based on the ancestor Form's validation state.
// This input triggers validation when the user interacts with the input, based on the ancestor Form's validatesWhen.
// This input sets its default value based on the ancestor Form's defaultValues.
// This input displays a validation error message, based on its own validation state.
const FormCheckbox = ({ className, id, children, name, label, dependencies = [] }: FormCheckboxProps) => {
  const { validationError, register } = useFormValidation(name);
  const baseClasses = '';
  const classes = toClasses([baseClasses, className]);
  const componentId = createComponentId('form_checkbox', id);
  return (
    <Checkbox
      className={classes}
      id={componentId}
      label={children ? (undefined as any) : label}
      errorMessage={validationError}
      {...register(name, { deps: dependencies })}
    >
      {children}
    </Checkbox>
  );
};

export default FormCheckbox;
