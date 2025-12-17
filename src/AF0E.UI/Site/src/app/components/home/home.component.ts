import {Component, DestroyRef, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {Card} from 'primeng/card';
import {RouterLink} from '@angular/router';
import {BreakpointObserver} from '@angular/cdk/layout';

@Component({
  standalone: true,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card, RouterLink],
})
export class HomeComponent implements OnInit {
  private _responsive = inject(BreakpointObserver);
  private _destroyRef = inject(DestroyRef);
  isLessThan1000px = signal(false);

  ngOnInit(): void {
    const sub = this._responsive.observe('(max-width: 1000px)')
      .subscribe(x => this.isLessThan1000px.set(x.matches));
    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }
}
