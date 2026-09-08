import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface NotificationDto {
  id: string;
  tenantId: string;
  recipientUserId?: string | null;
  title: string;
  body: string;
  category: string;
  entityType?: string | null;
  entityId?: string | null;
  href?: string | null;
  actorUserId?: string | null;
  isRead: boolean;
  readAtUtc?: string | null;
  createdAtUtc: string;
}

export interface NotificationListResponse {
  items: NotificationDto[];
  unreadCount: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/notifications`;

  list(take = 40) {
    return this.http.get<NotificationListResponse>(this.base, { params: { take } });
  }

  markRead(id: string) {
    return this.http.post<NotificationDto>(`${this.base}/${id}/read`, {});
  }

  markAllRead() {
    return this.http.post<number>(`${this.base}/read-all`, {});
  }
}
