import {Component, ViewEncapsulation} from '@angular/core';
import {RouterLink} from '@angular/router';
import {Button} from 'primeng/button';
import {Card} from 'primeng/card';
import {Divider} from 'primeng/divider';

@Component({
  templateUrl: './utils.component.html',
  styleUrl: './utils.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card, Divider, Button, RouterLink],
})
export class UtilsComponent {
  openExternal(url: string): void {
    window.open(url, '_blank');
  }
}
