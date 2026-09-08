import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface CrmSummaryReportDto {
  customerCount: number;
  openLeadCount: number;
  qualifiedLeadCount: number;
  openDealCount: number;
  wonDealCount: number;
  lostDealCount: number;
  openPipelineAmount: number;
  wonAmount: number;
  currency: string;
  generatedAtUtc: string;
}

export interface FunnelStageDto {
  stageId: string;
  stageName: string;
  sortOrder: number;
  isWon: boolean;
  isLost: boolean;
  count: number;
  amount: number;
}

export interface PipelineFunnelReportDto {
  pipelineId: string;
  pipelineName: string;
  pipelineType: string;
  stages: FunnelStageDto[];
}

export interface PipelineFunnelsResponse {
  pipelines: PipelineFunnelReportDto[];
}

@Injectable({ providedIn: 'root' })
export class ReportApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/reports`;

  summary() {
    return this.http.get<CrmSummaryReportDto>(`${this.base}/summary`);
  }

  pipelines() {
    return this.http.get<PipelineFunnelsResponse>(`${this.base}/pipelines`);
  }
}
