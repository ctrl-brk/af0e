import {Component, ViewEncapsulation} from '@angular/core';
import {Card} from 'primeng/card';

@Component({
  templateUrl: './stats.component.html',
  styleUrl: './stats.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card],
})
export class StatsComponent {
}
