import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface DealDto {
  id: string;
  stageId: string;
  title: string;
  amount: number;
  currency: string;
  status: string;
  rowVersion: string;
}

export interface DealBoardDto {
  pipelineId: string;
  pipelineName: string;
  openPipelineValue: number;
  columns: Array<{
    stageId: string;
    stageName: string;
    sortOrder: number;
    isWon: boolean;
    isLost: boolean;
    columnValue: number;
    items: DealDto[];
  }>;
}

@Injectable({ providedIn: 'root' })
export class DealApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/deals`;

  board() {
    return this.http.get<DealBoardDto>(`${this.base}/board`);
  }

  create(title: string, amount: number, currency = 'INR') {
    return this.http.post<DealDto>(this.base, { title, amount, currency });
  }

  move(id: string, stageId: string, rowVersion: string) {
    return this.http.post<DealDto>(`${this.base}/${id}/move`, { stageId, rowVersion });
  }
}
