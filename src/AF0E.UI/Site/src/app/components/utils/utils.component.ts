import {Component, ViewEncapsulation} from '@angular/core';
import {RouterLink} from '@angular/router';
import {ButtonDirective} from 'primeng/button';
import {ExternalLink} from '@primeicons/angular/external-link';
import {Card} from 'primeng/card';
import {Divider} from 'primeng/divider';

@Component({
  templateUrl: './utils.component.html',
  styleUrl: './utils.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card, Divider, ButtonDirective, ExternalLink, RouterLink],
})
export class UtilsComponent {
  openExternal(url: string): void {
    window.open(url, '_blank');
  }
}
