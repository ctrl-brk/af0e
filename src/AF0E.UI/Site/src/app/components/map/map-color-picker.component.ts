import {Component, input, model} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {
  InputColor,
  InputColorArea,
  InputColorAreaBackground,
  InputColorAreaHandle,
  InputColorInput,
  InputColorSlider,
  InputColorSliderHandle,
  InputColorSliderTrack,
  InputColorSwatch,
  InputColorSwatchBackground,
  InputColorTransparencyGrid
} from 'primeng/inputcolor';
import {Popover} from 'primeng/popover';

@Component({
  selector: 'app-map-color-picker',
  templateUrl: './map-color-picker.component.html',
  styleUrl: './map-color-picker.component.scss',
  imports: [
    FormsModule,
    Popover,
    InputColor,
    InputColorSwatch,
    InputColorTransparencyGrid,
    InputColorSwatchBackground,
    InputColorArea,
    InputColorAreaBackground,
    InputColorAreaHandle,
    InputColorSlider,
    InputColorSliderTrack,
    InputColorSliderHandle,
    InputColorInput,
  ]
})
export class MapColorPickerComponent {
  color = model<string>('#000000');
  disabled = input(false);
}

