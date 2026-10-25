insert into HamEvents (EventType, Name, Description, Url, StartDate, EndDate, LogDueDate, QslDueDate, QslInfo, Comments)
values (
 'E' -- C - contest, E - special event, D - DXpedition
 -- Name
,'Baseball World Series ''26'
-- Description
,null
-- Url
,null
-- StartDate
,'2026-10-17'
-- EndDate
,'2026-10-31'
-- LogDueDate
,null
-- QslDueDate
,'2026-11-07'
-- QslInfo
,'Marcus Boyd W2S
PO Box 371
Raven, VA 24639'
-- Comments
,null
)

select distinct e.* from HamEvents e
inner join HamEventContacts c on e.HamEventId = c.HamEventId
inner join TABLE_HRD_CONTACTS_V01 l on c.LogId = l.COL_PRIMARY_KEY and l.COL_QSL_SENT in ('N','Q')
where qslduedate is not null
and QslDueDate < getdate()
order by QslDueDate

--update HamEvents set QslDueDate = null where HamEventId = 14
