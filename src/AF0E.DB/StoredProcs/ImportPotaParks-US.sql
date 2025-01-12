alter proc ImportPotaParks-US
as
set nocount on

insert into PotaParks (ParkNum, ParkName, Lat, Long, Grid, [Location], Country, Activations, QSOs)
select reference, [name], latitude, longitude, grid, locationDesc, Country, activations, qsos
  from PotaParksImport
 where reference not in (select ParkNum from PotaParks)

update PotaParks
   set ParkName = i.name, Lat = i.latitude, Long = i.longitude, Grid = i.grid, [Location] = i.locationDesc, Country = i.Country, Activations = i.activations, QSOs = i.qsos, Active = 1
  from PotaParksImport i
 where ParkNum = i.reference

update PotaParks
   set Active = 0
 where Active = 1 and Country = 'US' and ParkNum not in (select reference from PotaParksImport)
