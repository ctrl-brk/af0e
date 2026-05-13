import {Component, DestroyRef, inject, OnInit, signal, viewChild, ViewEncapsulation} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {defaultTitle} from '../../shared/constants';
import {LogContentComponent} from './log-content.component';
import {Title} from '@angular/platform-browser';
import {Splitter} from 'primeng/splitter';
import {AppAuthService} from '../../services/auth.service';
import {DxClusterComponent} from '../dxcluster/dxcluster.component';
import {DxClusterSpotModel} from '../../models/dx-cluster-spot.model';

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
  private _route = inject(ActivatedRoute);
  private _logContent = viewChild(LogContentComponent);
  protected _authSvc = inject(AppAuthService);
  protected showDxCluster = signal(true);

  ngOnInit() {
    this._titleSvc.setTitle('AFØE - Logbook');

    const sub = this._route.queryParamMap.subscribe(params => {
      const val = params.get('dxcluster');
      this.showDxCluster.set(val === null || val === 'N' || val === '1' || val === 'false');
    });

    this._destroyRef.onDestroy(() => {
      this._titleSvc.setTitle(defaultTitle);
      sub.unsubscribe();
    });
  }

  protected onTune(spot: DxClusterSpotModel) {
    this._logContent()?.onAddQso();
  }
}
