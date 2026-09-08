import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CustomerApi } from '../data/customer.api';
import { CustomerDetailDto } from '../data/customer.models';

@Component({
  selector: 'nx-customer-detail-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="nx-page page" *ngIf="customer() as c">
      <a routerLink="/customers" class="back">← Customers</a>
      <header class="nx-page-header">
        <div>
          <h1>{{ c.displayName }}</h1>
          <p>{{ c.type }} · {{ c.status }} · {{ c.email || 'No email' }}</p>
          <div class="tags">
            @for (tag of c.tags; track tag) {
              <span>{{ tag }}</span>
            }
          </div>
        </div>
      </header>

      <div class="grid">
        <section class="nx-panel block">
          <h2>Notes</h2>
          <textarea class="nx-input area" [(ngModel)]="note" rows="3" placeholder="Add a note…"></textarea>
          <button class="nx-btn" type="button" (click)="addNote()">Save note</button>
          <ul>
            @for (n of c.notes; track n.id) {
              <li>
                <p>{{ n.body }}</p>
                <small>{{ n.createdAtUtc | date: 'medium' }}</small>
              </li>
            }
          </ul>
        </section>

        <section class="nx-panel block">
          <h2>Contacts</h2>
          <div class="inline">
            <input class="nx-input" [(ngModel)]="contactName" placeholder="Name" />
            <input class="nx-input" [(ngModel)]="contactEmail" placeholder="Email" type="email" />
            <button class="nx-btn" type="button" (click)="addContact()">Add</button>
          </div>
          <ul>
            @for (contact of c.contacts; track contact.id) {
              <li>
                <strong>{{ contact.name }}</strong>
                <span>{{ contact.email || '—' }}</span>
              </li>
            } @empty {
              <li>No contacts yet.</li>
            }
          </ul>
        </section>

        <section class="nx-panel block timeline">
          <h2>Timeline</h2>
          <ul>
            @for (entry of c.timeline; track entry.id) {
              <li>
                <strong>{{ entry.summary }}</strong>
                <small>{{ entry.eventType }} · {{ entry.occurredAtUtc | date: 'medium' }}</small>
              </li>
            }
          </ul>
        </section>
      </div>
    </section>
  `,
  styles: [
    `
      .page {
        max-width: 1100px;
      }
      .back {
        display: inline-flex;
        align-items: center;
        min-height: 2.5rem;
        color: var(--nx-brand);
        text-decoration: none;
        font-weight: 600;
        margin-bottom: 0.25rem;
      }
      .tags {
        display: flex;
        gap: 0.4rem;
        flex-wrap: wrap;
        margin-top: 0.65rem;
      }
      .tags span {
        background: var(--nx-brand-soft);
        padding: 0.25rem 0.55rem;
        border-radius: 999px;
        font-size: 0.8rem;
      }
      .grid {
        display: grid;
        grid-template-columns: 1.2fr 1fr;
        gap: 1rem;
      }
      .block {
        padding: 1rem;
      }
      .timeline {
        grid-column: 1 / -1;
      }
      h2 {
        margin: 0 0 0.75rem;
        font-size: 1rem;
        font-family: var(--nx-font-sans);
      }
      .area {
        margin-bottom: 0.5rem;
        min-height: 5rem;
        resize: vertical;
      }
      ul {
        list-style: none;
        padding: 0;
        margin: 0.75rem 0 0;
        display: grid;
        gap: 0.55rem;
      }
      li {
        display: grid;
        gap: 0.15rem;
        padding-bottom: 0.5rem;
        border-bottom: 1px solid var(--nx-border);
      }
      li p {
        margin: 0;
        word-break: break-word;
      }
      small,
      span {
        color: var(--nx-ink-muted);
        font-size: 0.82rem;
        word-break: break-word;
      }
      .inline {
        display: grid;
        grid-template-columns: 1fr 1fr auto;
        gap: 0.4rem;
        align-items: start;
      }
      @media (max-width: 900px) {
        .grid {
          grid-template-columns: 1fr;
        }
        .inline {
          grid-template-columns: 1fr;
        }
        .inline .nx-btn {
          width: 100%;
        }
        .block .nx-btn {
          width: 100%;
        }
      }
    `,
  ],
})
export class CustomerDetailPage implements OnInit {
  private readonly api = inject(CustomerApi);
  private readonly route = inject(ActivatedRoute);
  readonly customer = signal<CustomerDetailDto | null>(null);
  note = '';
  contactName = '';
  contactEmail = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.load(id);
    }
  }

  load(id: string): void {
    this.api.getById(id).subscribe({ next: (c) => this.customer.set(c) });
  }

  addNote(): void {
    const c = this.customer();
    if (!c || !this.note.trim()) {
      return;
    }

    this.api.addNote(c.id, this.note.trim()).subscribe({
      next: () => {
        this.note = '';
        this.load(c.id);
      },
    });
  }

  addContact(): void {
    const c = this.customer();
    if (!c || !this.contactName.trim()) {
      return;
    }

    this.api.addContact(c.id, this.contactName.trim(), this.contactEmail || undefined).subscribe({
      next: () => {
        this.contactName = '';
        this.contactEmail = '';
        this.load(c.id);
      },
    });
  }
}
