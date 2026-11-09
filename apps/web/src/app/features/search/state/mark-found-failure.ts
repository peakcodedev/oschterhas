import { HttpErrorResponse } from '@angular/common/http';
import { ApiProblem } from '../../../core/api/api-types';
import { MarkFoundFailure } from './search.models';

/** Maps an API error of "mark as found" to a reason the dialog can show. */
export function toMarkFoundFailure(error: unknown): MarkFoundFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return 'error';
  }
  const code = (error.error as ApiProblem | null)?.code;
  if (error.status === 429) {
    return 'rateLimited';
  }
  if (code === 'hare.already_found') {
    return 'alreadyFound';
  }
  if (code === 'instance.not_active') {
    return 'notActive';
  }
  if (code === 'hare.nickname_too_long' || code === 'hare.nickname_invalid') {
    return 'invalidName';
  }
  return 'error';
}
