import { inject } from '@angular/core';
import { CanMatchFn } from '@angular/router';
import { HOST_CONTEXT, HostContext } from './host-context';

/** Route only matches when the app runs on a host of the given kind. */
export function hostIs(kind: HostContext['kind']): CanMatchFn {
  return () => inject(HOST_CONTEXT).kind === kind;
}
