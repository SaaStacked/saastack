import React from 'react';
import { DefaultValues, FieldValues, FormProvider, useForm, ValidationMode } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import z, { ZodType } from 'zod';
import { ActionRequestData, ActionResult } from '../../actions/Actions.ts';
import Alert from '../alert/Alert.tsx';
import { createComponentId } from '../Components.ts';
import UnhandledError from '../error/UnhandledError.tsx';
import { ActionFormContext, ActionFormRequiredFieldsContext, ActionFromValidationContext } from './Contexts.tsx';

interface FormProps<TRequestData extends ActionRequestData, ExpectedErrorCode extends string = any, TResponse = any> {
  className?: string;
  id?: string;
  children: React.ReactNode;
  action: ActionResult<TRequestData, ExpectedErrorCode, TResponse>;
  expectedErrorMessages?: Record<ExpectedErrorCode, string>;
  onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void;
  validatesWhen?: keyof ValidationMode;
  validationSchema?: ZodType<any, FieldValues>;
  defaultValues?: DefaultValues<TRequestData>;
  disabled?: boolean;
}

// Creates a form that is wired into an action.
// Accepts an action, that defines the API call to be made
// Accepts a set of expected error messages to be displayed on the form, should the action fail with those errors
// Accepts an onSuccess callback, that is invoked if the action is successful, that can navigate to another page.
// Accepts a definition of validationSchema, that defines how request data is validated, and validateWhen defines when validation is performed.
// Accepts a definition of defaultValues, that defines the initial request data, and populates the form with those values.
// Note: this form does not define the layout of form components, nor a <FormSubmitButton/>, since the layout should be controlled by the consumer.
// It recommends the consumer provide at least one <FormSubmitButton/> within their layout.
// That <FormSubmitButton/> will be disabled if the form is not valid, or if the action is executing.
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
  validationSchema,
  defaultValues,
  disabled
}: FormProps<TRequestData, ExpectedErrorCode, TResponse>) {
  const formContext = { isSubmitted: false };

  type TValidations = z.infer<typeof validationSchema>;
  const formHooks = useForm<TValidations>({
    mode: validatesWhen,
    resolver: validationSchema ? zodResolver(validationSchema) : undefined,
    defaultValues: defaultValues as DefaultValues<TValidations>,
    context: formContext
  });
  formContext.isSubmitted = formHooks.formState.isSubmitted;
  const requiredFormFields = validationSchema ? getRequiredFields(validationSchema) : [];
  const baseClasses = 'bg-white rounded-lg transition-all';
  const classes = [baseClasses, className].filter(Boolean).join(' ');
  const isDisabled =
    disabled || action.isExecuting || !action.isReady || (action.isSuccess === true && formHooks.formState.isSubmitted);
  const componentId = createComponentId('action_form', id);
  return (
    <ActionFormContext.Provider value={action}>
      <ActionFormRequiredFieldsContext.Provider value={requiredFormFields}>
        <ActionFromValidationContext.Provider value={validatesWhen}>
          <FormProvider {...formHooks}>
            <fieldset disabled={isDisabled}>
              <form
                className={classes}
                data-testid={componentId}
                name={componentId}
                onSubmit={formHooks.handleSubmit((requestData) =>
                  action.execute(requestData, {
                    onSuccess: (successParams) => {
                      if (onSuccess) {
                        onSuccess(successParams);
                      }
                    }
                  })
                )}
                noValidate={true}
              >
                {children}
                <div className="mt-4">
                  <Alert
                    id={`${componentId}_expected_error`}
                    message={
                      action.lastExpectedError ? expectedErrorMessages?.[action.lastExpectedError.code] : undefined
                    }
                    type="error"
                  />
                  <UnhandledError id={`${componentId}_unexpected_error`} error={action.lastUnexpectedError} />
                </div>
              </form>
            </fieldset>
          </FormProvider>
        </ActionFromValidationContext.Provider>
      </ActionFormRequiredFieldsContext.Provider>
    </ActionFormContext.Provider>
  );
}

export default Form;

export function getRequiredFields(validationSchema?: ZodType<any, FieldValues>) {
  if (!validationSchema) {
    return [];
  }

  if (!(validationSchema instanceof z.ZodObject)) {
    return [];
  }

  const shape = validationSchema.shape;
  return Object.keys(shape).filter((key) => {
    const field = shape[key];
    return !field.isOptional();
  });
}
