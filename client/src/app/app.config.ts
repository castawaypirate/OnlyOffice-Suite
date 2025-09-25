import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';

export const environment = {
  production: false,
  apiUrl: 'http://localhost:5142/api',
  onlyOfficeServerUrl: 'http://localhost:3131/'
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),

  ]
};
