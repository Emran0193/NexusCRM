import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface PluginDescriptorDto {
  id: string;
  name: string;
  version: string;
  description: string;
  capabilities: string[];
  isEnabled: boolean;
  source: string;
}

@Injectable({ providedIn: 'root' })
export class PluginApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/plugins`;

  list() {
    return this.http.get<PluginDescriptorDto[]>(this.base);
  }

  toggle(pluginId: string, isEnabled: boolean) {
    return this.http.post<void>(`${this.base}/${encodeURIComponent(pluginId)}/toggle`, { isEnabled });
  }
}
