import {inject, Injectable} from '@angular/core';
import {AuthService} from '@auth0/auth0-angular';
import {map, Observable} from 'rxjs';
import {environment} from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AppAuthService {
  private auth = inject(AuthService);

  isAuthenticated$ = this.auth.isAuthenticated$;
  user$ = this.auth.user$;
  isLoading$ = this.auth.isLoading$;

  login() {
    this.auth.loginWithRedirect();
      /*.subscribe({
      next: () => console.log('✅ Login redirect initiated'),
      error: (err) => {
        console.error('❌ Login error:', err);
        alert(`Login failed: ${JSON.stringify(err)}`); // This will pause and show the error
      }
    });*/
  }

  logout() {
    this.auth.logout({
      logoutParams: {
        returnTo: window.location.origin
      }
    });
  }

  hasRole(role: string): Observable<boolean> {
    return this.auth.idTokenClaims$.pipe(
      map(claims => {
        const claimedRoles = claims?.[`${environment.claimType}/roles`] as string[] | undefined;
        return claimedRoles ? claimedRoles.includes(role) : false;
      })
    );
  }

  hasAnyRole(roles: string[]): Observable<boolean> {
    return this.auth.idTokenClaims$.pipe(
      map(claims => {
        const claimedRoles = claims?.[`${environment.claimType}/roles`] as string[] | undefined;
        return claimedRoles ? roles.some(r => claimedRoles.includes(r)) : false;
      })
    );
  }

  hasPermission(permission: string): Observable<boolean> {
    return this.auth.idTokenClaims$.pipe(
      map(claims => {
        const claimedPerms = claims?.[`${environment.claimType}/permissions`] as string[] | undefined;
        return claimedPerms ? claimedPerms.includes(permission) : false;
      })
    );
  }

  hasAnyPermission(permissions: string[]): Observable<boolean> {
    return this.auth.idTokenClaims$.pipe(
      map(claims => {
        const claimedPerms = claims?.[`${environment.claimType}/permissions`] as string[] | undefined;
        return claimedPerms ? permissions.some(p => permissions.includes(p)) : false;
      })
    );
  }
}
