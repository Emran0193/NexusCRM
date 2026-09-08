import { DatePipe } from '@angular/common';
import { Component, HostListener, OnInit, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthStore } from '../auth/data/auth.store';
import { NotificationStore } from '../notifications/data/notification.store';
import { ToastHostComponent } from '../shared/ui/toast-host.component';

@Component({
  selector: 'nx-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastHostComponent, DatePipe],
  template: `
    <div class="shell" [class.shell--nav-open]="menuOpen()">
      <header class="topbar">
        <button
          type="button"
          class="menu-btn"
          [attr.aria-expanded]="menuOpen()"
          aria-controls="primary-nav"
          aria-label="Open navigation"
          (click)="toggleMenu()"
        >
          <span class="menu-btn__bars" aria-hidden="true"></span>
        </button>
        <div class="brand nx-display">NexusCRM</div>
        <div class="notify">
          <button
            type="button"
            class="bell"
            [attr.aria-expanded]="notifications.isOpen()"
            aria-controls="notification-panel"
            aria-label="Notifications"
            (click)="onNotifyClick($event)"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
              <path
                fill="currentColor"
                d="M12 22a2.2 2.2 0 0 0 2.2-2.2h-4.4A2.2 2.2 0 0 0 12 22Zm7-6.2V11a7 7 0 1 0-14 0v4.8L3 17.8V19h18v-1.2l-2-1.8Z"
              />
            </svg>
            @if (notifications.hasUnread()) {
              <span class="badge">{{ notifications.unreadCount() }}</span>
            }
          </button>
          @if (notifications.isOpen()) {
            <div
              id="notification-panel"
              class="panel panel--top"
              role="dialog"
              aria-label="Notification center"
              (click)="$event.stopPropagation()"
            >
              <header>
                <strong>Notifications</strong>
                <button
                  type="button"
                  class="nx-btn nx-btn--ghost mark-all"
                  [disabled]="!notifications.hasUnread()"
                  (click)="notifications.markAllRead()"
                >
                  Mark all read
                </button>
              </header>
              @if (notifications.loading() && notifications.items().length === 0) {
                <p class="muted">Loading…</p>
              } @else if (notifications.items().length === 0) {
                <p class="muted">No notifications yet. Qualify a lead to trigger a workflow.</p>
              } @else {
                <ul>
                  @for (item of notifications.items(); track item.id) {
                    <li [class.unread]="!item.isRead">
                      <button type="button" class="item" (click)="onOpen(item.id, item.href)">
                        <span class="title">{{ item.title }}</span>
                        <span class="body">{{ item.body }}</span>
                        <span class="meta">{{ item.createdAtUtc | date: 'short' }} · {{ item.category }}</span>
                      </button>
                    </li>
                  }
                </ul>
              }
            </div>
          }
        </div>
      </header>

      <button type="button" class="backdrop" tabindex="-1" aria-label="Close navigation" (click)="closeMenu()"></button>

      <aside id="primary-nav" class="nav" aria-label="Primary">
        <div class="nav__head">
          <div class="brand nx-display">NexusCRM</div>
          <button type="button" class="close-nav" aria-label="Close navigation" (click)="closeMenu()">×</button>
        </div>
        <nav>
          <a routerLink="/reports" routerLinkActive="active" (click)="closeMenu()">Reports</a>
          <a routerLink="/search" routerLinkActive="active" (click)="closeMenu()">Search</a>
          <a routerLink="/customers" routerLinkActive="active" (click)="closeMenu()">Customers</a>
          <a routerLink="/leads" routerLinkActive="active" (click)="closeMenu()">Leads</a>
          <a routerLink="/deals" routerLinkActive="active" (click)="closeMenu()">Deals</a>
          <a routerLink="/workflows" routerLinkActive="active" (click)="closeMenu()">Workflows</a>
          <a routerLink="/plugins" routerLinkActive="active" (click)="closeMenu()">Plugins</a>
        </nav>
        <div class="user">
          <div class="user-meta">
            <strong>{{ auth.displayName() }}</strong>
            <span>{{ auth.profile()?.email }}</span>
          </div>
          <div class="user-actions">
            <div class="notify notify--side">
              <button
                type="button"
                class="bell"
                [attr.aria-expanded]="notifications.isOpen()"
                aria-controls="notification-panel-side"
                aria-label="Notifications"
                (click)="onNotifyClick($event)"
              >
                <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                  <path
                    fill="currentColor"
                    d="M12 22a2.2 2.2 0 0 0 2.2-2.2h-4.4A2.2 2.2 0 0 0 12 22Zm7-6.2V11a7 7 0 1 0-14 0v4.8L3 17.8V19h18v-1.2l-2-1.8Z"
                  />
                </svg>
                @if (notifications.hasUnread()) {
                  <span class="badge">{{ notifications.unreadCount() }}</span>
                }
              </button>
              @if (notifications.isOpen()) {
                <div
                  id="notification-panel-side"
                  class="panel"
                  role="dialog"
                  aria-label="Notification center"
                  (click)="$event.stopPropagation()"
                >
                  <header>
                    <strong>Notifications</strong>
                    <button
                      type="button"
                      class="nx-btn nx-btn--ghost mark-all"
                      [disabled]="!notifications.hasUnread()"
                      (click)="notifications.markAllRead()"
                    >
                      Mark all read
                    </button>
                  </header>
                  @if (notifications.loading() && notifications.items().length === 0) {
                    <p class="muted">Loading…</p>
                  } @else if (notifications.items().length === 0) {
                    <p class="muted">No notifications yet. Qualify a lead to trigger a workflow.</p>
                  } @else {
                    <ul>
                      @for (item of notifications.items(); track item.id) {
                        <li [class.unread]="!item.isRead">
                          <button type="button" class="item" (click)="onOpen(item.id, item.href)">
                            <span class="title">{{ item.title }}</span>
                            <span class="body">{{ item.body }}</span>
                            <span class="meta">{{ item.createdAtUtc | date: 'short' }} · {{ item.category }}</span>
                          </button>
                        </li>
                      }
                    </ul>
                  }
                </div>
              }
            </div>
            <button type="button" class="nx-btn nx-btn--ghost sign-out" (click)="auth.logout()">Sign out</button>
          </div>
        </div>
      </aside>

      <main id="main" (click)="onMainClick()">
        <router-outlet />
      </main>
      <nx-toast-host />
    </div>
  `,
  styles: [
    `
      .shell {
        min-height: 100vh;
        min-height: 100dvh;
        display: grid;
        grid-template-columns: 240px 1fr;
        grid-template-rows: 1fr;
      }
      .topbar {
        display: none;
      }
      .backdrop {
        display: none;
      }
      .nav__head .close-nav {
        display: none;
      }
      .notify--side {
        display: block;
      }
      .nav {
        padding: 1.5rem 1rem;
        border-right: 1px solid var(--nx-border);
        background: rgba(255, 255, 255, 0.78);
        backdrop-filter: blur(10px);
        display: grid;
        grid-template-rows: auto 1fr auto;
        gap: 1.25rem;
        position: sticky;
        top: 0;
        height: 100vh;
        height: 100dvh;
        z-index: 30;
      }
      .nav__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
      }
      .brand {
        font-size: 1.45rem;
        color: var(--nx-brand);
      }
      nav {
        display: grid;
        gap: 0.35rem;
        align-content: start;
      }
      nav a {
        text-decoration: none;
        color: var(--nx-ink);
        padding: 0.65rem 0.75rem;
        border-radius: 8px;
        font-weight: 500;
        min-height: 2.75rem;
        display: flex;
        align-items: center;
      }
      nav a:hover,
      nav a.active {
        background: var(--nx-brand-soft);
        color: var(--nx-brand);
      }
      .user {
        display: grid;
        gap: 0.65rem;
      }
      .user-meta strong {
        display: block;
      }
      .user-meta span {
        color: var(--nx-ink-muted);
        font-size: 0.82rem;
        word-break: break-word;
      }
      .user-actions {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
      .sign-out {
        flex: 1;
      }
      .notify {
        position: relative;
      }
      .bell {
        position: relative;
        border: 1px solid var(--nx-border);
        background: #fff;
        color: var(--nx-brand);
        border-radius: 8px;
        width: 2.75rem;
        height: 2.75rem;
        cursor: pointer;
        display: grid;
        place-items: center;
        touch-action: manipulation;
      }
      .badge {
        position: absolute;
        top: -0.35rem;
        right: -0.35rem;
        min-width: 1.1rem;
        height: 1.1rem;
        padding: 0 0.25rem;
        border-radius: 999px;
        background: var(--nx-accent);
        color: #fff;
        font-size: 0.68rem;
        font-weight: 700;
        display: grid;
        place-items: center;
      }
      .panel {
        position: absolute;
        bottom: calc(100% + 0.5rem);
        right: 0;
        width: min(22rem, calc(100vw - 2rem));
        max-height: min(22rem, 60vh);
        overflow: auto;
        background: #fff;
        border: 1px solid var(--nx-border);
        border-radius: var(--nx-radius);
        box-shadow: var(--nx-shadow);
        z-index: 40;
        padding: 0.75rem;
      }
      .panel--top {
        bottom: auto;
        top: calc(100% + 0.5rem);
        right: 0;
      }
      .panel header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.65rem;
      }
      .mark-all {
        padding: 0.35rem 0.55rem;
        font-size: 0.78rem;
      }
      .muted {
        color: var(--nx-ink-muted);
        font-size: 0.88rem;
        margin: 0.5rem 0;
      }
      ul {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.35rem;
      }
      .item {
        width: 100%;
        text-align: left;
        border: 0;
        background: transparent;
        border-radius: 8px;
        padding: 0.65rem 0.5rem;
        cursor: pointer;
        display: grid;
        gap: 0.2rem;
        font: inherit;
        touch-action: manipulation;
      }
      li.unread .item {
        background: var(--nx-brand-soft);
      }
      .item:hover {
        background: rgba(15, 61, 92, 0.08);
      }
      .title {
        font-weight: 650;
        color: var(--nx-brand);
      }
      .body {
        font-size: 0.9rem;
      }
      .meta {
        font-size: 0.75rem;
        color: var(--nx-ink-muted);
      }
      main {
        min-width: 0;
        overflow-x: clip;
      }

      @media (max-width: 900px) {
        .shell {
          grid-template-columns: 1fr;
          grid-template-rows: auto 1fr;
        }
        .topbar {
          display: flex;
          align-items: center;
          gap: 0.65rem;
          padding: 0.65rem 0.85rem;
          padding-top: max(0.65rem, env(safe-area-inset-top));
          padding-left: max(0.85rem, env(safe-area-inset-left));
          padding-right: max(0.85rem, env(safe-area-inset-right));
          border-bottom: 1px solid var(--nx-border);
          background: rgba(255, 255, 255, 0.92);
          backdrop-filter: blur(10px);
          position: sticky;
          top: 0;
          z-index: 25;
          grid-column: 1;
        }
        .topbar .brand {
          flex: 1;
          font-size: 1.25rem;
          min-width: 0;
        }
        .menu-btn {
          width: 2.75rem;
          height: 2.75rem;
          border: 1px solid var(--nx-border);
          border-radius: 8px;
          background: #fff;
          color: var(--nx-brand);
          cursor: pointer;
          display: grid;
          place-items: center;
          touch-action: manipulation;
          flex-shrink: 0;
        }
        .menu-btn__bars,
        .menu-btn__bars::before,
        .menu-btn__bars::after {
          display: block;
          width: 1.1rem;
          height: 2px;
          background: currentColor;
          border-radius: 2px;
          position: relative;
        }
        .menu-btn__bars::before,
        .menu-btn__bars::after {
          content: '';
          position: absolute;
          left: 0;
        }
        .menu-btn__bars::before {
          top: -5px;
        }
        .menu-btn__bars::after {
          top: 5px;
        }
        .notify--side {
          display: none;
        }
        .nav {
          position: fixed;
          left: 0;
          top: 0;
          bottom: 0;
          width: min(20rem, 86vw);
          height: 100dvh;
          transform: translateX(-105%);
          transition: transform 200ms ease;
          border-right: 1px solid var(--nx-border);
          box-shadow: none;
          padding-top: max(1rem, env(safe-area-inset-top));
          padding-bottom: max(1rem, env(safe-area-inset-bottom));
        }
        .shell--nav-open .nav {
          transform: translateX(0);
          box-shadow: var(--nx-shadow);
        }
        .nav__head .close-nav {
          display: grid;
          place-items: center;
          width: 2.5rem;
          height: 2.5rem;
          border: 0;
          background: transparent;
          font-size: 1.6rem;
          line-height: 1;
          color: var(--nx-ink-muted);
          cursor: pointer;
        }
        .backdrop {
          display: block;
          position: fixed;
          inset: 0;
          border: 0;
          padding: 0;
          margin: 0;
          background: rgba(20, 32, 51, 0.4);
          opacity: 0;
          pointer-events: none;
          transition: opacity 200ms ease;
          z-index: 28;
        }
        .shell--nav-open .backdrop {
          opacity: 1;
          pointer-events: auto;
        }
        main {
          grid-column: 1;
        }
      }
    `,
  ],
})
export class ShellComponent implements OnInit {
  readonly auth = inject(AuthStore);
  readonly notifications = inject(NotificationStore);
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);

  ngOnInit(): void {
    this.notifications.start();
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      this.closeMenu();
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
    this.notifications.close();
  }

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
    document.body.style.overflow = this.menuOpen() ? 'hidden' : '';
    if (this.menuOpen()) {
      this.notifications.close();
    }
  }

  closeMenu(): void {
    this.menuOpen.set(false);
    document.body.style.overflow = '';
  }

  onNotifyClick(event: Event): void {
    event.stopPropagation();
    this.closeMenu();
    this.notifications.toggle();
  }

  onMainClick(): void {
    this.notifications.close();
    this.closeMenu();
  }

  onOpen(id: string, href?: string | null): void {
    this.notifications.markRead(id);
    this.notifications.close();
    this.closeMenu();
    if (href) {
      void this.router.navigateByUrl(href);
    }
  }
}
