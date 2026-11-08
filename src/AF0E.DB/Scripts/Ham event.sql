insert into HamEvents (EventType, Name, Description, Url, StartDate, EndDate, LogDueDate, QslDueDate, QslInfo, Comments, CallSigns)
values (
 'E' -- C - contest, E - special event, D - DXpedition
 -- Name
,'40 years EUDXF'
-- Description
,'The European DX Foundation (EUDXF) is the first and only all European Foundation to support DX activities.
It was established as a non profit organisation in 1986 in the German town of Bad Bentheim by experienced DXers. 
Our goal is to support DXers and expeditions (e.g. financial support, printing of QSL-cards, etc…)
to allow the DX-community in Europe a fair share in their work.'
-- Url
,'http://www.eudxf.eu'
-- StartDate
,'2026-11-01'
-- EndDate
,'2026-11-30'
-- LogDueDate
,null
-- QslDueDate
,'2026-12-07'
-- QslInfo
,'Clublog OQRS'
-- Comments
,null
-- CallSigns
,'|DL40EUDXF|AO40EUDXF|EA6EUDXF|DU40EUDXF|HA40EUDXF|OR40EUDXF|PA40EUDXF|PB40EUDXF|PC40EUDXF|PD40EUDXF|PE40EUDXF|PF40EUDXF|PG40EUDXF|PH40EUDXF|PI40EUDXF|VI40EUDXF|'
)

/*
select distinct e.* from HamEvents e
inner join HamEventContacts c on e.HamEventId = c.HamEventId
inner join TABLE_HRD_CONTACTS_V01 l on c.LogId = l.COL_PRIMARY_KEY and l.COL_QSL_SENT in ('N','Q')
where qslduedate is not null
and QslDueDate < getdate()
order by QslDueDate
*/

--update HamEvents set QslDueDate = null where HamEventId = 14
