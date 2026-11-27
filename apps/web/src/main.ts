import { registerLocaleData } from '@angular/common';
import localeDeCh from '@angular/common/locales/de-CH';
import localeEnCh from '@angular/common/locales/en-CH';
import localeGsw from '@angular/common/locales/gsw';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Date and number formats for the UI languages (see core/i18n/languages.ts).
registerLocaleData(localeDeCh);
registerLocaleData(localeGsw);
registerLocaleData(localeEnCh);

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
