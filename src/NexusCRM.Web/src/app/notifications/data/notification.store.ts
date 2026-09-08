import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BoardHubService } from '../../core/realtime/board-hub.service';
import { ToastService } from '../../shared/ui/toast.service';
import { NotificationApi, NotificationDto } from './notification.api';

@Injectable({ providedIn: 'root' })
export class NotificationStore {
  private readonly api = inject(NotificationApi);
  private readonly hub = inject(BoardHubService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private started = false;

  private readonly itemsSignal = signal<NotificationDto[]>([]);
  private readonly unreadSignal = signal(0);
  private readonly openSignal = signal(false);
  private readonly loadingSignal = signal(false);

  readonly items = this.itemsSignal.asReadonly();
  readonly unreadCount = this.unreadSignal.asReadonly();
  readonly isOpen = this.openSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly hasUnread = computed(() => this.unreadSignal() > 0);

  start(): void {
    if (this.started) {
      return;
    }

    this.started = true;
    void this.hub.start();
    this.refresh();

    this.hub.notificationCreated$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((notification) => {
      this.prepend(notification);
      this.toasts.info(notification.body);
    });
  }

  toggle(): void {
    this.openSignal.update((v) => !v);
    if (this.openSignal()) {
      this.refresh();
    }
  }

  close(): void {
    this.openSignal.set(false);
  }

  refresh(): void {
    this.loadingSignal.set(true);
    this.api.list().subscribe({
      next: (response) => {
        this.itemsSignal.set(response.items.map((n) => this.normalize(n)));
        this.unreadSignal.set(response.unreadCount);
        this.loadingSignal.set(false);
      },
      error: () => {
        this.loadingSignal.set(false);
      },
    });
  }

  markRead(id: string): void {
    const current = this.itemsSignal().find((n) => n.id === id);
    if (!current || current.isRead) {
      return;
    }

    this.itemsSignal.update((items) =>
      items.map((n) => (n.id === id ? { ...n, isRead: true, readAtUtc: new Date().toISOString() } : n)),
    );
    this.unreadSignal.update((c) => Math.max(0, c - 1));

    this.api.markRead(id).subscribe({
      error: () => this.refresh(),
    });
  }

  markAllRead(): void {
    this.itemsSignal.update((items) =>
      items.map((n) => ({ ...n, isRead: true, readAtUtc: n.readAtUtc ?? new Date().toISOString() })),
    );
    this.unreadSignal.set(0);

    this.api.markAllRead().subscribe({
      error: () => this.refresh(),
    });
  }

  private prepend(notification: NotificationDto): void {
    const normalized = this.normalize(notification);
    this.itemsSignal.update((items) => {
      if (items.some((n) => n.id === normalized.id)) {
        return items;
      }
      return [normalized, ...items].slice(0, 40);
    });
    if (!normalized.isRead) {
      this.unreadSignal.update((c) => c + 1);
    }
  }

  private normalize(n: NotificationDto): NotificationDto {
    return {
      ...n,
      id: String(n.id),
      tenantId: String(n.tenantId),
      entityId: n.entityId ? String(n.entityId) : null,
      actorUserId: n.actorUserId ? String(n.actorUserId) : null,
      recipientUserId: n.recipientUserId ? String(n.recipientUserId) : null,
    };
  }
}
