import { http, HttpResponse } from 'msw';
import { GetProfileForCallerResponse } from '../../api/apiHost1';
import { HealthCheckResponse, LogoutResponse } from '../../api/websiteHost';
import { anonymousUser } from '../../constants';

// Mocked API calls executed during Storybook stories
export const handlers = [
  http.get('/api/health', () =>
    HttpResponse.json({
      name: 'WebsiteHost',
      status: 'OK'
    } as HealthCheckResponse)
  ),
  http.get('/profiles/me', () =>
    HttpResponse.json({
      profile: anonymousUser
    } as GetProfileForCallerResponse)
  ),
  http.post('/api/auth/logout', () => HttpResponse.json({} as LogoutResponse))
];
