-- F0025 local demo data for validating commission workspace behavior.
-- Safe to re-run: deletes only deterministic F0025 demo rows, then recreates them.

begin;

delete from "CommissionAdjustments"
where "Id" in (
  '25000000-0000-0000-0000-000000000701',
  '25000000-0000-0000-0000-000000000702'
);

delete from "RevenueAttributionProjections"
where "ExpectedCommissionId" in (
  '25000000-0000-0000-0000-000000000501',
  '25000000-0000-0000-0000-000000000502',
  '25000000-0000-0000-0000-000000000503'
);

delete from "ExpectedCommissions"
where "Id" in (
  '25000000-0000-0000-0000-000000000501',
  '25000000-0000-0000-0000-000000000502',
  '25000000-0000-0000-0000-000000000503'
);

delete from "ProducerSplitParticipants"
where "ProducerSplitAssignmentId" in (
  '25000000-0000-0000-0000-000000000301',
  '25000000-0000-0000-0000-000000000302',
  '25000000-0000-0000-0000-000000000303'
);

delete from "ProducerSplitAssignments"
where "Id" in (
  '25000000-0000-0000-0000-000000000301',
  '25000000-0000-0000-0000-000000000302',
  '25000000-0000-0000-0000-000000000303'
);

delete from "CommissionSchedules"
where "Id" in (
  '25000000-0000-0000-0000-000000000101',
  '25000000-0000-0000-0000-000000000102',
  '25000000-0000-0000-0000-000000000103'
);

with
admin_user as (
  select "Id" from "UserProfiles"
  where "RolesJson"::text like '%Admin%'
  order by "CreatedAt"
  limit 1
),
producer_users as (
  select
    "Id",
    row_number() over (order by "DisplayName") as rn
  from "UserProfiles"
),
source_policies as (
  select
    p."Id",
    p."PolicyNumber",
    p."LineOfBusiness",
    p."EffectiveDate",
    p."ExpirationDate",
    p."Premium",
    p."BrokerId",
    p."CarrierId",
    row_number() over (order by p."UpdatedAt" desc, p."PolicyNumber") as rn
  from "Policies" p
  where p."IsDeleted" = false
    and p."Premium" is not null
    and p."CarrierId" is not null
  limit 3
),
demo_rows as (
  select
    sp.*,
    au."Id" as admin_user_id,
    coalesce(pu."Id", au."Id") as producer_user_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000101'::uuid
      when 2 then '25000000-0000-0000-0000-000000000102'::uuid
      else '25000000-0000-0000-0000-000000000103'::uuid
    end as schedule_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000301'::uuid
      when 2 then '25000000-0000-0000-0000-000000000302'::uuid
      else '25000000-0000-0000-0000-000000000303'::uuid
    end as split_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000501'::uuid
      when 2 then '25000000-0000-0000-0000-000000000502'::uuid
      else '25000000-0000-0000-0000-000000000503'::uuid
    end as expected_id,
    case sp.rn
      when 1 then 12.5000::numeric
      when 2 then 10.0000::numeric
      else 0::numeric
    end as rate_percent,
    case sp.rn
      when 1 then 500.00::numeric
      when 2 then -250.00::numeric
      else 0.00::numeric
    end as approved_adjustment,
    case sp.rn
      when 3 then 'Exception'
      else 'Calculated'
    end as status,
    case sp.rn
      when 3 then 'MissingSchedule'
      else 'None'
    end as exception_state
  from source_policies sp
  cross join admin_user au
  left join producer_users pu on pu.rn = sp.rn
)
insert into "CommissionSchedules" (
  "Id", "CarrierMarketId", "LineOfBusiness", "State", "ProductCode", "Basis",
  "RatePercent", "FlatAmount", "EffectiveFrom", "EffectiveTo", "SourceNote",
  "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted"
)
select
  schedule_id,
  "CarrierId",
  "LineOfBusiness",
  null,
  'F0025-DEMO',
  'PercentOfPremium',
  nullif(rate_percent, 0),
  null,
  current_date - interval '120 days',
  current_date + interval '245 days',
  'F0025-demo commission schedule for local validation',
  now(),
  admin_user_id,
  now(),
  admin_user_id,
  false
from demo_rows
where rn in (1, 2);

with
admin_user as (
  select "Id" from "UserProfiles"
  where "RolesJson"::text like '%Admin%'
  order by "CreatedAt"
  limit 1
),
producer_users as (
  select
    "Id",
    row_number() over (order by "DisplayName") as rn
  from "UserProfiles"
),
source_policies as (
  select
    p."Id",
    p."PolicyNumber",
    p."LineOfBusiness",
    p."EffectiveDate",
    p."ExpirationDate",
    p."Premium",
    p."BrokerId",
    p."CarrierId",
    row_number() over (order by p."UpdatedAt" desc, p."PolicyNumber") as rn
  from "Policies" p
  where p."IsDeleted" = false
    and p."Premium" is not null
    and p."CarrierId" is not null
  limit 3
),
demo_rows as (
  select
    sp.*,
    au."Id" as admin_user_id,
    coalesce(pu."Id", au."Id") as producer_user_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000301'::uuid
      when 2 then '25000000-0000-0000-0000-000000000302'::uuid
      else '25000000-0000-0000-0000-000000000303'::uuid
    end as split_id
  from source_policies sp
  cross join admin_user au
  left join producer_users pu on pu.rn = sp.rn
)
insert into "ProducerSplitAssignments" (
  "Id", "PolicyId", "EffectiveFrom", "EffectiveTo", "Reason",
  "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted"
)
select
  split_id,
  "Id",
  current_date - interval '120 days',
  current_date + interval '245 days',
  'F0025-demo producer split for local validation',
  now(),
  admin_user_id,
  now(),
  admin_user_id,
  false
from demo_rows;

with
admin_user as (
  select "Id" from "UserProfiles"
  where "RolesJson"::text like '%Admin%'
  order by "CreatedAt"
  limit 1
),
producer_users as (
  select
    "Id",
    row_number() over (order by "DisplayName") as rn
  from "UserProfiles"
),
source_policies as (
  select
    p."Id",
    row_number() over (order by p."UpdatedAt" desc, p."PolicyNumber") as rn
  from "Policies" p
  where p."IsDeleted" = false
    and p."Premium" is not null
    and p."CarrierId" is not null
  limit 3
),
demo_rows as (
  select
    sp.*,
    au."Id" as admin_user_id,
    coalesce(pu."Id", au."Id") as producer_user_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000301'::uuid
      when 2 then '25000000-0000-0000-0000-000000000302'::uuid
      else '25000000-0000-0000-0000-000000000303'::uuid
    end as split_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000401'::uuid
      when 2 then '25000000-0000-0000-0000-000000000402'::uuid
      else '25000000-0000-0000-0000-000000000403'::uuid
    end as participant_id,
    case sp.rn
      when 1 then 60.0000::numeric
      when 2 then 100.0000::numeric
      else 100.0000::numeric
    end as split_percent
  from source_policies sp
  cross join admin_user au
  left join producer_users pu on pu.rn = sp.rn
)
insert into "ProducerSplitParticipants" (
  "Id", "ProducerSplitAssignmentId", "ProducerId", "SplitPercent",
  "SourceOwnershipSnapshotJson", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted"
)
select
  participant_id,
  split_id,
  producer_user_id,
  split_percent,
  jsonb_build_object('source', 'F0025-demo', 'policyRank', rn),
  now(),
  admin_user_id,
  now(),
  admin_user_id,
  false
from demo_rows;

with
admin_user as (
  select "Id" from "UserProfiles"
  where "RolesJson"::text like '%Admin%'
  order by "CreatedAt"
  limit 1
),
producer_users as (
  select
    "Id",
    row_number() over (order by "DisplayName") as rn
  from "UserProfiles"
),
source_policies as (
  select
    p."Id",
    p."PolicyNumber",
    p."LineOfBusiness",
    p."EffectiveDate",
    p."ExpirationDate",
    p."Premium",
    p."BrokerId",
    p."CarrierId",
    row_number() over (order by p."UpdatedAt" desc, p."PolicyNumber") as rn
  from "Policies" p
  where p."IsDeleted" = false
    and p."Premium" is not null
    and p."CarrierId" is not null
  limit 3
),
demo_rows as (
  select
    sp.*,
    au."Id" as admin_user_id,
    coalesce(pu."Id", au."Id") as producer_user_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000101'::uuid
      when 2 then '25000000-0000-0000-0000-000000000102'::uuid
      else null
    end as schedule_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000301'::uuid
      when 2 then '25000000-0000-0000-0000-000000000302'::uuid
      else '25000000-0000-0000-0000-000000000303'::uuid
    end as split_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000501'::uuid
      when 2 then '25000000-0000-0000-0000-000000000502'::uuid
      else '25000000-0000-0000-0000-000000000503'::uuid
    end as expected_id,
    case sp.rn
      when 1 then 12.5000::numeric
      when 2 then 10.0000::numeric
      else 0::numeric
    end as rate_percent,
    case sp.rn
      when 1 then 500.00::numeric
      when 2 then -250.00::numeric
      else 0.00::numeric
    end as approved_adjustment,
    case sp.rn
      when 3 then 'Exception'
      else 'Calculated'
    end as status,
    case sp.rn
      when 3 then 'MissingSchedule'
      else 'None'
    end as exception_state
  from source_policies sp
  cross join admin_user au
  left join producer_users pu on pu.rn = sp.rn
)
insert into "ExpectedCommissions" (
  "Id", "PolicyId", "PolicyVersionId", "CarrierMarketId", "CommissionScheduleId",
  "ProducerSplitAssignmentId", "PremiumBasisAmount", "ExpectedGrossCommission",
  "ApprovedAdjustmentTotal", "AdjustedExpectedCommission", "Status", "ExceptionState",
  "SourceSnapshotJson", "CalculatedAt", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted"
)
select
  expected_id,
  "Id",
  null,
  "CarrierId",
  schedule_id,
  split_id,
  "Premium",
  case when rn = 3 then null else round("Premium" * (rate_percent / 100), 2) end,
  approved_adjustment,
  case when rn = 3 then null else round("Premium" * (rate_percent / 100), 2) + approved_adjustment end,
  status,
  exception_state,
  jsonb_build_object(
    'source', 'F0025-demo',
    'policyNumber', "PolicyNumber",
    'lineOfBusiness', "LineOfBusiness",
    'seededAt', now()
  ),
  now(),
  now(),
  admin_user_id,
  now(),
  admin_user_id,
  false
from demo_rows;

with
admin_user as (
  select "Id" from "UserProfiles"
  where "RolesJson"::text like '%Admin%'
  order by "CreatedAt"
  limit 1
),
producer_users as (
  select
    "Id",
    row_number() over (order by "DisplayName") as rn
  from "UserProfiles"
),
source_policies as (
  select
    p."Id",
    p."PolicyNumber",
    p."LineOfBusiness",
    p."EffectiveDate",
    p."ExpirationDate",
    p."Premium",
    p."BrokerId",
    p."CarrierId",
    row_number() over (order by p."UpdatedAt" desc, p."PolicyNumber") as rn
  from "Policies" p
  where p."IsDeleted" = false
    and p."Premium" is not null
    and p."CarrierId" is not null
  limit 3
),
demo_rows as (
  select
    sp.*,
    au."Id" as admin_user_id,
    coalesce(pu."Id", au."Id") as producer_user_id,
    case sp.rn
      when 1 then '25000000-0000-0000-0000-000000000501'::uuid
      when 2 then '25000000-0000-0000-0000-000000000502'::uuid
      else '25000000-0000-0000-0000-000000000503'::uuid
    end as expected_id,
    case sp.rn
      when 1 then 12.5000::numeric
      when 2 then 10.0000::numeric
      else 0::numeric
    end as rate_percent,
    case sp.rn
      when 1 then 500.00::numeric
      when 2 then -250.00::numeric
      else 0.00::numeric
    end as approved_adjustment,
    case sp.rn
      when 1 then 60.0000::numeric
      when 2 then 100.0000::numeric
      else 0.0000::numeric
    end as split_percent
  from source_policies sp
  cross join admin_user au
  left join producer_users pu on pu.rn = sp.rn
)
insert into "RevenueAttributionProjections" (
  "Id", "ExpectedCommissionId", "PolicyId", "ProducerId", "BrokerId", "TerritoryId",
  "CarrierMarketId", "PolicyPeriodStart", "PolicyPeriodEnd", "LineOfBusiness",
  "ExpectedGrossCommission", "ApprovedAdjustmentTotal", "AdjustedExpectedCommission",
  "ProducerAllocationAmount", "UnresolvedExceptionCount", "SourceRefreshedAt",
  "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted"
)
select
  case rn
    when 1 then '25000000-0000-0000-0000-000000000601'::uuid
    when 2 then '25000000-0000-0000-0000-000000000602'::uuid
    else '25000000-0000-0000-0000-000000000603'::uuid
  end,
  expected_id,
  "Id",
  producer_user_id,
  "BrokerId",
  null,
  "CarrierId",
  "EffectiveDate",
  "ExpirationDate",
  "LineOfBusiness",
  case when rn = 3 then 0 else round("Premium" * (rate_percent / 100), 2) end,
  approved_adjustment,
  case when rn = 3 then 0 else round("Premium" * (rate_percent / 100), 2) + approved_adjustment end,
  case when rn = 3 then 0 else round((round("Premium" * (rate_percent / 100), 2) + approved_adjustment) * (split_percent / 100), 2) end,
  case when rn = 3 then 1 else 0 end,
  now(),
  now(),
  admin_user_id,
  now(),
  admin_user_id,
  false
from demo_rows;

with admin_user as (
  select "Id" from "UserProfiles"
  where "RolesJson"::text like '%Admin%'
  order by "CreatedAt"
  limit 1
)
insert into "CommissionAdjustments" (
  "Id", "ExpectedCommissionId", "Amount", "EffectiveDate", "Reason", "Status",
  "RequestedByUserId", "RequestedAt", "DecidedByUserId", "DecidedAt", "DecisionNote",
  "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted"
)
select
  '25000000-0000-0000-0000-000000000701'::uuid,
  '25000000-0000-0000-0000-000000000501'::uuid,
  500.00,
  current_date,
  'F0025-demo approved market correction',
  'Approved',
  "Id",
  now(),
  "Id",
  now(),
  'F0025-demo approval for validation',
  now(),
  "Id",
  now(),
  "Id",
  false
from admin_user
union all
select
  '25000000-0000-0000-0000-000000000702'::uuid,
  '25000000-0000-0000-0000-000000000502'::uuid,
  -250.00,
  current_date,
  'F0025-demo pending debit memo',
  'Pending',
  "Id",
  now(),
  null,
  null,
  null,
  now(),
  "Id",
  now(),
  "Id",
  false
from admin_user;

commit;
