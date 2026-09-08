import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomerApi } from '../data/customer.api';
import { CustomerDto } from '../data/customer.models';

@Component({
  selector: 'nx-customer-list-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="nx-page page">
      <header class="nx-page-header">
        <div>
          <h1>Customers</h1>
          <p>Tenant-scoped customer directory</p>
        </div>
      </header>

      <div class="nx-toolbar">
        <input
          class="nx-input"
          type="search"
          placeholder="Search customers…"
          [ngModel]="search()"
          (ngModelChange)="search.set($event); load()"
        />
        <button class="nx-btn" type="button" (click)="createSample()" [disabled]="creating()">
          {{ creating() ? 'Creating…' : 'Add sample' }}
        </button>
      </div>

      @if (error()) {
        <p class="nx-error" role="alert">{{ error() }}</p>
      }

      <ul class="list">
        @for (customer of customers(); track customer.id) {
          <li>
            <a [routerLink]="['/customers', customer.id]">
              <strong>{{ customer.displayName }}</strong>
              <span>{{ customer.type }} · {{ customer.status }}</span>
              <span>{{ customer.email || '—' }}</span>
            </a>
          </li>
        } @empty {
          <li class="empty">No customers yet.</li>
        }
      </ul>
    </section>
  `,
  styles: [
    `
      .page {
        max-width: 880px;
      }
      .list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.5rem;
      }
      .list li {
        margin: 0;
      }
      .list a {
        display: grid;
        gap: 0.2rem;
        padding: 0.95rem 0.15rem;
        border-bottom: 1px solid var(--nx-border);
        color: inherit;
        text-decoration: none;
        min-height: 3.25rem;
        touch-action: manipulation;
      }
      .list a:hover strong,
      .list a:active strong {
        color: var(--nx-brand);
      }
      .list span {
        color: var(--nx-ink-muted);
        font-size: 0.9rem;
        word-break: break-word;
      }
      .empty {
        color: var(--nx-ink-muted);
        padding: 1rem 0;
      }
    `,
  ],
})
export class CustomerListPage implements OnInit {
  private readonly api = inject(CustomerApi);

  readonly customers = signal<CustomerDto[]>([]);
  readonly search = signal('');
  readonly error = signal<string | null>(null);
  readonly creating = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error.set(null);
    this.api.search(this.search()).subscribe({
      next: (page) => this.customers.set(page.items),
      error: () => this.error.set('Unable to load customers. Is the API running?'),
    });
  }

  createSample(): void {
    this.creating.set(true);
    this.api
      .create({
        type: 'Individual',
        displayName: `Sample Customer ${new Date().toLocaleTimeString()}`,
        email: 'sample@nexuscrm.local',
      })
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.load();
        },
        error: () => {
          this.creating.set(false);
          this.error.set('Create failed. Ensure X-Tenant-Id is set and API is up.');
        },
      });
  }
}
