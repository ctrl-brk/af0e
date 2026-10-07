select top 1000 COL_CALL, COL_COMMENT, COL_TIME_ON, * from TABLE_HRD_CONTACTS_V01 where COL_COMMENT like '%/%' order by 3 desc

insert into HamEvents (EventType, Name, Description, Url, StartDate, EndDate, LogDueDate, QslDueDate, QslInfo, Comments)
values (
 'E' -- C - contest, E - special event, D - DXpedition
 -- Name
,'150 Years of A&M College of TX'
-- Description
,'On April 17, 1871, the Texas Legislature took advantage of the federal Morrill Land-Grant Act of 1862 to establish the Agricultural & Mechanical College of Texas. After years of construction and planning, the college officially opened its doors to its first classes on October 4, 1876.'
-- Url
,null
-- StartDate
,'2026-10-04'
-- EndDate
,'2026-10-18'
-- LogDueDate
,null
-- QslDueDate
,'2026-10-25'
-- QslInfo
,'TIFFANY M BLOXOM
P.O. Box 9535
COLLEGE STATION, TX 77842'
-- Comments
,null
)

select * from HamEvents order by HamEventId desc

insert into HamEventContacts (HamEventId, LogId)
values (
(select top 1 HamEventId from HamEvents order by HamEventId desc)
,92629
)

select COL_PRIMARY_KEY, COL_CALL, COL_COMMENT, COL_TIME_ON, * from TABLE_HRD_CONTACTS_V01 where COL_CALL = 'n6s' order by 1 desc
