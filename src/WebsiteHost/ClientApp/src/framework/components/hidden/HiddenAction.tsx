import React, { useImperativeHandle } from 'react';
import { ActionRequestData, ActionResult } from '../../actions/Actions.ts';
import Alert from '../alert/Alert.tsx';
import { createComponentId } from '../Components';
import UnhandledError from '../unhandledError/UnhandledError.tsx';


export interface HiddenActionProps<
  TRequestData extends ActionRequestData,
  ExpectedErrorCode extends string = any,
  TResponse = any
> {
  id?: string;
  children?: React.ReactNode;
  action: ActionResult<TRequestData, ExpectedErrorCode, TResponse>;
  expectedErrorMessages?: Record<ExpectedErrorCode, string>;
  onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void;
}

export interface HiddenActionRef<TRequestData extends ActionRequestData> {
  execute: (requestData?: TRequestData) => void;
  isExecuting: boolean;
}

// Creates a hidden action that can be executed by a ref.
// Accepts an action, that defines the API call to be made
// Accepts a set of expected error messages to be displayed on the form, should the action fail with those errors
// Accepts an onSuccess callback, that is invoked if the action is successful, that can navigate to another page.
// Accepts a ref that can be called with some requestData to execute the action with
const HiddenAction = React.forwardRef(function ActionHidden<
  TRequestData extends ActionRequestData,
  ExpectedErrorCode extends string = any,
  TResponse = any
>(props: HiddenActionProps<TRequestData, ExpectedErrorCode, TResponse>, ref: React.Ref<HiddenActionRef<TRequestData>>) {
  const { id, children, action, expectedErrorMessages, onSuccess } = props;
  const lastExpectedError = action.lastExpectedError
    ? (expectedErrorMessages?.[action.lastExpectedError.code] ?? action.lastExpectedError.code)
    : undefined;
  const lastUnexpectedError = action.lastUnexpectedError;
  const isExecuting = action?.isExecuting ?? false;
  const componentId = createComponentId('hidden_action', id);

  const executeAction = (requestData?: TRequestData) =>
    action.execute(requestData, {
      onSuccess: (successParams) => {
        if (onSuccess) {
          onSuccess({ ...successParams });
        }
      }
    });

  useImperativeHandle(ref, () => ({
    execute: executeAction,
    isExecuting
  }));

  return (
    <>
      <div className="hidden" data-testid={componentId}>
        {children}
      </div>
      <div className="mt-4">
        {lastExpectedError && <Alert id={`${componentId}_expected_error`} message={lastExpectedError} type="error" />}
        {lastUnexpectedError && <UnhandledError id={`${componentId}_unexpected_error`} error={lastUnexpectedError} />}
      </div>
    </>
  );
});

export default HiddenAction;
