import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

export interface CallbackNotification {
  fileId: string;
  status: number;
  message: string;
  success?: boolean;
  source?: string;  // "save-and-close" or null
  savedAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | null = null;
  private readonly hubUrl = 'http://localhost:5142/hubs/onlyoffice';

  // Observables for different events
  public callbackReceived$ = new Subject<CallbackNotification>();
  public documentSaved$ = new Subject<CallbackNotification>();
  public documentForceSaved$ = new Subject<CallbackNotification>();

  constructor() {}

  public startConnection(): Promise<void> {
    if (this.hubConnection) {
      console.log('[SIGNALR] Connection already exists');
      return Promise.resolve();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        withCredentials: true,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.registerEventHandlers();

    return this.hubConnection
      .start()
      .then(() => {
        console.log('[SIGNALR] Connection started successfully');
      })
      .catch(err => {
        console.error('[SIGNALR] Error starting connection:', err);
        throw err;
      });
  }

  public stopConnection(): Promise<void> {
    if (!this.hubConnection) {
      return Promise.resolve();
    }

    return this.hubConnection
      .stop()
      .then(() => {
        console.log('[SIGNALR] Connection stopped');
        this.hubConnection = null;
      })
      .catch(err => {
        console.error('[SIGNALR] Error stopping connection:', err);
        throw err;
      });
  }

  public joinFileRoom(fileId: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('Connection not established');
    }

    return this.hubConnection
      .invoke('JoinFileRoom', fileId)
      .then(() => {
        console.log(`[SIGNALR] Joined room for file ${fileId}`);
      })
      .catch(err => {
        console.error(`[SIGNALR] Error joining room for file ${fileId}:`, err);
        throw err;
      });
  }

  public leaveFileRoom(fileId: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.resolve();
    }

    return this.hubConnection
      .invoke('LeaveFileRoom', fileId)
      .then(() => {
        console.log(`[SIGNALR] Left room for file ${fileId}`);
      })
      .catch(err => {
        console.error(`[SIGNALR] Error leaving room for file ${fileId}:`, err);
        throw err;
      });
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) {
      return;
    }

    this.hubConnection.on('CallbackReceived', (data: CallbackNotification) => {
      console.log('[SIGNALR] CallbackReceived event:', data);
      this.callbackReceived$.next(data);
    });

    this.hubConnection.on('DocumentSaved', (data: CallbackNotification) => {
      console.log('[SIGNALR] DocumentSaved event:', data);
      this.documentSaved$.next(data);
    });

    this.hubConnection.on('DocumentForceSaved', (data: CallbackNotification) => {
      console.log('[SIGNALR] DocumentForceSaved event:', data);
      this.documentForceSaved$.next(data);
    });

    this.hubConnection.onreconnecting(error => {
      console.log('[SIGNALR] Connection lost, reconnecting...', error);
    });

    this.hubConnection.onreconnected(connectionId => {
      console.log('[SIGNALR] Reconnected with connectionId:', connectionId);
    });

    this.hubConnection.onclose(error => {
      console.log('[SIGNALR] Connection closed', error);
    });
  }

  public get isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  public get connectionState(): signalR.HubConnectionState | null {
    return this.hubConnection?.state ?? null;
  }
}
