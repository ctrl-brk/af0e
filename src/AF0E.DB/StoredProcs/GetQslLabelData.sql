alter proc GetQslLabelData(@Call varchar(32) = null, @QueuedOnly bit = 1, @Analyze bit = 0, @IncludeUS bit = 0)
as
set nocount on;
--exec GetQslLabelData null, 0, 1

if (@Call is null and @QueuedOnly = 0 and @Analyze = 0)
  throw 51000, 'Call cannot be null with QueuedOnly = 0 and Analyze = 0', 1

declare @CallsTbl table ([Call] varchar(32))
if (@Analyze = 0) begin
    if (@Call is null ) begin
      insert into @CallsTbl select distinct COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_QSL_SENT = 'Q'
    end else begin
      insert into @CallsTbl select top 1 COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_CALL = @Call
    end
end

select distinct
       l.COL_COUNTRY as Country
      ,l.COL_BAND as Band
      ,l.COL_MODE as Mode
      ,case @Analyze
        when 0 then cast('R' as char)
        when 1 then cast('N' as char)
      end  as QslStatus
  into #countries
  from TABLE_HRD_CONTACTS_V01 l
 where COL_COUNTRY is not null
   and COL_COUNTRY <> '[none]'
   and (
           (@Analyze = 0 and COL_QSL_RCVD in ('Y', 'V'))
        or (@Analyze = 1 and COL_QSL_RCVD = 'N' and COL_QSL_SENT = 'N')
       )

if (@Analyze = 1) begin

    delete from #countries
     where exists (select 1
                     from TABLE_HRD_CONTACTS_V01
                    where COL_COUNTRY = Country
                      and COL_BAND = Band
                      and COL_MODE = Mode
                      and COL_QSL_RCVD in ('Y', 'V'))

    if (@IncludeUS = 0)
        delete from #countries where Country in ('United States Of America', 'Alaska')

end else begin

    insert #countries
    select distinct
            l.COL_COUNTRY
            ,l.COL_BAND
            ,l.COL_MODE
            ,'S'
        from TABLE_HRD_CONTACTS_V01 l
        where COL_COUNTRY is not null and COL_COUNTRY <> '[none]' and COL_QSL_RCVD = 'N' and COL_QSL_SENT in ('Y', 'R', 'Q')

end
;

with
pota (LogId, Parks) as (
  select c.LogId, string_agg(p.ParkId, ',') within group (order by p.ParkId)
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
	  ,l.COL_QSL_SENT as sQSL
      ,isnull(l.COL_QSL_SENT_VIA, '') as Via --0 based
	  ,isnull(l.COL_USER_DEFINED_1, '') as Manager
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
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and Mode = l.COL_MODE and QslStatus = 'R') then 1
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and QslStatus = 'R') then 2
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and Mode = l.COL_MODE and QslStatus = 'S') then 3
         when exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and QslStatus = 'S') then 4
         else 0
       end as CountryQslStatus
  from TABLE_HRD_CONTACTS_V01 l
	   left join pota on pota.LogId = l.COL_PRIMARY_KEY
 where (    @Analyze = 0
        and l.COL_CALL in (select [Call] from @CallsTbl)
        and (@QueuedOnly = 0 or l.COL_QSL_SENT in ('Q', 'N'))
       )
    or (
            @Analyze = 1
        and l.COL_QSL_SENT = 'N'
        and exists (select 1 from #countries where Country = l.COL_COUNTRY and Band = l.COL_BAND and Mode = l.COL_MODE)
       )
 order by l.COL_CALL, l.COL_QSL_SENT desc, l.COL_TIME_ON desc
;

drop table #countries
