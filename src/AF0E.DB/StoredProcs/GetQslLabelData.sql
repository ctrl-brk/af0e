alter proc GetQslLabelData(@Call varchar(32) = null, @QueuedOnly bit = 1, @StartDate datetime2, @EndDate datetime2, @Analyze bit = 0, @DxOnly bit = 1, @ShowWaiting bit = 1)
as
set nocount on;
--exec GetQslLabelData null, 1, 1
--exec GetQslLabelData 'v51pj', 1, 1

if (@Call is null and @QueuedOnly = 0 and @Analyze = 0)
  throw 51000, 'Call cannot be null with QueuedOnly = 0 and Analyze = 0', 1

declare @CallsTbl table ([Call] varchar(32))
if (@Analyze = 0) begin
    if (@Call is null ) begin
      insert into @CallsTbl select distinct COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_QSL_SENT = 'Q'
    end else begin
      insert into @CallsTbl select distinct COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_CALL like @Call and COL_QSL_SENT <> 'I'
    end
end

select distinct
       l.COL_COUNTRY as Country
      ,l.COL_BAND as Band
      ,l.COL_MODE as Mode
      ,cast('R' as char) as QslStatus -- received
  into #countries
  from TABLE_HRD_CONTACTS_V01 l
 where COL_COUNTRY is not null
   and COL_COUNTRY <> '[none]'
   and COL_QSL_RCVD in ('Y', 'V')

if (@Analyze = 1) begin

    insert into @CallsTbl
    select distinct COL_CALL
      from TABLE_HRD_CONTACTS_V01
     where COL_TIME_ON between @StartDate and @EndDate
       and COL_QSL_SENT = 'N'
       and COL_COUNTRY is not null
       and COL_COUNTRY <> '[none]'
       and COL_COUNTRY not in (select Country from #countries)
       and (@DxOnly = 0 or (COL_COUNTRY <> 'United States Of America' and COL_COUNTRY <> 'Alaska'))

end

insert #countries
select distinct
       l.COL_COUNTRY
      ,l.COL_BAND
      ,l.COL_MODE
      ,'S' -- sent
  from TABLE_HRD_CONTACTS_V01 l
 where COL_COUNTRY is not null and COL_COUNTRY <> '[none]' and COL_QSL_RCVD = 'N' and COL_QSL_SENT in ('Y', 'R') --should 'Q' be included as sent?
;

with
pota (LogId, Parks) as (
  select c.LogId, string_agg(p.ParkNum, ',') within group (order by p.ParkNum)
    from PotaContacts c
         inner join PotaActivations a on a.ActivationId = c.ActivationId
         inner join PotaParks p on p.ParkId = a.ParkId
   group by c.LogId
)
select l.COL_PRIMARY_KEY as ID
      ,l.COL_TIME_ON as UTC
	  ,l.COL_CALL as [Call]
	  ,l.COL_MODE as Mode
	  ,l.COL_RST_SENT as RST
	  ,l.COL_BAND as Band
	  ,isnull(pota.Parks, '') as Parks
	  ,l.COL_SAT_NAME as Sat
	  ,upper(l.COL_QSL_SENT) as sQSL
      ,upper(isnull(l.COL_QSL_SENT_VIA, '')) as QslDeliveryMethod
      ,isnull(l.COL_QSL_VIA, '') as QrzQslInfo
	  ,isnull(l.COL_USER_DEFINED_1, '') as QslMgrCall
      ,isnull(l.COL_USER_DEFINED_0, '') as SiteComment
      ,l.COL_USER_DEFINED_2 as QslComment
	  ,l.COL_QSL_RCVD as rQSL
      ,l.COL_LOTW_QSL_RCVD as lQSL
	  ,l.[COL_NAME] as [Name]
	  ,l.COL_COUNTRY as Country
	  ,l.COL_COMMENT as Comment
	  ,substring(l.COL_MY_GRIDSQUARE, 1, 6) as MyGrid
	  ,l.COL_MY_STATE as MyState
	  ,l.COL_MY_CITY as MyCity
	  ,l.COL_MY_CNTY as MyCounty
      ,case
         when l.COL_COUNTRY is null then 1 -- unknown (see QslStatus tbl)
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and Mode = l.COL_MODE and QslStatus = 'R') then 2|4|8 -- rcvd band&mode
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and QslStatus = 'R') then 2|4 -- rcvd band
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and QslStatus = 'R') then 2 -- rcvd country
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and Mode = l.COL_MODE and QslStatus = 'S') then 16|32|64 -- sent band&mode
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and QslStatus = 'S') then 16|32 -- sent band
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and QslStatus = 'S') then 16 -- sent country
         else 0 -- no sent/rcvd
       end as CountryQslStatus
      ,isnull(l.COL_USER_DEFINED_3, '') as Metadata
  from TABLE_HRD_CONTACTS_V01 l
	   left join pota on pota.LogId = l.COL_PRIMARY_KEY
 where COL_TIME_ON between @StartDate and @EndDate
   and l.COL_CALL in (select [Call] from @CallsTbl)
   and (
         (    @Analyze = 0
          and (@QueuedOnly = 0 or l.COL_QSL_SENT in ('Q', 'N'))
         )
         or @Analyze = 1
       )
 order by l.COL_CALL, l.COL_QSL_SENT desc, l.COL_TIME_ON desc
;

drop table #countries
