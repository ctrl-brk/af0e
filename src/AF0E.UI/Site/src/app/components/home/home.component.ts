import {Component, ViewEncapsulation} from '@angular/core';
import {Card} from 'primeng/card';

@Component({
  standalone: true,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card],
})
export class HomeComponent {
}
