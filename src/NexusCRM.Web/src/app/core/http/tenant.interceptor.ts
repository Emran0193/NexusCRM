import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantId = localStorage.getItem('nexus.tenantId') ?? environment.defaultTenantId;
  return next(
    req.clone({
      setHeaders: {
        'X-Tenant-Id': tenantId,
      },
    }),
  );
};
