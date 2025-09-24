import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ActionRequestData, ActionResult } from '../../actions/Actions.ts';
import Alert from '../alert/Alert.tsx';
import Button from '../button/Button.tsx';
import { createComponentId, toClasses } from '../Components';
import { BusyLabelRevertAfterMs } from '../form/formSubmitButton/FormSubmitButton.tsx';
import UnhandledError from '../unhandledError/UnhandledError.tsx';


export interface ButtonActionProps<
  TRequestData extends ActionRequestData,
  ExpectedErrorCode extends string = any,
  TResponse = any
> {
  className?: string;
  id?: string;
  children?: React.ReactNode;
  action: ActionResult<TRequestData, ExpectedErrorCode, TResponse>;
  requestData?: TRequestData;
  expectedErrorMessages?: Record<ExpectedErrorCode, string>;
  onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void;
  label?: string;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  busyLabel?: string;
  completeLabel?: string;
}

// Creates a button that is wired into an action.
// Accepts an action, that defines the API call to be made
// Accepts a set of expected error messages to be displayed after the button, should the action fail with those errors
// Accepts an onSuccess callback, that is invoked if the action is successful, that can navigate to another page.
// Accepts a label, that is displayed when the button is not executing.
// Accepts a busyLabel, that is displayed when the button is executing.
// Accepts a completeLabel, that is displayed when the button has completed successfully.
// The button is disabled if the action is not ready
// When this button is clicked, it will:
// 1. Disable the button, set the label to busyLabel
// 2. Execute the action with the supplied requestData
// 3. Call the onSuccess callback if the action succeeds, set the label to completeLabel, then back to the label, and enable the button
// 4. Display any errors after the button, if the action fails.
export default function ButtonAction<
  TRequestData extends ActionRequestData,
  ExpectedErrorCode extends string = any,
  TResponse = any
>({
  className,
  id,
  children,
  action,
  requestData,
  expectedErrorMessages,
  onSuccess,
  label,
  variant,
  busyLabel,
  completeLabel
}: ButtonActionProps<TRequestData, ExpectedErrorCode, TResponse>) {
  const { t: translate } = useTranslation();
  const baseClasses = '';
  const classes = toClasses([baseClasses, className]);
  const lastExpectedError = action.lastExpectedError
    ? (expectedErrorMessages?.[action.lastExpectedError.code] ?? action.lastExpectedError.code)
    : undefined;
  const lastUnexpectedError = action.lastUnexpectedError;
  const [showSuccessLabel, setShowSuccessLabel] = useState(false);
  const isReady = action?.isReady ?? false;
  const isExecuting = action?.isExecuting ?? false;
  const isSuccess = action?.isSuccess;
  const isButtonDisabled = !isReady;
  const hasChildren = children !== undefined;

  useEffect(() => {
    if (isSuccess === true) {
      setShowSuccessLabel(true);
      const timer = setTimeout(() => setShowSuccessLabel(false), BusyLabelRevertAfterMs);

      return () => clearTimeout(timer);
    }
  }, [isSuccess]);

  const buttonLabel = isExecuting
    ? (busyLabel ?? translate('components.button.button_action.default_busy_label'))
    : showSuccessLabel
      ? (completeLabel ?? translate('components.button.button_action.default_completed_label'))
      : (label ?? translate('components.button.button_action.default_label'));
  const componentId = createComponentId('button_action', id);
  return (
    <>
      <Button
        className={classes}
        data-testid={componentId}
        id={componentId}
        label={!hasChildren ? buttonLabel : undefined}
        busy={isExecuting}
        disabled={isButtonDisabled}
        onClick={() =>
          action.execute(requestData, {
            onSuccess: (successParams) => {
              if (onSuccess) {
                onSuccess({ ...successParams });
              }
            }
          })
        }
        variant={variant}
        type="button"
        size="md"
        fullWidth={false}
      >
        {children}
      </Button>
      <div className="mt-4">
        {lastExpectedError && <Alert id={`${componentId}_expected_error`} message={lastExpectedError} type="error" />}
        {lastUnexpectedError && <UnhandledError id={`${componentId}_unexpected_error`} error={lastUnexpectedError} />}
      </div>
    </>
  );
}
