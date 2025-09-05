import { useContext } from 'react';
import { useFormState } from 'react-hook-form';
import { ActionResult } from '../../../actions/Actions.ts';
import Button from '../../button/Button';
import { createComponentId } from '../../Components.ts';
import { ActionFormContext } from '../Contexts';


interface FormSubmitButtonProps {
  id?: string;
  label?: string;
  busyLabel?: string;
  completeLabel?: string;
}

// Creates a submit button that is wired into the form.
// Accepts a label, that is displayed when the form is not executing.
// Accepts a busyLabel, that is displayed when the form is executing.
// The button is disabled if the form is not ready
// The button is disabled after the form has been submitted
// The button is disabled before the form has been submitted, and the form fails validation.
// When the button is clicked, it submits the ancestor Form.
export default function FormSubmitButton({
  id,
  label = 'Submit',
  busyLabel = 'Sending...',
  completeLabel = 'Success!'
}: FormSubmitButtonProps) {
  const action = useContext<ActionResult<any, any> | undefined>(ActionFormContext);
  const { isValid: isFormValid, isSubmitted: hasFormBeenSubmitted } = useFormState();
  const isReady = action?.isReady ?? false;
  const isExecuting = action?.isExecuting ?? false;
  const isSuccess = action?.isSuccess;
  const isCompleted = hasFormBeenSubmitted && isSuccess == true;
  const disabled = !isReady || isCompleted || !isFormValid;
  const buttonLabel = isExecuting ? busyLabel : isSuccess == true ? completeLabel : label;
  const componentId = createComponentId('form_submit', id);
  return <Button id={componentId} label={buttonLabel} busy={isExecuting} disabled={disabled} type="submit" />;
}
