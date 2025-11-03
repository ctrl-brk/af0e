import {QsoSummaryModel} from './qso-summary.model';

export interface LogSearchResponseModel {
  totalCount: number;
  contacts: QsoSummaryModel[];
}
