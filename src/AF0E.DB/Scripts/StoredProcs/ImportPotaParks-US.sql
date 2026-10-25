alter proc [dbo].[ImportPotaParks-US]
as
    set nocount on

declare @Inserted int = 0,
@Updated int = 0,
@Deactivated int = 0

insert into PotaParks (ParkNum, ParkName, Lat, Long, Grid, [Location], Country, Activations, QSOs)
select reference, [name], latitude, longitude, grid, locationDesc, Country, activations, qsos
from PotaParksImport
where reference not in (select ParkNum from PotaParks)

    set @Inserted = @@ROWCOUNT

update PotaParks
set ParkName = i.name, Lat = i.latitude, Long = i.longitude, Grid = i.grid, [Location] = i.locationDesc, Country = i.Country, Activations = i.activations, QSOs = i.qsos, Active = 1
from PotaParksImport i
where ParkNum = i.reference

    set @Updated = @@ROWCOUNT

update PotaParks
set Active = 0
where Active = 1 and Country = 'US' and not exists (select * from PotaParksImport i where i.reference = ParkNum)

    set @Deactivated = @@ROWCOUNT

select @Inserted as Inserted, @Updated as Updated, @Deactivated as Deactivated
