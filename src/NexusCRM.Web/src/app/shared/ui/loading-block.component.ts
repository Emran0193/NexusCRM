import { Component, Input } from '@angular/core';

@Component({
  selector: 'nx-loading-block',
  standalone: true,
  template: `
    <div class="nx-skeleton" role="status" [attr.aria-label]="label">
      @for (line of lines; track $index) {
        <div class="nx-skeleton__line" [style.width]="line"></div>
      }
    </div>
  `,
})
export class LoadingBlockComponent {
  @Input() label = 'Loading';
  @Input() lines: string[] = ['70%', '100%', '85%', '60%'];
}
