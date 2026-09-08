import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface LeadDto {
  id: string;
  stageId: string;
  title: string;
  source?: string | null;
  email?: string | null;
  companyName?: string | null;
  score: number;
  status: string;
  notes?: string | null;
  tags?: string[];
}

export interface LeadBoardDto {
  pipelineId: string;
  pipelineName: string;
  columns: Array<{
    stageId: string;
    stageName: string;
    sortOrder: number;
    isWon: boolean;
    isLost: boolean;
    items: LeadDto[];
  }>;
}

@Injectable({ providedIn: 'root' })
export class LeadApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/leads`;

  board() {
    return this.http.get<LeadBoardDto>(`${this.base}/board`);
  }

  create(title: string, source?: string) {
    return this.http.post<LeadDto>(this.base, { title, source });
  }

  move(id: string, stageId: string) {
    return this.http.post<LeadDto>(`${this.base}/${id}/move`, { stageId });
  }
}
