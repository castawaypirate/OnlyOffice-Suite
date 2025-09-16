import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app';

platformBrowserDynamic().bootstrapModule(AppModule)
  .catch((err: any) => console.error(err));
