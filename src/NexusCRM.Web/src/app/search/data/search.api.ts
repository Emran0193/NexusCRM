import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface SearchHitDto {
  entityType: string;
  id: string;
  title: string;
  subtitle?: string | null;
  href: string;
}

export interface GlobalSearchResponse {
  query: string;
  items: SearchHitDto[];
}

@Injectable({ providedIn: 'root' })
export class SearchApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/search`;

  search(q: string, takePerType = 8) {
    return this.http.get<GlobalSearchResponse>(this.base, {
      params: { q, takePerType },
    });
  }
}
