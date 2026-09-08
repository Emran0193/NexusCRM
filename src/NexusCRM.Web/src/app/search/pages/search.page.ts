import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, Subject, switchMap, of } from 'rxjs';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { LoadingBlockComponent } from '../../shared/ui/loading-block.component';
import { SearchApi, SearchHitDto } from '../data/search.api';

@Component({
  selector: 'nx-search-page',
  standalone: true,
  imports: [FormsModule, RouterLink, EmptyStateComponent, LoadingBlockComponent],
  template: `
    <section class="nx-page">
      <header class="nx-page-header">
        <div>
          <h1>Search</h1>
          <p>Find customers, leads, and deals across the tenant.</p>
        </div>
      </header>

      <label class="search-field">
        <span class="sr-only">Search query</span>
        <input
          class="nx-input"
          type="search"
          placeholder="Type at least 2 characters…"
          [ngModel]="query()"
          (ngModelChange)="onQuery($event)"
          autofocus
        />
      </label>

      @if (loading()) {
        <nx-loading-block label="Searching" />
      } @else if (query().trim().length < 2) {
        <nx-empty-state
          title="Start typing"
          message="Results update as you type. Try “Acme” or “Northwind” with the demo seed."
        />
      } @else if (!hits().length) {
        <nx-empty-state title="No matches" message="Nothing matched that query in this tenant." />
      } @else {
        <ul class="results">
          @for (hit of hits(); track hit.entityType + hit.id) {
            <li>
              <a [routerLink]="hit.href">
                <span class="type">{{ hit.entityType }}</span>
                <strong>{{ hit.title }}</strong>
                <span class="sub">{{ hit.subtitle || '—' }}</span>
              </a>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [
    `
      .search-field {
        display: block;
        margin-bottom: 1.25rem;
        max-width: 40rem;
      }
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        border: 0;
      }
      .results {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.35rem;
        max-width: 48rem;
      }
      .results a {
        display: grid;
        grid-template-columns: 6rem 1fr;
        column-gap: 0.85rem;
        row-gap: 0.15rem;
        padding: 0.95rem 0.15rem;
        border-bottom: 1px solid var(--nx-border);
        text-decoration: none;
        color: inherit;
        touch-action: manipulation;
      }
      .results a:hover strong {
        color: var(--nx-brand);
      }
      .type {
        grid-row: span 2;
        align-self: center;
        font-size: 0.75rem;
        font-weight: 700;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--nx-accent);
      }
      .sub {
        grid-column: 2;
        color: var(--nx-ink-muted);
        font-size: 0.9rem;
        word-break: break-word;
      }
      @media (max-width: 560px) {
        .results a {
          grid-template-columns: 1fr;
          gap: 0.2rem;
        }
        .type {
          grid-row: auto;
        }
        .sub {
          grid-column: 1;
        }
      }
    `,
  ],
})
export class SearchPage implements OnInit {
  private readonly api = inject(SearchApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly query$ = new Subject<string>();

  readonly query = signal('');
  readonly hits = signal<SearchHitDto[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.query$
      .pipe(
        debounceTime(220),
        distinctUntilChanged(),
        switchMap((q) => {
          const trimmed = q.trim();
          if (trimmed.length < 2) {
            this.loading.set(false);
            this.hits.set([]);
            return of(null);
          }
          this.loading.set(true);
          return this.api.search(trimmed);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          if (!response) {
            return;
          }
          this.hits.set(
            response.items.map((h) => ({
              ...h,
              id: String(h.id),
            })),
          );
          this.loading.set(false);
        },
        error: () => {
          this.hits.set([]);
          this.loading.set(false);
        },
      });
  }

  onQuery(value: string): void {
    this.query.set(value);
    this.query$.next(value);
  }
}
