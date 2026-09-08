import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { AuthApi } from './auth.api';
import { AuthTokenResponse, LoginRequest } from './auth.models';

const ACCESS_KEY = 'nexus.accessToken';
const REFRESH_KEY = 'nexus.refreshToken';
const TENANT_KEY = 'nexus.tenantId';
const PROFILE_KEY = 'nexus.profile';

interface StoredProfile {
  userId: string;
  email: string;
  displayName: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);

  private readonly session = signal<StoredProfile | null>(this.readProfile());
  private readonly accessTokenSignal = signal<string | null>(localStorage.getItem(ACCESS_KEY));

  readonly isAuthenticated = computed(() => !!this.accessTokenSignal());
  readonly profile = this.session.asReadonly();
  readonly displayName = computed(() => this.session()?.displayName ?? null);

  accessToken(): string | null {
    return this.accessTokenSignal();
  }

  refreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  login(request: LoginRequest) {
    return this.api.login(request).pipe(tap((response) => this.persist(response)));
  }

  refresh() {
    const token = this.refreshToken();
    if (!token) {
      throw new Error('Missing refresh token');
    }

    return this.api.refresh(token).pipe(tap((response) => this.persist(response)));
  }

  logout(): void {
    const refresh = this.refreshToken();
    if (refresh) {
      this.api.logout(refresh).subscribe({ error: () => undefined });
    }

    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(TENANT_KEY);
    localStorage.removeItem(PROFILE_KEY);
    this.accessTokenSignal.set(null);
    this.session.set(null);
    void this.router.navigateByUrl('/login');
  }

  hasPermission(permission: string): boolean {
    return !!this.session()?.permissions.some((p) => p.toLowerCase() === permission.toLowerCase());
  }

  private persist(response: AuthTokenResponse): void {
    localStorage.setItem(ACCESS_KEY, response.accessToken);
    localStorage.setItem(REFRESH_KEY, response.refreshToken);
    localStorage.setItem(TENANT_KEY, response.tenantId);

    const profile: StoredProfile = {
      userId: response.userId,
      email: response.email,
      displayName: response.displayName,
      tenantId: response.tenantId,
      roles: response.roles,
      permissions: response.permissions,
    };

    localStorage.setItem(PROFILE_KEY, JSON.stringify(profile));
    this.accessTokenSignal.set(response.accessToken);
    this.session.set(profile);
  }

  private readProfile(): StoredProfile | null {
    const raw = localStorage.getItem(PROFILE_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as StoredProfile;
    } catch {
      return null;
    }
  }
}
