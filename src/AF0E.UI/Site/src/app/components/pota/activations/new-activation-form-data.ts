import {maxLength, minLength, required, schema} from '@angular/forms/signals';

export interface NewActivationFormData {
  prevDayActivationId?: number;
  parkNumber: string;
  grid: string;
  county: string;
  state: string;
  lat: string;
  lon: string;
  stationCallsign: string;
  operatorCallsign: string;
  startDate?: Date;
}

export const initialActivationData: NewActivationFormData = {
  parkNumber: '',
  grid: '',
  county: '',
  state: '',
  lat: '',
  lon: '',
  stationCallsign: 'AF0E',
  operatorCallsign: 'AF0E',
}

export const activationSchema = schema<NewActivationFormData>((root) => {
  required(root.parkNumber, {message: 'Park number is required'});
  minLength(root.parkNumber, 7, {message: 'Park number must be 7 or 8 characters'});
  maxLength(root.parkNumber, 8, {message: 'Park number must be 7 or 8 characters'});
  required(root.grid, {message: 'Grid is required'});
  minLength(root.grid, 4, {message: 'Grid must be 4 or 6 characters'});
  maxLength(root.grid, 6, {message: 'Grid must be 4 or 6 characters'});
  required(root.county, {message: 'County is required'});
  maxLength(root.county, 200, {message: 'County cannot be longer than 200 characters'});
  required(root.state, {message: 'State is required'});
  minLength(root.state, 2, {message: 'State must be 2 characters long'});
  maxLength(root.state, 2, {message: 'State must be 2 characters long'});
  required(root.lat, {message: 'Latitude is required'});
  required(root.lon, {message: 'Longitude is required'});
  required(root.stationCallsign, {message: 'Callsign is required'});
  required(root.operatorCallsign, {message: 'Operator is required'});
});
