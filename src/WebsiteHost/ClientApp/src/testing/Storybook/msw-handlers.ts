import { AxiosResponse } from 'axios';
import { http, HttpResponse } from 'msw';
import { GetProfileForCallerResponse } from '../../api/apiHost1';
import { HealthCheckResponse, LogoutResponse } from '../../api/websiteHost';
import { anonymousUser } from '../../constants';


// Mocked API calls executed during Storybook stories
export const handlers = [
  http.get('/api/health', () =>
    HttpResponse.json({
      data: {
        name: 'WebsiteHost',
        status: 'OK'
      },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
      request: {},
      error: undefined
    } as AxiosResponse<HealthCheckResponse>)
  ),
  http.get('/profiles/me', () =>
    HttpResponse.json({
      data: {
        profile: anonymousUser
      },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
      request: {},
      error: undefined
    } as AxiosResponse<GetProfileForCallerResponse>)
  ),
  http.post('/api/auth/logout', async ({ request }) =>
    HttpResponse.json({
      data: {
        profile: anonymousUser
      },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
      request: {},
      error: undefined
    } as AxiosResponse<LogoutResponse>)
  )
];
