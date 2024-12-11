import {Component, ViewEncapsulation} from '@angular/core';
import {Card} from 'primeng/card';

@Component({
  standalone: true,
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card],
})
export class AboutComponent {
}
