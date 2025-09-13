import React from 'react';
import { DefaultValues, FieldValues, FormProvider, useForm, ValidationMode } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import z, { ZodType } from 'zod';
import { ActionRequestData, ActionResult } from '../../actions/Actions.tsx';
import Alert from '../alert/Alert.tsx';
import UnhandledError from '../error/UnhandledError.tsx';
import { ActionContext, FormValidationContext, RequiredFieldsContext } from './Contexts.tsx';


interface FormProps<TRequestData extends ActionRequestData, ExpectedErrorCode extends string = any, TResponse = any> {
  className?: string;
  id: string;
  children: React.ReactNode;
  action: ActionResult<TRequestData, ExpectedErrorCode, TResponse>;
  expectedErrorMessages?: Record<ExpectedErrorCode, string>;
  onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void;
  validatesWhen?: keyof ValidationMode;
  validations: ZodType<any, FieldValues>;
  defaultValues?: DefaultValues<TRequestData>;
}

// Creates a form that is wired into an action.
// Accepts an action, that defines the API call to be made
// Accepts a set of expected error messages to be displayed on the form, should the action fail with those errors
// Accepts an onSuccess callback, that is invoked if the action is successful, that cna navigate to another page.
// Accepts a definition of validations, that defines how request data is validated, and validateWhen defines when the validations are executed.
// Accepts a definition of defaultValues, that defines the initial request data.
// Note: this form does not define the layout of form components, nor a <SubmitButton/>, since the layout should be controlled by the consumer.
// It recommends the consumer provide at least one <SubmitButton/> within their layout.
// That <SubmitButton/> will be disabled if the form is not valid, or if the action is executing.
// When that button is clicked, the form will:
// 1. Validate all data according to validation rules
// 2. Execute the action with the supplied data
// 3. Call the onSuccess callback if the action succeeds.
// 4. Display any errors on the form, if the action fails.
function Form<TRequestData extends ActionRequestData, ExpectedErrorCode extends string = any, TResponse = any>({
  className = '',
  id,
  children,
  action,
  expectedErrorMessages,
  onSuccess,
  validatesWhen = 'onBlur',
  validations,
  defaultValues
}: FormProps<TRequestData, ExpectedErrorCode, TResponse>) {
  const context = { isSubmitted: false };
  const methods = useForm({
    mode: validatesWhen,
    resolver: validations ? zodResolver(validations) : undefined,
    defaultValues,
    context
  });
  context.isSubmitted = methods.formState.isSubmitted;

  const requiredFields = validations ? getRequiredFields(validations) : [];
  const baseClasses = 'bg-white rounded-lg transition-all';
  const classes = [baseClasses, className].filter(Boolean).join(' ');
  const componentId = `${id}_action_form`;
  return (
    <ActionContext.Provider value={action}>
      <RequiredFieldsContext.Provider value={requiredFields}>
        <FormValidationContext.Provider value={validatesWhen}>
          <FormProvider {...methods}>
            <form
              data-testid={componentId}
              className={classes}
              name={componentId}
              onSubmit={methods.handleSubmit((formData) =>
                action.execute(formData, {
                  onSuccess: (successParams) => {
                    if (onSuccess) {
                      onSuccess(successParams);
                    }
                  }
                })
              )}
              noValidate
            >
              {children}
              <Alert
                id={`${componentId}_expected_error`}
                message={action.lastExpectedError ? expectedErrorMessages?.[action.lastExpectedError.code] : undefined}
                type="error"
              />
              <UnhandledError id={`${componentId}_unexpected_error`} error={action.lastUnexpectedError} />
            </form>
          </FormProvider>
        </FormValidationContext.Provider>
      </RequiredFieldsContext.Provider>
    </ActionContext.Provider>
  );
}

export default Form;

export function getRequiredFields(schema: ZodType<any, FieldValues>) {
  if (!(schema instanceof z.ZodObject)) {
    return [];
  }

  const shape = schema.shape;
  return Object.keys(shape).filter((key) => {
    const field = shape[key];
    return !field.isOptional();
  });
}
