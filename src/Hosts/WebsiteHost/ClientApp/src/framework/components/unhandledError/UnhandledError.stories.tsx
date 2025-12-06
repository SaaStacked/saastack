import type { Meta, StoryObj } from '@storybook/react';
import { ErrorResponse } from '../../actions/Actions.ts';
import UnhandledError from './UnhandledError';

const meta: Meta<typeof UnhandledError> = {
  title: 'Components/UnhandledError',
  component: UnhandledError,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {}
};

export default meta;
type Story = StoryObj<typeof meta>;

export const InternalServerError: Story = {
  args: {
    error: {
      data: {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.6.1',
        title: 'An unexpected error occurred',
        status: 500,
        detail:
          'An unexpected error occured on the server, which should not have happened in normal operation. Please report this error, and the conditions under which it was discovered, to the support team.',
        instance: 'https://localhost:5001/post',
        exception:
          'System.InvalidOperationException: amessage\r\n   at ApiHost1.Api.TestingOnly.TestingWebApi.ErrorsThrows(ErrorsThrowTestingOnlyRequest request, CancellationToken cancellationToken) in C:\\Projects\\apath\\src\\ApiHost1\\Api\\TestingOnly\\TestingWebApi.cs:line 117\r\n   at ApiHost1.MinimalApiRegistration.<RegisterRoutes>g__Handle|0_94(IServiceProvider services, ErrorsThrowTestingOnlyRequest request, CancellationToken cancellationToken) in C:\\Projects\\apath\\src\\ApiHost1\\Generated\\Tools.Generators.Web.Api\\Tools.Generators.Web.Api.MinimalApiGenerator\\MinimalApiGeneratedHandlers.g.cs:line 324\r\n   at ApiHost1.MinimalApiRegistration.<>c.<<RegisterRoutes>b__0_21>d.MoveNext() in C:\\Projects\\apath\\src\\ApiHost1\\Generated\\Tools.Generators.Web.Api\\Tools.Generators.Web.Api.MinimalApiGenerator\\MinimalApiGeneratedHandlers.g.cs:line 316\r\n--- End of stack trace from previous location ---\r\n   at Microsoft.AspNetCore.Http.RequestDelegateFactory.<TaskOfTToValueTaskOfObject>g__ExecuteAwaited|92_0[T](Task`1 task)\r\n   at Infrastructure.Web.Api.Common.Endpoints.ValidationFilter`1.InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) in C:\\Projects\\apath\\src\\Infrastructure.Web.Api.Common\\Endpoints\\ValidationFilter.cs:line 33\r\n   at Infrastructure.Web.Api.Common.Endpoints.ContentNegotiationFilter.InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) in C:\\Projects\\apath\\src\\Infrastructure.Web.Api.Common\\Endpoints\\ContentNegotiationFilter.cs:line 24\r\n   at Infrastructure.Web.Api.Common.Endpoints.RequestCorrelationFilter.InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) in C:\\Projects\\apath\\src\\Infrastructure.Web.Api.Common\\Endpoints\\RequestCorrelationFilter.cs:line 32\r\n   at Infrastructure.Web.Api.Common.Endpoints.ApiUsageFilter.InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) in C:\\Projects\\apath\\src\\Infrastructure.Web.Api.Common\\Endpoints\\ApiUsageFilter.cs:line 68\r\n   at Infrastructure.Web.Api.Common.Endpoints.HttpRecordingFilter.InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) in C:\\Projects\\apath\\src\\Infrastructure.Web.Api.Common\\Endpoints\\HttpRecordingFilter.cs:line 42\r\n   at Microsoft.AspNetCore.Http.RequestDelegateFactory.<ExecuteValueTaskOfObject>g__ExecuteAwaited|129_0(ValueTask`1 valueTask, HttpContext httpContext, JsonTypeInfo`1 jsonTypeInfo)\r\n   at Microsoft.AspNetCore.Http.RequestDelegateFactory.<>c__DisplayClass101_0.<<HandleRequestBodyAndCompileRequestDelegate>b__0>d.MoveNext()\r\n--- End of stack trace from previous location ---\r\n   at Infrastructure.Web.Hosting.Common.Extensions.WebApplicationExtensions.<>c.<<EnableEventingPropagation>b__4_1>d.MoveNext() in C:\\Projects\\apath\\src\\Infrastructure.Web.Hosting.Common\\Extensions\\WebApplicationExtensions.cs:line 151\r\n--- End of stack trace from previous location ---\r\n   at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)\r\n   at Infrastructure.Web.Hosting.Common.Pipeline.MultiTenancyMiddleware.InvokeAsync(HttpContext context, ITenancyContext tenancyContext, ICallerContextFactory callerContextFactory, ITenantDetective tenantDetective, IEndUsersService endUsersService, IOrganizationsService organizationsService) in C:\\Projects\\apath\\src\\Infrastructure.Web.Hosting.Common\\Pipeline\\MultiTenancyMiddleware.cs:line 54\r\n   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)\r\n   at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.<Invoke>g__Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)'
      },
      response: {
        status: 500,
        statusText: 'Internal Server Error'
      } as Response
    } as ErrorResponse
  }
};
export const ValidationError: Story = {
  args: {
    error: {
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
};

export const NetworkError: Story = {
  args: {
    error: {
      data: new Error('Network Error'),
      response: undefined as any
    } as ErrorResponse
  }
};

export const TimeoutError: Story = {
  args: {
    error: {
      data: {},
      response: {
        status: 500,
        statusText: 'Timeout Exceeded'
      } as Response
    } as ErrorResponse
  }
};

export const NotFoundError: Story = {
  args: {
    error: {
      data: {},
      response: {
        status: 404,
        statusText: 'Not Found'
      } as Response
    } as ErrorResponse
  }
};

export const NoError: Story = {
  args: {
    error: undefined
  }
};
