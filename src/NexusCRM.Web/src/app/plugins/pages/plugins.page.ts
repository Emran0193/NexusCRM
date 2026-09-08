import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PluginApi, PluginDescriptorDto } from '../data/plugin.api';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { LoadingBlockComponent } from '../../shared/ui/loading-block.component';
import { ToastService } from '../../shared/ui/toast.service';

@Component({
  selector: 'nx-plugins-page',
  standalone: true,
  imports: [CommonModule, EmptyStateComponent, LoadingBlockComponent],
  template: `
    <section class="nx-page">
      <header class="nx-page-header">
        <div>
          <h1>Plugins</h1>
          <p>Tenant extensions for lead enrichment and notification channels.</p>
        </div>
        <button class="nx-btn nx-btn--ghost" type="button" (click)="reload()">Refresh</button>
      </header>

      @if (loading()) {
        <nx-loading-block label="Loading plugins" />
      } @else if (!plugins().length) {
        <nx-empty-state
          title="No plugins installed"
          message="Sample plugins load when Plugins:LoadSamples is enabled."
        />
      } @else {
        <section class="nx-panel list">
          @for (plugin of plugins(); track plugin.id) {
            <article>
              <div>
                <strong>{{ plugin.name }}</strong>
                <p>{{ plugin.description }}</p>
                <small>
                  {{ plugin.id }} · v{{ plugin.version }} · {{ plugin.source }}
                  @if (plugin.capabilities.length) {
                    · {{ plugin.capabilities.join(', ') }}
                  }
                </small>
              </div>
              <button
                class="nx-btn"
                [class.nx-btn--ghost]="plugin.isEnabled"
                type="button"
                (click)="toggle(plugin)"
              >
                {{ plugin.isEnabled ? 'Disable' : 'Enable' }}
              </button>
            </article>
          }
        </section>
      }
    </section>
  `,
  styles: [
    `
      .list {
        padding: 1rem;
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
      small {
        color: var(--nx-ink-muted);
        word-break: break-word;
      }
      p {
        margin: 0.25rem 0;
      }
      @media (max-width: 900px) {
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
export class PluginsPage implements OnInit {
  private readonly api = inject(PluginApi);
  private readonly toasts = inject(ToastService);

  readonly plugins = signal<PluginDescriptorDto[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: (items) => {
        this.plugins.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toasts.error('Could not load plugins');
      },
    });
  }

  toggle(plugin: PluginDescriptorDto): void {
    this.api.toggle(plugin.id, !plugin.isEnabled).subscribe({
      next: () => {
        this.toasts.success(plugin.isEnabled ? 'Plugin disabled' : 'Plugin enabled');
        this.reload();
      },
      error: () => this.toasts.error('Toggle failed'),
    });
  }
}
