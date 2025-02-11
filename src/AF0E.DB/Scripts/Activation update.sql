/* 
  Activation Update Script Start
  (some of this is duplicated lower in the file, but this is the one to use with new activations
  Don't forget to run QrzLookup and PotaLookup
*/
truncate table #tmp
create table #tmp (id int, call varchar(32))
-- CHECK IF THERE ARE TWO ACTIVATIONS BECAUSE OF THE UTC DAY
declare @parkId int, @parkNum varchar(64), @grid varchar(10), @city nvarchar(100), @county nvarchar(200), @state char(2)
declare @startDate varchar(20), @endDate varchar(20), @submitDate varchar(20)
declare @lat decimal(10,6), @long decimal(10,6)
select @parkId = 168, @parkNum = 'US-0225', @grid = 'DN79jv', @city = null, @county = 'Jefferson', @state = 'CO', @lat = 39.911110, @long = -105.183235 -- US-0225 (Rocky Flats NWR)
--select @parkId = 11269, @grid = 'DM70ja', @city = 'Boulder', @county = 'Boulder', @state = 'CO', @lat = 40.039693, @long = -105.184286 -- US-9669 (Sawhill Ponds SWA)
																									   
-- MAKE SURE ALL THREE ARE CORRECT!
select @startDate = '2025-02-09', @endDate = '2025-02-09 00:37', @submitDate = '2025-02-09 07:01'

insert into #tmp select COL_PRIMARY_KEY, COL_CALL from [HamLog].[dbo].[TABLE_HRD_CONTACTS_V01] where COL_TIME_ON between @startDate and @endDate

begin tran

declare @activationId int
insert into [HamLog].[dbo].[PotaActivations](ParkId, Grid, City, County, State, StartDate, EndDate, LogSubmittedDate, Comments, Lat, Long) values (@parkId, @grid, @city, @county, @state, @startDate , @endDate, @submitDate, null, @lat, @long)
select @activationId = max(activationid) from [HamLog].[dbo].[PotaActivations]
--print 'Activation ID: ' + convert(varchar, @activationId)
insert into [HamLog].[dbo].[PotaContacts](ActivationId, LogId) select @activationId, id from #tmp

update [HamLog].[dbo].[TABLE_HRD_CONTACTS_V01]
   set COL_MY_GRIDSQUARE = @grid, COL_MY_CITY = @city, COL_MY_CNTY = @county, COL_MY_STATE = @state, COL_SIG = isnull(COL_SIG, 'POTA'), COL_SIG_INFO = isnull(COL_SIG_INFO, @parkNum), COL_MY_LAT = @lat, COL_MY_LON = @long
 where COL_PRIMARY_KEY in (select LogId from PotaContacts where ActivationId = @activationId)

select * from PotaActivations where ActivationId = @activationId
select a.*, l.col_call, l.COL_TIME_ON from PotaContacts a inner join TABLE_HRD_CONTACTS_V01 l on l.COL_PRIMARY_KEY = a.LogId where ActivationId = @activationId order by a.LogId desc

-- rollback
-- commit

/* Activation Upate Script End */

create table #tmp (id int, call varchar(32))
select COL_PRIMARY_KEY, col_call, COL_COMMENT, COL_TIME_ON as date, COL_MODE, COL_USER_DEFINED_9, * from TABLE_HRD_CONTACTS_V01 where COL_TIME_ON between '2024-11-29 20:00' and '2024-11-29 22:10' order by COL_TIME_ON
truncate table #tmp
insert into #tmp select COL_PRIMARY_KEY, COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_TIME_ON between '2024-11-29 20:00' and '2024-11-29 22:10'
insert into PotaActivations(ParkId, Grid, County, State, StartDate, EndDate, LogSubmittedDate, Comments) values (168, 'DM79jv', 'Jefferson', 'CO', '2024-11-03 21:24' , '2024-11-14 07:52', '2024-10-25 03:18', null)
select max(activationid) from PotaActivations
insert into PotaContacts(ActivationId, LogId) select 108, id from #tmp
update PotaContacts set P2P = 'Y' where ActivationId = 108 and LogId in (select COL_PRIMARY_KEY from TABLE_HRD_CONTACTS_V01 where COL_CALL in ('KI7QCF','wr7b','w4mpt'))



--create table #tmp (id int, call varchar(32))
--drop table #tmp
--truncate table #tmp
--select * from #tmp

--select COL_PRIMARY_KEY into #tmp from TABLE_HRD_CONTACTS_V01 where COL_COMMENT like '%activation%'

--update TABLE_HRD_CONTACTS_V01 set COL_USER_DEFINED_9 = 'Y' where COL_PRIMARY_KEY in (select id from #tmp)

select * from #tmp

select col_call, COL_COMMENT, COL_TIME_ON as date, COL_MODE, * from TABLE_HRD_CONTACTS_V01 where COL_COMMENT like '%activation%' and isnull(COL_USER_DEFINED_9,'N') <> 'Y' order by COL_TIME_ON
select col_call, COL_COMMENT, COL_TIME_ON as date, * from TABLE_HRD_CONTACTS_V01 where COL_COMMENT like '%9621%' or COL_COMMENT like '%boedecker%' and isnull(COL_USER_DEFINED_9,'N') <> 'Y' order by COL_TIME_ON
select COL_PRIMARY_KEY, col_call, COL_COMMENT, COL_TIME_ON as date, COL_MODE, COL_USER_DEFINED_9, * from TABLE_HRD_CONTACTS_V01 where COL_TIME_ON between '2024-11-03 21:24' and '2024-11-03 23:18' and isnull(COL_USER_DEFINED_9,'N') <> 'Y' order by COL_TIME_ON


select col_call, COL_COMMENT, COL_TIME_ON as date, * from TABLE_HRD_CONTACTS_V01 where COL_COMMENT like 'POTA activation US-1222 (Jackson Lake SP)'
--and isnull(COL_USER_DEFINED_9,'N') <> 'Y'
order by COL_TIME_ON

truncate table #tmp

insert into #tmp select COL_PRIMARY_KEY, COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_TIME_ON between '2024-11-03 21:24' and '2024-11-03 23:18'
insert into #tmp select COL_PRIMARY_KEY, COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_COMMENT = 'POTA activation US-1222 (Jackson Lake SP)' and COL_TIME_ON between '2024-06-22' and '2024-06-22 18:00'
insert into #tmp select COL_PRIMARY_KEY, COL_CALL from TABLE_HRD_CONTACTS_V01 where COL_PRIMARY_KEY between 18169 and 18188

-- US-0225 (Rocky Flats NWR)
update TABLE_HRD_CONTACTS_V01 set COL_COMMENT = 'POTA activation US-9669(Sawhill Ponds SWA)', COL_USER_DEFINED_9 = 'Y' where COL_PRIMARY_KEY in (select id from #tmp)
update TABLE_HRD_CONTACTS_V01 set COL_USER_DEFINED_9 = 'Y' where COL_PRIMARY_KEY in (select id from #tmp)
update TABLE_HRD_CONTACTS_V01 set COL_USER_DEFINED_9 = 'Y' where COL_PRIMARY_KEY in (select logid from PotaContacts where ActivationId between 30 and 33)

select * from PotaParks where ParkNum = 'US-1241'

-- US-0225: 168, 'DM79jv', 'Jefferson', 'CO', 39.911110, -105.183235
-- 0227: 170, 'DM79ku', 'Jefferson', 'CO'
-- 9669: 11269, 'DN70ja', 'Boulder', 'CO'
-- 2996: 2996, 'DM79np', 'Arapahoe', 'CO'
-- 4406: 6159, '??', 'Boulder', 'CO'
-- 1219: 3002, 'DM79gu', 'Gilpin', 'CO'
-- 4400: 6153, 'DM79gu', 'Gilpin', 'CO'
insert into PotaActivations(ParkId, Grid, County, State, StartDate, EndDate, LogSubmittedDate, Comments) values (168, 'DM79jv', 'Jefferson', 'CO', '2024-11-29 20:48' , '2024-11-29 22:09', '2024-11-30 00:12', null)
select max(activationid) from PotaActivations

insert into PotaContacts(ActivationId, LogId) select 109, id from #tmp

update PotaContacts set P2P = 'Y' where ActivationId = 109 and LogId in (select COL_PRIMARY_KEY from TABLE_HRD_CONTACTS_V01 where COL_CALL in ('KN6UDK','KE8NJW','NA7DO','K0LAR','WB0RLJ','W1ND','NM5BG','WN7JT','K5DGR','KN1MT','N0DNF','N5YCO','AF8E'))



select * from PotaParks where ParkId=11269
select p.ActivationId, p.Grid, p.County, pp.* from PotaActivations p inner join potaparks pp on pp.parkid = p.parkid order by ActivationId
select l.COL_CALL from PotaContacts p inner join TABLE_HRD_CONTACTS_V01 l on l.COL_PRIMARY_KEY = p.logid where isnull(p.p2p,'') <> ''
select c.LogId, count(c.logId) from PotaContacts c inner join PotaActivations a on a.ActivationId = c.ActivationId and a.ActivationId between 30 and 33
group by c.LogId

----- [Update log with location from activation] ------
declare @logId int, @grid varchar(10), @city nvarchar(100), @county nvarchar(200), @state char(2)
declare cSel cursor for
select c.LogId, a.Grid, a.City, a.County, a.State from PotaContacts c inner join PotaActivations a on a.ActivationId = c.ActivationId where a.ActivationId = 109
open cSel
fetch next from cSel into @logId, @grid, @city, @county, @state
while @@fetch_status = 0 begin
  update TABLE_HRD_CONTACTS_V01 set COL_MY_GRIDSQUARE = @grid, COL_MY_CITY = @city, COL_MY_CNTY = @county, COL_MY_STATE = @state where COL_PRIMARY_KEY = @logId
  fetch next from cSel into @logId, @grid, @city, @county, @state
end
close cSel
deallocate cSel

select distinct l.COL_MODE from PotaContacts a inner join TABLE_HRD_CONTACTS_V01 l on l.COL_PRIMARY_KEY = a.LogId
