import { HttpErrorResponse } from '@angular/common/http';
import { ApiProblem } from './api-types';

/** Error codes the UI has a translated message for (key `errors.<code>`). */
const KNOWN_CODES = new Set([
  'auth.platform_admin_required',
  'instance.not_found',
  'instance.slug_required',
  'instance.slug_invalid',
  'instance.slug_reserved',
  'instance.slug_taken',
  'instance.slug_change_forbidden',
  'instance.name_required',
  'instance.name_too_long',
  'instance.description_too_long',
  'instance.changed',
  'organizer.email_invalid',
  'organizer.name_invalid',
  'organizer.already_assigned',
  'organizer.already_active',
  'organizer.invitation_failed',
  'organizer.not_found',
  'identity.unavailable',
  'identity.user_conflict',
  'hare.not_found',
  'hare.changed',
  'hare.coordinates_invalid',
  'hare.name_too_long',
  'hare.description_too_long',
  'gpx.file_missing',
  'gpx.file_too_large',
  'gpx.invalid_xml',
  'gpx.dtd_not_allowed',
  'gpx.not_gpx',
  'gpx.no_waypoints',
  'gpx.too_many_waypoints',
  'user.cannot_demote_self',
  'user.not_found',
  'rate_limited',
  'csrf_invalid',
  'unauthorized',
  'network',
]);

/** Stable error code of an API error, e.g. `instance.slug_taken`; `unknown` if there is none we can show. */
export function problemCode(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'unknown';
  }
  if (error.status === 0) {
    return 'network';
  }
  if (error.status === 401) {
    return 'unauthorized';
  }
  if (error.status === 413) {
    return 'gpx.file_too_large';
  }
  const code = (error.error as ApiProblem | null)?.code;
  return code && KNOWN_CODES.has(code) ? code : 'unknown';
}
