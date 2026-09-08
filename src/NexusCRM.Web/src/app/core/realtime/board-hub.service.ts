import { Injectable, inject, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthStore } from '../../auth/data/auth.store';
import { LeadDto } from '../../leads/data/lead.api';
import { DealDto } from '../../deals/data/deal.api';
import { NotificationDto } from '../../notifications/data/notification.api';

export interface LeadBoardChangedEvent {
  changeType: string;
  lead: LeadDto;
  fromStageId?: string | null;
  actorUserId?: string | null;
  occurredAtUtc: string;
}

export interface DealBoardChangedEvent {
  changeType: string;
  deal: DealDto;
  fromStageId?: string | null;
  actorUserId?: string | null;
  occurredAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class BoardHubService implements OnDestroy {
  private readonly auth = inject(AuthStore);
  private connection: signalR.HubConnection | null = null;
  private started = false;

  private readonly leadChangedSubject = new Subject<LeadBoardChangedEvent>();
  private readonly dealChangedSubject = new Subject<DealBoardChangedEvent>();
  private readonly notificationCreatedSubject = new Subject<NotificationDto>();
  private readonly connectionStateSubject = new Subject<'connected' | 'reconnecting' | 'disconnected'>();

  readonly leadChanged$ = this.leadChangedSubject.asObservable();
  readonly dealChanged$ = this.dealChangedSubject.asObservable();
  readonly notificationCreated$ = this.notificationCreatedSubject.asObservable();
  readonly connectionState$ = this.connectionStateSubject.asObservable();

  async start(): Promise<void> {
    if (this.started) {
      return;
    }

    const token = this.auth.accessToken();
    if (!token) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => this.auth.accessToken() ?? '',
      })
      .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.on('LeadBoardChanged', (payload: LeadBoardChangedEvent) => {
      this.leadChangedSubject.next(this.normalizeLead(payload));
    });

    this.connection.on('DealBoardChanged', (payload: DealBoardChangedEvent) => {
      this.dealChangedSubject.next(this.normalizeDeal(payload));
    });

    this.connection.on('NotificationCreated', (payload: NotificationDto) => {
      this.notificationCreatedSubject.next(payload);
    });

    this.connection.onreconnecting(() => this.connectionStateSubject.next('reconnecting'));
    this.connection.onreconnected(() => this.connectionStateSubject.next('connected'));
    this.connection.onclose(() => {
      this.started = false;
      this.connectionStateSubject.next('disconnected');
    });

    await this.connection.start();
    this.started = true;
    this.connectionStateSubject.next('connected');
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.started = false;
      this.connectionStateSubject.next('disconnected');
    }
  }

  ngOnDestroy(): void {
    void this.stop();
  }

  private normalizeLead(payload: LeadBoardChangedEvent): LeadBoardChangedEvent {
    return {
      ...payload,
      lead: {
        ...payload.lead,
        id: String(payload.lead.id),
        stageId: String(payload.lead.stageId),
      },
      fromStageId: payload.fromStageId ? String(payload.fromStageId) : null,
    };
  }

  private normalizeDeal(payload: DealBoardChangedEvent): DealBoardChangedEvent {
    return {
      ...payload,
      deal: {
        ...payload.deal,
        id: String(payload.deal.id),
        stageId: String(payload.deal.stageId),
      },
      fromStageId: payload.fromStageId ? String(payload.fromStageId) : null,
    };
  }
}
