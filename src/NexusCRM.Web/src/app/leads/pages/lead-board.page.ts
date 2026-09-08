import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeadApi, LeadBoardDto, LeadDto } from '../data/lead.api';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { LoadingBlockComponent } from '../../shared/ui/loading-block.component';
import { ToastService } from '../../shared/ui/toast.service';
import { BoardHubService, LeadBoardChangedEvent } from '../../core/realtime/board-hub.service';
import { AuthStore } from '../../auth/data/auth.store';

@Component({
  selector: 'nx-lead-board-page',
  standalone: true,
  imports: [CommonModule, FormsModule, EmptyStateComponent, LoadingBlockComponent],
  template: `
    <section class="nx-page">
      <header class="nx-page-header">
        <div>
          <h1>{{ board()?.pipelineName || 'Leads' }}</h1>
          <p>
            Drag cards between stages.
            <span class="live" [class.live--on]="live() === 'connected'" [class.live--warn]="live() === 'reconnecting'">
              {{ liveLabel() }}
            </span>
          </p>
        </div>
        <form class="create" (ngSubmit)="create()">
          <label class="sr-only" for="lead-title">Lead title</label>
          <input id="lead-title" class="nx-input" [(ngModel)]="newTitle" name="title" placeholder="New lead title" />
          <button class="nx-btn" type="submit" [disabled]="!newTitle.trim() || busy()">Add lead</button>
        </form>
      </header>
      <p class="board-hint">On phones, swipe columns sideways or use Move on a card.</p>

      @if (error()) {
        <p class="nx-error" role="alert">{{ error() }}</p>
      }

      @if (loading()) {
        <nx-loading-block label="Loading lead board" />
      } @else if (!board()?.columns?.length) {
        <nx-empty-state title="No pipeline configured" message="Seed data creates a default lead pipeline in development." />
      } @else {
        <div class="nx-board" role="list">
          @for (column of board()!.columns; track column.stageId) {
            <section
              class="nx-column"
              role="listitem"
              [attr.aria-label]="column.stageName"
              (dragover)="$event.preventDefault()"
              (drop)="onDrop($event, column.stageId)"
            >
              <h2>
                {{ column.stageName }}
                <span>{{ column.items.length }}</span>
              </h2>

              @if (!column.items.length) {
                <nx-empty-state title="No leads" message="Drop a card here or create one." />
              }

              @for (lead of column.items; track lead.id) {
                <article
                  class="nx-card"
                  [class.nx-card--optimistic]="pendingIds().has(lead.id)"
                  draggable="true"
                  (dragstart)="onDragStart($event, lead)"
                >
                  <strong>{{ lead.title }}</strong>
                  <span>{{ lead.companyName || lead.source || 'No source' }}</span>
                  <span>Score {{ lead.score }} · {{ lead.status }}</span>
                  @if (lead.tags?.length) {
                    <span class="tags">{{ lead.tags!.join(' · ') }}</span>
                  }
                  <label class="nx-card__move">
                    <span class="sr-only">Move {{ lead.title }}</span>
                    <select
                      [ngModel]="lead.stageId"
                      [ngModelOptions]="{ standalone: true }"
                      (ngModelChange)="moveToStage(lead, $event)"
                      [disabled]="pendingIds().has(lead.id)"
                    >
                      @for (stage of board()!.columns; track stage.stageId) {
                        <option [value]="stage.stageId">{{ stage.stageName }}</option>
                      }
                    </select>
                  </label>
                </article>
              }
            </section>
          }
        </div>
      }
    </section>
  `,
  styles: [
    `
      .create {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
        width: min(100%, 28rem);
      }
      .create .nx-input {
        flex: 1 1 12rem;
        min-width: 0;
        width: auto;
      }
      .board-hint {
        display: none;
        margin: -0.5rem 0 1rem;
        color: var(--nx-ink-muted);
        font-size: 0.85rem;
      }
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
      }
      .live {
        margin-left: 0.35rem;
        font-size: 0.8rem;
        font-weight: 700;
        color: var(--nx-ink-muted);
      }
      .live--on {
        color: var(--nx-success);
      }
      .live--warn {
        color: var(--nx-accent);
      }
      .tags {
        display: block;
        margin-top: 0.2rem;
        font-size: 0.75rem;
        color: var(--nx-ink-muted);
      }
      @media (max-width: 900px) {
        .create {
          width: 100%;
        }
        .create .nx-btn {
          width: 100%;
        }
        .board-hint {
          display: block;
        }
      }
    `,
  ],
})
export class LeadBoardPage implements OnInit {
  private readonly api = inject(LeadApi);
  private readonly toasts = inject(ToastService);
  private readonly hub = inject(BoardHubService);
  private readonly auth = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  readonly board = signal<LeadBoardDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly pendingIds = signal(new Set<string>());
  readonly live = signal<'connected' | 'reconnecting' | 'disconnected'>('disconnected');
  newTitle = '';
  private dragging: LeadDto | null = null;

  ngOnInit(): void {
    this.load();
    void this.hub.start();

    this.hub.connectionState$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((state) => {
      this.live.set(state);
    });

    this.hub.leadChanged$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      this.applyRemote(event);
    });
  }

  liveLabel(): string {
    switch (this.live()) {
      case 'connected':
        return '● Live';
      case 'reconnecting':
        return '● Reconnecting…';
      default:
        return '○ Offline';
    }
  }

  load(): void {
    this.loading.set(true);
    this.api.board().subscribe({
      next: (board) => {
        this.board.set(board);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load lead board.');
        this.loading.set(false);
      },
    });
  }

  create(): void {
    if (!this.newTitle.trim()) {
      return;
    }

    this.busy.set(true);
    this.api.create(this.newTitle.trim(), 'Web').subscribe({
      next: (lead) => {
        this.busy.set(false);
        this.newTitle = '';
        this.toasts.success('Lead created');
        this.upsertLead(lead);
      },
      error: () => {
        this.busy.set(false);
        this.toasts.error('Could not create lead');
      },
    });
  }

  onDragStart(event: DragEvent, lead: LeadDto): void {
    this.dragging = lead;
    event.dataTransfer?.setData('text/plain', lead.id);
  }

  onDrop(event: DragEvent, stageId: string): void {
    event.preventDefault();
    const lead = this.dragging;
    this.dragging = null;
    if (!lead || lead.stageId === stageId) {
      return;
    }
    this.moveToStage(lead, stageId);
  }

  moveToStage(lead: LeadDto, stageId: string): void {
    if (!lead || lead.stageId === stageId || this.pendingIds().has(lead.id)) {
      return;
    }

    const previous = structuredClone(this.board());
    this.applyOptimisticMove(lead.id, stageId);
    this.pendingIds.update((set) => new Set(set).add(lead.id));

    this.api.move(lead.id, stageId).subscribe({
      next: (updated) => {
        this.pendingIds.update((set) => {
          const next = new Set(set);
          next.delete(lead.id);
          return next;
        });
        this.upsertLead(updated);
        this.toasts.success('Lead moved');
      },
      error: () => {
        this.board.set(previous);
        this.pendingIds.update((set) => {
          const next = new Set(set);
          next.delete(lead.id);
          return next;
        });
        this.toasts.error('Move failed — restored previous board');
      },
    });
  }

  private applyRemote(event: LeadBoardChangedEvent): void {
    const actor = this.auth.profile()?.userId;
    if (event.actorUserId && actor && event.actorUserId === actor) {
      this.upsertLead(event.lead);
      return;
    }

    this.upsertLead(event.lead);
    if (event.changeType === 'moved') {
      this.toasts.info(`Live update: ${event.lead.title} moved`);
    } else if (event.changeType === 'created') {
      this.toasts.info(`Live update: ${event.lead.title} added`);
    }
  }

  private upsertLead(lead: LeadDto): void {
    const current = this.board();
    if (!current) {
      return;
    }

    const without = current.columns.map((column) => ({
      ...column,
      items: column.items.filter((item) => item.id !== lead.id),
    }));

    this.board.set({
      ...current,
      columns: without.map((column) =>
        column.stageId === lead.stageId
          ? { ...column, items: [lead, ...column.items] }
          : column,
      ),
    });
  }

  private applyOptimisticMove(leadId: string, stageId: string): void {
    const current = this.board();
    if (!current) {
      return;
    }

    let moving: LeadDto | undefined;
    const columns = current.columns.map((column) => {
      const remaining = column.items.filter((item) => {
        if (item.id === leadId) {
          moving = { ...item, stageId };
          return false;
        }
        return true;
      });
      return { ...column, items: remaining };
    });

    if (!moving) {
      return;
    }

    this.board.set({
      ...current,
      columns: columns.map((column) =>
        column.stageId === stageId ? { ...column, items: [moving!, ...column.items] } : column,
      ),
    });
  }
}
