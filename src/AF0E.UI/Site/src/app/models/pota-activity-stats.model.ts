import {PotaActivityModel} from './pota-activity.model';

export interface PotaActivityStatsModel {
  activity: PotaActivityModel,
  parkContactsByBandMode: [
    {
      band: string;
      mode: string;
      count: number;
    }
  ],
  totalParkContacts: number;
  totalCallSignContacts: number;
}
