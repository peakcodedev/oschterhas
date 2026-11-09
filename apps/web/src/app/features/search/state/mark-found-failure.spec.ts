import { HttpErrorResponse } from '@angular/common/http';
import { toMarkFoundFailure } from './mark-found-failure';

const problem = (status: number, code?: string) =>
  new HttpErrorResponse({ status, error: code ? { status, code } : null });

describe('toMarkFoundFailure', () => {
  it.each([
    [problem(409, 'hare.already_found'), 'alreadyFound'],
    [problem(409, 'instance.not_active'), 'notActive'],
    [problem(429, 'rate_limited'), 'rateLimited'],
    [problem(429), 'rateLimited'],
    [problem(422, 'hare.nickname_too_long'), 'invalidName'],
    [problem(422, 'hare.nickname_invalid'), 'invalidName'],
    [problem(404, 'hare.not_found'), 'error'],
    [problem(0), 'error'],
    [new Error('boom'), 'error'],
  ])('maps %o to %s', (error, expected) => {
    expect(toMarkFoundFailure(error)).toBe(expected);
  });
});
