import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthTokenResponse, LoginRequest, MeResponse } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/auth`;

  login(request: LoginRequest) {
    return this.http.post<AuthTokenResponse>(`${this.base}/login`, request);
  }

  refresh(refreshToken: string) {
    return this.http.post<AuthTokenResponse>(`${this.base}/refresh`, { refreshToken });
  }

  logout(refreshToken: string) {
    return this.http.post<void>(`${this.base}/logout`, { refreshToken });
  }

  me() {
    return this.http.get<MeResponse>(`${this.base}/me`);
  }
}
