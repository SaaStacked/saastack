import React, { useImperativeHandle } from 'react';
import { useTranslation } from 'react-i18next';
import { ActionRequestData, ActionResult } from '../../actions/Actions.ts';
import Alert from '../alert/Alert.tsx';
import { createComponentId } from '../Components';
import Loader from '../loader/Loader.tsx';
import UnhandledError from '../unhandledError/UnhandledError.tsx';


export interface PageActionProps<
  TRequestData extends ActionRequestData,
  ExpectedErrorCode extends string = any,
  TResponse = any
> {
  id?: string;
  children?: React.ReactNode;
  action: ActionResult<TRequestData, ExpectedErrorCode, TResponse>;
  expectedErrorMessages?: Record<ExpectedErrorCode, string>;
  onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void;
  loadingMessage?: string;
}

export interface PageActionRef<TRequestData extends ActionRequestData> {
  execute: (requestData?: TRequestData) => void;
  isExecuting: boolean;
}

// Creates a hidden action that can be executed by a ref.
// Accepts an action, that defines the API call to be made
// Accepts a set of expected error messages to be displayed on the form, should the action fail with those errors
// Accepts an onSuccess callback, that is invoked if the action is successful, that can navigate to another page.
// Accepts a ref that can be called with some requestData to execute the action with
// When the action is executing, the children are hidden, and a loader is displayed.
// When the action is successful, the children are revealed.
const PageAction = React.forwardRef(function ActionHidden<
  TRequestData extends ActionRequestData,
  ExpectedErrorCode extends string = any,
  TResponse = any
>(props: PageActionProps<TRequestData, ExpectedErrorCode, TResponse>, ref: React.Ref<PageActionRef<TRequestData>>) {
  const { t: translate } = useTranslation();
  const { id, children, action, expectedErrorMessages, onSuccess, loadingMessage } = props;
  const lastExpectedError = action.lastExpectedError
    ? (expectedErrorMessages?.[action.lastExpectedError.code] ?? action.lastExpectedError.code)
    : undefined;
  const lastUnexpectedError = action.lastUnexpectedError;
  const isExecuting = action?.isExecuting ?? false;
  const componentId = createComponentId('page_action', id);
  const isSuccess = action?.isSuccess ?? false;

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
    isExecuting,
    isSuccess
  }));

  const canShowContent = !isExecuting && isSuccess;

  return (
    <div data-testid={componentId}>
      {isExecuting && (
        <Loader
          id={`${componentId}_loader`}
          message={loadingMessage ? loadingMessage : translate('components.page.page_action.loader.title')}
        />
      )}
      {canShowContent && children && <div data-testid={`${componentId}_content`}>{children}</div>}
      <div className="mt-4">
        {lastExpectedError && <Alert id={`${componentId}_expected_error`} message={lastExpectedError} type="error" />}
        {lastUnexpectedError && <UnhandledError id={`${componentId}_unexpected_error`} error={lastUnexpectedError} />}
      </div>
    </div>
  );
});

export default PageAction;
