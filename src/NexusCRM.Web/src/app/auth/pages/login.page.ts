import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthStore } from '../data/auth.store';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'nx-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="login">
      <form class="panel nx-panel" (ngSubmit)="submit()">
        <p class="eyebrow">Enterprise CRM platform</p>
        <h1 class="nx-display">NexusCRM</h1>
        <p>Sign in to your tenant workspace</p>

        <label>
          Email
          <input class="nx-input" type="email" [(ngModel)]="email" name="email" autocomplete="username" required />
        </label>
        <label>
          Password
          <input
            class="nx-input"
            type="password"
            [(ngModel)]="password"
            name="password"
            autocomplete="current-password"
            required
          />
        </label>
        <label>
          MFA code <span>(optional)</span>
          <input class="nx-input" type="text" [(ngModel)]="mfaCode" name="mfa" inputmode="numeric" maxlength="6" />
        </label>

        @if (error()) {
          <p class="nx-error" role="alert">{{ error() }}</p>
        }

        <button class="nx-btn" type="submit" [disabled]="busy()">
          {{ busy() ? 'Signing in…' : 'Sign in' }}
        </button>

        <p class="hint">Demo: admin&#64;nexuscrm.local / ChangeMe!12345</p>
      </form>
    </section>
  `,
  styles: [
    `
      .login {
        min-height: 100vh;
        min-height: 100dvh;
        display: grid;
        place-items: center;
        padding: 1.25rem;
        padding-top: max(1.25rem, env(safe-area-inset-top));
        padding-bottom: max(1.25rem, env(safe-area-inset-bottom));
        padding-left: max(1.25rem, env(safe-area-inset-left));
        padding-right: max(1.25rem, env(safe-area-inset-right));
      }
      .panel {
        width: min(420px, 100%);
        display: grid;
        gap: 0.85rem;
        padding: 1.75rem 1.25rem;
      }
      .eyebrow {
        margin: 0;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        font-size: 0.72rem;
        color: var(--nx-accent);
        font-weight: 700;
      }
      h1 {
        margin: 0;
        color: var(--nx-brand);
        font-size: clamp(1.85rem, 7vw, 2.2rem);
      }
      p {
        margin: 0;
        color: var(--nx-ink-muted);
      }
      label {
        display: grid;
        gap: 0.35rem;
        font-size: 0.9rem;
      }
      label span {
        color: var(--nx-ink-muted);
      }
      .hint {
        font-size: 0.8rem;
        word-break: break-word;
      }
      .nx-btn {
        width: 100%;
      }
      @media (max-width: 480px) {
        .panel {
          padding: 1.35rem 1rem;
        }
      }
    `,
  ],
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  email = 'admin@nexuscrm.local';
  password = 'ChangeMe!12345';
  mfaCode = '';
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    this.busy.set(true);
    this.error.set(null);

    this.auth
      .login({
        email: this.email,
        password: this.password,
        tenantId: environment.defaultTenantId,
        mfaCode: this.mfaCode || null,
      })
      .subscribe({
        next: async () => {
          this.busy.set(false);
          await this.router.navigateByUrl('/customers');
        },
        error: (err) => {
          this.busy.set(false);
          this.error.set(err?.error?.detail ?? 'Sign-in failed.');
        },
      });
  }
}
