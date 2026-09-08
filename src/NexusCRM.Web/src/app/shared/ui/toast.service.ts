import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  type: 'success' | 'error' | 'info';
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  readonly messages = signal<ToastMessage[]>([]);

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  info(text: string): void {
    this.push('info', text);
  }

  dismiss(id: number): void {
    this.messages.update((items) => items.filter((m) => m.id !== id));
  }

  private push(type: ToastMessage['type'], text: string): void {
    const id = ++this.seq;
    this.messages.update((items) => [...items, { id, type, text }]);
    window.setTimeout(() => this.dismiss(id), 3200);
  }
}
