import { useContext } from 'react';
import { useFormState } from 'react-hook-form';
import Button from '../../Button';
import { ActionContext } from '../Contexts';


interface FormSubmitButtonProps {
  id?: string;
  label?: string;
}

// Creates a submit button that is wired into the form.
export default function FormSubmitButton({ id, label = 'Submit' }: FormSubmitButtonProps) {
  const action = useContext(ActionContext);
  const formState = useFormState();
  const isExecuting = action?.isExecuting ?? false;
  const disabled = !action?.isReady || (formState.isSubmitted && !formState.isValid);
  const componentId = id ? `${id}_form_submit` : 'form_submit';
  return <Button id={componentId} label={label ?? 'Submit'} busy={isExecuting} disabled={disabled} type="submit" />;
}
