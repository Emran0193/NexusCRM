export interface LoginRequest {
  email: string;
  password: string;
  tenantId?: string | null;
  mfaCode?: string | null;
}

export interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
  userId: string;
  tenantId: string;
  email: string;
  displayName: string;
  roles: string[];
  permissions: string[];
  requiresMfa: boolean;
}

export interface MeResponse {
  userId: string;
  email: string;
  displayName: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
  mfaEnabled: boolean;
}
