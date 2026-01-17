import type { Meta, StoryObj } from '@storybook/react';
import { toast } from 'react-toastify';
import { ActionResult, ErrorResponse } from '../../actions/Actions.ts';
import { ExpectedErrorDetails } from '../../actions/ApiErrorState.ts';
import { OfflineServiceContext } from '../../providers/OfflineServiceContext.tsx';
import { IOfflineService } from '../../services/IOfflineService.ts';
import Alert from '../alert/Alert.tsx';
import { OfflineBanner } from '../offline/OfflineBanner.tsx';
import ButtonAction from './ButtonAction.tsx';


const meta: Meta<typeof ButtonAction> = {
  title: 'Components/Button/ButtonAction',
  component: ButtonAction,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    className: {
      control: 'text'
    },
    label: {
      control: 'text'
    },
    variant: {
      control: { type: 'select' },
      options: ['brand-primary', 'brand-secondary', 'outline', 'ghost', 'danger']
    },
    busyLabel: {
      control: 'text'
    },
    completeLabel: {
      control: 'text'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

const mockOfflineService = {
  status: 'offline',
  onStatusChanged(_: (status: 'online' | 'offline') => void): () => void {
    return () => {};
  }
} as IOfflineService;

const createMockAction = <TRequestData extends any, TResponse = any, ExpectedErrorCode extends string = any>(
  initial: { isReady: boolean; isExecuting: boolean } = { isReady: true, isExecuting: false },
  final: {
    isSuccess?: boolean;
    lastSuccessResponse?: TResponse;
    lastExpectedError?: ExpectedErrorDetails<ExpectedErrorCode>;
    lastUnexpectedError?: ErrorResponse;
  } = {
    isSuccess: true,
    lastSuccessResponse: { data: {} } as TResponse,
    lastExpectedError: undefined,
    lastUnexpectedError: undefined
  }
): ActionResult<TRequestData, ExpectedErrorCode, TResponse> => {
  const action = {
    execute: async (
      requestData?: any,
      options?: { onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void }
    ) => {
      action.isExecuting = true;
      await new Promise((resolve) => setTimeout(resolve, 1500));
      action.isReady = true;
      action.isExecuting = false;
      action.isSuccess = final.isSuccess;
      action.lastSuccessResponse = final.lastSuccessResponse;
      action.lastExpectedError = final.lastExpectedError;
      action.lastUnexpectedError = final.lastUnexpectedError;
      options?.onSuccess?.({ requestData, response: action.lastSuccessResponse! });
      return;
    },
    isSuccess: undefined,
    lastSuccessResponse: undefined,
    lastExpectedError: undefined,
    lastUnexpectedError: undefined,
    isExecuting: initial.isExecuting,
    isReady: initial.isReady,
    lastRequestValues: undefined
  } as ActionResult<TRequestData, ExpectedErrorCode, TResponse>;

  return action;
};

export const ReturnsSuccess: Story = {
  args: {
    action: createMockAction(),
    label: 'Click me',
    onSuccess: (_params) => toast.success(`API call succeeded: HTTP - OK`, { autoClose: 1500 })
  },
  render: (args) => <ButtonAction {...args}></ButtonAction>
};
export const ReturnsExpectedError: Story = {
  args: {
    action: createMockAction(
      { isReady: true, isExecuting: false },
      {
        isSuccess: false,
        lastExpectedError: { code: 'ANERROR' }
      }
    ),
    label: 'Click me',
    expectedErrorMessages: {
      ANERROR: 'Cannot perform this action at this time'
    },
    onSuccess: (_params) => toast.error(`API call failed: HTTP - BadRequest`, { autoClose: 1500 })
  },
  decorators: [
    (Story, _context) => (
      <div className="space-y-8">
        <Alert title="Note">
          <p>When this action is executed, an 'HTTP - 405' is returned.</p>
          <p>This error is expected, and a custom UI message is displayed to the user.</p>
        </Alert>
        <Story />
      </div>
    )
  ],
  render: (args) => <ButtonAction {...args}></ButtonAction>
};

export const ReturnsUnexpectedError: Story = {
  args: {
    action: createMockAction(
      { isReady: true, isExecuting: false },
      {
        isSuccess: false,
        lastExpectedError: undefined,
        lastUnexpectedError: {
          data: {
            type: 'https://datatracker.ietf.org/doc/html/rfc9110#section-15.5',
            title: 'Bad Request',
            status: 400,
            detail: "'First Name' must not be empty.'",
            instance: 'https://localhost:5001/post',
            errors: [
              {
                reason: "'First Name' must not be empty.",
                rule: 'NotEmptyValidator',
                value: ''
              },
              {
                reason: "The 'FirstName' was either missing or is invalid",
                rule: 'ValidatorValidator',
                value: ''
              }
            ]
          },
          response: {
            status: 400,
            statusText: 'Bad Request'
          } as Response
        } as ErrorResponse
      }
    ),
    label: 'Click me',
    onSuccess: (_params) => toast.error(`API call failed: HTTP - InternalServerError`, { autoClose: 1500 })
  },
  decorators: [
    (Story, _context) => (
      <div className="space-y-8">
        <Alert title="Note">
          <p>When this action is executed, an 'HTTP - 500' is returned.</p>
          <p>This error would never be expected, and a general fatal error is displayed.</p>
        </Alert>
        <Story />
      </div>
    )
  ],
  render: (args) => <ButtonAction {...args}></ButtonAction>
};

export const BrowserIsOffline: Story = {
  args: {
    action: createMockAction({ isReady: false, isExecuting: false }, {}),
    label: 'Click me',
    onSuccess: (_params) => toast.error(`Should never have seen this!`, { autoClose: 1500 })
  },
  decorators: [
    (Story, _context) => (
      <OfflineServiceContext value={{ offlineService: mockOfflineService }}>
        <OfflineBanner />
        <div className="mt-32"></div>
        <Story />
      </OfflineServiceContext>
    )
  ],
  render: (args) => <ButtonAction {...args}></ButtonAction>
};
