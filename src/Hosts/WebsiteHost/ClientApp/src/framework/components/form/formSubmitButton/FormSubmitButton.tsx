import { useContext, useEffect, useState } from 'react';
import { useFormState } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { ActionResult } from '../../../actions/Actions.ts';
import Button from '../../button/Button';
import { createComponentId } from '../../Components.ts';
import { FormActionContext } from '../FormActionContexts.tsx';

export const BusyLabelRevertAfterMs = 2000;

interface FormSubmitButtonProps {
  id?: string;
  label?: string;
  busyLabel?: string;
  completeLabel?: string;
}

// Creates a submit button that is wired into the form.
// Accepts a label, that is displayed when the form is not executing.
// Accepts a busyLabel, that is displayed when the form is executing.
// Accepts a completeLabel, that is displayed when the form has completed successfully.
// The button is disabled if the form is not ready
// The button is disabled after the form has been submitted successfully. A reset of the form re-enables the button.
// The button is disabled before the form has been submitted, if the form fails validation.
// When this button is clicked, it will:
// 1. Disable the button, set the label to busyLabel
// 2. Execute the action with the supplied requestData
// 3. Call the onSuccess callback if the action succeeds, set the label to completeLabel, then back to the label
// 4. Display any errors after the button, if the action fails.
export default function FormSubmitButton({ id, label, busyLabel, completeLabel }: FormSubmitButtonProps) {
  const { t: translate } = useTranslation();
  const classes = 'w-full';
  const action = useContext<ActionResult<any, any> | undefined>(FormActionContext);
  const { isSubmitted: hasFormBeenSubmitted } = useFormState();
  const [showSuccessLabel, setShowSuccessLabel] = useState(false);
  const isReady = action?.isReady ?? false;
  const isExecuting = action?.isExecuting ?? false;
  const isSuccess = action?.isSuccess;
  const isCompleted = hasFormBeenSubmitted && isSuccess == true;
  const isButtonDisabled = !isReady || isCompleted;

  useEffect(() => {
    if (isSuccess === true && hasFormBeenSubmitted) {
      setShowSuccessLabel(true);
      const timer = setTimeout(() => setShowSuccessLabel(false), BusyLabelRevertAfterMs);

      return () => clearTimeout(timer);
    }
  }, [isSuccess, hasFormBeenSubmitted]);

  const buttonLabel = isExecuting
    ? (busyLabel ?? translate('components.form.form_submit_button.default_busy_label'))
    : showSuccessLabel
      ? (completeLabel ?? translate('components.form.form_submit_button.default_completed_label'))
      : (label ?? translate('components.form.form_submit_button.default_label'));
  const componentId = createComponentId('form_submit', id);
  return (
    <div className="flex mt-4">
      <Button
        className={classes}
        data-testid={componentId}
        id={componentId}
        label={buttonLabel}
        busy={isExecuting}
        disabled={isButtonDisabled}
        type="submit"
      />
    </div>
  );
}
