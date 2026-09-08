import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { LoadingBlockComponent } from '../../shared/ui/loading-block.component';
import {
  CrmSummaryReportDto,
  PipelineFunnelReportDto,
  ReportApi,
} from '../data/report.api';

@Component({
  selector: 'nx-reports-page',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, EmptyStateComponent, LoadingBlockComponent],
  template: `
    <section class="nx-page">
      <header class="nx-page-header">
        <div>
          <h1>Reports</h1>
          <p>Pipeline health and CRM KPIs for the current tenant.</p>
        </div>
        <button class="nx-btn nx-btn--ghost" type="button" (click)="load()">Refresh</button>
      </header>

      @if (loading()) {
        <nx-loading-block label="Loading reports" />
      } @else if (error()) {
        <nx-empty-state title="Unable to load reports" [message]="error()!" />
      } @else if (summary()) {
        <dl class="kpis">
          <div>
            <dt>Customers</dt>
            <dd>{{ summary()!.customerCount }}</dd>
          </div>
          <div>
            <dt>Open leads</dt>
            <dd>{{ summary()!.openLeadCount }}</dd>
          </div>
          <div>
            <dt>Qualified leads</dt>
            <dd>{{ summary()!.qualifiedLeadCount }}</dd>
          </div>
          <div>
            <dt>Open deals</dt>
            <dd>{{ summary()!.openDealCount }}</dd>
          </div>
          <div>
            <dt>Open pipeline</dt>
            <dd>
              {{ summary()!.openPipelineAmount | currency: summary()!.currency : 'symbol-narrow' : '1.0-0' }}
            </dd>
          </div>
          <div>
            <dt>Won</dt>
            <dd>
              {{ summary()!.wonDealCount }}
              <small>
                {{ summary()!.wonAmount | currency: summary()!.currency : 'symbol-narrow' : '1.0-0' }}
              </small>
            </dd>
          </div>
        </dl>
        <p class="generated">Generated {{ summary()!.generatedAtUtc | date: 'medium' }}</p>

        <div class="funnels">
          @for (pipeline of funnels(); track pipeline.pipelineId) {
            <section class="funnel">
              <h2>{{ pipeline.pipelineName }}</h2>
              <p>{{ pipeline.pipelineType }} funnel</p>
              @for (stage of pipeline.stages; track stage.stageId) {
                <div class="stage">
                  <div class="stage-meta">
                    <strong>{{ stage.stageName }}</strong>
                    <span>
                      {{ stage.count }}
                      @if (pipeline.pipelineType === 'Deal' && stage.amount) {
                        ·
                        {{ stage.amount | currency: summary()!.currency : 'symbol-narrow' : '1.0-0' }}
                      }
                    </span>
                  </div>
                  <div class="bar" aria-hidden="true">
                    <span [style.width.%]="barWidth(pipeline, stage.count)"></span>
                  </div>
                </div>
              }
            </section>
          }
        </div>
      }
    </section>
  `,
  styles: [
    `
      .kpis {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(7.5rem, 1fr));
        gap: 1rem 1.25rem;
        margin: 0 0 0.5rem;
      }
      .kpis div {
        padding-bottom: 0.75rem;
        border-bottom: 1px solid var(--nx-border);
      }
      dt {
        margin: 0;
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--nx-ink-muted);
      }
      dd {
        margin: 0.25rem 0 0;
        font-family: var(--nx-font-display);
        font-size: 1.55rem;
        color: var(--nx-brand);
      }
      dd small {
        display: block;
        margin-top: 0.15rem;
        font-family: var(--nx-font-sans);
        font-size: 0.85rem;
        color: var(--nx-ink-muted);
      }
      .generated {
        color: var(--nx-ink-muted);
        font-size: 0.85rem;
        margin: 0 0 1.5rem;
      }
      .funnels {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 16rem), 1fr));
        gap: 1.75rem;
      }
      .funnel h2 {
        margin: 0;
        font-size: 1.2rem;
        color: var(--nx-brand);
      }
      .funnel > p {
        margin: 0.25rem 0 1rem;
        color: var(--nx-ink-muted);
      }
      .stage {
        margin-bottom: 0.75rem;
      }
      .stage-meta {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        font-size: 0.92rem;
        margin-bottom: 0.3rem;
      }
      .stage-meta span {
        color: var(--nx-ink-muted);
        white-space: nowrap;
        flex-shrink: 0;
      }
      @media (max-width: 480px) {
        .kpis {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
        .stage-meta {
          flex-wrap: wrap;
        }
        .stage-meta span {
          white-space: normal;
        }
      }
      .bar {
        height: 0.45rem;
        background: var(--nx-brand-soft);
        border-radius: 999px;
        overflow: hidden;
      }
      .bar span {
        display: block;
        height: 100%;
        background: var(--nx-accent);
        border-radius: inherit;
        min-width: 0;
        transition: width 280ms ease;
      }
    `,
  ],
})
export class ReportsPage implements OnInit {
  private readonly api = inject(ReportApi);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly summary = signal<CrmSummaryReportDto | null>(null);
  readonly funnels = signal<PipelineFunnelReportDto[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.summary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.api.pipelines().subscribe({
          next: (response) => {
            this.funnels.set(response.pipelines);
            this.loading.set(false);
          },
          error: () => {
            this.loading.set(false);
            this.error.set('Pipeline funnels could not be loaded. Check reports.read permission.');
          },
        });
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Reports require the reports.read permission. Sign out and sign in again after upgrading.');
      },
    });
  }

  barWidth(pipeline: PipelineFunnelReportDto, count: number): number {
    const max = Math.max(...pipeline.stages.map((s) => s.count), 1);
    return Math.round((count / max) * 100);
  }
}
