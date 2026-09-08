import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DealApi, DealBoardDto, DealDto } from '../data/deal.api';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { LoadingBlockComponent } from '../../shared/ui/loading-block.component';
import { ToastService } from '../../shared/ui/toast.service';
import { BoardHubService, DealBoardChangedEvent } from '../../core/realtime/board-hub.service';
import { AuthStore } from '../../auth/data/auth.store';

@Component({
  selector: 'nx-deal-board-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, EmptyStateComponent, LoadingBlockComponent],
  template: `
    <section class="nx-page">
      <header class="nx-page-header">
        <div>
          <h1>{{ board()?.pipelineName || 'Deals' }}</h1>
          <p>
            Open pipeline
            {{ board()?.openPipelineValue || 0 | currency: 'INR':'symbol':'1.0-0' }}
            <span class="live" [class.live--on]="live() === 'connected'" [class.live--warn]="live() === 'reconnecting'">
              {{ liveLabel() }}
            </span>
          </p>
        </div>
        <form class="create" (ngSubmit)="create()">
          <label class="sr-only" for="deal-title">Deal title</label>
          <input id="deal-title" class="nx-input" [(ngModel)]="newTitle" name="title" placeholder="Deal title" />
          <label class="sr-only" for="deal-amount">Amount</label>
          <input id="deal-amount" class="nx-input amount" type="number" [(ngModel)]="newAmount" name="amount" />
          <button class="nx-btn" type="submit" [disabled]="!newTitle.trim() || busy()">Add deal</button>
        </form>
      </header>
      <p class="board-hint">On phones, swipe columns sideways or use Move on a card.</p>

      @if (error()) {
        <p class="nx-error" role="alert">{{ error() }}</p>
      }

      @if (loading()) {
        <nx-loading-block label="Loading deal board" />
      } @else {
        <div class="nx-board">
          @for (column of board()?.columns || []; track column.stageId) {
            <section
              class="nx-column"
              (dragover)="$event.preventDefault()"
              (drop)="onDrop($event, column.stageId)"
            >
              <h2>
                {{ column.stageName }}
                <span>{{ column.columnValue | currency: 'INR':'symbol':'1.0-0' }}</span>
              </h2>

              @if (!column.items.length) {
                <nx-empty-state title="Empty stage" message="Drag a deal here." />
              }

              @for (deal of column.items; track deal.id) {
                <article
                  class="nx-card"
                  [class.nx-card--optimistic]="pendingIds().has(deal.id)"
                  draggable="true"
                  (dragstart)="onDragStart($event, deal)"
                >
                  <strong>{{ deal.title }}</strong>
                  <span>{{ deal.amount | currency: deal.currency:'symbol':'1.0-0' }}</span>
                  <span>{{ deal.status }}</span>
                  <label class="nx-card__move">
                    <span class="sr-only">Move {{ deal.title }}</span>
                    <select
                      [ngModel]="deal.stageId"
                      [ngModelOptions]="{ standalone: true }"
                      (ngModelChange)="moveToStage(deal, $event)"
                      [disabled]="pendingIds().has(deal.id)"
                    >
                      @for (stage of board()?.columns || []; track stage.stageId) {
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
        width: min(100%, 32rem);
      }
      .create .nx-input {
        flex: 1 1 10rem;
        min-width: 0;
        width: auto;
      }
      .amount {
        flex: 0 1 7.5rem !important;
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
      @media (max-width: 900px) {
        .create {
          width: 100%;
        }
        .amount {
          flex: 1 1 100% !important;
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
export class DealBoardPage implements OnInit {
  private readonly api = inject(DealApi);
  private readonly toasts = inject(ToastService);
  private readonly hub = inject(BoardHubService);
  private readonly auth = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  readonly board = signal<DealBoardDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly pendingIds = signal(new Set<string>());
  readonly live = signal<'connected' | 'reconnecting' | 'disconnected'>('disconnected');
  newTitle = '';
  newAmount = 100000;
  private dragging: DealDto | null = null;

  ngOnInit(): void {
    this.load();
    void this.hub.start();

    this.hub.connectionState$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((state) => {
      this.live.set(state);
    });

    this.hub.dealChanged$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
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
        this.error.set('Unable to load deal board.');
        this.loading.set(false);
      },
    });
  }

  create(): void {
    this.busy.set(true);
    this.api.create(this.newTitle.trim(), Number(this.newAmount) || 0).subscribe({
      next: (deal) => {
        this.busy.set(false);
        this.newTitle = '';
        this.toasts.success('Deal created');
        this.upsertDeal(deal);
      },
      error: () => {
        this.busy.set(false);
        this.toasts.error('Could not create deal');
      },
    });
  }

  onDragStart(event: DragEvent, deal: DealDto): void {
    this.dragging = deal;
    event.dataTransfer?.setData('text/plain', deal.id);
  }

  onDrop(event: DragEvent, stageId: string): void {
    event.preventDefault();
    const deal = this.dragging;
    this.dragging = null;
    if (!deal || deal.stageId === stageId) {
      return;
    }
    this.moveToStage(deal, stageId);
  }

  moveToStage(deal: DealDto, stageId: string): void {
    if (!deal || deal.stageId === stageId || this.pendingIds().has(deal.id)) {
      return;
    }

    const previous = structuredClone(this.board());
    this.applyOptimisticMove(deal.id, stageId);
    this.pendingIds.update((set) => new Set(set).add(deal.id));

    this.api.move(deal.id, stageId, deal.rowVersion).subscribe({
      next: (updated) => {
        this.pendingIds.update((set) => {
          const next = new Set(set);
          next.delete(deal.id);
          return next;
        });
        this.upsertDeal(updated);
        this.toasts.success('Deal moved');
      },
      error: (err) => {
        this.board.set(previous);
        this.pendingIds.update((set) => {
          const next = new Set(set);
          next.delete(deal.id);
          return next;
        });
        this.toasts.error(err?.error?.detail ?? 'Move failed — restored board');
      },
    });
  }

  private applyRemote(event: DealBoardChangedEvent): void {
    const actor = this.auth.profile()?.userId;
    this.upsertDeal(event.deal);
    if (event.actorUserId && actor && event.actorUserId === actor) {
      return;
    }

    if (event.changeType === 'moved') {
      this.toasts.info(`Live update: ${event.deal.title} moved`);
    } else if (event.changeType === 'created') {
      this.toasts.info(`Live update: ${event.deal.title} added`);
    }
  }

  private upsertDeal(deal: DealDto): void {
    const current = this.board();
    if (!current) {
      return;
    }

    const columns = current.columns.map((column) => ({
      ...column,
      items: column.items.filter((item) => item.id !== deal.id),
    }));

    const nextColumns = columns.map((column) => {
      const items = column.stageId === deal.stageId ? [deal, ...column.items] : column.items;
      return {
        ...column,
        items,
        columnValue: items.reduce((sum, d) => sum + d.amount, 0),
      };
    });

    const openPipelineValue = nextColumns
      .filter((c) => !c.isWon && !c.isLost)
      .reduce((sum, c) => sum + c.columnValue, 0);

    this.board.set({ ...current, columns: nextColumns, openPipelineValue });
  }

  private applyOptimisticMove(dealId: string, stageId: string): void {
    const current = this.board();
    if (!current) {
      return;
    }

    let moving: DealDto | undefined;
    const columns = current.columns.map((column) => ({
      ...column,
      items: column.items.filter((item) => {
        if (item.id === dealId) {
          moving = { ...item, stageId };
          return false;
        }
        return true;
      }),
    }));

    if (!moving) {
      return;
    }

    const nextColumns = columns.map((column) => {
      const items = column.stageId === stageId ? [moving!, ...column.items] : column.items;
      return {
        ...column,
        items,
        columnValue: items.reduce((sum, d) => sum + d.amount, 0),
      };
    });

    this.board.set({ ...current, columns: nextColumns });
  }
}
