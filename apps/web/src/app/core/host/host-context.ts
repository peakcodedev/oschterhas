import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * Which part of the app the current host name belongs to.
 * - `admin`: administration (`admin.<baseDomain>`)
 * - `instance`: a search, either `<slug>.<baseDomain>` or a custom domain (slug unknown, the API resolves it)
 * - `landing`: the base domain itself
 */
export type HostContext =
  | { readonly kind: 'admin' }
  | { readonly kind: 'instance'; readonly slug: string | null }
  | { readonly kind: 'landing' };

export function resolveHostContext(
  hostname: string,
  baseDomain: string,
  adminSubdomain: string,
): HostContext {
  const host = hostname.trim().toLowerCase().replace(/\.$/, '');
  const base = baseDomain.toLowerCase();

  if (host === base || host === `www.${base}`) {
    return { kind: 'landing' };
  }
  if (host === `${adminSubdomain}.${base}`) {
    return { kind: 'admin' };
  }
  if (host.endsWith(`.${base}`)) {
    const label = host.slice(0, -(base.length + 1));
    // Only a single label is a valid slug; deeper subdomains are not instances.
    return { kind: 'instance', slug: label.includes('.') ? null : label };
  }
  return { kind: 'instance', slug: null };
}

export const HOST_CONTEXT = new InjectionToken<HostContext>('HOST_CONTEXT', {
  providedIn: 'root',
  factory: () =>
    resolveHostContext(
      window.location.hostname,
      environment.baseDomain,
      environment.adminSubdomain,
    ),
});
