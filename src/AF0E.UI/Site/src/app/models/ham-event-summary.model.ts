export interface HamEventSummaryModel {
  id: number;
  eventType: string;
  name: string;
  startDate: Date | null;
  endDate: Date | null;
}
