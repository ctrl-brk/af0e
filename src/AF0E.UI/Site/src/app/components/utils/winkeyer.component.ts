import {Component, HostListener, inject, ViewEncapsulation} from '@angular/core';
import {Card} from 'primeng/card';
import {FloatLabel} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {Tooltip} from 'primeng/tooltip';
import {Select} from 'primeng/select';
import {InfraService} from '../../services/infra.service';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {Utils} from '../../shared/utils';

type FKey = `F${number}`;

interface WinkeyerMacro {
  text: string;
  shortcut: FKey | null;
}

@Component({
  selector: 'app-winkeyer',
  templateUrl: './winkeyer.component.html',
  styleUrl: './winkeyer.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card, FloatLabel, InputText, FormsModule, Button, Tooltip, Select],
})
export class WinkeyerComponent {
  private _infraSvc = inject(InfraService);
  private _ntfSvc = inject(NotificationService);
  private _log = inject(LogService);

  protected fKeyOptions = Array.from({length: 12}, (_, i) => {
    const key = `F${i + 1}` as FKey;
    return {label: key, value: key};
  });

  protected macros: WinkeyerMacro[] = [
    {shortcut: 'F1', text: 'CQ CQ DE AF0|E AF0|E K'},
    {shortcut: 'F2', text: 'CQ POTA AF0|E AF0|E K'},
    {shortcut: 'F3', text: 'TU 599'},
    {shortcut: 'F4', text: 'AF0|E'},
    {shortcut: 'F5', text: ''},
    {shortcut: 'F6', text: ''},
  ];

  @HostListener('window:keydown', ['$event'])
  protected onKeyDown(event: KeyboardEvent): void {
    const key = event.key?.toUpperCase();

    if (key === 'ESCAPE') {
      event.preventDefault();
      this.stopCw();
      return;
    }

    if (!/^F([1-9]|1[0-2])$/.test(key))
      return;

    if (event.altKey || event.ctrlKey || event.metaKey)
      return;

    const index = this.macros.findIndex(x => x.shortcut === key);
    if (index < 0)
      return;

    const text = (this.macros[index].text || '').trim();
    if (!text)
      return;

    event.preventDefault();
    this.sendCw(index);
  }

  protected sendCw(index: number): void {
    const row = this.macros[index];

    if (!row)
      return;

    const text = (row.text || '').trim();
    if (!text)
      return;

    this._infraSvc.sendCw(text, null).subscribe({
      error: e => {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    });
  }

  stopCw() {
    this._infraSvc.cancelCw().subscribe({
      error: e => {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    })
  }
}
