import { Component, Input } from '@angular/core';

@Component({
  selector: 'nx-empty-state',
  standalone: true,
  template: `
    <div class="nx-empty" role="status">
      <strong>{{ title }}</strong>
      @if (message) {
        <p>{{ message }}</p>
      }
      <ng-content />
    </div>
  `,
  styles: [
    `
      strong {
        display: block;
        color: var(--nx-ink);
        margin-bottom: 0.35rem;
      }
      p {
        margin: 0;
      }
    `,
  ],
})
export class EmptyStateComponent {
  @Input({ required: true }) title!: string;
  @Input() message = '';
}
