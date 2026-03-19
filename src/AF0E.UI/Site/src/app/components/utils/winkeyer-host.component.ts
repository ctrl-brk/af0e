import {Component} from '@angular/core';
import {WinkeyerComponent} from './winkeyer.component';

@Component({
  templateUrl: './winkeyer-host.component.html',
  styleUrl: './winkeyer-host.component.scss',
  imports: [WinkeyerComponent]
})
export class WinkeyerHostComponent {
}
