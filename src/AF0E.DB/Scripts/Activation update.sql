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
--select @parkId = 168,   @parkNum = 'US-0225',  @grid = 'DM79jv', @city = null,         @county = 'Jefferson', @state = 'CO', @lat = 39.911110, @long = -105.183235 -- US-0225 (Rocky Flats NWR)
--select @parkId = 169,   @parkNum = 'US-0226',  @grid = 'DM79nu', @city = null,         @county = 'Adams',     @state = 'CO', @lat = 39.813758, @long = -104.860711 -- US-0226 (Rocky Mountains Arsenal NWR)
--select @parkId = 170,   @parkNum = 'US-0227',  @grid = 'DM79ku', @city = 'Arvada',     @county = 'Jefferson', @state = 'CO', @lat = 39.841670, @long = -105.102664 -- US-0227 (Two Ponds NWR)
--select @parkId = 2994,  @parkNum = 'US-1211',  @grid = 'DM79pi', @city = 'Franktown',  @county = 'Douglas',   @state = 'CO', @lat = 39.333209, @long = -104.744105 -- US-1211 (Castlewood Canyon SP)
--select @parkId = 2995,  @parkNum = 'US-1212',  @grid = 'DM79lm', @city = null,         @county = 'Jefferson', @state = 'CO', @lat = 39.519166, @long = -105.081798 -- US-1212 (Chatfield SP)
--select @parkId = 4192,  @parkNum = 'US-1241',  @grid = 'DN70me', @city = 'Longmont',   @county = 'Weld',      @state = 'CO', @lat = 40.170502, @long = -104.985152 -- US-1241 (St. Vrain SP)
--select @parkId = 11269, @parkNum = 'US-9669',  @grid = 'DM70ja', @city = 'Boulder',    @county = 'Boulder',   @state = 'CO', @lat = 40.039693, @long = -105.184286 -- US-9669 (Sawhill Ponds SWA)
--select @parkId = 6153,  @parkNum = 'US-4400',  @grid = 'DM79gu', @city = null,         @county = 'Gilpin',    @state = 'CO', @lat = 39.842408, @long = -105.495729 -- US-4400 (Arapaho NF - Cold Springs CG)
--select @parkId = 3026,  @parkNum = 'US-1244',  @grid = 'DM79hl', @city = null,         @county = 'Jefferson', @state = 'CO', @lat = 39.494522, @long = -105.380843 -- US-1244 (Staunton SP)
--select @parkId = 2877,  @parkNum = 'US-11895', @grid = 'DM79ef', @city = null,         @county = 'Park',      @state = 'CO', @lat = 39.222075, @long = -105.603500 -- US-11895 (Tarryall Reservoir SWA)
--select @parkId = 11728, @parkNum = 'US-12170', @grid = 'DM79bj', @city = null,         @county = 'Park',      @state = 'CO', @lat = 39.378275, @long = -105.847921 -- US-12170 (Teter-Michigan Creek SWA)
--select @parkId = 11697, @parkNum = 'US-12139', @grid = 'DM79bh', @city = null,         @county = 'Park',      @state = 'CO', @lat = 39.334618, @long = -105.869179 -- US-12139 (Cline Ranch SWA)
--select @parkId = 11729, @parkNum = 'US-12171', @grid = 'DM79bc', @city = null,         @county = 'Park',      @state = 'CO', @lat = 39.076476, @long = -105.858825 -- US-12171 (Tomahawk SWA)
--select @parkId = 11698, @parkNum = 'US-12140', @grid = 'DM79da', @city = null,         @county = 'Park',      @state = 'CO', @lat = 39.014228, @long = -105.730110 -- US-12140 (Spinney Mountain SWA)
--select @parkId = 11696, @parkNum = 'US-12138', @grid = 'DM78ex', @city = null,         @county = 'Park',      @state = 'CO', @lat = 38.965340, @long = -105.597257 -- US-12138 (Charlie Meyers SWA)
--select @parkId = 3012,  @parkNum = 'US-1230',  @grid = 'DM78kv', @city = null,         @county = 'Teller',    @state = 'CO', @lat = 38.891478, @long = -105.181292 -- US-1230 (Mueller SP)
--select @parkId = 6157,  @parkNum = 'US-4404',  @grid = 'DM79kb', @city = null,         @county = 'Teller',    @state = 'CO', @lat = 39.063954, @long = -105.095754 -- US-4404 (Pike NF)
--select @parkId = 11663, @parkNum = 'US-12106', @grid = 'DN70fb', @city = null,         @county = 'Boulder',   @state = 'CO', @lat = 40.078216, @long = -105.571317 -- US-12106 (Brainard Lake)
--select @parkId = 6159,  @parkNum = 'US-4406',  @grid = 'DN70fb', @city = null,         @county = 'Boulder',   @state = 'CO', @lat = 40.078216, @long = -105.571317 -- US-4406 (Roosevelt NF - Brainard Lake)
--select @parkId = 6159,  @parkNum = 'US-4406',  @grid = 'DN70hq', @city = null,         @county = 'Larimer',   @state = 'CO', @lat = 40.683447, @long = -105.397615 -- US-4406 (Roosevelt NF - Stove Prairie CG)
--select @parkId = 6159,  @parkNum = 'US-4406',  @grid = 'DN70es', @city = null,         @county = 'Larimer',   @state = 'CO', @lat = 40.771897, @long = -105.622089 -- US-4406 (Roosevelt NF - Bellair Lake CG)
--select @parkId = 11739, @parkNum = 'US-12181', @grid = 'DN70rb', @city = 'Keenesburg', @county = 'Weld',      @state = 'CO', @lat = 40.073021, @long = -104.555451 -- US-12181 (Banner Lakes SWA)
--select @parkId = 11746, @parkNum = 'US-12188', @grid = 'DN80ef', @city = null,         @county = 'Morgan',    @state = 'CO', @lat = 40.218628, @long = -103.623280 -- US-12188 (Brush Prairie Ponds SWA)
--select @parkId = 3005,  @parkNum = 'US-1222',  @grid = 'DN70wj', @city = null,         @county = 'Morgan',    @state = 'CO', @lat = 40.381658, @long = -104.089411 -- US-1222 (Jackson Lake SP)
--select @parkId = 6163,  @parkNum = 'US-4410',  @grid = 'DM69uu', @city = null,         @county = 'Summit',    @state = 'CO', @lat = 39.873072, @long = -106.283051 -- US-4410 (White River NF)
--select @parkId = 2924,  @parkNum = 'US-11938', @grid = 'DN70fs', @city = null,         @county = 'Larimer',   @state = 'CO', @lat = 40.786392, @long = -105.563379 -- US-11938 (Parvin Lake SWA)
--select @parkId = 6160,  @parkNum = 'US-4407',  @grid = 'DM68vr', @city = null,         @county = 'Chaffee',   @state = 'CO', @lat = 38.712666, @long = -106.232957 -- US-4407 (San Isabel NF - Chalk Lake CG)
--select @parkId = 3000,  @parkNum = 'US-1217',  @grid = 'DM78fw', @city = null,         @county = 'Park',      @state = 'CO', @lat = 38.920444, @long = -105.514727 -- US-1217 (Eleven Mile SP - Howbert Point CG)
--select @parkId = 11747, @parkNum = 'US-12189', @grid = 'DM68wu', @city = null,         @county = 'Chaffee',   @state = 'CO', @lat = 38.846959, @long = -106.122510 -- US-12189 (Buena Vista SWA)
--select @parkId = 6164,  @parkNum = 'US-4411',  @grid = 'DM68xs', @city = null,         @county = 'Chaffee',   @state = 'CO', @lat = 38.752378, @long = -106.065068 -- US-4411 (Browns Canyon National Monument)

-- MAKE SURE ALL THREE ARE CORRECT and include minutes!
select @startDate = '2025-09-01 18:42', @endDate = '2025-09-01 19:55', @submitDate = '2025-09-02 02:45'
-- MAKE SURE IT'S ONE UTC DAY!

insert into #tmp select COL_PRIMARY_KEY, COL_CALL from [HamLog].[dbo].[TABLE_HRD_CONTACTS_V01] where COL_TIME_ON between @startDate and @endDate

begin tran

declare @activationId int
insert into [HamLog].[dbo].[PotaActivations](ParkId, Grid, City, County, State, StartDate, EndDate, LogSubmittedDate, Comments, Lat, Long) values (@parkId, @grid, @city, @county, @state, @startDate , @endDate, @submitDate, null, @lat, @long)
select @activationId = max(activationid) from [HamLog].[dbo].[PotaActivations]
--print 'Activation ID: ' + convert(varchar, @activationId)
insert into [HamLog].[dbo].[PotaContacts](ActivationId, LogId) select @activationId, id from #tmp

update [HamLog].[dbo].[TABLE_HRD_CONTACTS_V01]
   set COL_MY_GRIDSQUARE = @grid, COL_MY_CITY = @city, COL_MY_CNTY = @county, COL_MY_STATE = @state, COL_MY_SIG = isnull(COL_MY_SIG, 'POTA'), COL_MY_SIG_INFO = isnull(COL_MY_SIG_INFO, @parkNum), COL_MY_LAT = @lat, COL_MY_LON = @long
 where COL_PRIMARY_KEY in (select LogId from PotaContacts where ActivationId = @activationId)

select * from PotaActivations where ActivationId = @activationId
select a.*, l.col_call, l.COL_TIME_ON from PotaContacts a inner join TABLE_HRD_CONTACTS_V01 l on l.COL_PRIMARY_KEY = a.LogId where ActivationId = @activationId order by a.LogId desc

-- rollback
-- commit

select distinct COL_MY_SIG_INFO from [HamLog].[dbo].[TABLE_HRD_CONTACTS_V01] where COL_COMMENT like 'POTA activation US-0225%'
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

select * from PotaParks where ParkNum = 'US-4411'

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
