import {Component, DestroyRef, inject, OnInit, output, ViewEncapsulation} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {AutoCompleteCompleteEvent, AutoCompleteModule, AutoCompleteSelectEvent} from 'primeng/autocomplete';
import {FloatLabelModule} from 'primeng/floatlabel';
import {LogbookService} from '../../services/logbook.service';
import {Utils} from '../../shared/utils';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {NavigationEnd, Router} from '@angular/router';
import {MenubarModule} from 'primeng/menubar';
import {MenuItem} from 'primeng/api';
import {BreakpointObserver} from '@angular/cdk/layout';
import {ButtonModule} from 'primeng/button';
import {MenuModule} from 'primeng/menu';
import {filter} from 'rxjs';
import {TieredMenu} from 'primeng/tieredmenu';
import {AppAuthService} from '../../services/auth.service';

// noinspection JSIgnoredPromiseFromCall
@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    AutoCompleteModule,
    ButtonModule,
    FloatLabelModule,
    FormsModule,
    MenuModule,
    MenubarModule,
    TieredMenu,
  ],
})
export class HeaderComponent implements OnInit {
  private _router = inject(Router);
  private _responsive = inject(BreakpointObserver);
  private _destroyRef = inject(DestroyRef);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _logbookSvc = inject(LogbookService);
  private _authSvc = inject(AppAuthService);
  searchCall!: string;
  callsFound: any[] = [];
  isLessThan1000px = false;
  menuItems: MenuItem[] | undefined;
  callSelected = output();
  searchTitle  = '';

  ngOnInit(): void {
    const routerSub = this._router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        let arr = event.url.split('/');
        if (arr[1].toLowerCase() == 'logbook') {
          switch (arr.length) {
            case 3: this.searchCall = decodeURIComponent(arr[2]);
              break;
            case 4: this.searchCall = `${decodeURIComponent(arr[2])}/${decodeURIComponent(arr[3])}`;
              break;
            case 5: this.searchCall = `${decodeURIComponent(arr[2])}/${decodeURIComponent(arr[3])}/${decodeURIComponent(arr[4])}`;
              break;
              default: this.searchCall = '';
          }
        }
        else
          this.searchCall = '';
      });

    this.configureMenu();
    this.onSearchFocus(false);
    this.setResponsive();

    const authSub = this._authSvc.isAuthenticated$.subscribe(isAuthenticated => this.onAuthChanged(isAuthenticated));

    this._destroyRef.onDestroy(() => routerSub.unsubscribe());
    this._destroyRef.onDestroy(() => authSub.unsubscribe());
  }

  search(event: AutoCompleteCompleteEvent) {
    this._logbookSvc.lookupPartial(event.query).subscribe({
      next: r => this.callsFound = r,
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    })
  }

  onSearchFocus(focus: boolean) {
    this.searchTitle = focus ? 'enter call sign' : 'log search...'
  }

  onSelect(event: AutoCompleteSelectEvent) {
    this.callSelected.emit(event.value);
    this._router.navigate(['/logbook', event.value]);
  }

  private setResponsive(): void {
    const sub = this._responsive.observe('(max-width: 1000px)')
      .subscribe(x => this.isLessThan1000px = x.matches);
    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private configureMenu() {
    this.menuItems = [
      {
        label: 'Home',
        icon: 'pi pi-home',
        command: () => {
          this._router.navigate(['/']);
        }
      },
      {
        label: 'Logbook',
        icon: 'pi pi-book',
        command: () => {
          this._router.navigate(['/logbook']);
        }
      },
      {
        label: 'POTA',
        icon: 'pi pi-image',
        items: [
          {
            label: 'Activations',
            icon: 'pi pi-bolt',
            command: () => {
              this._router.navigate(['/pota/activations']);
            }
          },
          {
            label: 'Map',
            icon: 'pi pi-map',
            command: () => {
              this._router.navigate(['/map']);
            }
          },
          {
            label: 'Unconfirmed',
            icon: 'pi pi-hourglass',
            command: () => {
              this._router.navigate(['/pota/log/unconfirmed']);
            }
          },
        ]
      },
      {
        label: 'Info',
        icon: 'pi pi-info-circle',
        items: [
          {
            label: 'Stats',
            icon: 'pi pi-chart-bar',
            command: () => {
              this._router.navigate(['/stats']);
            }
          },
          {
            label: 'About',
            icon: 'pi pi-face-smile',
            command: () => {
              this._router.navigate(['/about']);
            }
          }
        ]
      },
      {
        label: 'User',
        icon: 'pi pi-user',
        items: [
          {
            label: 'Login',
            icon: 'pi pi-sign-in',
            command: () => this._authSvc.login()
          },
        ]
      }
    ]
  }

  private onAuthChanged(isAuthenticated: boolean) {
    const userMenu = this.menuItems!.find(item => item.label === 'User');

    if (!userMenu)
      return;

    userMenu.items = isAuthenticated ? [
      {
        label: 'Logout',
        icon: 'pi pi-sign-out',
        command: () => this._authSvc.logout()
      },
    ] : [
      {
        label: 'Login',
        icon: 'pi pi-sign-in',
        command: () => this._authSvc.login()
      }
    ];

    this.menuItems = [...this.menuItems!];
  }
}
