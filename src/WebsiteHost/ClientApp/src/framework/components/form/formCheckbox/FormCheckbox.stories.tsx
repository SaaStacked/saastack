import type { Meta, StoryObj } from '@storybook/react';
import { within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { z } from 'zod';
import { ActionResult } from '../../../actions/Actions.ts';
import FormAction from '../FormAction.tsx';
import FormSubmitButton from '../formSubmitButton/FormSubmitButton.tsx';
import FormCheckbox from './FormCheckbox';


const meta: Meta<typeof FormCheckbox> = {
  title: 'Components/Form/FormCheckbox',
  component: FormCheckbox,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    className: {
      control: 'text'
    },
    dependencies: {
      control: 'object'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

function createMockAction(
  initialState: Partial<ActionResult<any, any, any>> = {},
  finalState: Partial<ActionResult<any, any, any>> = {}
): ActionResult<any, any, any> {
  return {
    isReady: true,
    isExecuting: false,
    isSuccess: false,
    error: undefined,
    unexpectedError: undefined,
    result: undefined,
    submittedValues: undefined,
    execute: () => Promise.resolve(),
    ...initialState,
    ...finalState
  } as ActionResult<any, any, any>;
}

export const Checkbox: Story = {
  args: {
    id: 'terms',
    name: 'terms',
    label: 'I agree to the terms and conditions'
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      validationSchema={z.object({
        terms: z.boolean().optional()
      })}
    >
      <div className="space-y-4">
        <FormCheckbox {...args} />
        <FormSubmitButton />
      </div>
    </FormAction>
  )
};

export const WithValidationError: Story = {
  args: {
    id: 'terms',
    name: 'terms',
    label: 'I agree to the terms and conditions'
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      validationSchema={z.object({
        terms: z.literal(true, 'You must agree to the terms')
      })}
    >
      <div className="space-y-4">
        <FormCheckbox data-testid="checkbox" {...args} />
        <FormSubmitButton />
      </div>
    </FormAction>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const checkbox = canvas.getByTestId('terms_form_checkbox_checkbox');

    await userEvent.click(checkbox);
    await userEvent.click(checkbox);
    await userEvent.tab();
  }
};
