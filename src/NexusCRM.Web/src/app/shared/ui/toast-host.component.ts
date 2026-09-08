import { Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'nx-toast-host',
  standalone: true,
  template: `
    <div class="toasts" aria-live="polite" aria-relevant="additions">
      @for (toast of toasts.messages(); track toast.id) {
        <div class="toast" [class]="toast.type" role="status">
          <span>{{ toast.text }}</span>
          <button type="button" (click)="toasts.dismiss(toast.id)" aria-label="Dismiss">×</button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .toasts {
        position: fixed;
        right: max(1rem, env(safe-area-inset-right));
        bottom: max(1rem, env(safe-area-inset-bottom));
        left: auto;
        display: grid;
        gap: 0.5rem;
        z-index: 50;
        width: min(360px, calc(100vw - 2rem - env(safe-area-inset-left) - env(safe-area-inset-right)));
      }
      @media (max-width: 480px) {
        .toasts {
          left: max(0.75rem, env(safe-area-inset-left));
          right: max(0.75rem, env(safe-area-inset-right));
          width: auto;
        }
      }
      .toast {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        padding: 0.8rem 0.9rem;
        border-radius: 10px;
        border: 1px solid var(--nx-border);
        background: #fff;
        box-shadow: var(--nx-shadow);
      }
      .toast.success {
        border-color: rgba(15, 106, 70, 0.35);
      }
      .toast.error {
        border-color: rgba(155, 28, 28, 0.35);
      }
      button {
        border: 0;
        background: transparent;
        cursor: pointer;
        font-size: 1.1rem;
        line-height: 1;
      }
    `,
  ],
})
export class ToastHostComponent {
  readonly toasts = inject(ToastService);
}
