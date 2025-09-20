import { useContext } from 'react';
import { useFormState } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
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
export default function FormSubmitButton({ id, label, busyLabel, completeLabel }: FormSubmitButtonProps) {
  const { t: translate } = useTranslation();
  const action = useContext<ActionResult<any, any> | undefined>(ActionFormContext);
  const { isSubmitted: hasFormBeenSubmitted } = useFormState();
  const isReady = action?.isReady ?? false;
  const isExecuting = action?.isExecuting ?? false;
  const isSuccess = action?.isSuccess;
  const isCompleted = hasFormBeenSubmitted && isSuccess == true;
  const isButtonDisabled = !isReady || isCompleted;
  const buttonLabel = isExecuting
    ? (busyLabel ?? translate('components.form.form_submit_button.default_busy_label'))
    : isSuccess == true
      ? (completeLabel ?? translate('components.form.form_submit_button.default_completed_label'))
      : (label ?? translate('components.form.form_submit_button.default_label'));
  const componentId = createComponentId('form_submit', id);
  return (
    <div className="flex mt-16">
      <Button
        className="w-full"
        id={componentId}
        label={buttonLabel}
        busy={isExecuting}
        disabled={isButtonDisabled}
        type="submit"
      />
    </div>
  );
}
