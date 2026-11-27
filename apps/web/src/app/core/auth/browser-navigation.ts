import { DOCUMENT, inject, Injectable } from '@angular/core';

/** Full-page navigations the SPA cannot do with the router (login and logout at Peakcode Identity). */
@Injectable({ providedIn: 'root' })
export class BrowserNavigation {
  private readonly document = inject(DOCUMENT);

  goTo(url: string): void {
    this.document.location.assign(url);
  }

  /** Posts a form so the browser follows the server's redirect (to Keycloak's logout). */
  postForm(action: string, fields: Record<string, string>): void {
    const form = this.document.createElement('form');
    form.method = 'POST';
    form.action = action;
    for (const [name, value] of Object.entries(fields)) {
      const input = this.document.createElement('input');
      input.type = 'hidden';
      input.name = name;
      input.value = value;
      form.append(input);
    }
    this.document.body.append(form);
    form.submit();
  }

  cookie(name: string): string | null {
    const prefix = `${name}=`;
    const entry = this.document.cookie.split('; ').find((c) => c.startsWith(prefix));
    return entry ? decodeURIComponent(entry.slice(prefix.length)) : null;
  }
}
