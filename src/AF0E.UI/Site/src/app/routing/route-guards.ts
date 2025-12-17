import {inject} from '@angular/core';
import {Router} from '@angular/router';
import {map, switchMap} from 'rxjs/operators';
import {AppAuthService} from '../services/auth.service';
import {of} from 'rxjs';

export function rolePermissionGuard(roles?: string[], permissions?: string[]) {
  return () => {
    const authSvc = inject(AppAuthService);
    const router = inject(Router);

    if (roles && roles.length > 0) {
      return of(roles).pipe(
        switchMap(roles =>
          authSvc.hasAnyRoleAsync(roles).pipe(
            map(has => has ? true : router.createUrlTree(['/unauthorized']))
          )
        )
      );
    }
    if (permissions && permissions.length > 0) {
      return of(permissions).pipe(
        switchMap(perms =>
          authSvc.hasAnyPermissionAsync(perms).pipe(
            map(has => has ? true : router.createUrlTree(['/unauthorized']))
          )
        )
      );
    }
    return true;
  };
}
