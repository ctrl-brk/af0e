import {Component, DestroyRef, inject, OnInit, ViewEncapsulation} from '@angular/core';
import {defaultTitle} from '../../shared/constants';
import {LogContentComponent} from './log-content.component';
import {Title} from '@angular/platform-browser';
import {Splitter} from 'primeng/splitter';
import {AppAuthService} from '../../services/auth.service';
import {DxClusterComponent} from '../dxcluster/dxcluster.component';

@Component({
  selector: 'app-logbook',
  templateUrl: './logbook.component.html',
  styleUrl: './logbook.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    LogContentComponent,
    Splitter,
    DxClusterComponent,
  ],
})
export class LogbookComponent implements OnInit {
  private _titleSvc = inject(Title);
  private _destroyRef = inject(DestroyRef);
  protected _authSvc = inject(AppAuthService);

  ngOnInit() {
    this._titleSvc.setTitle('AFØE - Logbook');

    this._destroyRef.onDestroy(() => {
      this._titleSvc.setTitle(defaultTitle);
    });
  }
}
