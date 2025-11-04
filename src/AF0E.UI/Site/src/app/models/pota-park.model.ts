export interface PotaParkModel {
  parkId?: number;
  parkNum: string;
  parkName: string;
  parkDesc?: string;
  lat?: number;
  long?: number;
  grid?: string;
  location?: string;
  country?: string;
  totalActivationCount?: number;
  totalQsoCount?: number;
  active?: boolean;
}
