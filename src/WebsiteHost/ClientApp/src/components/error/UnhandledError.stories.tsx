import type { Meta, StoryObj } from '@storybook/react';
import { AxiosError } from 'axios';
import UnhandledError from './UnhandledError';

const meta: Meta<typeof UnhandledError> = {
  title: 'Components/UnhandledError',
  component: UnhandledError,
  parameters: {
    layout: 'padded'
  },
  tags: ['autodocs'],
  argTypes: {
    id: {
      control: 'text'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Primary: Story = {
  args: {
    id: 'demo',
    error: {
      response: {
        status: 500,
        statusText: 'Internal Server Error',
        headers: {},
        data: {
          responseStatus: {
            message: 'Internal Server Error',
            errorCode: '500',
            stackTrace: `
System.SomeException:
   at Infrastructure.Common.ApplicationServices.External.MicrosoftIdentityApi.Post (MicrosoftIdentityApi.cs:74)
   at Infrastructure.Common.ApplicationServices.External.MicrosoftIdentityServiceClient.RenewTokens (MicrosoftIdentityServiceClient.cs:58)
            `
          }
        }
      },
      message: 'Request failed with status code 500',
      code: 'ERR_BAD_RESPONSE',
      stack:
        'Error: Request failed with status code 500\n    at createError (createError.js:16:15)\n    at settle (settle.js:17:12)'
    } as AxiosError
  }
};

export const NetworkError: Story = {
  args: {
    id: 'demo',
    error: {
      message: 'Network Error',
      code: 'ERR_NETWORK',
      stack: 'Error: Network Error\n    at XMLHttpRequest.handleError (xhr.js:117:14)'
    } as AxiosError
  }
};

export const TimeoutError: Story = {
  args: {
    id: 'demo',
    error: {
      message: 'timeout of 5000ms exceeded',
      code: 'ECONNABORTED',
      stack: 'Error: timeout of 5000ms exceeded\n    at createError (createError.js:16:15)'
    } as AxiosError
  }
};

export const NotFoundError: Story = {
  args: {
    id: 'demo',
    error: {
      response: {
        status: 404,
        statusText: 'Not Found',
        headers: {},
        data: {
          responseStatus: {
            message: 'The requested resource was not found',
            errorCode: '404'
          }
        }
      },
      message: 'Request failed with status code 404',
      code: 'ERR_BAD_REQUEST'
    } as AxiosError
  }
};

export const NoError: Story = {
  args: {
    id: 'demo',
    error: undefined
  }
};
