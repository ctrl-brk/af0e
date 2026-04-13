import {maxLength, minLength, required, schema, SchemaPathTree, validate} from '@angular/forms/signals';

export interface PotaActivationModel {
  id: number;
  startDate: Date;
  endDate: Date | null;
  logSubmittedDate: Date | null;
  parkNum: string;
  parkName: string;
  siteComments: string|null;
  city: string|null;
  county: string;
  state: string;
  grid: string;
  lat: number;
  long: number;
  count: number;
  cwCount: number;
  digiCount: number;
  phoneCount: number;
  p2pCount: number;
  status: string;
}

export const activationSchema = schema<PotaActivationModel>((root) => {
  required(root.parkNum, {message: 'Park number is required'});
  maxLength(root.parkNum, 8, {message: 'Park number cannot be longer than 8 characters'});
  required(root.grid, {message: 'Grid is required'});
  minLength(root.grid, 4, {message: 'Grid must be 4 or 6 characters'});
  maxLength(root.grid, 6, {message: 'Grid must be 4 or 6 characters'});
  required(root.county, {message: 'County is required'});
  maxLength(root.county, 200, {message: 'County cannot be longer than 200 characters'});
  required(root.state, {message: 'State is required'});
  minLength(root.state, 2, {message: 'State must be 2 characters long'});
  maxLength(root.state, 2, {message: 'State must be 2 characters long'});
  required(root.lat, {message: 'Latitude is required'});
  required(root.long, {message: 'Longitude is required'});
  activationStatusValidator(root.status, {message: 'Status must be C or P'});
});

function activationStatusValidator(field: SchemaPathTree<string>, options?: {message?: string}) {
  validate(field, (ctx) => {
    if (ctx.value() === 'C' || ctx.value() === 'P')
      return null;

    return {
      kind: 'status',
      message: options?.message || 'Status must be C or P'
    }
  });
}
