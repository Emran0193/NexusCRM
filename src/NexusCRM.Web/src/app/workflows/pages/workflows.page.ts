import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkflowApi, WorkflowDefinitionDto, WorkflowRunDto } from '../data/workflow.api';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { LoadingBlockComponent } from '../../shared/ui/loading-block.component';
import { ToastService } from '../../shared/ui/toast.service';

@Component({
  selector: 'nx-workflows-page',
  standalone: true,
  imports: [CommonModule, EmptyStateComponent, LoadingBlockComponent],
  template: `
    <section class="nx-page">
      <header class="nx-page-header">
        <div>
          <h1>Workflows</h1>
          <p>Rules that fire on lead/deal stage transitions and related events.</p>
        </div>
        <button class="nx-btn nx-btn--ghost" type="button" (click)="reload()">Refresh</button>
      </header>

      @if (loading()) {
        <nx-loading-block label="Loading workflows" />
      } @else {
        <div class="grid">
          <section class="nx-panel list">
            <h2>Definitions</h2>
            @if (!workflows().length) {
              <nx-empty-state title="No workflows" message="Seeded rules appear in development." />
            }
            @for (workflow of workflows(); track workflow.id) {
              <article>
                <div>
                  <strong>{{ workflow.name }}</strong>
                  <p>{{ workflow.description }}</p>
                  <small>{{ workflow.triggerType }}</small>
                  <ul>
                    @for (c of workflow.conditions; track $index) {
                      <li>WHEN {{ c.field }} {{ c.operator }} {{ c.value }}</li>
                    }
                    @for (a of workflow.actions; track $index) {
                      <li>THEN {{ a.type }}{{ a.value ? ': ' + a.value : '' }}</li>
                    }
                  </ul>
                </div>
                <button
                  class="nx-btn"
                  [class.nx-btn--ghost]="workflow.isEnabled"
                  type="button"
                  (click)="toggle(workflow)"
                >
                  {{ workflow.isEnabled ? 'Disable' : 'Enable' }}
                </button>
              </article>
            }
          </section>

          <section class="nx-panel list">
            <h2>Recent runs</h2>
            @if (!runs().length) {
              <nx-empty-state
                title="No runs yet"
                message="Move a lead to Qualified or a deal to Won to trigger seeded workflows."
              />
            }
            @for (run of runs(); track run.id) {
              <article>
                <div>
                  <strong>{{ run.triggerType }} · {{ run.status }}</strong>
                  <p>{{ run.resultSummary || run.error || 'Queued / running' }}</p>
                  <small>{{ run.entityType }} {{ run.entityId }} · {{ run.createdAtUtc | date: 'medium' }}</small>
                </div>
              </article>
            }
          </section>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .grid {
        display: grid;
        grid-template-columns: 1.2fr 1fr;
        gap: 1rem;
      }
      .list {
        padding: 1rem;
      }
      h2 {
        margin: 0 0 0.85rem;
        font-size: 1rem;
        font-family: var(--nx-font-sans);
      }
      article {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        padding: 0.85rem 0;
        border-bottom: 1px solid var(--nx-border);
      }
      article > div {
        min-width: 0;
      }
      p,
      small,
      li {
        color: var(--nx-ink-muted);
        word-break: break-word;
      }
      p {
        margin: 0.25rem 0;
      }
      ul {
        margin: 0.4rem 0 0;
        padding-left: 1rem;
        font-size: 0.85rem;
      }
      @media (max-width: 900px) {
        .grid {
          grid-template-columns: 1fr;
        }
        article {
          flex-direction: column;
        }
        article .nx-btn {
          width: 100%;
        }
      }
    `,
  ],
})
export class WorkflowsPage implements OnInit {
  private readonly api = inject(WorkflowApi);
  private readonly toasts = inject(ToastService);

  readonly workflows = signal<WorkflowDefinitionDto[]>([]);
  readonly runs = signal<WorkflowRunDto[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: (items) => {
        this.workflows.set(items);
        this.api.runs().subscribe({
          next: (runs) => {
            this.runs.set(runs);
            this.loading.set(false);
          },
          error: () => {
            this.loading.set(false);
            this.toasts.error('Could not load workflow runs');
          },
        });
      },
      error: () => {
        this.loading.set(false);
        this.toasts.error('Could not load workflows');
      },
    });
  }

  toggle(workflow: WorkflowDefinitionDto): void {
    this.api.toggle(workflow.id, !workflow.isEnabled).subscribe({
      next: () => {
        this.toasts.success(workflow.isEnabled ? 'Workflow disabled' : 'Workflow enabled');
        this.reload();
      },
      error: () => this.toasts.error('Toggle failed'),
    });
  }
}
