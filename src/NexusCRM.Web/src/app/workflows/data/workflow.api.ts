import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface WorkflowDefinitionDto {
  id: string;
  name: string;
  description: string;
  triggerType: string;
  isEnabled: boolean;
  conditions: Array<{ field: string; operator: string; value: string }>;
  actions: Array<{ type: string; target?: string | null; value?: string | null }>;
  createdAtUtc: string;
}

export interface WorkflowRunDto {
  id: string;
  workflowDefinitionId: string;
  triggerType: string;
  entityType: string;
  entityId: string;
  status: string;
  resultSummary?: string | null;
  error?: string | null;
  createdAtUtc: string;
  completedAtUtc?: string | null;
}

@Injectable({ providedIn: 'root' })
export class WorkflowApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/workflows`;

  list() {
    return this.http.get<WorkflowDefinitionDto[]>(this.base);
  }

  runs(take = 30) {
    return this.http.get<WorkflowRunDto[]>(`${this.base}/runs`, { params: { take } });
  }

  toggle(id: string, isEnabled: boolean) {
    return this.http.post<WorkflowDefinitionDto>(`${this.base}/${id}/toggle`, { isEnabled });
  }
}
