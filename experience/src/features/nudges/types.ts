export interface NudgeCardDto {
  nudgeType: NudgeType;
  title: string;
  description: string;
  linkedEntityType: string;
  linkedEntityId: string;
  linkedEntityName: string;
  urgencyValue: number;
  ctaLabel: string;
}

export type NudgeType = 'OverdueTask' | 'StaleSubmission' | 'UpcomingRenewal';

export interface NudgesResponseDto {
  nudges: NudgeCardDto[];
}
